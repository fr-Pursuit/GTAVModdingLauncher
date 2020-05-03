using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using PursuitLib;
using PursuitLib.Windows.WPF;
using PursuitLib.Extensions;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using PursuitLib.Windows;
using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Windows.WPF.Modern;

namespace GTAVModdingLauncher.Ui.Popup
{
	/// <summary>
	/// The "Settings" popup
	/// </summary>
	public partial class PopupSettings : ModernWindow
	{
		private static Dictionary<string, string> supportedGtaLanguages = null;

		private delegate void Callback();
		private string OldLanguage;
		private Thread verifyUpdatesThread;

		public PopupSettings(Window parent)
		{
			this.OldLanguage = Launcher.Instance.Config.Language;
			InitializeComponent();
			this.SetParent(parent);

			for(int i = 0; i < I18n.SupportedLanguages.Count; i++)
			{
				string language = I18n.SupportedLanguages[i];
				this.Languages.Items.Add(CultureInfo.GetCultureInfo(language).NativeName);
				if(this.OldLanguage == language)
					this.Languages.SelectedIndex = i;
			}

			string currentGtaLanguage = Launcher.Instance.Config.GetGtaLanguage();
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

			this.KillLauncher.IsChecked = Launcher.Instance.Config.KillLauncher;
			this.UseRph.IsChecked = Launcher.Instance.Config.UseRph;
			this.Delete.IsChecked = Launcher.Instance.Config.DeleteLogs;
			this.Offline.IsChecked = Launcher.Instance.Config.OfflineMode;
			this.CheckUpdates.IsChecked = Launcher.Instance.Config.CheckUpdates;
			this.DisplayNews.IsChecked = Launcher.Instance.Config.DisplayNews;
			this.UseLogFile.IsChecked = Launcher.Instance.Config.UseLogFile;
			this.DarkMode.IsChecked = Launcher.Instance.Config.Theme == Theme.Dark;
			this.SelectedVersion.Text = Launcher.Instance.Config.SelectedInstall?.Path;
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

		private void Save(object sender, EventArgs e)
		{
			Launcher.Instance.Config.KillLauncher = this.KillLauncher.IsChecked.Value;
			Launcher.Instance.Config.UseRph = this.UseRph.IsChecked.Value;
			Launcher.Instance.Config.DeleteLogs = this.Delete.IsChecked.Value;
			Launcher.Instance.Config.OfflineMode = this.Offline.IsChecked.Value;
			Launcher.Instance.Config.CheckUpdates = this.CheckUpdates.IsChecked.Value;
			Launcher.Instance.Config.DisplayNews = this.DisplayNews.IsChecked.Value;
			Launcher.Instance.Config.UseLogFile = this.UseLogFile.IsChecked.Value;
            Launcher.Instance.Config.Language = I18n.SupportedLanguages[this.Languages.SelectedIndex];
			Launcher.Instance.Config.GtaLanguage = this.GetSupportedGtaLanguages()[(string)this.GtaLanguages.SelectedItem];

			Launcher.Instance.Config.Save();

			if(!Launcher.Instance.Config.DisplayNews)
				Launcher.Instance.UiManager.NewsVisible = false;
			if(!Launcher.Instance.Window.News.CycleStarted && Launcher.Instance.Config.DisplayNews)
				Launcher.Instance.Window.News.StartCycle();
			else if(Launcher.Instance.Window.News.CycleStarted && !Launcher.Instance.Config.DisplayNews)
				Launcher.Instance.Window.News.StopCycle();

			if(Log.HasLogFile && !Launcher.Instance.Config.UseLogFile)
			{
				Log.Info("The user chose to disable logging.");
				Log.LogFile = null;
			}
			else if(!Log.HasLogFile && Launcher.Instance.Config.UseLogFile)
			{
				Log.LogFile = Path.Combine(Launcher.Instance.UserDirectory, "latest.log");
				Log.Info("GTA V Modding Launcher " + Launcher.Instance.Version);
				Log.Info("Using PursuitLib " + typeof(Log).GetVersion());
				Log.Info("The user chose to enable logging.");
			}

			if(Launcher.Instance.Config.Language != this.OldLanguage)
				I18n.LoadLanguage(Launcher.Instance.Config.Language);

			this.Close();
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

		private void CheckForUpdates(object sender, RoutedEventArgs e)
		{
			if(this.verifyUpdatesThread == null || !this.verifyUpdatesThread.IsAlive)
			{
				this.verifyUpdatesThread = new Thread(VerifyUpdates);
				this.verifyUpdatesThread.Start();
			}
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
			LocalizedMessage.Show(this, "UpToDate", "Info", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Ok);
		}

		private void ManageInstalls(object sender, RoutedEventArgs e)
		{
			new PopupChooseInstall(this).ShowDialog();
			this.SelectedVersion.Text = Launcher.Instance.Config.SelectedInstall?.Path;
		}

		private void UpdateTheme(object sender, EventArgs e)
		{
			Launcher.Instance.Config.Theme = this.DarkMode.IsChecked.Value ? Theme.Dark : Theme.Light;
			Launcher.Instance.Theme = Launcher.Instance.Config.Theme;
		}
	}
}
