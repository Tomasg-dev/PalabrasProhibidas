using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PalabraProhibida;

/// <summary>
/// La parte de red y de rondas, sobre el Photon que ya usa el juego:
///  1. Al empezar cada nivel el host sortea las palabras prohibidas (más cuantos más niveles
///     superados) y las publica en propiedades de la sala; los clientes las leen y las escuchan.
///  2. Un cliente que detecta una palabra en su micro manda un aviso al host. No viaja audio.
///  3. El host decide si aparece un enemigo y, si aparece, anuncia a todos quién dijo qué.
/// </summary>
internal static class Red
{
	// Códigos de evento de Photon (0-199 son para el juego y los mods). Como otro mod podría usar
	// los mismos, cada mensaje lleva además una marca para reconocerlo como nuestro.
	private const byte EventoDeteccion = 147;

	private const byte EventoAnuncio = 148;

	private const string Marca = "tommy777.forbiddenwords";

	private const string PropiedadPalabras = "ForbiddenWords.words";

	// Número que sube cada vez que el host sortea: así los clientes saben que empieza una ronda nueva.
	private const string PropiedadRonda = "ForbiddenWords.round";

	private static float proximaRevision;

	private static bool suscrito;

	private static bool eraHost;

	private static int nivelSorteado;

	private static int ronda;

	private static int rondaMostrada = -1;

	/// <summary>Palabras prohibidas de la ronda actual.</summary>
	internal static string[] Activas = Array.Empty<string>();

