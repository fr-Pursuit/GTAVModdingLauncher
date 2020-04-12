using GTAVModdingLauncher.Legacy;
using GTAVModdingLauncher.Popup;
using GTAVModdingLauncher.Work;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PursuitLib;
using PursuitLib.Extensions;
using PursuitLib.IO;
using PursuitLib.Windows;
using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Work;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using PursuitLib.IO.PPF;
using PursuitLib.Windows.WPF.Modern;

namespace GTAVModdingLauncher
{
	/// <summary>
	/// Main launcher class
	/// </summary>
	public class Launcher : ModernApp
	{
		public static Launcher Instance { get; private set; }

		public MainWindow Window { get; private set; } = null;
		public InstallList Installs { get; private set; }
		public ProfileList Profiles { get; private set; }
		public UserSettings Settings { get; private set; }
		public UIManager UiManager { get; private set; }
		private readonly BinaryFormatter formatter;

		/// <summary>
		/// Indicates if the user chose to close the application. <see cref="CloseLauncher"/> <seealso cref="OnWindowClosing"/>
		/// </summary>
		private bool closeRequested = false;
		
		public Launcher()
		{
			Instance = this;

			ResourceManager.RegisterProvider("appRsrc", new PPFFile("resources.ppf"));

			//Legacy support
			this.formatter = new BinaryFormatter();
			this.formatter.Binder = new LegacyBinder();

			I18n.Initialize();
			this.Installs = new InstallList();
			this.LoadSettings();

			if(this.Settings.UseLogFile)
				Log.LogFile = Path.Combine(this.UserDirectory, "latest.log");
			Log.Info(this.DisplayName);
			Log.Info("Using PursuitLib " + typeof(Log).GetVersion());

			Log.Info("Loading languages...");
			I18n.LoadLanguage(this.Settings.Language);
		}

		/// <summary>
		/// Links MainWindow with this class using events
		/// </summary>
		public void InitUI(MainWindow window)
		{
			Log.Info("Initializing user interface...");
			this.Window = window;
			this.Window.Closing += OnWindowClosing;
			this.Window.ProfileList.SelectionChanged += OnSelectionChange;
			this.Window.AboutButton.Click += ShowAboutPopup;
			this.Window.SettingsButton.Click += OpenSettingsPopup;
			this.Window.CreateButton.Click += CreateNewProfile;
			this.Window.EditButton.Click += EditSelectedProfile;
			this.Window.ApplyButton.Click += SwitchToSelectedProfile;
			this.Window.DeleteButton.Click += DeleteSelectedProfile;
			this.Window.PlayButton.Click += PressPlay;
			this.Window.PlayOnlineButton.Click += PressPlay;
			this.UiManager = new UIManager(this.Window);
			this.UiManager.WindowTitle = this.DisplayName;
			this.WorkManager.ProgressDisplay = new MultiProgressDisplay((WPFProgressBar)this.Window.Progress, new TaskBarProgress());

			this.UiManager.randomizeBackground();

			this.InitLauncher();

			this.UiManager.Profiles.RemoveAt(0);
			foreach(string profile in this.Profiles)
				this.UiManager.Profiles.Add(profile);
			this.UiManager.SelectedProfile = this.Profiles.CurrentProfile;

			this.UiManager.ButtonsEnabled = true;
			this.UiManager.CanPlayOnline = this.UiManager.SelectedProfile == 0;
			this.UiManager.LauncherVersion = "Launcher " + this.Version;

			I18n.Reload += OnI18nReload;
			this.OnI18nReload(null);

			if(this.Profiles.CurrentProfile >= this.Profiles.Count)
			{
				string name = this.GetNewProfileName();
				string path = Path.Combine(this.Settings.GetProfileFolder(), name);

				if(!Directory.Exists(path))
					Directory.CreateDirectory(path);

				this.Profiles.Add(name);
				this.Profiles.CurrentProfile = this.Profiles.Count - 1;
				this.UiManager.Profiles.Add(name);
				this.UiManager.SelectedProfile = this.Profiles.CurrentProfile;
				this.SaveProfiles();

				MessageDialog.Show(this.Window, I18n.Localize("Dialog", "InvalidActiveProfile", new object[] { name }), I18n.Localize("Dialog.Caption", "Warn"), TaskDialogStandardIcon.Warning, TaskDialogStandardButtons.Ok);
			}

			if(this.Settings.CheckUpdates)
			{
				this.WorkManager.StartWork(CheckUpdates);
			}
		}

