using PursuitLib;
using PursuitLib.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GTAVModdingLauncher
{
	public partial class CrashReporter : Window
	{
		private const string ReportSite = "www.gta5-mods.com";
		private const string ReportURL = "https://www.gta5-mods.com/tools/gta-v-modding-launcher";

		private readonly Exception exception;

		public CrashReporter(Exception exception, bool createInterface)
		{
			Log.Error("Caught unhandled exception: " + exception);
			this.exception = exception;

			if(createInterface)
			{
				this.InitializeComponent();

				if(!I18n.IsLoaded())
				{
					Log.Warn("Unable to load localized version of the report.");
					this.Title = "Crash report";
					this.Message.Content = "The program has encountered an unexpected error and cannot continue.\nPlease report the following error at {0}. Sorry for any inconvenience caused.";
				}
				this.Message.Content = ((string)this.Message.Content).Replace("{0}", ReportSite);

				Log.Info("Created crash report interface successfully.");
			}
			else
			{
				MessageBox.Show("The program has encountered an unexpected error and cannot continue.\nPlease report the following error at "+ReportSite+". Sorry for any inconvenience caused.", "Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
				Log.Warn("Unable to create crash report interface.");
			}

			string message = "GTA V Modding Launcher " + Launcher.Version + " crash report.\n";
			message += "Generated on " + String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + "\n\n\n";
			message += this.exception;

			this.Report.Text = message;

			string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pursuit\\GTA V Modding Launcher");
			string filePath = Path.Combine(dir, "crash-" + String.Format("{0:dd-MM-yyyy-HH.mm.ss}", DateTime.Now) + ".txt");

			Log.Info("Creating " + filePath + "...");

			if(!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			using(StreamWriter writer = new StreamWriter(filePath))
			{
				writer.Write(message);
			}
		}

		private void Close(object sender, EventArgs e)
		{
			this.Close();
		}

		private void ReportCrash(object sender, EventArgs e)
		{
			Process.Start(ReportURL);
		}
	}
}
