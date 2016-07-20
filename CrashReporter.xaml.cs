using PursuitLib;
using PursuitLib.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace GTAVModdingLauncher
{
	public partial class CrashReporter : Window
	{
		private const string ReportSite = "github.com";
		private const string ReportURL = "https://github.com/fr-Pursuit/GTAVModdingLauncher/issues/new";

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

			string message = this.FillCrashReport();

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

		private string FillCrashReport()
		{
			StringBuilder report = new StringBuilder();
			report.Append("---- GTA V Modding Launcher " + Launcher.Version + " crash report ----\n");
			report.Append("Generated on " + String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + "\n\n\n");

			report.Append("-- Thrown exception --\n");
			report.Append(this.exception.ToString() + "\n\n");

			report.Append("-- Launcher state --\n");
			try
			{
				report.Append("Is initialized: " + (Launcher.Instance != null) + '\n');
				report.Append("Is window initialized: " + (Launcher.Instance.Window != null) + '\n');
				if(Launcher.Instance.Profiles != null)
				{
					report.Append("Current profile index: " + Launcher.Instance.Profiles.CurrentProfile + '\n');
					report.Append("Current profile: ");
					try
					{
						report.Append(Launcher.Instance.Profiles[Launcher.Instance.Profiles.CurrentProfile]);
					}
					catch(Exception)
					{
						report.Append("~Unexpected error~");
					}
					report.Append('\n');
				}
			}
			catch(Exception)
			{
				report.Append("~Unexpected error~\n");
			}

			report.Append('\n');

			report.Append("-- UI state --\n");
			try
			{
				if(Launcher.Instance != null && Launcher.Instance.UiManager != null)
				{
					report.Append("Selected profile index: " + Launcher.Instance.UiManager.SelectedProfile + '\n');
					report.Append("Selected profile: ");
					try
					{
						report.Append(Launcher.Instance.Profiles[Launcher.Instance.UiManager.SelectedProfile]);
					}
					catch(Exception)
					{
						report.Append("~Unexpected error~");
					}
					report.Append('\n');
					report.Append("Is working: " + Launcher.Instance.UiManager.Working + '\n');
					report.Append("Are buttons enabled: " + Launcher.Instance.UiManager.ButtonsEnabled + '\n');
				}
				else report.Append("Unable to get UI state");
			}
			catch(Exception)
			{
				report.Append("~Unexpected error~\n");
			}


			return report.ToString();
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
