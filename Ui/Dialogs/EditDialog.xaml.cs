using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Windows.WPF.Modern.Dialogs.Base;
using System;
using System.Windows;
using System.Windows.Input;

namespace GTAVModdingLauncher.Ui.Dialogs
{
	/// <summary>
	/// The "Edit profile" dialog
	/// </summary>
	public partial class EditDialog : ModernDialogBase
	{
		private ProfileEntry profile;

		public EditDialog(Window parent, ProfileEntry profile)
		{
			this.Parent = (WPFWindow)parent;
			this.profile = profile;

			InitializeComponent();
			this.ProfileName.Text = profile.ProfileName;
		}

		private void Save(object sender, EventArgs e)
		{
			if(this.ProfileName.Text.Equals(this.profile.ProfileName, StringComparison.OrdinalIgnoreCase))
				return;

			try
			{
				if(this.ProfileName.Text == "" || this.ProfileName.Text.Contains("/") || this.ProfileName.Text.Contains("\\"))
					throw new ArgumentException();

				Launcher launcher = Launcher.Instance;

				if(!launcher.Config.ProfileExists(this.ProfileName.Text))
				{
					this.profile.ProfileName = this.ProfileName.Text;
					launcher.Config.Save();
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
