using System.Windows;
using PursuitLib.IO;
using PursuitLib.Windows.WPF.Modern;

namespace GTAVModdingLauncher
{
	public partial class App : AppBase
	{
		protected override ModernApp CreateApp()
		{
			return new Launcher();
		}
	}
}
