using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Windows.WPF.Modern.Dialogs.Base;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using PursuitLib.Windows.WPF;

namespace GTAVModdingLauncher.Ui.Dialogs
{
	/// <summary>
	/// The "Create new profile" dialog
	/// </summary>
	public partial class CreateDialog : ModernDialogBase
	{
		public CreateDialog(Window parent)
		{
			this.Parent = (WPFWindow)parent;
			this.InitializeComponent();

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
				else LocalizedMessage.Show(Launcher.Instance.Window, "ProfileSameName", "Error", DialogIcon.Warning, DialogButtons.Ok);
			}
			catch(Exception)
			{
				LocalizedMessage.Show(Launcher.Instance.Window, "InvalidName", "Error", DialogIcon.Warning, DialogButtons.Ok);
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
