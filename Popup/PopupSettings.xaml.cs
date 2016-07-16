using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using PursuitLib;
using PursuitLib.Wpf;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// The "Settings" popup
	/// </summary>
	public partial class PopupSettings : Window
	{
		private static Dictionary<string, string> supportedGtaLanguages = null;

		private delegate void Callback();
		private string OldLanguage;
		private string OldFolder;

		public PopupSettings()
		{
			this.OldLanguage = Launcher.Instance.Settings.Language;
			this.OldFolder = Launcher.Instance.Settings.GetProfileFolder();
			InitializeComponent();

			for(int i = 0; i < I18n.SupportedLanguages.Count; i++)
			{
				string language = I18n.SupportedLanguages[i];
				this.Languages.Items.Add(CultureInfo.GetCultureInfo(language).NativeName);
				if(this.OldLanguage == language)
					this.Languages.SelectedIndex = i;
			}

			string currentGtaLanguage = Launcher.Instance.Settings.GetGtaLanguage();
			int index = 0;
			foreach(string language in this.GetSupportedGtaLanguages().Keys)
				this.GtaLanguages.Items.Add(language);
			foreach(string language in supportedGtaLanguages.Values)
			{
				if(language == currentGtaLanguage)
				{
					this.GtaLanguages.SelectedIndex = index;
					break;
				}
				index++;
			}
			
			this.UseRph.IsChecked = Launcher.Instance.Settings.UseRph;
			this.Delete.IsChecked = Launcher.Instance.Settings.DeleteLogs;
			this.Offline.IsChecked = Launcher.Instance.Settings.OfflineMode;
			this.CheckUpdates.IsChecked = Launcher.Instance.Settings.CheckUpdates;
			this.UseLogFile.IsChecked = Launcher.Instance.Settings.UseLogFile;
			this.ProfileFolder.Text = Launcher.Instance.Settings.GetProfileFolder();
			this.UseFolder.IsChecked = Launcher.Instance.Settings.CustomFolder != null;
			this.UseFolderCheckedChange(null, null);
		}

		private Dictionary<string,string> GetSupportedGtaLanguages()
		{
			if(supportedGtaLanguages != null)
				return supportedGtaLanguages;
			else
			{
				supportedGtaLanguages = new Dictionary<string, string>();
				supportedGtaLanguages.Add("English", "american");
				supportedGtaLanguages.Add("French", "french");
				supportedGtaLanguages.Add("Italian", "italian");
				supportedGtaLanguages.Add("German", "german");
				supportedGtaLanguages.Add("Spanish", "spanish");
				supportedGtaLanguages.Add("Japanese", "japanese");
				supportedGtaLanguages.Add("Russian", "russian");
				supportedGtaLanguages.Add("Polish", "polish");
				supportedGtaLanguages.Add("Portuguese", "portuguese");
				supportedGtaLanguages.Add("Traditional Chinese", "chinese");
				supportedGtaLanguages.Add("Latin American Spanish", "mexican");
				supportedGtaLanguages.Add("Korean", "korean");
				return supportedGtaLanguages;
			}
		}

		private void UseFolderCheckedChange(object sender, EventArgs e)
		{
			this.ProfileFolder.IsEnabled = (bool)this.UseFolder.IsChecked;
			this.Browse.IsEnabled = this.ProfileFolder.IsEnabled;
		}

		private void BrowseFolder(object sender, EventArgs e)
		{
			System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();

			if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				this.ProfileFolder.Text = dialog.SelectedPath;
		}

		private void Save(object sender, EventArgs e)
		{
			if(this.ProfileFolder.Text.Contains(Launcher.Instance.GtaPath))
			{
				Messages.Show(Launcher.Instance.Window, "CantMoveProfiles", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Launcher.Instance.Settings.UseRph = (bool)this.UseRph.IsChecked;
			Launcher.Instance.Settings.DeleteLogs = (bool)this.Delete.IsChecked;
			Launcher.Instance.Settings.OfflineMode = (bool)this.Offline.IsChecked;
			Launcher.Instance.Settings.CheckUpdates = (bool)this.CheckUpdates.IsChecked;
			Launcher.Instance.Settings.UseLogFile = (bool)this.UseLogFile.IsChecked;
			Launcher.Instance.Settings.CustomFolder = (bool)this.UseFolder.IsChecked && this.ProfileFolder.Text != Launcher.Instance.UserDirPath ? this.ProfileFolder.Text : null;
			Launcher.Instance.Settings.Language = I18n.SupportedLanguages[this.Languages.SelectedIndex];
			Launcher.Instance.Settings.GtaLanguage = this.GetSupportedGtaLanguages()[(string)this.GtaLanguages.SelectedItem];

			Launcher.Instance.SaveSettings();

			if(Log.HasLogFile() && !Launcher.Instance.Settings.UseLogFile)
			{
				Log.Info("The user chose to disable logging.");
				Log.RemoveLogFile();
			}
			else if(!Log.HasLogFile() && Launcher.Instance.Settings.UseLogFile)
			{
				Log.SetLogFile(Path.Combine(Launcher.Instance.UserDirPath, "latest.log"));
				Log.Info("GTA V Modding Launcher " + Launcher.Version);
				Log.Info("Using PursuitLib " + Versions.GetTypeVersion(typeof(Log)));
				Log.Info("The user chose to enable logging.");
			}

			if(Launcher.Instance.Settings.GetProfileFolder() != this.OldFolder)
			{
				Log.Info("Moving profiles from '" + this.OldFolder + "' to '" + Launcher.Instance.Settings.GetProfileFolder() + "'");

				if(!Directory.Exists(Launcher.Instance.Settings.GetProfileFolder()))
					Directory.CreateDirectory(Launcher.Instance.Settings.GetProfileFolder());

				PopupMoveProfiles popup = new PopupMoveProfiles(this.OldFolder);
				Launcher.Instance.CurrentThread = popup.StartThread();
				popup.ShowDialog();
			}

			if(Launcher.Instance.Settings.Language != this.OldLanguage)
				I18n.LoadLanguage(Launcher.Instance.Settings.Language);

			this.Close();
		}

		private void Cancel(object sender, EventArgs e)
		{
			this.Close();
		}

		private void Verify(object sender, RoutedEventArgs e)
		{
			PopupVerifyIntegrity popup = new PopupVerifyIntegrity();
			Launcher.Instance.CurrentThread = popup.StartThread();
			popup.ShowDialog();
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Return)
				this.Save(null, null);
		}

		private void CheckForUpdates(object sender, RoutedEventArgs e)
		{
			Launcher.Instance.CurrentThread = new Thread(VerifyUpdates);
			Launcher.Instance.CurrentThread.Start();
		}

		private void VerifyUpdates()
		{
			JObject obj = Launcher.Instance.IsUpToDate();

			if(obj != null)
				Launcher.Instance.ShowUpdatePopup(this, obj);
			else this.Dispatcher.Invoke(new Callback(ShowUpToDatePopup));
		}

		private void ShowUpToDatePopup()
		{
			Messages.Show(this, "UpToDate", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}
