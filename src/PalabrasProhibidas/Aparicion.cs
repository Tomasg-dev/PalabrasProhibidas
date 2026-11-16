using System.Collections.Generic;
using UnityEngine;

namespace PalabraProhibida;

/// <summary>
/// Castigo por decir la palabra: el host crea un enemigo de los del nivel junto al jugador que la
/// dijo, lo deja un rato y luego lo retira. Todo se hace con las funciones del propio juego, así que
/// los clientes lo ven como cualquier otro enemigo aunque no tengan el mod.
/// </summary>
internal static class Aparicion
{
	private enum Fase
	{
		/// <summary>Creado, esperando a que el juego lo haga aparecer en nuestro punto.</summary>
		Esperando,

		/// <summary>En el nivel, con el reloj de duración corriendo.</summary>
		Activo,

		/// <summary>Se le ha pedido que se vaya y todavía no se ha ido.</summary>
		Retirando,

		/// <summary>Ya no está. Se le impide volver a aparecer.</summary>
		Retirado,
	}

	private sealed class Invocado
	{
		public EnemyParent Padre = null!;

		public LevelPoint Punto = null!;

		public Vector3 Origen;

		public Fase Fase;

		public float Reloj;

		public float TiempoVisible;
	}

	// A qué distancia del jugador se busca el punto de aparición.
	private const float DistanciaMinima = 4f;

	private const float DistanciaMaxima = 12f;

	// El juego nunca debe volver a sacar por su cuenta a un enemigo retirado.
	private const float Nunca = 1e9f;

	private static readonly List<Invocado> invocados = new List<Invocado>();

	private static float proximaPermitida;

