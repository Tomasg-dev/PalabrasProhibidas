using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Vosk;

namespace PalabraProhibida;

internal enum Modo
{
	/// <summary>Dictado libre: Vosk transcribe todo y las palabras compiten con el resto del idioma.</summary>
	Strict,

	/// <summary>Gramática: Vosk solo puede oír las palabras prohibidas o "desconocido". Oye más, pero se equivoca más.</summary>
	Sensitive,
}

internal struct Deteccion
{
	public string Palabra;

	public string Origen;
}

/// <summary>
/// Hilo propio que recibe trozos de audio, se los pasa a Vosk y avisa cuando aparece alguna de las
/// palabras prohibidas. No toca nada de Unity: se comunica con el plugin mediante colas.
/// </summary>
internal static class Reconocedor
{
	private struct Trozo
	{
		public short[] Muestras;

		public int Frecuencia;
	}

	private struct Oida
	{
		public string Palabra;

		public float Confianza;
	}

	// Cada palabra del resultado de Vosk: {"conf": 0.68, "end": 0.39, "start": 0.0, "word": "vuestro"}
	private static readonly Regex PalabraJson = new Regex("\"conf\"\\s*:\\s*([0-9.eE+-]+)[^}]*?\"word\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.Compiled);

	// Unos 4 segundos de audio en trozos de 20 ms. Si el hilo se atrasa más, se descarta audio.
	private const int MaximoEnCola = 200;

	private const double SegundosEntreDetecciones = 2.0;

	// En modo Sensible la confianza casi siempre es alta, así que se exige al menos esto.
	private const float ConfianzaMinimaSensible = 0.9f;

	private static readonly ConcurrentQueue<Trozo> cola = new ConcurrentQueue<Trozo>();

	private static readonly AutoResetEvent hayAudio = new AutoResetEvent(false);

	private static readonly List<Oida> oidas = new List<Oida>();

	// Palabras válidas en uso. Solo la toca el hilo del reconocedor.
	private static readonly HashSet<string> activas = new HashSet<string>();

	private static Thread? hilo;

	private static volatile bool salir;

	private static string carpeta = "";

	private static bool ingles;

	// El modelo cargado, para consultar su vocabulario desde fuera del hilo.
	private static volatile Model? modeloCargado;

	private static Modo modo;

	private static float confianzaMinima;

	private static long ultimaDeteccion;

	// Palabras que se ha pedido usar. El hilo las valida y las aplica cuando puede.
	private static volatile string[] pedidas = Array.Empty<string>();

	/// <summary>Avisos de palabra detectada, para leer desde el hilo de Unity.</summary>
	internal static readonly ConcurrentQueue<Deteccion> Detecciones = new ConcurrentQueue<Deteccion>();

	internal static void Iniciar(string carpetaPlugin, bool enIngles, Modo modoElegido, float confianza)
	{
		carpeta = carpetaPlugin;
		ingles = enIngles;
		modo = modoElegido;
		confianzaMinima = modo == Modo.Sensitive ? Math.Max(confianza, ConfianzaMinimaSensible) : confianza;
		hilo = new Thread(Bucle) { IsBackground = true, Name = "ForbiddenWords-Vosk" };
		hilo.Start();
	}

	/// <summary>Cambia las palabras a escuchar sin reiniciar (por ejemplo, al empezar una ronda).</summary>
	internal static void CambiarPalabras(string[] palabras)
	{
		string[] actuales = pedidas;
		if (actuales.Length == palabras.Length)
		{
			bool iguales = true;
			for (int i = 0; i < palabras.Length && iguales; i++)
			{
				iguales = actuales[i] == palabras[i];
			}

			if (iguales)
			{
				return;
			}
		}

		pedidas = palabras;
		hayAudio.Set();
	}

	/// <summary>¿Existe la palabra en el vocabulario del modelo? Si aún no ha cargado, se da por buena.</summary>
	internal static bool Conoce(string palabra)
	{
		Model? m = modeloCargado;
		return m == null || (palabra.Length > 0 && palabra.IndexOf(' ') < 0 && m.FindWord(palabra) >= 0);
	}

	internal static void Detener()
	{
		salir = true;
		hayAudio.Set();
	}

	/// <summary>Lo llama el oído desde el hilo de audio de Photon. Las muestras ya son una copia.</summary>
	internal static void Alimentar(short[] muestras, int frecuencia)
	{
		if (cola.Count >= MaximoEnCola)
		{
			return;
		}

		cola.Enqueue(new Trozo { Muestras = muestras, Frecuencia = frecuencia });
		hayAudio.Set();
	}

	private static void Bucle()
	{
		Model? modelo = null;
		VoskRecognizer? reconocedor = null;
		int frecuenciaActual = 0;
		try
		{
			string? rutaModelo = BuscarModelo();
			if (rutaModelo == null)
			{
				Plugin.Log.LogError($"No encuentro el modelo de Vosk de {NombreIdioma()} dentro de {carpeta}");
				return;
			}

			Vosk.Vosk.SetLogLevel(-1);
			Stopwatch reloj = Stopwatch.StartNew();
			modelo = new Model(rutaModelo);
			modeloCargado = modelo;
			Plugin.Log.LogInfo($"Modelo de {NombreIdioma()} cargado en {reloj.ElapsedMilliseconds} ms");

			string ultimoParcial = "";
			string[]? aplicadas = null;
			while (!salir)
			{
				hayAudio.WaitOne(500);
				string[] nuevas = pedidas;
				if (!ReferenceEquals(nuevas, aplicadas))
				{
					aplicadas = nuevas;
					Validar(modelo, nuevas);
					if (modo == Modo.Sensitive)
					{
						// En este modo las palabras forman parte del reconocedor: hay que crearlo de nuevo.
						reconocedor?.Dispose();
						reconocedor = null;
					}
				}

				while (!salir && cola.TryDequeue(out Trozo trozo))
				{
					if (reconocedor == null || trozo.Frecuencia != frecuenciaActual)
					{
						reconocedor?.Dispose();
						frecuenciaActual = trozo.Frecuencia;
						reconocedor = modo == Modo.Sensitive && activas.Count > 0
							? new VoskRecognizer(modelo, frecuenciaActual, Gramatica())
							: new VoskRecognizer(modelo, frecuenciaActual);
						// Pedimos la confianza (0 a 1) de cada palabra, también a mitad de frase.
						reconocedor.SetWords(true);
						reconocedor.SetPartialWords(true);
						Plugin.Log.LogInfo($"Escuchando el micrófono del juego a {frecuenciaActual} Hz ({NombreIdioma()}, modo {modo}, confianza mínima {confianzaMinima:0.00})");
					}

					// AcceptWaveform devuelve true cuando Vosk da una frase por terminada (hubo un silencio).
					if (reconocedor.AcceptWaveform(trozo.Muestras, trozo.Muestras.Length))
					{
						Leer(reconocedor.Result());
						ultimoParcial = "";
						if (oidas.Count > 0)
						{
							// Lo que dice el jugador solo se escribe en el registro si él activa el registro detallado.
							if (Plugin.RegistroDetallado.Value)
							{
								Plugin.Log.LogInfo($"Frase: {Describir()}");
							}

							Comprobar(oidas.Count, "frase completa", null);
						}
					}
					else if (modo == Modo.Strict)
					{
						string json = reconocedor.PartialResult();
						if (json != ultimoParcial)
						{
							ultimoParcial = json;
							Leer(json);
							if (oidas.Count > 0)
							{
								// A mitad de frase la última palabra aún puede cambiar ("monstruo" -> "monstruoso"),
								// así que solo cuenta si ya hay otra palabra detrás.
								Comprobar(oidas.Count - 1, "a mitad de frase", reconocedor);
							}
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			Plugin.Log.LogError($"Fallo en el hilo de reconocimiento: {e}");
		}
		finally
		{
			modeloCargado = null;
			reconocedor?.Dispose();
			modelo?.Dispose();
		}
	}

	private static string NombreIdioma()
	{
		return ingles ? "inglés" : "español";
	}

	/// <summary>Se queda con las palabras que el modelo conoce; las demás no podría oírlas nunca.</summary>
	private static void Validar(Model modelo, string[] palabras)
	{
		activas.Clear();
		List<string> malas = new List<string>();
		foreach (string p in palabras)
		{
			if (p.Length > 0 && p.IndexOf(' ') < 0 && modelo.FindWord(p) >= 0)
			{
				activas.Add(p);
			}
			else
			{
				malas.Add(p);
			}
		}

		if (malas.Count > 0)
		{
			Plugin.Log.LogWarning($"Palabras de esta ronda que tu modelo de {NombreIdioma()} no conoce y no puede detectar: {string.Join(", ", malas)}");
		}
	}

	private static string Gramatica()
	{
		StringBuilder sb = new StringBuilder("[");
		foreach (string p in activas)
		{
			sb.Append('"').Append(p).Append("\", ");
		}

		return sb.Append("\"[unk]\"]").ToString();
	}

	/// <summary>Saca de la respuesta de Vosk la lista de palabras con su confianza.</summary>
	private static void Leer(string json)
	{
		oidas.Clear();
		foreach (Match m in PalabraJson.Matches(json))
		{
			float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float confianza);
			oidas.Add(new Oida { Palabra = m.Groups[2].Value, Confianza = confianza });
		}
	}

	private static string Describir()
	{
		StringBuilder sb = new StringBuilder();
		foreach (Oida o in oidas)
		{
			sb.Append(o.Palabra).Append('(').Append(o.Confianza.ToString("0.00", CultureInfo.InvariantCulture)).Append(") ");
		}

		return sb.ToString().TrimEnd();
	}

	/// <summary>Busca una palabra prohibida entre las primeras <paramref name="cuantas"/> oídas, con confianza suficiente.</summary>
	private static void Comprobar(int cuantas, string origen, VoskRecognizer? reiniciar)
	{
		int cual = -1;
		for (int i = 0; i < cuantas; i++)
		{
			if (oidas[i].Confianza >= confianzaMinima && activas.Contains(oidas[i].Palabra))
			{
				cual = i;
				break;
			}
		}

		if (cual < 0)
		{
			return;
		}

		// Tras detectarla a mitad de frase, se vacía lo oído para que no vuelva a saltar con la misma.
		reiniciar?.Reset();

		long ahora = Stopwatch.GetTimestamp();
		if (ultimaDeteccion != 0 && (ahora - ultimaDeteccion) / (double)Stopwatch.Frequency < SegundosEntreDetecciones)
		{
			return;
		}

		ultimaDeteccion = ahora;
		Detecciones.Enqueue(new Deteccion
		{
			Palabra = oidas[cual].Palabra,
			Origen = $"{origen}, confianza {oidas[cual].Confianza.ToString("0.00", CultureInfo.InvariantCulture)}",
		});
	}

	/// <summary>
	/// Los gestores de mods (Gale, r2modman) aplanan las carpetas del paquete, y Vosk necesita la
	/// estructura de carpetas del modelo intacta. Por eso cada modelo viaja como un único .zip y se
	/// descomprime la primera vez en BepInEx/cache/&lt;nombre del mod&gt;.
	/// </summary>
	private static string? BuscarModelo()
	{
		// Los modelos de Vosk llevan el idioma en el nombre: vosk-model-small-es-0.42, vosk-model-small-en-us-0.15...
		string marca = ingles ? "-en-" : "-es-";

		// Instalación manual: una carpeta de modelo ya descomprimida junto al plugin.
		string? suelto = BuscarModeloEn(carpeta, marca);
		if (suelto != null)
		{
			return suelto;
		}

		string[] zips = Directory.GetFiles(carpeta, $"vosk-model*{marca}*.zip", SearchOption.AllDirectories);
		if (zips.Length == 0)
		{
			return null;
		}

		string destino = Path.Combine(BepInEx.Paths.CachePath, Plugin.Nombre, Path.GetFileNameWithoutExtension(zips[0]));
		string hecho = Path.Combine(destino, ".descomprimido");
		if (!File.Exists(hecho))
		{
			if (Directory.Exists(destino))
			{
				Directory.Delete(destino, true);
			}

			Directory.CreateDirectory(destino);
			ZipFile.ExtractToDirectory(zips[0], destino);
			File.WriteAllText(hecho, "");
			Plugin.Log.LogInfo($"Modelo descomprimido en {destino}");
		}

		return BuscarModeloEn(destino, marca);
	}

	private static string? BuscarModeloEn(string raiz, string marca)
	{
		foreach (string mdl in Directory.GetFiles(raiz, "final.mdl", SearchOption.AllDirectories))
		{
			// Estructura de un modelo de Vosk: <modelo>/am/final.mdl
			string? am = Path.GetDirectoryName(mdl);
			if (am == null || Path.GetFileName(am) != "am")
			{
				continue;
			}

			string? modelo = Path.GetDirectoryName(am);
			if (modelo != null && Path.GetFileName(modelo).Contains(marca))
			{
				return modelo;
			}
		}

		return null;
	}
}