	/// <summary>Se llama cuando el micrófono propio ha oído una palabra prohibida.</summary>
	internal static void DeteccionLocal(string palabra)
	{
		if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
		{
			// Soy el host (o juego solo): decido aquí mismo.
			Resolver(PlayerAvatar.instance, PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : -1, palabra);
			return;
		}

		PhotonNetwork.RaiseEvent(EventoDeteccion, new object[] { Marca, palabra },
			new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
	}

	/// <summary>Se llama cada fotograma desde el plugin.</summary>
	internal static void Tick()
	{
		if (!suscrito)
		{
			PhotonNetwork.NetworkingClient.EventReceived += AlRecibir;
			suscrito = true;
		}

		if (Time.unscaledTime < proximaRevision)
		{
			return;
		}

		proximaRevision = Time.unscaledTime + 0.5f;
		bool enNivel = SemiFunc.RunIsLevel() && LevelGenerator.Instance && LevelGenerator.Instance.Generated;
		bool soyHost = !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
		ExitGames.Client.Photon.Hashtable? propiedades = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.CustomProperties : null;

		if (soyHost)
		{
			if (!eraHost && propiedades != null && propiedades[PropiedadPalabras] is string heredadas && enNivel)
			{
				// Paso a ser host a mitad de nivel: sigo con las palabras que ya había.
				Usar(heredadas.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
				nivelSorteado = LevelGenerator.Instance!.GetInstanceID();
				ronda = propiedades[PropiedadRonda] is int r ? r : 0;
				rondaMostrada = ronda;
			}

			// Cada nivel es una escena nueva con su propio LevelGenerator: sirve para saber que ha cambiado.
			if (enNivel && LevelGenerator.Instance!.GetInstanceID() != nivelSorteado)
			{
				nivelSorteado = LevelGenerator.Instance!.GetInstanceID();
				Sortear();
				if (PhotonNetwork.InRoom)
				{
					PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
					{
						{ PropiedadPalabras, string.Join("|", Activas) },
						{ PropiedadRonda, ronda },
					});
				}

				rondaMostrada = ronda;
				Pantalla.MostrarLista(Activas);
			}
		}
		else if (propiedades != null && propiedades[PropiedadPalabras] is string publicadas)
		{
			Usar(publicadas.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
			if (enNivel && Activas.Length > 0 && propiedades[PropiedadRonda] is int r && r != rondaMostrada)
			{
				rondaMostrada = r;
				Pantalla.MostrarLista(Activas);
			}
		}

		eraHost = soyHost;
		Reconocedor.CambiarPalabras(Activas);
	}

	/// <summary>El host elige las palabras de la ronda: más cuantos más niveles se han superado.</summary>
	private static void Sortear()
	{
		int superados = RunManager.instance ? RunManager.instance.levelsCompleted : 0;
		int cuantas = Plugin.PalabrasIniciales.Value + Plugin.PalabrasPorNivel.Value * superados;
		cuantas = Mathf.Clamp(cuantas, 1, Mathf.Max(1, Plugin.MaximoPalabras.Value));
		List<string> bolsa = Listas.Bolsa(Plugin.PalabrasPersonalizadas.Value);
		// Fuera las palabras que el modelo de voz no conoce: nadie podría "decirlas" nunca.
		int antes = bolsa.Count;
		bolsa.RemoveAll(p => !Reconocedor.Conoce(p));
		if (bolsa.Count < antes)
		{
			Plugin.Log.LogWarning($"{antes - bolsa.Count} palabra(s) de la lista no existen en el vocabulario del modelo de voz y se ignoran.");
		}

		Activas = Listas.Sortear(bolsa, cuantas);
		ronda++;
		Plugin.Log.LogInfo($"Ronda {ronda} (niveles superados: {superados}): {string.Join(", ", Activas)}");
	}

	private static void Usar(string[] palabras)
	{
		if (palabras.Length == Activas.Length)
		{
			bool iguales = true;
			for (int i = 0; i < palabras.Length && iguales; i++)
			{
				iguales = palabras[i] == Activas[i];
			}

			if (iguales)
			{
				return;
			}
		}

		Activas = palabras;
	}

	private static void AlRecibir(EventData evento)
	{
		if (evento.Code != EventoDeteccion && evento.Code != EventoAnuncio)
		{
			return;
		}

		if (!(evento.CustomData is object[] datos) || datos.Length < 2 || !(datos[0] is string marca) || marca != Marca)
		{
			return;
		}

		if (evento.Code == EventoDeteccion)
		{
			// Solo el host decide, y solo valen las palabras de la ronda. Quién habló se saca del
			// remitente que da Photon, no del contenido del mensaje.
			if (PhotonNetwork.IsMasterClient && datos[1] is string palabra && Array.IndexOf(Activas, palabra) >= 0)
			{
				Resolver(BuscarJugador(evento.Sender), evento.Sender, palabra);
			}
		}
		else if (datos.Length >= 3 && datos[1] is int actor && datos[2] is string dicha)
		{
			// Los anuncios solo valen si vienen del host.
			if (PhotonNetwork.MasterClient != null && evento.Sender == PhotonNetwork.MasterClient.ActorNumber)
			{
				Anunciar(BuscarJugador(actor), dicha);
			}
		}
	}

	private static void Resolver(PlayerAvatar? jugador, int actor, string palabra)
	{
		if (!Aparicion.Pedir(jugador))
		{
			return;
		}

		if (PhotonNetwork.InRoom)
		{
			PhotonNetwork.RaiseEvent(EventoAnuncio, new object[] { Marca, actor, palabra },
				new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
		}
		else
		{
			Anunciar(jugador, palabra);
		}
	}

	private static void Anunciar(PlayerAvatar? jugador, string palabra)
	{
		string nombre = jugador ? jugador!.playerName : Listas.Alguien(Plugin.Ingles);
		Pantalla.MostrarDicha(nombre, palabra);
	}

	private static PlayerAvatar? BuscarJugador(int actor)
	{
		if (!GameDirector.instance)
		{
			return null;
		}

		foreach (PlayerAvatar p in GameDirector.instance.PlayerList)
		{
			if (p && p.photonView && p.photonView.OwnerActorNr == actor)
			{
				return p;
			}
		}

		return null;
	}
}
