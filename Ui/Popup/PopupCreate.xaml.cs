using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Windows.WPF.Modern;

namespace GTAVModdingLauncher.Ui.Popup
{
	/// <summary>
	/// The "Create new profile" popup
	/// </summary>
	public partial class PopupCreate : ModernWindow
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

				if(!launcher.Config.ProfileExists(this.ProfileName.Text))
				{
					string path = Path.Combine(Launcher.Instance.UserDirectory, "Profiles", this.ProfileName.Text);

					if(!Directory.Exists(path))
						Directory.CreateDirectory(path);

					Profile profile = new Profile(this.ProfileName.Text);
					launcher.Config.Profiles.Add(profile);
					launcher.Config.Save();
					Launcher.Instance.UiManager.AddProfile(profile);
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
