using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace PalabraProhibida;

/// <summary>
/// Cada nivel tiene unas palabras prohibidas. Vosk las reconoce en el micrófono del juego de cada
/// jugador; quien dice una avisa al host, y el host hace aparecer un rato un enemigo a su lado.
/// </summary>
[BepInPlugin(Guid, Nombre, Version)]
public class Plugin : BaseUnityPlugin
{
	// El idioma se fija al compilar (constante INGLES del .csproj). El mismo código da dos mods:
	// ForbiddenWords (inglés) y PalabrasProhibidas (español), cada uno con su modelo de voz.
#if INGLES
	public const string Guid = "tommy777.forbiddenwords";

	public const string Nombre = "ForbiddenWords";

	internal static readonly bool Ingles = true;
#else
	public const string Guid = "tommy777.palabrasprohibidas";

	public const string Nombre = "PalabrasProhibidas";

	internal static readonly bool Ingles = false;
#endif

	public const string Version = "1.0.0";

	/// <summary>Marca común a los dos mods: si están instalados a la vez, solo trabaja el primero.</summary>
	private const string MarcaActivo = "tommy777.forbiddenwords.activo";

	internal static ManualLogSource Log = null!;

	internal static ConfigEntry<string> PalabrasPersonalizadas = null!;

	internal static ConfigEntry<int> PalabrasIniciales = null!;

	internal static ConfigEntry<int> PalabrasPorNivel = null!;

	internal static ConfigEntry<int> MaximoPalabras = null!;

	internal static ConfigEntry<float> SegundosLista = null!;

	internal static ConfigEntry<Modo> ModoReconocimiento = null!;

	internal static ConfigEntry<float> ConfianzaMinima = null!;

	internal static ConfigEntry<float> DuracionEnemigo = null!;

	internal static ConfigEntry<float> EsperaEntreApariciones = null!;

	internal static ConfigEntry<int> MaximoALaVez = null!;

	internal static ConfigEntry<bool> PermitirGrupos = null!;

	internal static ConfigEntry<bool> RegistroDetallado = null!;

