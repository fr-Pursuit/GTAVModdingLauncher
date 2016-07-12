using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using PursuitLib;
using System.ComponentModel;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// This popup appears when the launcher needs to make the game vanilla
	/// </summary>
	public partial class PopupDeletingMods : Window
	{
		public bool CreatedModdedState { get; internal set; } = false;
		private bool canClose = false;
		private delegate void Callback();
		private delegate void IntCallback(int value);

		public PopupDeletingMods()
		{
			InitializeComponent();
		}

		public Thread StartThread()
		{
			Thread thread = new Thread(DeleteMods);
			thread.Start();
			return thread;
		}

		private void DeleteMods()
		{
			try
			{
				List<string> files;
				List<string> dirs;
				GameScanner.ListRootMods(out files, out dirs);
				List<string> dlc = GameScanner.ListDlcMods();

				this.SetBarMaximum(files.Count + dirs.Count + dlc.Count);

				foreach(string mod in files)
				{
					IOUtils.Delete(mod);
					this.IncrBarValue();
				}

				foreach(string dir in dirs)
				{
					if(Directory.Exists(dir))
						IOUtils.Delete(dir);
					this.IncrBarValue();
				}

				foreach(string mod in dlc)
				{
					IOUtils.Delete(mod);
					this.IncrBarValue();
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
			else this.Bar.Value += 1;
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
			else
			{
				string name = Launcher.Instance.GetNewProfileName();
				Launcher.Instance.Profiles.Add(name);
				Launcher.Instance.UiManager.Profiles.Add(name);
				Launcher.Instance.Profiles.CurrentProfile = Launcher.Instance.Profiles.Count - 1;
				Launcher.Instance.UiManager.SelectedProfile = Launcher.Instance.Profiles.Count - 1;
				this.CreatedModdedState = true;

				this.ClosePopup();
			}
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
