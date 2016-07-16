using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.IO;
using Microsoft.Win32;
using PursuitLib;
using PursuitLib.Wpf;
using System.Diagnostics;
using System.Collections.Generic;
using GTAVModdingLauncher.Popup;
using System.Windows;
using System.ComponentModel;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Windows.Controls;

namespace GTAVModdingLauncher
{
	/// <summary>
	/// Main launcher class
	/// </summary>
	public class Launcher
	{
		public static Version Version { get; } = Versions.GetTypeVersion(typeof(Launcher));
		public static Launcher Instance { get; internal set; }
		private delegate void Callback();
		private delegate void DoubleCallback(double value);
		private delegate void BoolCallback(bool value);
		private delegate void BoolIntCallback(bool value, int intValue);
		private delegate void UpdateCallback(Window window, JObject obj);

		public MainWindow Window { get; internal set; } = null;
		public string UserDirPath { get; internal set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pursuit\\GTA V Modding Launcher");
		public string SteamPath { get; internal set; } = null;
		public string GtaPath { get; internal set; } = null;
		public ProfileList Profiles { get; internal set; }
		public UserSettings Settings { get; internal set; }
		public UIManager UiManager { get; internal set; }
		public Thread CurrentThread { get; set; } = null;
		private BinaryFormatter formatter = new BinaryFormatter();

		/// <summary>
		/// Indicates if the user chose to close the application. <see cref="CloseLauncher"/> <seealso cref="OnWindowClosing"/>
		/// </summary>
		private bool closeRequested = false;

		public Launcher(MainWindow window)
		{
			Instance = this;
			this.Window = window;

			this.LoadSettings();

			if(this.Settings.UseLogFile)
				Log.SetLogFile(Path.Combine(UserDirPath, "latest.log"));
			Log.Info("GTA V Modding Launcher " + Version);
			Log.Info("Using PursuitLib " + Versions.GetTypeVersion(typeof(Log)));

			Log.Info("Loading languages...");
			I18n.SupportedLanguages.Add("en-US");
			I18n.SupportedLanguages.Add("fr-FR");
			I18n.LoadLanguage(this.Settings.Language);
		}

		/// <summary>
		/// Links MainWindow with this class using events
		/// </summary>
		public void InitUI()
		{
			Log.Info("Initializing user interface...");
			this.Window.Closing += OnWindowClosing;
			this.Window.ProfileList.SelectionChanged += OnSelectionChange;
			this.Window.AboutButton.Click += ShowAboutPopup;
			this.Window.SettingsButton.Click += OpenSettingsPopup;
			this.Window.CreateButton.Click += CreateNewProfile;
			this.Window.EditButton.Click += EditSelectedProfile;
			this.Window.FolderButton.Click += OpenProfileFolder;
			this.Window.DeleteButton.Click += DeleteSelectedProfile;
			this.Window.PlayButton.Click += PressPlay;
			this.Window.PlayOnlineButton.Click += PressPlay;
			this.UiManager = new UIManager(this.Window);
			this.UiManager.WindowTitle += " " + Version + " " + Versions.GetVersionType(Version);

			this.CurrentThread = new Thread(InitLauncher);
			this.CurrentThread.Start();
		}

		private void InitLauncher()
		{
			Log.Info("Initializing launcher...");

			RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Rockstar Games\\Grand Theft Auto V", false);

			if(regKey != null)
			{
				Instance.GtaPath = (string)regKey.GetValue("InstallFolder");
				regKey.Close();
			}

			if(Instance.GtaPath == null)
			{
				regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Valve\\Steam", false);

				if(regKey != null)
				{
					Instance.SteamPath = (string)regKey.GetValue("InstallPath");
					regKey.Close();

					regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Rockstar Games\\GTAV", false);

					if(regKey != null)
					{
						Instance.GtaPath = (string)regKey.GetValue("InstallFolderSteam");
						regKey.Close();

						if(Instance.GtaPath != null)
							Instance.GtaPath = Instance.GtaPath.Substring(0, Instance.GtaPath.Length - 4);
					}

					if(Instance.GtaPath == null)
					{
						string path = Path.Combine(Instance.SteamPath, "steamapps\\common\\Grand Theft Auto V");

						if(File.Exists(Path.Combine(path, "gta5.exe")))
							Instance.GtaPath = path;
					}
				}
			}

			if(Instance.SteamPath != null && Instance.SteamPath.EndsWith("\\"))
				Instance.SteamPath = Instance.SteamPath.Substring(0, Instance.SteamPath.Length - 1);

			if(Instance.GtaPath != null)
			{
				if(Instance.GtaPath.EndsWith("\\"))
					Instance.GtaPath = Instance.GtaPath.Substring(0, Instance.GtaPath.Length - 1);

				Log.Info("Found GTA V installation at " + Instance.GtaPath + ' ' + (Instance.IsSteamVersion() ? "(Steam version)" : "(Retail version)"));

				if(Directory.Exists(Instance.GtaPath))
				{
					GameScanner.Init();

					if(File.Exists(Path.Combine(UserDirPath, "profiles.dat")))
					{
						try
						{
							using(Stream stream = File.Open(Path.Combine(UserDirPath, "profiles.dat"), FileMode.Open))
							{
								Instance.Profiles = (ProfileList)formatter.Deserialize(stream);
							}
						}
						catch(Exception ex)
						{
							Log.Error("Unable to read saved profiles.");
							Log.Error(ex.ToString());
							Messages.UnableToReadProfiles();
							Instance.createDefaultProfiles();
						}
					}
					else
					{
						Log.Info("No profiles.dat file found.");
						Instance.createDefaultProfiles();
					}

					UpdateUI();
				}
				else
				{
					Log.Error("GTA V wasn't found at the specified location.");
					Messages.GTANotFound();
				}
			}
			else
			{
				Log.Error("No GTA installation found.");
				Messages.NoGTA();
			}
		}

		/// <summary>
		/// Updates the UI after <see cref="InitLauncher"/>
		/// </summary>
		private void UpdateUI()
		{
			if(this.GtaPath != null && Directory.Exists(this.GtaPath))
			{
				if(!File.Exists(Path.Combine(Instance.GtaPath, "GTAVLauncher.exe")))
				{
					if(Messages.Show(this.Window, "NoGTALauncher", "Warn", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
					{
						this.CloseLauncher();
						return;
					}
				}

				this.UiManager.Profiles.RemoveAt(0);
				foreach(string profile in this.Profiles)
					this.UiManager.Profiles.Add(profile);
				this.UiManager.SelectedProfile = this.Profiles.CurrentProfile;

				this.UiManager.ButtonsEnabled = true;
				this.UiManager.CanPlayOnline = this.UiManager.SelectedProfile == 0;

				this.UiManager.LauncherVersion = "Launcher " + Version.ToString();
				this.UiManager.GtaVersion = "GTA V " + (Instance.IsSteamVersion() ? (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Rockstar Games\\GTAV", "PatchVersion", "unknown") : (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Rockstar Games\\Grand Theft Auto V", "PatchVersion", "unknown"));
				I18n.Reload += OnI18nReload;
				this.OnI18nReload(null, null);

				if(!this.Settings.IntegrityVerified)
				{
					Log.Info("The game's integrity has not been verified.");
					this.Window.Dispatcher.Invoke(new Callback(AskForIntegrityCheck));
				}
				else if(this.Settings.CheckUpdates)
					this.CheckUpdates();
			}
		}

		private void AskForIntegrityCheck()
		{
			if(Messages.Show(this.Window, "IntegrityNotVerified", "IntegrityCheck", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
			{
				PopupVerifyIntegrity popup = new PopupVerifyIntegrity();
				this.CurrentThread = popup.StartThread();
				popup.ShowDialog();
			}
			else
			{
				Log.Info("The user chose to skip the game integrity check.");
				this.Settings.IntegrityVerified = true;
				this.SaveSettings();
			}

			if(this.Settings.CheckUpdates)
			{
				this.CurrentThread = new Thread(CheckUpdates);
				this.CurrentThread.Start();
			}
		}

		private void CheckUpdates()
		{
			JObject obj = this.IsUpToDate();
			if(obj != null)
				this.Window.Dispatcher.Invoke(new UpdateCallback(ShowUpdatePopup), this.Window, obj);
		}


		/// <summary>
		/// Checks whether the software is up to date or not
		/// </summary>
		/// <returns>A JObject representing the latest update, or null if it's up to date</returns>
		public JObject IsUpToDate()
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
							Log.Info("New update found ("+obj["name"]+')');
							return obj;
						}
					}
				}
				else Log.Warn("Unable to check for updates. Response code was " + response.StatusCode+" ("+response.StatusDescription+')');
			}

			return null;
		}

