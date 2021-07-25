using PursuitLib;
using PursuitLib.IO;
using PursuitLib.Windows.WPF.Dialogs;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PursuitLib.Threading.Tasks;

namespace GTAVModdingLauncher.Task
{
	public class DeleteMods : TaskElement
	{
		public override void Perform()
		{
			try
			{
				this.SendMessage(I18n.Localize("Label", "DeletingMods"));

				GameScanner.ListRootMods(out List<string> files, out List<string> dirs);
				List<string> dlc = GameScanner.ListDlcMods();

				foreach(string mod in files)
					IOUtil.Delete(mod);

				foreach(string dir in dirs)
				{
					if(Directory.Exists(dir))
						IOUtil.Delete(dir);
				}

				foreach(string mod in dlc)
					IOUtil.Delete(mod);
			}
			catch(IOException e)
			{
				Log.Error(e.ToString());
				LocalizedMessage.Show(Launcher.Instance.Window, "ProfileSwitchError", "FatalError", DialogIcon.Error, DialogButtons.Ok);
				Process.GetCurrentProcess().Kill();
			}
		}
	}
}
