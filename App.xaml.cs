using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace GTAVModdingLauncher
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs a)
		{
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
		}

		private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			string assemblyName = args.Name.Split(',')[0] + ".dll";

			try
			{
				Stream stream = GetResourceStream(new Uri("/GTAVModdingLauncher;component/libs/" + assemblyName, UriKind.Relative)).Stream;

				if(stream != null)
				{
					byte[] bytes = new byte[stream.Length];
					stream.Read(bytes, 0, (int)stream.Length);
					return Assembly.Load(bytes);
				}
				else throw new IOException();
			}
			catch(IOException)
			{
				MessageBox.Show("Unable to load " + assemblyName, "Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
				Environment.Exit(1);
				return null;
			}
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			Exception e = (Exception)args.ExceptionObject;

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
