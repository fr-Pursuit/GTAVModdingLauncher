using Microsoft.WindowsAPICodePack.Dialogs;
using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Windows.WPF.Modern;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GTAVModdingLauncher.Ui.Popup
{
	/// <summary>
	/// The "Edit profile" popup
	/// </summary>
	public partial class PopupEdit : ModernWindow
	{
		private ProfileEntry profile;

		public PopupEdit(Window parent, ProfileEntry profile)
		{
			this.profile = profile;

			InitializeComponent();
			this.ProfileName.Text = profile.ProfileName;

			this.SetParent(parent);
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
