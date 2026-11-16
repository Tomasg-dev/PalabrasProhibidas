using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PalabraProhibida;

/// <summary>
/// Mensajes grandes en el centro de la pantalla. Es un lienzo propio que se dibuja por encima de
/// todo (mismo enfoque que el aviso de PlayerMonster), con la fuente del juego y animación propia:
/// entrada con golpe de escala, temblor, banda oscura detrás y destello rojo a pantalla completa.
/// </summary>
internal static class Pantalla
{
	private enum Tipo
	{
		Nada,
		Dicha,
		Lista,
	}

	private const int PalabrasPorLinea = 4;

	private const float SegundosDicha = 4.5f;

	private static GameObject? raiz;

	private static TextMeshProUGUI? texto;

	private static Image? banda;

	private static Image? destello;

	private static Tipo mostrando = Tipo.Nada;

	// Momento en que empezó el mensaje que se está mostrando, para animarlo.
	private static float inicio;

	private static string textoDicha = "";

	private static float dichaDesde;

	private static float dichaHasta;

	private static string textoLista = "";

	private static float listaDesde = float.MaxValue;

	private static float listaHasta = float.MaxValue;

	private static bool listaPendiente;

	/// <summary>Alguien ha dicho una palabra prohibida.</summary>
	internal static void MostrarDicha(string nombre, string palabra)
	{
		string verbo = Listas.Dijo(Plugin.Ingles);
		textoDicha = $"<size=45%>{nombre.ToUpperInvariant()} {verbo}</size>\n\"{palabra.ToUpperInvariant()}\"";
		dichaDesde = Time.unscaledTime;
		dichaHasta = dichaDesde + SegundosDicha;
		mostrando = Tipo.Nada; // Fuerza a reiniciar la animación aunque ya hubiera otro mensaje.

		// Además del mensaje: fallo de imagen largo del juego (con su sonido) y sacudida de cámara.
		if (CameraGlitch.Instance)
		{
			CameraGlitch.Instance.PlayLong();
		}

		if (GameDirector.instance)
		{
			SemiFunc.CameraShake(5f, 0.8f);
		}
	}

	/// <summary>Empieza una ronda: se enseñan las palabras prohibidas unos segundos.</summary>
	internal static void MostrarLista(string[] palabras)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("<size=45%><color=#FF8A00>").Append(Listas.TituloLista(Plugin.Ingles)).Append("</color></size>");
		for (int i = 0; i < palabras.Length; i++)
		{
			sb.Append(i % PalabrasPorLinea == 0 ? "\n" : "  ·  ");
			sb.Append(palabras[i].ToUpperInvariant());
		}

