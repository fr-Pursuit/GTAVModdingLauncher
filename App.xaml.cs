using System;
using System.IO;
using System.Windows;

namespace GTAVModdingLauncher
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs a)
		{
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			Exception e = (Exception)args.ExceptionObject;

			if(e is BadImageFormatException)
				MessageBox.Show("Unable to read " + (e as BadImageFormatException).FileName.Split(',')[0] + ".dll", "Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
			else if(e is FileNotFoundException && (e as FileNotFoundException).FileName.Contains(", PublicKeyToken"))
				MessageBox.Show("Unable to load " + (e as FileNotFoundException).FileName.Split(',')[0] + ".dll", "Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
			else
			{
				if(Launcher.Instance != null && Launcher.Instance.Window != null)
				{
					Launcher.Instance.Window.Dispatcher.Invoke(delegate
					{
						Launcher.Instance.Window.Visibility = Visibility.Hidden;
						new CrashReporter(e, true).ShowDialog();
					});
				}
				else new CrashReporter(e, false);
			}
		}
	}
}
