using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// The "Create new profile" popup
	/// </summary>
	public partial class PopupCreate : Window
	{
		public PopupCreate()
		{
			InitializeComponent();
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
				else Messages.Show(this, "ProfileSameName", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			catch(Exception)
			{
				Messages.Show(this, "InvalidName", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