	private void Awake()
	{
		Log = Logger;

		if (System.AppDomain.CurrentDomain.GetData(MarcaActivo) is string otro)
		{
			Log.LogWarning(T(
				$"{otro} is also installed. ForbiddenWords and PalabrasProhibidas are the same mod in two languages: keep only one. {Nombre} stays off.",
				$"{otro} también está instalado. ForbiddenWords y PalabrasProhibidas son el mismo mod en dos idiomas: deja solo uno. {Nombre} queda apagado."));
			enabled = false;
			return;
		}

		System.AppDomain.CurrentDomain.SetData(MarcaActivo, Nombre);

		// Las claves son las mismas en los dos mods; solo cambia el idioma de las descripciones.
		PalabrasPersonalizadas = Config.Bind("Words", "CustomList", "", T(
			"Your own word list, comma separated (e.g. cat, dog, house). Empty = use the built-in list. One word per entry, lower case, and it must exist in the voice model's vocabulary (unknown words are skipped). Host only.",
			"Tu propia lista de palabras, separadas por comas (p. ej. gato, perro, casa). Vacío = lista de fábrica. Una palabra por entrada, en minúsculas, y tiene que existir en el vocabulario del modelo de voz (las desconocidas se saltan). Solo anfitrión."));
		PalabrasIniciales = Config.Bind("Words", "StartingWords", 2,
			new ConfigDescription(T("Forbidden words on the first level. Host only.", "Palabras prohibidas en el primer nivel. Solo anfitrión."), new AcceptableValueRange<int>(1, 30)));
		PalabrasPorNivel = Config.Bind("Words", "WordsPerLevel", 1,
			new ConfigDescription(T("Extra forbidden words for each level completed. Host only.", "Palabras prohibidas de más por cada nivel superado. Solo anfitrión."), new AcceptableValueRange<int>(0, 10)));
		MaximoPalabras = Config.Bind("Words", "MaxWords", 8,
			new ConfigDescription(T("Most forbidden words at once. Host only.", "Máximo de palabras prohibidas a la vez. Solo anfitrión."), new AcceptableValueRange<int>(1, 30)));
		SegundosLista = Config.Bind("Words", "ListSeconds", 8f,
			new ConfigDescription(T("How long the forbidden words stay on screen when a level starts.", "Segundos que se muestran las palabras prohibidas al empezar un nivel."), new AcceptableValueRange<float>(2f, 30f)));

		ModoReconocimiento = Config.Bind("Recognition", "Mode", Modo.Strict, T(
			"Strict: free dictation, the word must be heard whole and clear (almost no false triggers, misses a few). Sensitive: the recogniser can only hear the forbidden words (misses almost none, but similar or half-said words trigger it).",
			"Strict: dictado libre, la palabra tiene que oírse entera y clara (casi nunca salta por error, se le escapa alguna). Sensitive: el reconocedor solo puede oír las palabras prohibidas (casi no se le escapa ninguna, pero saltan palabras parecidas o a medio decir)."));
		ConfianzaMinima = Config.Bind("Recognition", "MinConfidence", 0.5f,
			new ConfigDescription(T(
				"How sure (0 to 1) the recogniser must be. Raise it if words trigger that you did not say; lower it if real words are missed. Never below 0.9 in Sensitive mode.",
				"Seguridad (de 0 a 1) que debe tener el reconocedor. Súbela si saltan palabras que no dijiste; bájala si se le escapan las de verdad. Nunca menos de 0.9 en modo Sensitive."), new AcceptableValueRange<float>(0f, 1f)));

		DuracionEnemigo = Config.Bind("Enemy", "Seconds", 60f,
			new ConfigDescription(T("How long a summoned enemy stays before it vanishes. 0 = it stays for good. Host only.", "Segundos que se queda un enemigo invocado antes de desaparecer. 0 = se queda para siempre. Solo anfitrión."), new AcceptableValueRange<float>(0f, 600f)));
		EsperaEntreApariciones = Config.Bind("Enemy", "Cooldown", 0f,
			new ConfigDescription(T("Minimum seconds between two summons. 0 = no cooldown. Host only.", "Segundos mínimos entre dos invocaciones. 0 = sin espera. Solo anfitrión."), new AcceptableValueRange<float>(0f, 300f)));
		MaximoALaVez = Config.Bind("Enemy", "MaxAtOnce", 0,
			new ConfigDescription(T("Most summoned enemies alive at once. 0 = no limit. Host only.", "Máximo de enemigos invocados vivos a la vez. 0 = sin límite. Solo anfitrión."), new AcceptableValueRange<int>(0, 50)));
		PermitirGrupos = Config.Bind("Enemy", "AllowGroups", false, T(
			"Also summon enemies that come in groups (several at once). They may misbehave when created mid-level. Host only.",
			"Invocar también enemigos que vienen en grupo (varios a la vez). Pueden fallar al crearse a mitad de nivel. Solo anfitrión."));

		RegistroDetallado = Config.Bind("Debug", "VerboseLog", false, T(
			"Writes every phrase the recogniser hears from YOUR microphone to the BepInEx log, with confidence values. Only for troubleshooting.",
			"Escribe en el log de BepInEx cada frase que el reconocedor oye de TU micrófono, con su confianza. Solo para buscar fallos."));

		string carpeta = Path.GetDirectoryName(Info.Location)!;
		if (!CargaNativa.Cargar(carpeta))
		{
			return;
		}

		Reconocedor.Iniciar(carpeta, Ingles, ModoReconocimiento.Value, ConfianzaMinima.Value);
		new Harmony(Guid).PatchAll();
		Log.LogInfo($"{Nombre} {Version} cargado. Idioma: {(Ingles ? "inglés" : "español")}");
	}

	/// <summary>Texto en el idioma del mod.</summary>
	private static string T(string ingles, string espanol)
	{
		return Ingles ? ingles : espanol;
	}

	private void Update()
	{
		// El reconocedor trabaja en otro hilo; aquí recogemos sus avisos en el hilo de Unity.
		while (Reconocedor.Detecciones.TryDequeue(out Deteccion d))
		{
			Log.LogInfo($"Palabra prohibida detectada: \"{d.Palabra}\" ({d.Origen})");
			Red.DeteccionLocal(d.Palabra);
		}

		Red.Tick();
		Aparicion.Tick();
		Pantalla.Tick();
	}

	private void OnDestroy()
	{
		Reconocedor.Detener();
	}
}