		private void InitLauncher()
		{
			Log.Info("Initializing launcher...");

			SteamHelper.Initialize();

			if(this.Installs.Selected == null)
			{
				GTAInstall[] installs = GTAInstall.FindInstalls();

				if(installs.Length > 0)
					this.Installs.Selected = installs[0];
				else
				{
					LocalizedMessage.Show("GTANotFound", "Info", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Ok);
					new PopupChooseInstall().ShowDialog();

					if(this.Installs.Selected == null)
					{
						Process.GetCurrentProcess().Kill();
						return;
					}
				}

				this.Installs.Save();
			}

			Log.Info("Using GTA V installation at " + this.Installs.Selected.Path);
			Log.Info("Installation type: " + this.Installs.Selected.Type);

			if(Path.GetFullPath(this.WorkingDirectory).Equals(Path.GetFullPath(this.Installs.Selected.Path)))
			{
				LocalizedMessage.Show("InstalledInGTA", "Error", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
				Process.GetCurrentProcess().Kill();
			}

			GameScanner.Init();

			if(File.Exists(Path.Combine(this.UserDirectory, "profiles.dat")))
			{
				try
				{
					using(Stream stream = File.Open(Path.Combine(this.UserDirectory, "profiles.dat"), FileMode.Open))
					{
						this.Profiles = (ProfileList) formatter.Deserialize(stream);
					}
				}
				catch(Exception ex)
				{
					Log.Error("Unable to read saved profiles.");
					Log.Error(ex.ToString());
					LocalizedMessage.Show("ReadProfilesError", "Error", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
					this.CreateDefaultProfiles();
				}
			}
			else
			{
				Log.Info("No profiles.dat file found.");
				this.CreateDefaultProfiles();
			}
		}

		private void CheckUpdates()
		{
			JObject obj = this.IsUpToDate();
			if(obj != null)
				this.Window.Dispatcher.Invoke(() => ShowUpdatePopup(this.Window, obj));
		}


		/// <summary>
		/// Checks whether the software is up to date or not
		/// </summary>
		/// <returns>A JObject representing the latest update, or null if it's up to date</returns>
		public JObject IsUpToDate()
		{
			try
			{
				HttpWebRequest request = WebRequest.CreateHttp("https://api.github.com/repos/fr-Pursuit/GTAVModdingLauncher/releases/latest");
				request.UserAgent = "GTAVModdingLauncher-" + Version;

				using(HttpWebResponse response = request.GetResponse() as HttpWebResponse)
				{
					if(response.StatusCode == HttpStatusCode.OK)
					{
						using(StreamReader streamReader = new StreamReader(response.GetResponseStream()))
						using(JsonReader reader = new JsonTextReader(streamReader))
						{
							JObject obj = JObject.Load(reader);
							if(new Version(obj["tag_name"].ToString()) > Version)
							{
								Log.Info("New update found (" + obj["name"] + ')');
								return obj;
							}
						}
					}
					else Log.Warn("Unable to check for updates. Response code was " + response.StatusCode + " (" + response.StatusDescription + ')');
				}
			}
			catch(Exception e)
			{
				Log.Warn("Unable to check for updates.");
				Log.Warn(e.ToString());
			}

			return null;
		}

		/// <summary>
		/// Shows a popup telling the user a new update is out.
		/// </summary>
		/// <param name="obj">A JObject representing the update</param>
		public void ShowUpdatePopup(Window window, JObject obj) //TODO use RFS
		{
			if(this.Window.CheckAccess())
			{
				if(MessageDialog.Show(window, I18n.Localize("Dialog", "Update", obj["name"], obj["body"]), I18n.Localize("Dialog.Caption", "Update"), TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No) == TaskDialogResult.Yes)
				{
					Process.Start(obj["assets"][0]["browser_download_url"].ToString());
					this.CloseLauncher();
				}
			}
			else this.Window.Dispatcher.Invoke(() => ShowUpdatePopup(window, obj));
		}

		private void LoadSettings()
		{
			if(File.Exists(Path.Combine(this.UserDirectory, "settings.dat")))
			{
				try
				{
					using(Stream stream = File.Open(Path.Combine(this.UserDirectory, "settings.dat"), FileMode.Open))
					{
						Instance.Settings = (UserSettings)formatter.Deserialize(stream);
					}

					if(!Directory.Exists(Instance.Settings.GetProfileFolder()))
						Directory.CreateDirectory(Instance.Settings.GetProfileFolder());
				}
				catch(Exception ex)
				{
					Log.Error("Unable to read settings.");
					Log.Error(ex.ToString());
					Instance.Settings = new UserSettings();
					this.SaveSettings();
					return;
				}
			}
			else
			{
				Log.Info("No settings.dat file found.");
				Instance.Settings = new UserSettings();
				this.SaveSettings();
			}
		}

		public void SaveSettings()
		{
			try
			{
				if(!Directory.Exists(this.UserDirectory))
					Directory.CreateDirectory(this.UserDirectory);

				using(Stream stream = File.Open(Path.Combine(this.UserDirectory, "settings.dat"), FileMode.Create))
				{
					formatter.Serialize(stream, this.Settings);
				}

				Log.Info("Settings saved.");
			}
			catch(Exception ex)
			{
				Log.Error("Unable to save settings.");
				Log.Error(ex.ToString());
			}
		}

		/// <summary>
		/// Do basic checks to the profile (is modded but supposed to be vanilla, are mods in the wrong directory, ...)
		/// </summary>
		/// <returns>true if the game can be launched, false otherwise</returns>
		public bool CheckCurrentProfile()
		{
			if(Instance.Profiles.CurrentProfile == 0 && GameScanner.IsGTAModded())
			{
				Log.Warn("GTA V is modded while the selected profile is \"Vanilla\" !");

				TaskDialogResult result = LocalizedMessage.Show(this.Window, "ModsOnVanilla", "Warn", TaskDialogStandardIcon.Warning, TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No | TaskDialogStandardButtons.Cancel);

				if(result == TaskDialogResult.Yes)
				{
					string name = this.GetNewProfileName();

					this.Window.Dispatcher.Invoke(() =>
					{
						this.Profiles.Add(name);
						this.Profiles.CurrentProfile = Instance.Profiles.Count - 1;
						this.UiManager.Profiles.Add(name);
						this.UiManager.SelectedProfile = this.UiManager.Profiles.Count - 1;
						this.SaveProfiles();
					});

					return true;
				}
				else if(result == TaskDialogResult.No)
				{
					new PerformJobDialog(this.WorkManager, new DeleteMods()).Show(this.WorkManager);
					return true;
				}
				else return false;
			}
			else if(Instance.Profiles.CurrentProfile != 0 && Directory.Exists(Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[this.Profiles.CurrentProfile])))
			{
				string path = Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[this.Profiles.CurrentProfile]);

				if(Directory.GetFileSystemEntries(path).GetLength(0) != 0)
				{
					TaskDialogResult result = LocalizedMessage.Show(this.Window, "UpdateProfile", "UpdateProfile", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No);

					if(result == TaskDialogResult.Yes)
					{
						GameScanner.ListRootMods(out List<string> modFiles, out List<string> modDirs);
						List<string> dlcMods = GameScanner.ListDlcMods();

						foreach(string dir in modDirs)
							this.WorkManager.QueueJob(new MoveJob(dir, dir.Replace(this.Installs.Selected.Path, path)));
						foreach(string file in modFiles)
						{
							if(this.Settings.DeleteLogs && file.EndsWith(".log"))
								this.WorkManager.QueueJob(new DeleteJob(file));
							else this.WorkManager.QueueJob(new MoveJob(file, file.Replace(this.Installs.Selected.Path, path)));
						}
						foreach(string mod in dlcMods)
							this.WorkManager.QueueJob(new MoveJob(mod, mod.Replace(this.Installs.Selected.Path, path)));

						new PerformJobsDialog(this.WorkManager).Show();
					}
				}

				return true;
			}
			else return true;
		}

