using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PalabraProhibida;

/// <summary>
/// Vosk es una DLL nativa (libvosk.dll) con tres DLL de apoyo. Windows no busca DLL nativas
/// en la carpeta del plugin, así que la cargamos nosotros por ruta completa antes de usarla.
/// Una vez cargada, el [DllImport("libvosk")] de Vosk.dll la encuentra por nombre.
/// </summary>
internal static class CargaNativa
{
	// Hace que las dependencias de la DLL se busquen en su misma carpeta.
	private const uint BuscarJuntoALaDll = 0x00000008;

	[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern IntPtr LoadLibraryExW(string ruta, IntPtr reservado, uint opciones);

	internal static bool Cargar(string carpetaPlugin)
	{
		string[] encontradas = Directory.GetFiles(carpetaPlugin, "libvosk.dll", SearchOption.AllDirectories);
		if (encontradas.Length == 0)
		{
			Plugin.Log.LogError($"No encuentro libvosk.dll dentro de {carpetaPlugin}");
			return false;
		}

		IntPtr modulo = LoadLibraryExW(encontradas[0], IntPtr.Zero, BuscarJuntoALaDll);
		if (modulo == IntPtr.Zero)
		{
			Plugin.Log.LogError($"No se pudo cargar {encontradas[0]} (error de Windows {Marshal.GetLastWin32Error()})");
			return false;
		}

		Plugin.Log.LogInfo($"Vosk nativo cargado desde {encontradas[0]}");
		return true;
	}
}
