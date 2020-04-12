using Microsoft.Win32;
using System.IO;

namespace GTAVModdingLauncher
{
	public static class SteamHelper
	{
		public static bool IsAvailable => ExecutablePath != null;

		public static string ExecutablePath { get; set; } = null;

		public static void Initialize()
		{
			RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Valve\\Steam", false);

			if(regKey != null)
			{
				ExecutablePath = Path.Combine((string) regKey.GetValue("InstallPath"), "steam.exe");
				regKey.Close();
			}
			else ExecutablePath = null;
		}
	}
}