		public bool CanLaunchGame()
		{
			if(this.Installs.Selected.Type == InstallType.Retail)
			{
				if(File.Exists(Path.Combine(this.Installs.Selected.Path, "GTA5.exe")))
					return true;
				else
				{
					LocalizedMessage.Show(this.Window, "NoExecutable", "Error", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
					return false;
				}
			}
			else if(this.Installs.Selected.Type == InstallType.Steam)
			{
				if(SteamHelper.IsAvailable)
					return true;
				else
				{
					LocalizedMessage.Show(this.Window, "NoSteam", "Error", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
					return false;
				}
			}
			else return false;
		}

		/// <summary>
		/// Used when a profile needs to be created without the user giving it a name
		/// </summary>
		/// <returns>A generic profile name that doesn't already exist</returns>
		public string GetNewProfileName()
		{
			if(this.Profiles.Contains("ModdedState"))
			{
				int i = 0;
				while(this.Profiles.Contains("ModdedState"+i))
					i++;
				return "ModdedState"+i;
			}
			else return "ModdedState";
		}

		private void CreateDefaultProfiles()
		{
			Log.Info("Creating default profiles.");
			if(Directory.Exists(Path.Combine(this.UserDirectory, "profiles.dat")))
				Directory.Delete(Path.Combine(this.UserDirectory, "profiles.dat"));

			this.Profiles = new ProfileList();
			this.Profiles.Add("Vanilla");

			bool isModded = GameScanner.IsGTAModded();
			if(isModded)
			{
				Log.Info("GTA V is currently modded.");
				this.Profiles.Add("Modded");
			}
			else Log.Info("GTA V is not currently modded");

			this.Profiles.CurrentProfile = isModded ? 1 : 0;

			this.SaveProfiles();
		}

		public void SaveProfiles()
		{
			try
			{
				if(!Directory.Exists(this.UserDirectory))
					Directory.CreateDirectory(this.UserDirectory);

				using(Stream stream = File.Open(Path.Combine(this.UserDirectory, "profiles.dat"), FileMode.Create))
				{
					formatter.Serialize(stream, this.Profiles);
				}

				Log.Info("Profiles saved.");
			}
			catch(Exception ex)
			{
				Log.Error("Unable to save profiles.");
				Log.Error(ex.ToString());
			}
		}

		public void UpdateGameInfo()
		{
			string exePath = Path.Combine(this.Installs.Selected.Path, "GTA5.exe");

			if(File.Exists(exePath))
				this.UiManager.GtaVersion = "GTA V " + FileVersionInfo.GetVersionInfo(exePath).FileVersion;

			if(this.Installs.Selected.Type == InstallType.Steam)
				this.UiManager.GtaType = I18n.Localize("Label", "SteamVersion");
			else if(this.Installs.Selected.Type == InstallType.Retail)
				this.UiManager.GtaType = I18n.Localize("Label", "RetailVersion");

			if(Directory.Exists(this.Installs.Selected.Path))
			{
				DirectoryInfo dir = new DirectoryInfo(this.Installs.Selected.Path);
				DirectorySecurity sec = dir.GetAccessControl();

				IdentityReference id = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
				bool hasPerms = false;

				foreach(FileSystemAccessRule rule in sec.GetAccessRules(true, true, typeof(SecurityIdentifier)))
				{
					if(rule.IdentityReference == id && rule.FileSystemRights == FileSystemRights.FullControl && rule.AccessControlType == AccessControlType.Allow)
					{
						hasPerms = true;
						break;
					}
				}

				if(!hasPerms)
				{
					Log.Warn("The launcher doesn't have full control over the game's directory!");

					if(User.HasElevatedPrivileges)
					{
						Log.Info("The launcher is running with elevated privileges. Getting full control...");
						sec.SetAccessRule(new FileSystemAccessRule(id, FileSystemRights.FullControl, AccessControlType.Allow));
						dir.SetAccessControl(sec);
					}
					else
					{
						Log.Info("Asking for elevated privileges...");

						Process process = Process.GetCurrentProcess();

						if(LocalizedCMessage.Show(this.Window, "LauncherNeedsPerms", "Info", TaskDialogStandardIcon.Shield, new DialogButton("Ok", true), "Cancel") == "Ok")
						{
							ProcessBuilder builder = new ProcessBuilder(Assembly.GetEntryAssembly().Location);
							builder.LaunchAsAdmin = true;
							builder.StartProcess();
						}

						process.Kill();
					}
				}
			}
			else new PopupChooseInstall(this.Window).ShowDialog();
		}

		/// <summary>
		/// Change the active profile and move the mods accordingly
		/// </summary>
		/// <param name="selected"></param>
		private void SwitchProfileTo(int selected)
		{
			if(this.Profiles.CurrentProfile != selected)
			{
				this.UiManager.Working = true;

				Log.Info("Switching from profile '" + this.Profiles[this.Profiles.CurrentProfile] + "' to '" + this.Profiles[selected] + "'.");
				this.WorkManager.ProgressDisplay.ProgressState = ProgressState.Indeterminate;

				try
				{
					if(this.Profiles.CurrentProfile != 0)
					{
						string profilePath = Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[this.Profiles.CurrentProfile]);
						GameScanner.ListRootMods(out List<string> modFiles, out List<string> modDirs);
						List<string> dlcMods = GameScanner.ListDlcMods();

						foreach(string dir in modDirs)
							this.WorkManager.QueueJob(new MoveJob(dir, dir.Replace(this.Installs.Selected.Path, profilePath)));
						foreach(string file in modFiles)
						{
							if(this.Settings.DeleteLogs && file.EndsWith(".log"))
								this.WorkManager.QueueJob(new DeleteJob(file));
							else this.WorkManager.QueueJob(new MoveJob(file, file.Replace(this.Installs.Selected.Path, profilePath)));
						}
						foreach(string mod in dlcMods)
							this.WorkManager.QueueJob(new MoveJob(mod, mod.Replace(this.Installs.Selected.Path, profilePath)));
					}

					if(selected != 0)
					{
						string profilePath = Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[selected]);

						foreach(string entry in Directory.GetFileSystemEntries(profilePath))
							this.WorkManager.QueueJob(new MoveJob(entry, Path.Combine(this.Installs.Selected.Path, Path.GetFileName(entry))));
					}

					this.WorkManager.PerformJobs();

					this.Profiles.CurrentProfile = selected;
					this.SaveProfiles();
				}
				catch(IOException e)
				{
					Log.Error(e.ToString());
					LocalizedMessage.Show(this.Window, "ProfileSwitchError", "FatalError", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
					Process.GetCurrentProcess().Kill();
				}

				this.WorkManager.ProgressDisplay.ProgressState = ProgressState.NoProgress;
				this.UiManager.Working = false;
			}
		}

		private void LaunchGame(bool online)
		{
			Log.Info("Launching game...");

			if(this.CanLaunchGame() && this.CheckCurrentProfile())
			{
				if(online && this.Profiles.CurrentProfile != 0)
				{
					LocalizedMessage.Show(this.Window, "CantPlayOnline", "Impossible", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
					this.UiManager.Working = false;
					this.UiManager.ButtonsEnabled = true;
					this.UiManager.CanPlayOnline = this.UiManager.SelectedProfile == 0;
				}
				else
				{
					ProcessBuilder builder = new ProcessBuilder();
					builder.WorkingDirectory = this.Installs.Selected.Path;

					if(this.Settings.UseRph && File.Exists(Path.Combine(this.Installs.Selected.Path, "RAGEPluginHook.exe")))
					{
						Log.Info("Starting RAGE Plugin Hook process...");
						builder.FilePath = Path.Combine(this.Installs.Selected.Path, "RAGEPluginHook.exe");
					}
					else if(this.Installs.Selected.Type == InstallType.Steam)
					{
						Log.Info("Starting steam game process...");
						builder.FilePath = SteamHelper.ExecutablePath;
						builder.AddArgument("-applaunch 271590");
					}
					else
					{
						Log.Info("Starting game process...");
						builder.FilePath = Path.Combine(this.Installs.Selected.Path, "GTAVLauncher.exe");
					}

					Log.Info("Setting game language to " + this.Settings.GtaLanguage);
					builder.AddArgument("-uilanguage " + this.Settings.GtaLanguage);

					if(online)
						builder.AddArgument("-StraightIntoFreemode");
					else if(this.Profiles.CurrentProfile != 0 && this.Settings.OfflineMode)
						builder.AddArgument("-scOfflineOnly");

					Log.Info("Executing "+builder);
					builder.StartProcess();

					this.CloseLauncher();
				}
			}
			else
			{
				Log.Info("Aborting game launch.");
				this.UiManager.Working = false;
				this.UiManager.ButtonsEnabled = true;
				this.UiManager.CanPlayOnline = this.UiManager.SelectedProfile == 0;
			}
		}

		private void PressPlay(object sender, EventArgs e)
		{
			bool online = ((Button)sender).Name == "PlayOnlineButton";
			Log.Info("Starting " + (online ? "online" : "normal") + " game launch process...");

			this.UiManager.CanApplyChanges = false;
			this.UiManager.ButtonsEnabled = false;
			this.UiManager.CanPlayOnline = false;

			this.WorkManager.StartWork(() =>
			{
				if(this.UiManager.SelectedProfile != this.Profiles.CurrentProfile)
					this.SwitchProfileTo(this.UiManager.SelectedProfile);
				this.LaunchGame(online);
			});
		}

		private void OnSelectionChange(object sender, EventArgs e)
		{
			if(this.UiManager.SelectedProfile == -1)
			{
				this.UiManager.SelectedProfile = this.UiManager.LastSelectedProfile;
				return;
			}

			this.UiManager.CanPlayOnline = this.UiManager.SelectedProfile == 0;
			this.UiManager.CanApplyChanges = this.Profiles.CurrentProfile != this.UiManager.SelectedProfile;

			this.UiManager.LastSelectedProfile = this.UiManager.SelectedProfile;
			Log.Info("Selecting profile "+this.UiManager.SelectedProfile);
		}

		private void CreateNewProfile(object sender, EventArgs e)
		{
			PopupCreate popup = new PopupCreate(this.Window);
			popup.ShowDialog();
		}

		private void EditSelectedProfile(object sender, EventArgs e)
		{
			if(this.UiManager.SelectedProfile != 0)
			{
				PopupEdit popup = new PopupEdit(this.Window, this.UiManager.SelectedProfile, this.UiManager.Profiles[this.UiManager.SelectedProfile]);
				popup.ShowDialog();
			}
			else LocalizedMessage.Show(this.Window, "CantEditProfile", "Impossible", TaskDialogStandardIcon.Warning, TaskDialogStandardButtons.Ok);
		}

		private void SwitchToSelectedProfile(object sender, RoutedEventArgs e)
		{
			this.UiManager.CanApplyChanges = false;
			this.WorkManager.StartWork(() =>
			{
				this.UiManager.ButtonsEnabled = false;
				this.SwitchProfileTo(this.UiManager.SelectedProfile);
				this.UiManager.ButtonsEnabled = true;
			});
		}

		private void DeleteSelectedProfile(object sender, EventArgs e)
		{
			if(this.UiManager.SelectedProfile == 0)
				LocalizedMessage.Show(this.Window, "CantDeleteProfile", "Impossible", TaskDialogStandardIcon.Warning, TaskDialogStandardButtons.Ok);
			else
			{
				TaskDialogResult result = LocalizedMessage.Show(this.Window, "SureDelete", "Sure", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.Cancel);

				if(result == TaskDialogResult.Yes)
				{
					string selected = this.UiManager.Profiles[this.UiManager.SelectedProfile];
					int index = this.UiManager.SelectedProfile;
					if(Directory.Exists(Path.Combine(this.Settings.GetProfileFolder(),  selected)))
						IOUtil.Delete(Path.Combine(this.Settings.GetProfileFolder(), selected));
					this.Profiles.Remove(selected);
					this.UiManager.Profiles.Remove(selected);

					if(index == this.Profiles.CurrentProfile)
					{
						new PerformJobDialog(this.WorkManager, new DeleteMods()).Show(this.WorkManager);
						this.Profiles.CurrentProfile = 0;
					}
					else if(index < this.Profiles.CurrentProfile)
						this.Profiles.CurrentProfile--;
					this.UiManager.SelectedProfile = this.Profiles.CurrentProfile;
					
					this.SaveProfiles();
				}
			}
		}

		public void CloseLauncher()
		{
			this.closeRequested = true;

			if(Application.Current.CheckAccess())
				Application.Current.Shutdown();
			else Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
		}

		private void ShowAboutPopup(object sender, EventArgs e)
		{
			MessageDialog.Show(this.Window, I18n.Localize("Dialog", "About", this.Version), I18n.Localize("Dialog.Caption", "About"), TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Ok);
		}

		private void OpenSettingsPopup(object sender, EventArgs e)
		{
			PopupSettings popup = new PopupSettings(this.Window);
			popup.ShowDialog();
		}

		private void OnI18nReload(string newLanguage)
		{
			this.UpdateGameInfo();
		}

		protected override void FillCrashReport(Exception exception, StringBuilder report)
		{
			base.FillCrashReport(exception, report);
			
			report.Append("-- Launcher state --\n");
			try
			{
				report.Append("Is window initialized: " + (this.Window != null) + '\n');
				if(this.Profiles != null)
				{
					report.Append("Current profile index: " + this.Profiles.CurrentProfile + '\n');
					report.Append("Current profile: ");
					try
					{
						report.Append(this.Profiles[this.Profiles.CurrentProfile]);
					}
					catch(Exception)
					{
						report.Append("~Unexpected error~");
					}
					report.Append('\n');
				}
			}
			catch(Exception)
			{
				report.Append("~Unexpected error~\n");
			}

			report.Append('\n');

			report.Append("-- UI state --\n");
			try
			{
				if(this.UiManager != null)
				{
					report.Append("Selected profile index: " + this.UiManager.SelectedProfile + '\n');
					report.Append("Selected profile: ");
					try
					{
						report.Append(this.Profiles[this.UiManager.SelectedProfile]);
					}
					catch(Exception)
					{
						report.Append("~Unexpected error~");
					}
					report.Append('\n');
					report.Append("Is working: " + this.UiManager.Working + '\n');
					report.Append("Are buttons enabled: " + this.UiManager.ButtonsEnabled + '\n');
				}
				else report.Append("Unable to get UI state");
			}
			catch(Exception)
			{
				report.Append("~Unexpected error~\n");
			}
		}

		private void OnWindowClosing(object sender, CancelEventArgs e)
		{
			if(!this.closeRequested && this.WorkManager.IsWorking)
			{
				e.Cancel = true;
				LocalizedMessage.Show(this.Window, "LauncherWorking", "Impossible", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Ok);
			}
		}
	}
}