		textoLista = sb.ToString();
		// Cuando el host sortea, el nivel está generado pero aún se ve la pantalla de carga y la
		// animación de llegada. La cuenta empieza en Tick, cuando el jugador ya puede moverse.
		listaPendiente = true;
		listaDesde = float.MaxValue;
		listaHasta = float.MaxValue;
	}

	/// <summary>Se llama cada fotograma desde el plugin.</summary>
	internal static void Tick()
	{
		float ahora = Time.unscaledTime;
		if (listaPendiente && GameDirector.instance && GameDirector.instance.currentState == GameDirector.gameState.Main
			&& (!LoadingUI.instance || LoadingUI.instance.levelAnimationCompleted))
		{
			listaPendiente = false;
			listaDesde = ahora + 1.5f;
			listaHasta = listaDesde + Plugin.SegundosLista.Value;
		}

		// Qué toca mostrar ahora. Quien dice una palabra tiene prioridad sobre la lista.
		Tipo toca = Tipo.Nada;
		float fin = 0f;
		if (ahora < dichaHasta)
		{
			toca = Tipo.Dicha;
			fin = dichaHasta;
		}
		else if (ahora >= listaDesde && ahora < listaHasta)
		{
			toca = Tipo.Lista;
			fin = listaHasta;
		}

		if (toca == Tipo.Nada)
		{
			mostrando = Tipo.Nada;
			if (raiz && raiz!.activeSelf)
			{
				raiz.SetActive(false);
			}

			return;
		}

		Crear();
		if (!raiz || !texto || !banda || !destello)
		{
			return;
		}

		if (toca != mostrando)
		{
			mostrando = toca;
			inicio = toca == Tipo.Dicha ? dichaDesde : ahora;
			texto!.text = toca == Tipo.Dicha ? textoDicha : textoLista;
			texto.fontSizeMax = toca == Tipo.Dicha ? 190f : 120f;
			raiz!.SetActive(true);
			if (toca == Tipo.Lista && CameraGlitch.Instance)
			{
				CameraGlitch.Instance.PlayShort();
			}
		}

		// La fuente del juego se toma de su propio mensaje grande, en cuanto existe.
		if (BigMessageUI.instance && BigMessageUI.instance.Text && texto!.font != BigMessageUI.instance.Text.font)
		{
			texto.font = BigMessageUI.instance.Text.font;
		}

		bool dicha = toca == Tipo.Dicha;
		float t = ahora - inicio;

		// "Golpe": vale 1 al aparecer y baja a 0 en un instante. Mueve la escala, el temblor y el destello.
		float golpe = Mathf.Clamp01(1f - t / (dicha ? 0.45f : 0.3f));
		golpe *= golpe;

		// Aparece casi de golpe y se desvanece al final.
		float alfa = Mathf.Min(Mathf.Clamp01(t / 0.08f), Mathf.Clamp01((fin - ahora) / 0.5f));

		RectTransform rt = texto!.rectTransform;
		rt.localScale = Vector3.one * (1f + (dicha ? 1.6f : 0.4f) * golpe);
		rt.anchoredPosition = Random.insideUnitCircle * ((dicha ? 30f : 8f) * golpe + (dicha ? 2.5f : 0f));

		Color color;
		if (dicha)
		{
			// Blanco al golpear, luego rojo que late.
			float latido = 0.5f + 0.5f * Mathf.Sin(t * 14f);
			color = Color.Lerp(Color.Lerp(new Color(1f, 0.05f, 0.05f), new Color(1f, 0.4f, 0.25f), latido), Color.white, golpe);
		}
		else
		{
			color = Color.Lerp(Color.white, new Color(1f, 0.54f, 0f), golpe);
		}

		color.a = alfa;
		texto.color = color;
		banda!.color = new Color(0f, 0f, 0f, 0.6f * alfa);
		destello!.color = new Color(0.9f, 0f, 0f, dicha ? 0.55f * Mathf.Clamp01(1f - t / 0.7f) : 0f);
	}

	private static void Crear()
	{
		if (raiz)
		{
			return;
		}

		raiz = new GameObject("ForbiddenWords Pantalla");
		Object.DontDestroyOnLoad(raiz);
		raiz.hideFlags = HideFlags.HideAndDontSave;
		Canvas lienzo = raiz.AddComponent<Canvas>();
		lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
		lienzo.sortingOrder = 30001;
		CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
		escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		escala.referenceResolution = new Vector2(1920f, 1080f);
		escala.matchWidthOrHeight = 0.5f;

		// Destello rojo a pantalla completa (solo cuando alguien dice una palabra).
		destello = NuevaImagen("Destello");
		destello.rectTransform.anchorMin = Vector2.zero;
		destello.rectTransform.anchorMax = Vector2.one;
		destello.rectTransform.sizeDelta = Vector2.zero;

		// Banda oscura de lado a lado, para que el texto se lea sobre cualquier fondo.
		banda = NuevaImagen("Banda");
		banda.rectTransform.anchorMin = new Vector2(0f, 0.6f);
		banda.rectTransform.anchorMax = new Vector2(1f, 0.6f);
		banda.rectTransform.sizeDelta = new Vector2(0f, 330f);

		GameObject objeto = new GameObject("Texto");
		objeto.transform.SetParent(raiz.transform, false);
		texto = objeto.AddComponent<TextMeshProUGUI>();
		texto.alignment = TextAlignmentOptions.Center;
		texto.raycastTarget = false;
		texto.fontStyle = FontStyles.Bold;
		texto.richText = true;
		// El tamaño se ajusta solo para que quepa una lista larga.
		texto.enableAutoSizing = true;
		texto.fontSizeMin = 30f;
		texto.fontSizeMax = 120f;
		texto.lineSpacing = -10f;
		texto.rectTransform.anchorMin = new Vector2(0.5f, 0.6f);
		texto.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
		texto.rectTransform.sizeDelta = new Vector2(1750f, 310f);
		raiz.SetActive(false);
	}

	private static Image NuevaImagen(string nombre)
	{
		GameObject objeto = new GameObject(nombre);
		objeto.transform.SetParent(raiz!.transform, false);
		Image imagen = objeto.AddComponent<Image>();
		imagen.raycastTarget = false;
		imagen.color = Color.clear;
		return imagen;
	}
}
