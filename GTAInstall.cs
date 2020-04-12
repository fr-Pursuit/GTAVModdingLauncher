using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;

namespace GTAVModdingLauncher
{
	public class GTAInstall
	{
		public static GTAInstall[] FindInstalls()
		{
			List<GTAInstall> installs = new List<GTAInstall>();

			RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Rockstar Games\\Grand Theft Auto V", false);

			if(regKey != null)
			{
				installs.Add(new GTAInstall(false, (string)regKey.GetValue("InstallFolder"), InstallType.Retail));
				regKey.Close();
			}

			regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Rockstar Games\\GTAV", false);

			if(regKey != null)
			{
				string path = (string)regKey.GetValue("InstallFolderSteam");
				regKey.Close();

				if(path != null)
					path = path.Substring(0, path.Length - 4);

				installs.Add(new GTAInstall(false, path, InstallType.Steam));
			}
			else if(SteamHelper.IsAvailable)
			{
				string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(SteamHelper.ExecutablePath), "steamapps\\common\\Grand Theft Auto V");

				if(File.Exists(System.IO.Path.Combine(path, "gta5.exe")))
					installs.Add(new GTAInstall(false, path, InstallType.Steam));
			}

			return installs.ToArray();
		}

		public bool IsCustom { get; set; }

		public string Path { get; set; }

		public InstallType Type { get; set; }

		public GTAInstall(string path, InstallType type) : this(true, path, type) {}

		private GTAInstall(bool custom, string path, InstallType type)
		{
			this.IsCustom = custom;
			this.Path = path;
			this.Type = type;
		}
	}
}
