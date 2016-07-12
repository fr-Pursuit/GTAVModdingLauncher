using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using PursuitLib;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// This popup appears when mods that are in the wrong folder are moved to the GTA V's directory
	/// </summary>
	public partial class PopupMoveMods : Window
	{
		private string sourceDir;
		private bool canClose = false;
		private delegate void Callback();
		private delegate void IntCallback(int value);

		public PopupMoveMods(string source)
		{
			this.sourceDir = source;
			InitializeComponent();
		}

		public Thread StartThread()
		{
			Thread thread = new Thread(MoveMods);
			thread.Start();
			return thread;
		}

		private void MoveMods()
		{
			try
			{
				string[] dirs = Directory.GetDirectories(this.sourceDir, "*", SearchOption.AllDirectories);
				string[] files = Directory.GetFiles(this.sourceDir, "*", SearchOption.AllDirectories);

				this.SetBarMaximum(dirs.Length + files.Length);

				foreach(string dir in dirs)
				{
					string name = dir.Replace(this.sourceDir, Launcher.Instance.GtaPath);
					if(!Directory.Exists(name))
						Directory.CreateDirectory(name);
					this.IncrBarValue();
				}

				foreach(string file in files)
				{
					string name = file.Replace(this.sourceDir, Launcher.Instance.GtaPath);
					if(!File.Exists(name))
						File.Move(file, name);
					else File.Delete(file);
					this.IncrBarValue();
				}

				foreach(string dir in dirs)
				{
					if(Directory.Exists(dir))
						IOUtils.DeleteDirectory(dir);
				}

				this.ClosePopup();
			}
			catch(IOException)
			{
				this.Dispatcher.Invoke(new Callback(RetryAfterPermissionUpgrade));
			}
			catch(UnauthorizedAccessException)
			{
				this.Dispatcher.Invoke(new Callback(RetryAfterPermissionUpgrade));
			}
		}

		private void SetBarMaximum(int max)
		{
			if(!this.CheckAccess())
				this.Dispatcher.Invoke(new IntCallback(SetBarMaximum), max);
			else
			{
				this.Bar.Maximum = max;
				this.Bar.IsIndeterminate = false;
			}
		}

		private void IncrBarValue()
		{
			if(!this.CheckAccess())
				this.Dispatcher.Invoke(new Callback(IncrBarValue));
			else
				this.Bar.Value += 1;
		}

		/// <summary>
		/// Asks the user to allow the launcher to modify the game's folder, then retry
		/// </summary>
		private void RetryAfterPermissionUpgrade()
		{
			MessageBoxResult result = Messages.Show(this, "CantModifyGTAV", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

			if(result == MessageBoxResult.OK)
			{
				Launcher.Instance.CurrentThread = this.StartThread();
			}
			else this.ClosePopup();
		}

		private void ClosePopup()
		{
			if(!this.CheckAccess())
				this.Dispatcher.Invoke(new Callback(ClosePopup));
			else
			{
				this.canClose = true;
				this.Close();
			}
		}

		private void OnPopupClosing(object sender, CancelEventArgs e)
		{
			e.Cancel = !this.canClose;
		}
	}
}
