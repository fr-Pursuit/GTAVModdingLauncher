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
	/// The "Edit profile" popup
	/// </summary>
	public partial class PopupEdit : Window
	{
		private int profileIndex;
		private string oldName;

		public PopupEdit(Window parent, int index, string oldName)
		{
			this.profileIndex = index;
			this.oldName = oldName;

			InitializeComponent();
			this.ProfileName.Text = oldName;

			this.SetParent(parent);
		}

		private void Save(object sender, EventArgs e)
		{
			if(this.ProfileName.Text.ToLower() == this.oldName.ToLower())
				return;

			try
			{
				if(this.ProfileName.Text == "" || this.ProfileName.Text.Contains("/") || this.ProfileName.Text.Contains("\\"))
					throw new ArgumentException();

				Launcher launcher = Launcher.Instance;

				if(!launcher.Profiles.ProfileExists(this.ProfileName.Text))
				{
					if(Directory.Exists(Path.Combine(Launcher.Instance.Settings.GetProfileFolder(), this.oldName)))
						Directory.Move(Path.Combine(Launcher.Instance.Settings.GetProfileFolder(), this.oldName), Path.Combine(Launcher.Instance.Settings.GetProfileFolder(), this.ProfileName.Text));
					else Directory.CreateDirectory(Path.Combine(Launcher.Instance.Settings.GetProfileFolder(), this.ProfileName.Text));

					launcher.Profiles[this.profileIndex] = this.ProfileName.Text;
					launcher.UiManager.Profiles[this.profileIndex] = this.ProfileName.Text;
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
