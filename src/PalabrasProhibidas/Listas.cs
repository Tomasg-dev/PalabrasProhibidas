using System;
using System.Collections.Generic;

namespace PalabraProhibida;

/// <summary>
/// La lista de palabras de fábrica y los textos de pantalla. Cada mod lleva solo la lista de su idioma;
/// todas las palabras están comprobadas contra el vocabulario del modelo pequeño de Vosk de ese idioma.
/// </summary>
internal static class Listas
{
#if INGLES
	private const string DeFabrica =
		"monster careful help running cart money dead door wait truck pistol enemy head extraction shop upgrade " +
		"health heavy quickly slowly quiet hiding upstairs downstairs left right behind forward danger relax clown " +
		"duck robot hunter ghost bomb grenade flashlight map exit entrance hallway room stairs kitchen vase diamond " +
		"valuable fragile broken drop grab throw push lift revive heal energy battery crystal sword hammer sorry " +
		"thanks buddy brother dude damn really never always maybe problem please okay inside outside";
#else
	private const string DeFabrica =
		"monstruo cuidado ayuda corre carro dinero muerto puerta vamos espera camión pistola enemigo cabeza " +
		"extracción tienda mejora salud pesado rápido despacio silencio escondite arriba abajo izquierda derecha " +
		"detrás delante socorro peligro tranquilo payaso pato robot cazador fantasma bomba granada linterna mapa " +
		"salida entrada pasillo habitación escalera cocina jarrón diamante valioso frágil roto suelta agarra coge " +
		"tira empuja levanta revive curar energía batería cristal espada martillo perdón gracias amigo hermano tío " +
		"joder mierda hostia verdad mentira nunca siempre quizás problema favor vale dentro fuera";
#endif

	/// <summary>Lista de la que se sortean las palabras: la personalizada si existe, si no la de fábrica.</summary>
	internal static List<string> Bolsa(string personalizadas)
	{
		List<string> salida = Partir(personalizadas, ',', ';', '\n');
		if (salida.Count == 0)
		{
			salida = Partir(DeFabrica, ' ');
		}

		return salida;
	}

	/// <summary>Sortea <paramref name="cuantas"/> palabras distintas de la bolsa.</summary>
	internal static string[] Sortear(List<string> bolsa, int cuantas)
	{
		List<string> copia = new List<string>(bolsa);
		List<string> elegidas = new List<string>();
		while (elegidas.Count < cuantas && copia.Count > 0)
		{
			int i = UnityEngine.Random.Range(0, copia.Count);
			elegidas.Add(copia[i]);
			copia.RemoveAt(i);
		}

		return elegidas.ToArray();
	}

	private static List<string> Partir(string texto, params char[] separadores)
	{
		List<string> salida = new List<string>();
		foreach (string trozo in texto.Split(separadores, StringSplitOptions.RemoveEmptyEntries))
		{
			string palabra = trozo.Trim().ToLowerInvariant();
			// Una sola palabra por entrada, y sin el separador que usamos al enviarlas por red.
			if (palabra.Length > 0 && palabra.IndexOf(' ') < 0 && palabra.IndexOf('|') < 0 && !salida.Contains(palabra))
			{
				salida.Add(palabra);
			}
		}

		return salida;
	}

	// Textos de pantalla. Cada jugador los ve en el idioma de SU configuración.

	internal static string TituloLista(bool ingles)
	{
		return ingles ? "FORBIDDEN WORDS" : "PALABRAS PROHIBIDAS";
	}

	internal static string Dijo(bool ingles)
	{
		return ingles ? "SAID" : "DIJO";
	}

	internal static string Alguien(bool ingles)
	{
		return ingles ? "SOMEONE" : "ALGUIEN";
	}
}
