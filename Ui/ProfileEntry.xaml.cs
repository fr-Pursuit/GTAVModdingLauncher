using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GTAVModdingLauncher.Ui.Popup;
using GTAVModdingLauncher.Work;
using Microsoft.WindowsAPICodePack.Dialogs;
using PursuitLib;
using PursuitLib.Extensions;
using PursuitLib.IO;
using PursuitLib.Windows.WPF.Dialogs;

namespace GTAVModdingLauncher.Ui
{
	public partial class ProfileEntry : UserControl, IDisposable
	{
		public string ProfileName
		{
			get => this.profile.Name;
			set
			{
				if(!this.profile.IsVanilla)
				{
					if(Directory.Exists(Path.Combine(Launcher.Instance.UserDirectory, "Profiles", this.profile.Name)))
						Directory.Move(Path.Combine(Launcher.Instance.UserDirectory, "Profiles", this.profile.Name), Path.Combine(Launcher.Instance.UserDirectory, "Profiles", value));
					else Directory.CreateDirectory(Path.Combine(Launcher.Instance.UserDirectory, "Profiles", value));
				}

				if(Launcher.Instance.Config.Profile == this.profile)
					Launcher.Instance.Config.CurrentProfile = value;

				this.profile.Name = value;

				if(this.CheckAccess())
					this.DisplayName.Content = value;
				else this.Dispatcher.Invoke(() => this.DisplayName.Content = value);
			}
		}

		private readonly Profile profile;
		private readonly MenuItem play;
		private readonly MenuItem setActive;
		private readonly MenuItem edit;
		private readonly MenuItem openFolder;
		private readonly MenuItem delete;

		public ProfileEntry(Profile profile)
		{
			this.profile = profile;
			this.InitializeComponent();
			this.DisplayName.Content = profile.Name;
			this.MoveButton.ControlMoved += OnMoved;

			this.ContextMenu = new ContextMenu();
			
			this.play = new MenuItem();
			this.play.Click += LaunchProfile;
			this.play.FontWeight = FontWeights.Bold;
			this.ContextMenu.Items.Add(this.play);

			this.setActive = new MenuItem();
			this.setActive.Click += SetActive;
			this.ContextMenu.Items.Add(this.setActive);

			this.edit = new MenuItem();
			this.edit.Click += Edit;
			this.ContextMenu.Items.Add(this.edit);

			this.openFolder = new MenuItem();
			this.openFolder.Click += OpenFolder;
			this.ContextMenu.Items.Add(this.openFolder);

			this.delete = new MenuItem();
			this.delete.IsEnabled = !profile.IsVanilla;
			this.delete.Click += DeleteProfile;
			this.ContextMenu.Items.Add(this.delete);

			I18n.Reload += ReloadLanguage;
			this.ReloadLanguage(I18n.CurrentLanguage);
		}

		public void Update()
		{
			if(Launcher.Instance.Config.Profile == this.profile)
			{
				this.Icon.SetResourceReference(ForegroundProperty, "HighlightBrush");
				this.openFolder.IsEnabled = true;
			}
			else
			{
				this.Icon.SetResourceReference(ForegroundProperty, "BlackBrush");
				this.openFolder.IsEnabled = !this.profile.IsVanilla;
			}
		}

		private void OnMoved(Control control, int oldPos, int newPos)
		{
			Launcher.Instance.Config.Profiles.Swap(oldPos, newPos);
			Launcher.Instance.Config.Dirty = true;
		}

		private void LaunchProfile(object sender, RoutedEventArgs e)
		{
			Log.Info("Starting  game launch process...");

			Launcher.Instance.UiManager.UIEnabled = false;

			Launcher.Instance.WorkManager.StartWork(() =>
			{
				if(Launcher.Instance.Config.Profile != this.profile)
					Launcher.Instance.SwitchProfileTo(this.profile);
				Launcher.Instance.LaunchGame(false);
			});
		}

		private void SetActive(object sender, RoutedEventArgs e)
		{
			Launcher.Instance.WorkManager.StartWork(() =>
			{
				Launcher.Instance.UiManager.UIEnabled = false;
				Launcher.Instance.SwitchProfileTo(this.profile);
				Launcher.Instance.UiManager.UIEnabled = true;
			});
		}

		private void Edit(object sender, EventArgs e)
		{
			PopupEdit popup = new PopupEdit(Launcher.Instance.Window, this);
			popup.ShowDialog();
		}

		private void OpenFolder(object sender, RoutedEventArgs e)
		{
			if(Launcher.Instance.Config.Profile == this.profile)
				Process.Start(Launcher.Instance.Config.SelectedInstall.Path);
			else if(!this.profile.IsVanilla)
			{
				string path = Path.Combine(Launcher.Instance.UserDirectory, "Profiles", this.profile.Name);

				if(!Directory.Exists(path))
					Directory.CreateDirectory(path);

				Process.Start(path);
			}
		}

		private void DeleteProfile(object sender, EventArgs e)
		{
			if(!this.profile.IsVanilla)
			{
				TaskDialogResult result = LocalizedMessage.Show(Launcher.Instance.Window, "SureDelete", "Sure", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.Cancel);

				if(result == TaskDialogResult.Yes)
				{
					if(Directory.Exists(Path.Combine(Launcher.Instance.UserDirectory, "Profiles", this.profile.Name)))
						IOUtil.Delete(Path.Combine(Launcher.Instance.UserDirectory, "Profiles", this.profile.Name));

					Launcher.Instance.Config.Profiles.Remove(this.profile);
					Launcher.Instance.UiManager.RemoveProfile(this);

					if(Launcher.Instance.Config.CurrentProfile.Equals(this.profile.Name, StringComparison.OrdinalIgnoreCase))
					{
						new PerformJobDialog(Launcher.Instance.WorkManager, new DeleteMods()).Show(Launcher.Instance.WorkManager);
						Launcher.Instance.Config.Profile = Launcher.Instance.Config.VanillaProfile;
						Launcher.Instance.UiManager.UpdateActiveProfile();
					}

					Launcher.Instance.Config.Save();
				}
			}
		}

		private void ReloadLanguage(string lang)
		{
			this.play.Header = I18n.Localize("ContextMenu", "Play");
			this.setActive.Header = I18n.Localize("ContextMenu", "SetActive");
			this.edit.Header = I18n.Localize("ContextMenu", "EditProfile");
			this.openFolder.Header = I18n.Localize("ContextMenu", "OpenProfileFolder");
			this.delete.Header = I18n.Localize("ContextMenu", "DeleteProfile");
		}

		public void Dispose()
		{
			I18n.Reload -= ReloadLanguage;
		}
	}
}
