using System;
using System.IO;
using System.Threading;
using System.Windows;
using PursuitLib;
using System.ComponentModel;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// This popup appears when the user changes the profiles' folder
	/// </summary>
	public partial class PopupMoveProfiles : Window
	{
		private string oldDir;
		private bool canClose = false;
		private delegate void Callback();
		private delegate void IntCallback(int value);

		public PopupMoveProfiles(string oldDir)
		{
			this.oldDir = oldDir;
			InitializeComponent();
		}

		public Thread StartThread()
		{
			Thread thread = new Thread(MoveProfiles);
			thread.Start();
			return thread;
		}

		private void MoveProfiles()
		{
			try
			{
				int max = 0;
				int index = 0;
				string[][] dirs = new string[Launcher.Instance.Profiles.Count - 1][];
				string[][] files = new string[Launcher.Instance.Profiles.Count - 1][];

				foreach(string profile in Launcher.Instance.Profiles)
				{
					if(profile == "Vanilla")
						continue;

					string sourceDir = Path.Combine(this.oldDir, profile);
					string newDir = Path.Combine(Launcher.Instance.Settings.GetProfileFolder(), profile);

					if(Directory.Exists(sourceDir))
					{
						dirs[index] = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);
						max += dirs[index].Length;
						files[index] = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
						max += files[index].Length;
					}

					index++;
				}

				this.SetBarMaximum(max);

				index = 0;

				foreach(string profile in Launcher.Instance.Profiles)
				{
					if(profile == "Vanilla")
						continue;

					string sourceDir = Path.Combine(this.oldDir, profile);
					string newDir = Path.Combine(Launcher.Instance.Settings.GetProfileFolder(), profile);

					Directory.CreateDirectory(newDir);

					if(Directory.Exists(sourceDir))
					{
						foreach(string dir in dirs[index])
						{
							string name = dir.Replace(sourceDir, newDir);
							if(!Directory.Exists(name))
								Directory.CreateDirectory(name);
							this.IncrBarValue();
						}

						foreach(string file in files[index])
						{
							string name = file.Replace(sourceDir, newDir);
							if(!File.Exists(name))
								File.Move(file, name);
							this.IncrBarValue();
						}

						foreach(string dir in dirs[index])
						{
							if(Directory.Exists(dir))
								IOUtils.DeleteDirectory(dir);
						}
					}

					IOUtils.DeleteDirectory(sourceDir);

					index++;
				}

				this.ClosePopup();
			}
			catch(Exception e)
			{
				Log.Info("Error while moving profiles:");
				Log.Info(e.ToString());
				Launcher.Instance.Settings.CustomFolder = this.oldDir == Launcher.Instance.UserDirPath ? null : this.oldDir;
				Launcher.Instance.SaveSettings();
				Messages.CantMoveProfiles();
				this.ClosePopup();
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
