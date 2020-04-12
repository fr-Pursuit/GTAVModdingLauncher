using PursuitLib.IO;
using System.Collections.Generic;
using System.IO;

namespace GTAVModdingLauncher
{
	public class InstallList : XMLFile
	{
		public List<GTAInstall> CustomInstalls { get; set; } = new List<GTAInstall>();
		public GTAInstall Selected { get; set; }

		public InstallList() : base(Path.Combine(Launcher.Instance.UserDataDirectory, "Installs.xml"), true) {}
	}
}