	/// <summary>Pide un enemigo junto a <paramref name="jugador"/>. Solo hace algo en el host. Devuelve si apareció.</summary>
	internal static bool Pedir(PlayerAvatar? jugador)
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			Avisar("no soy el host: no hago aparecer nada");
			return false;
		}

		if (!SemiFunc.RunIsLevel() || !LevelGenerator.Instance || !LevelGenerator.Instance.Generated || !EnemyDirector.instance)
		{
			Avisar("fuera de un nivel: no aparece nada");
			return false;
		}

		if (!jugador || jugador!.isDisabled)
		{
			Avisar("el jugador no está vivo: no aparece nada");
			return false;
		}

		// Por defecto no hay espera ni tope: cada vez que se dice una palabra aparece un enemigo.
		if (Time.time < proximaPermitida)
		{
			Avisar($"en espera {proximaPermitida - Time.time:0} s más");
			return false;
		}

		int vivos = 0;
		foreach (Invocado i in invocados)
		{
			if (i.Fase != Fase.Retirado)
			{
				vivos++;
			}
		}

		if (Plugin.MaximoALaVez.Value > 0 && vivos >= Plugin.MaximoALaVez.Value)
		{
			Avisar("ya hay demasiados enemigos invocados a la vez");
			return false;
		}

		EnemySetup? elegido = ElegirEnemigo();
		if (!elegido)
		{
			Avisar("no hay ningún enemigo que elegir en este nivel");
			return false;
		}

		LevelPoint? punto = ElegirPunto(jugador.transform.position);
		if (!punto)
		{
			Avisar("no encuentro un punto del nivel cerca del jugador");
			return false;
		}

		// El juego no devuelve lo que crea, así que comparamos los enemigos de antes y de después.
		Transform raiz = LevelGenerator.Instance.EnemyParent.transform;
		HashSet<EnemyParent> antes = new HashSet<EnemyParent>(raiz.GetComponentsInChildren<EnemyParent>(true));
		LevelGenerator.Instance.EnemySpawn(elegido!, punto!.transform.position);

		int creados = 0;
		foreach (EnemyParent padre in raiz.GetComponentsInChildren<EnemyParent>(true))
		{
			if (antes.Contains(padre))
			{
				continue;
			}

			// El juego le asigna como primer punto de aparición el más alejado; lo cambiamos por el nuestro.
			padre.firstSpawnPoint = punto;
			padre.firstSpawnPointUsed = false;

			// Al crear enemigos a mitad de nivel el juego no les presenta a los jugadores; sin esto no los ven.
			if (padre.Enemy)
			{
				foreach (PlayerAvatar p in GameDirector.instance.PlayerList)
				{
					padre.Enemy.PlayerAdded(p.photonView.ViewID);
				}
			}

			SinNucleo(padre);
			invocados.Add(new Invocado { Padre = padre, Punto = punto, Origen = jugador.transform.position, Fase = Fase.Esperando, Reloj = 20f });
			creados++;
		}

		if (creados == 0)
		{
			Avisar($"el juego no creó ningún enemigo para {elegido!.name}");
			return false;
		}

		proximaPermitida = Time.time + Plugin.EsperaEntreApariciones.Value;
		float distancia = Vector3.Distance(punto.transform.position, jugador.transform.position);
		Avisar($"aparece {elegido!.name} a {distancia:0} m de {jugador.playerName}");
		return true;
	}

	/// <summary>Se llama cada fotograma desde el plugin. Lleva la cuenta de cada enemigo invocado.</summary>
	internal static void Tick()
	{
		if (invocados.Count == 0)
		{
			return;
		}

		// Al cambiar de nivel el juego destruye los enemigos; olvidamos los que ya no existen.
		invocados.RemoveAll(i => !i.Padre);
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !EnemyDirector.instance)
		{
			return;
		}

		for (int n = invocados.Count - 1; n >= 0; n--)
		{
			Invocado i = invocados[n];
			EnemyParent padre = i.Padre;
			switch (i.Fase)
			{
				case Fase.Esperando:
					if (!EnemyDirector.instance.enemiesSpawned.Contains(padre))
					{
						break; // El juego aún lo está preparando.
					}

					i.Reloj -= Time.deltaTime;
					SinNucleo(padre);
					if (!padre.Spawned)
					{
						// Si el punto estaba ocupado el juego le asigna otro lejano y reintenta; insistimos en el nuestro.
						padre.firstSpawnPoint = i.Punto;
						padre.firstSpawnPointUsed = false;
						padre.DespawnedTimer = Mathf.Min(padre.DespawnedTimer, 0.5f);
						i.TiempoVisible = 0f;
					}
					else
					{
						i.TiempoVisible += Time.deltaTime;
					}

					if (padre.Spawned && (padre.firstSpawnPointUsed || i.TiempoVisible > 3f))
					{
						float duracion = Plugin.DuracionEnemigo.Value;
						// Hace que los enemigos cercanos (incluido el nuevo) vayan a mirar donde se dijo la palabra.
						EnemyDirector.instance.SetInvestigate(i.Origen, 15f);
						if (duracion <= 0f)
						{
							invocados.RemoveAt(n); // Se queda para siempre, con el ciclo normal del juego.
							break;
						}

						i.Fase = Fase.Activo;
						i.Reloj = duracion;
					}
					else if (i.Reloj <= 0f)
					{
						Plugin.Log.LogWarning($"{padre.enemyName} no llegó a aparecer; se retira.");
						Retirar(i);
					}

					break;

				case Fase.Activo:
					if (!padre.Spawned)
					{
						i.Fase = Fase.Retirado; // Lo mataron o se fue solo.
						break;
					}

					i.Reloj -= Time.deltaTime;
					if (i.Reloj <= 0f)
					{
						Plugin.Log.LogInfo($"Se acabó el tiempo de {padre.enemyName}; se retira.");
						Retirar(i);
					}

					break;

				case Fase.Retirando:
					if (!padre.Spawned)
					{
						i.Fase = Fase.Retirado;
						break;
					}

					i.Reloj -= Time.deltaTime;
					if (i.Reloj <= 0f)
					{
						padre.Despawn(); // No se fue por las buenas: se le desactiva directamente.
						i.Reloj = 5f;
					}

					break;

				case Fase.Retirado:
					if (padre.Spawned)
					{
						Retirar(i);
					}
					else
					{
						padre.DespawnedTimer = Nunca;
					}

					break;
			}
		}
	}

	/// <summary>Los enemigos invocados son un castigo, no botín: si los matan no sueltan su núcleo.</summary>
	private static void SinNucleo(EnemyParent padre)
	{
		if (padre.Enemy && padre.Enemy.HasHealth && padre.Enemy.Health)
		{
			padre.Enemy.Health.spawnValuable = false;
		}
	}

	/// <summary>Le pide al enemigo que se vaya igual que cuando al juego se le acaba su tiempo.</summary>
	private static void Retirar(Invocado i)
	{
		if (i.Padre.Spawned)
		{
			i.Padre.SpawnedTimerSet(0f);
			i.Fase = Fase.Retirando;
			i.Reloj = 10f;
		}
		else
		{
			i.Fase = Fase.Retirado;
		}
	}

	private static EnemySetup? ElegirEnemigo()
	{
		// enemyList son los enemigos que el juego eligió para este nivel.
		List<EnemySetup> candidatos = Filtrar(EnemyDirector.instance.enemyList);
		if (candidatos.Count == 0)
		{
			List<EnemySetup> todos = new List<EnemySetup>();
			todos.AddRange(EnemyDirector.instance.enemiesDifficulty1);
			todos.AddRange(EnemyDirector.instance.enemiesDifficulty2);
			todos.AddRange(EnemyDirector.instance.enemiesDifficulty3);
			candidatos = Filtrar(todos);
		}

		return candidatos.Count == 0 ? null : candidatos[Random.Range(0, candidatos.Count)];
	}

	private static List<EnemySetup> Filtrar(List<EnemySetup> lista)
	{
		List<EnemySetup> salida = new List<EnemySetup>();
		foreach (EnemySetup e in lista)
		{
			if (!e || e.spawnObjects == null || e.spawnObjects.Count == 0 || salida.Contains(e))
			{
				continue;
			}

			// Los grupos (varios objetos, a veces con un "director" común) no están pensados para crearse a mitad de nivel.
			if (e.spawnObjects.Count > 1 && !Plugin.PermitirGrupos.Value)
			{
				continue;
			}

			salida.Add(e);
		}

		return salida;
	}

	private static LevelPoint? ElegirPunto(Vector3 posicion)
	{
		// Los LevelPoint son los puntos de ruta del nivel: sitios donde seguro cabe y camina un enemigo.
		List<LevelPoint> cerca = new List<LevelPoint>();
		LevelPoint? masCercano = null;
		float mejor = float.MaxValue;
		foreach (LevelPoint p in LevelGenerator.Instance.LevelPathPoints)
		{
			if (!p || (p.Room && p.Room.Truck))
			{
				continue;
			}

			float d = Vector3.Distance(p.transform.position, posicion);
			if (d >= DistanciaMinima && d <= DistanciaMaxima)
			{
				cerca.Add(p);
			}

			if (d < mejor)
			{
				mejor = d;
				masCercano = p;
			}
		}

		return cerca.Count > 0 ? cerca[Random.Range(0, cerca.Count)] : masCercano;
	}

	private static void Avisar(string texto)
	{
		Plugin.Log.LogInfo($"Aparición: {texto}");
	}
}
