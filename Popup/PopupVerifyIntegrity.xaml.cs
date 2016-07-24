using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using PursuitLib;
using System.ComponentModel;
using PursuitLib.Wpf;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// This popup appears when the game's integrity needs to be verified, or when the user asks it
	/// </summary>
	public partial class PopupVerifyIntegrity : Window
	{
		private Thread thread;
		private bool integrityVerified = false;
		private bool canClose = false;
		private delegate void Callback();
		private delegate void IntCallback(int value);
		private delegate void StringCallback(string value);

		public PopupVerifyIntegrity()
		{
			InitializeComponent();
		}

		public Thread StartThread()
		{
			Thread thread = new Thread(VerifyIntegrity);
			this.thread = thread;
			thread.Start();
			return thread;
		}

		private void VerifyIntegrity()
		{
			Log.Info("Verifying game integrity...");

			List<string> queue = new List<string>();

			foreach(string filePath in Directory.GetFileSystemEntries(Launcher.Instance.GtaPath))
			{
				string file = Path.GetFileName(filePath);
				if(GameScanner.CanFileBeVerified(file))
					queue.Add(file);
			}

			if(queue.Count == GameScanner.CheckableFileCount)
			{
				this.SetBarMaximum(queue.Count);

				Log.Info("Verifying integrity of " + queue.Count + " files...");

				this.integrityVerified = true;

				foreach(string file in queue)
				{
					Log.Info("Verifying integrity of " + file + "...");
					this.SetState(file);

					if(GameScanner.CalculateFileHash(file) != GameScanner.GetVanillaFileHash(file))
					{
						this.integrityVerified = false;
						break;
					}

					this.IncrBarValue();
				}
			}
			else
			{
				Log.Info("Found "+queue.Count+" checkable files instead of "+GameScanner.CheckableFileCount+'.');
				this.integrityVerified = false;
			}

			this.Conclude();
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

		private void SetState(string file)
		{
			if(!this.CheckAccess())
				this.Dispatcher.Invoke(new StringCallback(SetState), file);
			else this.State.Content = I18n.Localize("PopupVerifyIntegrity.State", "Verifying", (object)file);
		}

		private void IncrBarValue()
		{
			if(!this.CheckAccess())
				this.Dispatcher.Invoke(new Callback(IncrBarValue));
			else this.Bar.Value += 1;
		}

		private void Conclude()
		{
			if(!this.CheckAccess())
				this.Dispatcher.Invoke(new Callback(Conclude));
			else
			{
				if(this.integrityVerified)
				{
					Log.Info("Game integrity verified successfully.");
					Launcher.Instance.Settings.IntegrityVerified = true;
					Launcher.Instance.SaveSettings();
					Messages.Show(this, "IntegrityVerified", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
					this.ClosePopup();
				}
				else
				{
					if(Messages.Show(this, "IntegrityFalse", "Warn", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
					{
						this.State.Content = I18n.Localize("Label", "VerifyingGame");
						this.Bar.Value = 0;
						this.Bar.IsIndeterminate = true;

						Launcher.Instance.CurrentThread = this.StartThread();
					}
					else
					{
						Launcher.Instance.Settings.IntegrityVerified = false;
						Launcher.Instance.SaveSettings();
						this.ClosePopup();
						Launcher.Instance.CloseLauncher();
					}
				}
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

			if(e.Cancel)
			{
				this.Abort(true, null);
				e.Cancel = !this.canClose;
			}
		}

		private void Abort(object sender, RoutedEventArgs e)
		{
			if(this.thread != null && this.thread.IsAlive)
			{
				if(Messages.Show(this, "SureAbortGameCheck", "Sure", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
				{
					Log.Info("The user chose to abort the game integrity check.");
					this.thread.Abort();
					Launcher.Instance.Settings.IntegrityVerified = true;
					Launcher.Instance.SaveSettings();

					if((sender as bool?) == true)
						this.canClose = true;
					else this.ClosePopup();
				}
			}
			else this.ClosePopup();
		}
	}
}
