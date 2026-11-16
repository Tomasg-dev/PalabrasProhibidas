using System;
using HarmonyLib;
using Photon.Voice;

namespace PalabraProhibida;

/// <summary>
/// Se engancha a la voz local de Photon Voice: recibe el mismo audio que el juego envía a los
/// demás jugadores, lo copia para Vosk y lo devuelve intacto. Así no abrimos el micrófono dos veces.
/// Ojo: Photon solo procesa audio mientras se está transmitiendo, así que con el micro
/// silenciado en el juego no se oye nada (y tampoco te oirían los demás).
/// </summary>
internal sealed class Oido : IProcessor<float>, IProcessor<short>
{
	private readonly int frecuencia;

	private readonly int canales;

	internal Oido(int frecuencia, int canales)
	{
		this.frecuencia = frecuencia;
		this.canales = Math.Max(1, canales);
	}

	public float[] Process(float[] buf)
	{
		// Photon reutiliza el búfer, por eso se copia. De paso se mezcla a mono y se pasa a 16 bits.
		short[] copia = new short[buf.Length / canales];
		for (int i = 0; i < copia.Length; i++)
		{
			float suma = 0f;
			for (int c = 0; c < canales; c++)
			{
				suma += buf[i * canales + c];
			}

			float muestra = Math.Max(-1f, Math.Min(1f, suma / canales));
			copia[i] = (short)(muestra * short.MaxValue);
		}

		Reconocedor.Alimentar(copia, frecuencia);
		return buf;
	}

	public short[] Process(short[] buf)
	{
		short[] copia = new short[buf.Length / canales];
		for (int i = 0; i < copia.Length; i++)
		{
			int suma = 0;
			for (int c = 0; c < canales; c++)
			{
				suma += buf[i * canales + c];
			}

			copia[i] = (short)(suma / canales);
		}

		Reconocedor.Alimentar(copia, frecuencia);
		return buf;
	}

	public void Dispose()
	{
	}
}

/// <summary>
/// El juego prepara la voz local en PlayerVoiceChat.PhotonVoiceCreated. Justo después añadimos
/// nuestro oído a esa misma voz. Cada vez que el juego recrea la voz, se vuelve a añadir.
/// </summary>
[HarmonyPatch(typeof(PlayerVoiceChat), nameof(PlayerVoiceChat.PhotonVoiceCreated))]
internal static class PlayerVoiceChatPatch
{
	private static void Postfix(PlayerVoiceChat __instance)
	{
		if (!__instance.photonView.IsMine)
		{
			return;
		}

		LocalVoice voz = __instance.localVoice;
		if (voz == null)
		{
			return;
		}

		Oido oido = new Oido(voz.Info.SamplingRate, voz.Info.Channels);
		if (voz is LocalVoiceAudioFloat vozFloat)
		{
			vozFloat.AddPostProcessor(oido);
		}
		else if (voz is LocalVoiceAudioShort vozShort)
		{
			vozShort.AddPostProcessor(oido);
		}
		else
		{
			Plugin.Log.LogWarning($"Tipo de voz inesperado: {voz.GetType().Name}");
			return;
		}

		Plugin.Log.LogInfo($"Oído enganchado a la voz local: {voz.Info.SamplingRate} Hz, {voz.Info.Channels} canal(es), {voz.GetType().Name}");
	}
}
