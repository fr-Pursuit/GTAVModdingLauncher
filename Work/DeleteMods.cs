using Microsoft.WindowsAPICodePack.Dialogs;
using PursuitLib;
using PursuitLib.IO;
using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Work;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GTAVModdingLauncher.Work
{
	public class DeleteMods : Job
	{
		public override bool Queueable => false;
		public override long ProgressMaximum => 0;

		public override void Perform(WorkManager manager)
		{
			try
			{
				if(manager.ProgressDisplay is IJobDisplay)
					((IJobDisplay) manager.ProgressDisplay).Description = I18n.Localize("Label", "DeletingMods");

				List<string> files;
				List<string> dirs;
				GameScanner.ListRootMods(out files, out dirs);
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
				LocalizedMessage.Show(Launcher.Instance.Window, "ProfileSwitchError", "FatalError", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
				Process.GetCurrentProcess().Kill();
			}
		}
	}
}
