using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Dialogs;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// The "Create new profile" popup
	/// </summary>
	public partial class PopupCreate : Window
	{
		public PopupCreate(Window parent)
		{
			this.InitializeComponent();
			this.SetParent(parent);
		}

		private void Save(object sender, EventArgs e)
		{
			try
			{
				if(this.ProfileName.Text == "" || this.ProfileName.Text.Contains("/") || this.ProfileName.Text.Contains("\\"))
					throw new ArgumentException();

				Launcher launcher = Launcher.Instance;

				if(!launcher.Profiles.Contains(this.ProfileName.Text))
				{
					string path = Path.Combine(Launcher.Instance.Settings.GetProfileFolder(), this.ProfileName.Text);

					if(!Directory.Exists(path))
						Directory.CreateDirectory(path);

					launcher.Profiles.Add(this.ProfileName.Text);
					launcher.UiManager.Profiles.Add(this.ProfileName.Text);
					launcher.UiManager.SelectedProfile = launcher.UiManager.Profiles.Count - 1;
					launcher.SaveProfiles();
					this.Close();
				}
				else LocalizedMessage.Show(this, "ProfileSameName", "Error", TaskDialogStandardIcon.Warning, TaskDialogStandardButtons.Ok);
			}
			catch(Exception)
			{
				LocalizedMessage.Show(this, "InvalidName", "Error", TaskDialogStandardIcon.Warning, TaskDialogStandardButtons.Ok);
			}
		}

		private void Cancel(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Return)
				this.Save(null, null);
		}
	}
}