		/// <summary>
		/// Shows a popup telling the user a new update is out.
		/// </summary>
		/// <param name="obj">A JObject representing the update</param>
		public void ShowUpdatePopup(Window window, JObject obj)
		{
			if(this.Window.CheckAccess())
			{
				if(Messages.Show(window, "Update", "Update", MessageBoxButton.YesNo, MessageBoxImage.Information, obj["name"], obj["body"]) == MessageBoxResult.Yes)
				{
					Process.Start(obj["assets"][0]["browser_download_url"].ToString());
					this.CloseLauncher();
				}
			}
			else this.Window.Dispatcher.Invoke(new UpdateCallback(ShowUpdatePopup), window, obj);
		}

		private void LoadSettings()
		{
			if(File.Exists(Path.Combine(UserDirPath, "settings.dat")))
			{
				try
				{
					using(Stream stream = File.Open(Path.Combine(UserDirPath, "settings.dat"), FileMode.Open))
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
				if(!Directory.Exists(UserDirPath))
					Directory.CreateDirectory(UserDirPath);

				using(Stream stream = File.Open(Path.Combine(UserDirPath, "settings.dat"), FileMode.Create))
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

		public bool IsSteamVersion()
		{
			return this.SteamPath != null;
		}

		/// <summary>
		/// Does basic checks to the profile (is modded but supposed to be vanilla, are mods in the wrong directory, ...)
		/// </summary>
		/// <returns>true if the game can be launched, false otherwise</returns>
		public bool CheckCurrentProfile()
		{
			if(Instance.Profiles.CurrentProfile == 0 && GameScanner.IsGTAModded())
			{
				Log.Warn("GTA V is modded while the selected profile is \"Vanilla\" !");

				MessageBoxResult result = Messages.Show(this.Window, "ModsOnVanilla", "Warn", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

				if(result == MessageBoxResult.Yes)
				{
					string name = Instance.GetNewProfileName();
					this.Profiles.Add(name);
					this.Profiles.CurrentProfile = Instance.Profiles.Count - 1;
					this.UiManager.Profiles.Add(name);
					this.UiManager.SelectedProfile = this.UiManager.Profiles.Count - 1;
					this.SaveProfiles();
					return true;
				}
				else if(result == MessageBoxResult.No)
				{
					PopupDeletingMods popup = new PopupDeletingMods();
					this.CurrentThread = popup.StartThread();
					popup.ShowDialog();
					return true;
				}
				else return false;
			}
			else if(Instance.Profiles.CurrentProfile != 0 && Directory.Exists(Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[this.Profiles.CurrentProfile])))
			{
				string path = Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[this.Profiles.CurrentProfile]);

				if(Directory.GetFileSystemEntries(path).GetLength(0) != 0)
				{
					MessageBoxResult result = Messages.Show(this.Window, "UpdateProfile", "UpdateProfile", MessageBoxButton.YesNo, MessageBoxImage.Question);

					if(result == MessageBoxResult.Yes)
					{
						PopupMoveMods popup = new PopupMoveMods(path);
						this.CurrentThread = popup.StartThread();
						popup.ShowDialog();
					}
				}

				return true;
			}
			else return true;
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

		private void createDefaultProfiles()
		{
			Log.Info("Creating default profiles.");
			if(Directory.Exists(Path.Combine(UserDirPath, "profiles.dat")))
				Directory.Delete(Path.Combine(UserDirPath, "profiles.dat"));

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
				if(!Directory.Exists(UserDirPath))
					Directory.CreateDirectory(UserDirPath);

				using(Stream stream = File.Open(Path.Combine(UserDirPath, "profiles.dat"), FileMode.Create))
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
		
		/// <summary>
		/// Gets called when the user press the "Play" or "Play online" button
		/// </summary>
		/// <param name="online">Did the user chose to play online</param>
		/// <param name="selected">the selected profile ID</param>
		private void SwitchProfileAndPlay(bool online, int selected)
		{
			if(this.Profiles.CurrentProfile != selected)
			{
				Log.Info("Switching from profile '"+this.Profiles[this.Profiles.CurrentProfile]+"' to '"+this.Profiles[selected]+"'.");

				try
				{
					List<string> modFiles = null;
					List<string> modDirs = null;
					List<string> dlcMods = null;
					string[] profileDirs = null;
					string[] profileFiles = null;
					int moveCount = 0;

					if(this.Profiles.CurrentProfile != 0)
					{
						GameScanner.ListRootMods(out modFiles, out modDirs);
						dlcMods = GameScanner.ListDlcMods();
						moveCount += modFiles.Count + modDirs.Count + dlcMods.Count;
					}

					if(selected != 0)
					{
						string profilePath = Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[selected]);
						profileDirs = Directory.GetDirectories(profilePath, "*", SearchOption.AllDirectories);
						profileFiles = Directory.GetFiles(profilePath, "*", SearchOption.AllDirectories);
						moveCount += profileDirs.Length + profileFiles.Length;
					}

					this.SetProgressBarMaximum(moveCount);

					if(this.Profiles.CurrentProfile != 0)
					{
						string profilePath = Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[this.Profiles.CurrentProfile]);
						if(!Directory.Exists(profilePath))
							Directory.CreateDirectory(profilePath);


						foreach(string dir in modDirs)
						{
							string name = dir.Replace(this.GtaPath, profilePath);
							if(!Directory.Exists(name))
								Directory.CreateDirectory(name);
							this.IncrProgressBarValue();
						}

						foreach(string file in modFiles)
						{
							if(this.Settings.DeleteLogs && file.EndsWith(".log"))
								File.Delete(file);
							else
							{
								string name = file.Replace(this.GtaPath, profilePath);
								if(!File.Exists(name))
									File.Move(file, name);
								else File.Delete(file);
							}
							this.IncrProgressBarValue();
						}

						foreach(string dir in modDirs)
						{
							if(Directory.Exists(dir))
								IOUtils.DeleteDirectory(dir);
						}

						foreach(string mod in dlcMods)
						{
							if(File.Exists(mod))
							{
								string name = mod.Replace(this.GtaPath, profilePath);
								if(File.Exists(name))
									File.Delete(name);
								File.Move(mod, name);
							}
							else IOUtils.MoveDirectory(mod, profilePath, false);
							this.IncrProgressBarValue();
						}
					}

					if(selected != 0)
					{
						string profilePath = Path.Combine(this.Settings.GetProfileFolder(), this.Profiles[selected]);
						if(!Directory.Exists(profilePath))
							Directory.CreateDirectory(profilePath);


						foreach(string dir in profileDirs)
						{
							string name = dir.Replace(profilePath, this.GtaPath);
							if(!Directory.Exists(name))
								Directory.CreateDirectory(name);
							this.IncrProgressBarValue();
						}

						foreach(string file in profileFiles)
						{
							string name = file.Replace(profilePath, this.GtaPath);
							if(!File.Exists(name))
								File.Move(file, name);
							else File.Delete(file);
							this.IncrProgressBarValue();
						}

						foreach(string dir in profileDirs)
						{
							if(Directory.Exists(dir))
								IOUtils.DeleteDirectory(dir);
						}
					}

					this.Profiles.CurrentProfile = selected;
					this.SaveProfiles();

					this.Window.Dispatcher.Invoke(new BoolCallback(LaunchGame), online);
				}
				catch(IOException e)
				{
					Log.Error(e.ToString());
					this.Window.Dispatcher.Invoke(new BoolIntCallback(RetryAfterPermissionUpgrade), online, selected);
				}
				catch(UnauthorizedAccessException e)
				{
					Log.Error(e.ToString());
					this.Window.Dispatcher.Invoke(new BoolIntCallback(RetryAfterPermissionUpgrade), online, selected);
				}
			}
			else this.Window.Dispatcher.Invoke(new BoolCallback(LaunchGame), online);
		}

		/// <summary>
		/// Asks the user to allow the launcher to modify the game's folder, then retry
		/// </summary>
		private void RetryAfterPermissionUpgrade(bool online, int index)
		{
			MessageBoxResult result = Messages.Show(this.Window, "CantModifyGTAV", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

			if(result == MessageBoxResult.OK)
			{
				this.CurrentThread = new Thread(() => SwitchProfileAndPlay(online, index));
				this.CurrentThread.Start();
			}
			else this.CloseLauncher();
		}

		private void LaunchGame(bool online)
		{
			Log.Info("Launching game...");

			if(this.CheckCurrentProfile())
			{
				if(online && this.Profiles.CurrentProfile != 0)
				{
					Messages.Show(this.Window, "CantPlayOnline", "Impossible", MessageBoxButton.OK, MessageBoxImage.Error);
					this.UiManager.Working = false;
					this.UiManager.ButtonsEnabled = true;
					this.UiManager.CanPlayOnline = this.UiManager.SelectedProfile == 0;
				}
				else
				{
					ProcessBuilder builder = new ProcessBuilder();
					builder.WorkingDirectory = this.GtaPath;

					if(this.Settings.UseRph && File.Exists(Path.Combine(this.GtaPath, "RAGEPluginHook.exe")))
					{
						Log.Info("Starting RAGE Plugin Hook process...");
						builder.FilePath = Path.Combine(this.GtaPath, "RAGEPluginHook.exe");
					}
					else if(this.IsSteamVersion())
					{
						Log.Info("Starting steam game process...");
						builder.FilePath = Path.Combine(this.SteamPath, "steam.exe");
						builder.AddArgument("-applaunch 271590");
					}
					else
					{
						Log.Info("Starting game process...");
						builder.FilePath = Path.Combine(this.GtaPath, "GTAVLauncher.exe");
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
			Log.Info("Starting "+(online ? "online" : "normal")+" game launch process...");

			if(this.UiManager.SelectedProfile != this.Profiles.CurrentProfile)
			{
				this.UiManager.Working = true;
				this.UiManager.ProgressIndeterminate = true;
			}

			this.UiManager.ButtonsEnabled = false;
			this.UiManager.CanPlayOnline = false;
			int index = this.UiManager.SelectedProfile;
			this.CurrentThread = new Thread(() => SwitchProfileAndPlay(online, index));
			this.CurrentThread.Start();
		}

		private void OnSelectionChange(object sender, EventArgs e)
		{
			if(this.UiManager.SelectedProfile == -1)
			{
				this.UiManager.SelectedProfile = this.UiManager.LastSelectedProfile;
				return;
			}
			else if(this.UiManager.SelectedProfile == 0)
				this.UiManager.CanPlayOnline = true;
			else this.UiManager.CanPlayOnline = false;

			this.UiManager.LastSelectedProfile = this.UiManager.SelectedProfile;
			Log.Info("Selecting profile "+this.UiManager.SelectedProfile);
		}

		private void CreateNewProfile(object sender, EventArgs e)
		{
			PopupCreate popup = new PopupCreate();
			popup.ShowDialog();
		}

		private void EditSelectedProfile(object sender, EventArgs e)
		{
			if(this.UiManager.SelectedProfile != 0)
			{
				PopupEdit popup = new PopupEdit(this.UiManager.SelectedProfile, this.UiManager.Profiles[this.UiManager.SelectedProfile]);
				popup.ShowDialog();
			}
			else Messages.Show(this.Window, "CantEditProfile", "Impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		private void OpenProfileFolder(object sender, EventArgs e)
		{
			if(this.UiManager.SelectedProfile == this.Profiles.CurrentProfile)
				Process.Start(this.GtaPath);
			else if(this.UiManager.SelectedProfile != 0)
			{
				string path = Path.Combine(this.Settings.GetProfileFolder(), this.UiManager.Profiles[this.UiManager.SelectedProfile]);

				if(!Directory.Exists(path))
					Directory.CreateDirectory(path);
				
				Process.Start(path);
			}
			else Messages.Show(this.Window, "NoProfileFolder", "Impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		private void DeleteSelectedProfile(object sender, EventArgs e)
		{
			if(this.UiManager.SelectedProfile == 0)
				Messages.Show(this.Window, "CantDeleteProfile", "Impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
			else
			{
				MessageBoxResult result = Messages.Show(this.Window, "SureDelete", "Sure", MessageBoxButton.YesNo, MessageBoxImage.Question);

				if(result == MessageBoxResult.Yes)
				{
					string selected = this.UiManager.Profiles[this.UiManager.SelectedProfile];
					int index = this.UiManager.SelectedProfile;
					if(Directory.Exists(Path.Combine(this.Settings.GetProfileFolder(),  selected)))
						IOUtils.DeleteDirectory(Path.Combine(this.Settings.GetProfileFolder(), selected));
					this.Profiles.Remove(selected);
					this.UiManager.SelectedProfile = 0;
					this.UiManager.Profiles.Remove(selected);

					if(index == this.Profiles.CurrentProfile)
					{
						PopupDeletingMods popup = new PopupDeletingMods();
						this.CurrentThread = popup.StartThread();
						popup.ShowDialog();
						if(!popup.CreatedModdedState)
							this.Profiles.CurrentProfile = 0;
					}
					
					this.SaveProfiles();
				}
			}
		}

		public void CloseLauncher()
		{
			this.closeRequested = true;
			Application.Current.Shutdown();
		}

		private void ShowAboutPopup(object sender, EventArgs e)
		{
			Messages.Show(this.Window, "About", "About", MessageBoxButton.OK, MessageBoxImage.Information, Version);
		}

		private void OpenSettingsPopup(object sender, EventArgs e)
		{
			PopupSettings popup = new PopupSettings();
			popup.ShowDialog();
		}

		private void OnI18nReload(object sender, EventArgs e)
		{
			this.UiManager.GtaType = this.IsSteamVersion() ? I18n.Localize("Label", "SteamVersion") : I18n.Localize("Label", "RetailVersion");
		}
		
		private void OnWindowClosing(object sender, CancelEventArgs e)
		{
			if(!this.closeRequested && this.CurrentThread != null && this.CurrentThread.IsAlive)
			{
				e.Cancel = true;
				Messages.Show(this.Window, "LauncherWorking", "Impossible", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void SetProgressBarMaximum(double value)
		{
			if(!this.Window.Dispatcher.CheckAccess())
				this.Window.Dispatcher.Invoke(new DoubleCallback(SetProgressBarMaximum), value);
			else
			{
				this.UiManager.ProgressIndeterminate = false;
				this.UiManager.ProgressMaximum = value;
			}
		}

		private void IncrProgressBarValue()
		{
			if(!this.Window.Dispatcher.CheckAccess())
				this.Window.Dispatcher.Invoke(new Callback(IncrProgressBarValue));
			else this.UiManager.Progress++;
		}
	}
}
