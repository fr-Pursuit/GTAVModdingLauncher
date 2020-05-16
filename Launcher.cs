using GTAVModdingLauncher.Legacy;
using GTAVModdingLauncher.Ui;
using GTAVModdingLauncher.Ui.Popup;
using GTAVModdingLauncher.Work;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PursuitLib;
using PursuitLib.Extensions;
using PursuitLib.IO;
using PursuitLib.IO.PPF;
using PursuitLib.Windows;
using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Windows.WPF.Modern;
using PursuitLib.Work;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.OleDb;
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

namespace GTAVModdingLauncher
{
	/// <summary>
	/// Main launcher class
	/// </summary>
	public class Launcher : ModernApp
	{
		public static Launcher Instance { get; private set; }
		private const int GameInitTime = 60000;
		private const int GameCheckInterval = 5000;
		private const int GameWaitTimeout = 360000;

		public MainWindow Window { get; private set; } = null;
		public UserConfig Config { get; private set; }
		public UIManager UiManager { get; private set; }

		/// <summary>
		/// Indicates if the user chose to close the application. <see cref="CloseLauncher"/> <seealso cref="OnWindowClosing"/>
		/// </summary>
		private bool closeRequested = false;
		
		public Launcher()
		{
			Instance = this;

			if(File.Exists("Resources.ppf"))
				ResourceManager.RegisterProvider("appRsrc", new PPFFile("Resources.ppf"));

			I18n.Initialize();
			this.LoadConfig();

			if(this.Config.UseLogFile)
				Log.LogFile = Path.Combine(this.UserDirectory, "latest.log");
			Log.Info(this.DisplayName);
			Log.Info("Using PursuitLib " + typeof(Log).GetVersion());

			Log.Info("Loading languages...");
			I18n.LoadLanguage(this.Config.Language);

			if(!Directory.Exists("Profiles"))
				Directory.CreateDirectory("Profiles");
		}

		/// <summary>
		/// Links MainWindow with this class using events
		/// </summary>
		public void InitUI(MainWindow window)
		{
			Log.Info("Initializing user interface...");
			this.Window = window;
			this.Window.EnsureFitsScreen();
			this.Window.Closing += OnWindowClosing;
			this.Window.Closed += OnWindowClosed;
			this.Window.AboutButton.Click += ShowAboutPopup;
			this.Window.SettingsButton.Click += OpenSettingsPopup;
			this.Window.CreateButton.Click += CreateNewProfile;
			this.UiManager = new UIManager(this.Window);
			this.UiManager.WindowTitle = this.DisplayName;
			this.WorkManager.ProgressDisplay = new MultiProgressDisplay((WPFProgressBar)this.Window.Progress, new TaskBarProgress());

			this.Theme = this.Config.Theme;
			this.InitLauncher();

			this.UiManager.RandomizeBackground();
			this.UiManager.UpdateProfiles();
			this.UiManager.UIEnabled = true;
			this.UiManager.LauncherVersion = "Launcher " + this.Version;

			I18n.Reload += OnI18nReload;
			this.OnI18nReload(null);

			if(this.Config.DisplayNews)
				this.Window.News.StartCycle();
			if(this.Config.CheckUpdates)
				this.WorkManager.StartWork(CheckUpdates);
		}

		private void InitLauncher()
		{
			Log.Info("Initializing launcher...");
			SteamHelper.Initialize();

			if(this.Config.SelectedInstall != null && (this.Config.SelectedInstall.Path == null || !Directory.Exists(this.Config.SelectedInstall.Path)))
				this.Config.SelectedInstall = null;

			if(this.Config.SelectedInstall == null)
			{
				GTAInstall[] installs = GTAInstall.FindInstalls();

				if(installs.Length > 0)
					this.Config.SelectedInstall = installs[0];
				else
				{
					LocalizedMessage.Show("GTANotFound", "Info", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Ok);
					new PopupChooseInstall().ShowDialog();

					if(this.Config.SelectedInstall == null)
					{
						Environment.Exit(1);
						return;
					}
				}

				this.Config.Save();
			}

			Log.Info("Using GTA V installation at " + this.Config.SelectedInstall.Path);
			Log.Info("Installation type: " + this.Config.SelectedInstall.Type);

			if(Path.GetFullPath(this.WorkingDirectory).Equals(Path.GetFullPath(this.Config.SelectedInstall.Path)))
			{
				LocalizedMessage.Show("InstalledInGTA", "Error", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
				Environment.Exit(1);
			}

			GameScanner.Init();

			Log.Info("Loading profiles...");

			string profilesDir = Path.Combine(this.UserDirectory, "Profiles");

			if(!Directory.Exists(profilesDir))
				Directory.CreateDirectory(profilesDir);

			if(this.Config.VanillaProfile == null)
			{
				Log.Info("Vanilla profile not found. Creating it");
				this.Config.Profiles.Add(new Profile("Vanilla", true));
			}

			foreach(string dir in Directory.EnumerateDirectories(profilesDir))
			{
				string name = Path.GetFileName(dir);
				if(!this.Config.ProfileExists(name))
					this.Config.Profiles.Add(new Profile(name));
			}

			if(this.Config.Profile == null)
			{
				Log.Info("Current profile is invalid");

				if(GameScanner.IsGTAModded())
				{
					Log.Info("GTA is currently modded: creating new modded profile");
					Profile profile = new Profile(this.GetNewProfileName());
					this.Config.Profiles.Add(profile);
					this.Config.Profile = profile;
				}
				else
				{
					Log.Info("GTA is currently not modded: considering the game vanilla");
					this.Config.CurrentProfile = this.Config.VanillaProfile.Name;
				}
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
		/// <param name="window">The parent window</param>
		/// <param name="obj">A JObject representing the update</param>
		public void ShowUpdatePopup(Window window, JObject obj)
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

		private void LoadConfig()
		{
			this.Config = new UserConfig();

			//TODO Remove legacy support when not needed anymore
			string userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pursuit", "GTA V Modding Launcher");

			if(File.Exists(Path.Combine(userDir, "settings.dat")))
			{
				try
				{
					Log.Info("Legacy config file found. Converting it...");

					string profilesFolder = Path.Combine(this.UserDirectory, "Profiles");
					if(!Directory.Exists(profilesFolder))
						Directory.CreateDirectory(profilesFolder);

					string oldProfiles = userDir;

					using(Stream stream = File.Open(Path.Combine(userDir, "settings.dat"), FileMode.Open))
					{
						//Legacy support
						BinaryFormatter formatter = new BinaryFormatter();
						formatter.Binder = new LegacyBinder();
						UserSettings settings = (UserSettings)formatter.Deserialize(stream);

						this.Config.UseRph = settings.UseRph;
						this.Config.DeleteLogs = settings.DeleteLogs;
						this.Config.OfflineMode = settings.OfflineMode;
						this.Config.CheckUpdates = settings.CheckUpdates;
						this.Config.UseLogFile = settings.UseLogFile;
						this.Config.Language = settings.Language;
						this.Config.GtaLanguage = settings.GtaLanguage;
						this.Config.Save();

						if(settings.CustomFolder != null)
							oldProfiles = settings.CustomFolder;
					}

					if(Directory.Exists(oldProfiles))
					{
						Log.Info("Legacy profiles found. Moving them...");

						foreach(string dir in Directory.EnumerateDirectories(oldProfiles))
							this.WorkManager.QueueJob(new MoveJob(dir, Path.Combine(this.UserDirectory, "Profiles", Path.GetFileName(dir))));

						new PerformJobsDialog(this.WorkManager).Show();
					}

					File.Delete(Path.Combine(userDir, "settings.dat"));
				}
				catch(Exception ex)
				{
					Log.Error("Unable to read legacy settings.");
					Log.Error(ex.ToString());
				}
			}
		}

		/// <summary>
		/// Do basic checks to the profile (is modded but supposed to be vanilla, are mods in the wrong directory, ...)
		/// </summary>
		/// <returns>true if the game can be launched, false otherwise</returns>
		public bool CheckCurrentProfile()
		{
			Profile currentProfile = this.Config.Profile;

			if(currentProfile.IsVanilla && GameScanner.IsGTAModded())
			{
				Log.Warn("GTA V is modded, but the vanilla profile is selected !");

				TaskDialogResult result = LocalizedMessage.Show(this.Window, "ModsOnVanilla", "Warn", TaskDialogStandardIcon.Warning, TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No | TaskDialogStandardButtons.Cancel);

				if(result == TaskDialogResult.Yes)
				{
					string name = this.GetNewProfileName();

					Profile profile = new Profile(name);
					this.Config.Profiles.Add(profile);
					this.Config.Profile = profile;
					this.Config.Save();
					this.UiManager.AddProfile(profile);
					this.UiManager.UpdateActiveProfile();
					return true;
				}
				else if(result == TaskDialogResult.No)
				{
					new PerformJobDialog(this.WorkManager, new DeleteMods()).Show(this.WorkManager);
					return true;
				}
				else return false;
			}
			else if(!currentProfile.IsVanilla && Directory.Exists(currentProfile.ExtFolder))
			{
				string path = currentProfile.ExtFolder;

				if(Directory.GetFileSystemEntries(path).GetLength(0) != 0)
				{
					TaskDialogResult result = LocalizedMessage.Show(this.Window, "UpdateProfile", "UpdateProfile", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No);

					if(result == TaskDialogResult.Yes)
					{
						GameScanner.ListRootMods(out List<string> modFiles, out List<string> modDirs);
						List<string> dlcMods = GameScanner.ListDlcMods();

						foreach(string dir in modDirs)
							this.WorkManager.QueueJob(new MoveJob(dir, Path.Combine(path, IOUtil.GetRelativePath(dir, this.Config.SelectedInstall.Path))));
						foreach(string file in modFiles)
						{
							if(this.Config.DeleteLogs && file.EndsWith(".log"))
								this.WorkManager.QueueJob(new DeleteJob(file));
							else this.WorkManager.QueueJob(new MoveJob(file, Path.Combine(path, IOUtil.GetRelativePath(file, this.Config.SelectedInstall.Path))));
						}
						foreach(string mod in dlcMods)
							this.WorkManager.QueueJob(new MoveJob(mod, Path.Combine(path, IOUtil.GetRelativePath(mod, this.Config.SelectedInstall.Path))));

						new PerformJobsDialog(this.WorkManager).Show();
					}
				}

				return true;
			}
			else return true;
		}

		public bool CanLaunchGame()
		{
			if(this.Config.SelectedInstall.Type == InstallType.Retail)
			{
				if(File.Exists(Path.Combine(this.Config.SelectedInstall.Path, "GTA5.exe")))
					return true;
				else
				{
					LocalizedMessage.Show(this.Window, "NoExecutable", "Error", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
					return false;
				}
			}
			else if(this.Config.SelectedInstall.Type == InstallType.Steam)
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
			string baseName = I18n.Localize("Random", "ModdedProfile");

			if(this.Config.ProfileExists(baseName))
			{
				int i = 0;
				while(this.Config.ProfileExists(baseName + i))
					i++;
				return baseName + i;
			}
			else return baseName;
		}

		public void UpdateGameInfo()
		{
			string exePath = Path.Combine(this.Config.SelectedInstall.Path, "GTA5.exe");

			if(File.Exists(exePath))
				this.UiManager.GtaVersion = "GTA V " + FileVersionInfo.GetVersionInfo(exePath).FileVersion;

			if(this.Config.SelectedInstall.Type == InstallType.Steam)
				this.UiManager.GtaType = I18n.Localize("Label", "SteamVersion");
			else if(this.Config.SelectedInstall.Type == InstallType.Retail)
				this.UiManager.GtaType = I18n.Localize("Label", "RetailVersion");

			if(Directory.Exists(this.Config.SelectedInstall.Path))
			{
				DirectoryInfo dir = new DirectoryInfo(this.Config.SelectedInstall.Path);
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

						if(LocalizedCMessage.Show(this.Window, "LauncherNeedsPerms", "Info", TaskDialogStandardIcon.Shield, new DialogButton("Ok", true), "Cancel") == "Ok")
						{
							ProcessBuilder builder = new ProcessBuilder(Assembly.GetEntryAssembly().Location);
							builder.LaunchAsAdmin = true;
							builder.StartProcess();
						}

						Environment.Exit(0);
					}
				}
			}
			else new PopupChooseInstall(this.Window).ShowDialog();
		}

		/// <summary>
		/// Change the active profile and move the mods accordingly
		/// </summary>
		/// <param name="selected"></param>
		public void SwitchProfileTo(Profile selected)
		{
			if(this.Config.Profile != selected)
			{
				Profile oldProfile = this.Config.Profile;

				this.UiManager.Working = true;

				Log.Info("Switching from profile '" + oldProfile + "' to '" + selected + "'.");
				this.WorkManager.ProgressDisplay.ProgressState = ProgressState.Indeterminate;

				try
				{
					if(!oldProfile.IsVanilla)
					{
						string profilePath = oldProfile.ExtFolder;
						GameScanner.ListRootMods(out List<string> modFiles, out List<string> modDirs);
						List<string> dlcMods = GameScanner.ListDlcMods();

						foreach(string dir in modDirs)
							this.WorkManager.QueueJob(new MoveJob(dir, Path.Combine(profilePath, IOUtil.GetRelativePath(dir, this.Config.SelectedInstall.Path))));
						foreach(string file in modFiles)
						{
							if(this.Config.DeleteLogs && file.EndsWith(".log"))
								this.WorkManager.QueueJob(new DeleteJob(file));
							else this.WorkManager.QueueJob(new MoveJob(file, Path.Combine(profilePath, IOUtil.GetRelativePath(file, this.Config.SelectedInstall.Path))));
						}
						foreach(string mod in dlcMods)
							this.WorkManager.QueueJob(new MoveJob(mod, Path.Combine(profilePath, IOUtil.GetRelativePath(mod, this.Config.SelectedInstall.Path))));
					}

					if(!selected.IsVanilla)
					{
						string profilePath = selected.ExtFolder;

						foreach(string entry in Directory.GetFileSystemEntries(profilePath))
							this.WorkManager.QueueJob(new MoveJob(entry, Path.Combine(this.Config.SelectedInstall.Path, Path.GetFileName(entry))));
					}

					this.WorkManager.PerformJobs();

					this.Config.Profile = selected;
					this.Config.Save();
					this.UiManager.UpdateActiveProfile();
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

		public void LaunchGame(bool online)
		{
			Log.Info("Launching game...");
			Profile profile = this.Config.Profile;

			if(this.CanLaunchGame() && this.CheckCurrentProfile())
			{
				if(online && !profile.IsVanilla)
				{
					LocalizedMessage.Show(this.Window, "CantPlayOnline", "Impossible", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
					this.UiManager.Working = false;
					this.UiManager.UIEnabled = true;
				}
				else
				{
					ProcessBuilder builder = new ProcessBuilder();
					builder.WorkingDirectory = this.Config.SelectedInstall.Path;

					if(this.Config.UseRph && File.Exists(Path.Combine(this.Config.SelectedInstall.Path, "RAGEPluginHook.exe")))
					{
						Log.Info("Starting RAGE Plugin Hook process...");
						builder.FilePath = Path.Combine(this.Config.SelectedInstall.Path, "RAGEPluginHook.exe");
					}
					else if(this.Config.SelectedInstall.Type == InstallType.Steam)
					{
						Log.Info("Starting steam game process...");
						builder.FilePath = SteamHelper.ExecutablePath;
						builder.AddArgument("-applaunch 271590");
					}
					else
					{
						Log.Info("Starting game process...");
						builder.FilePath = Path.Combine(this.Config.SelectedInstall.Path, "GTAVLauncher.exe");
					}

					Log.Info("Setting game language to " + this.Config.GtaLanguage);
					builder.AddArgument("-uilanguage " + this.Config.GtaLanguage);

					if(online)
						builder.AddArgument("-StraightIntoFreemode");
					else if(!profile.IsVanilla && this.Config.OfflineMode)
						builder.AddArgument("-scOfflineOnly");

					Log.Info("Executing "+builder);
					builder.StartProcess();

					this.Window.Dispatcher.Invoke(() => this.Window.Visibility = Visibility.Hidden);
					
					if(this.Config.KillLauncher)
					{
						Log.Info("Waiting for game to launch...");
						long start = Environment.TickCount;

						while(true)
						{
							if(Process.GetProcessesByName("GTA5").Length > 0)
							{
								Process process = Process.GetProcessesByName("GTA5")[0];
								if(DateTime.Now - process.StartTime > TimeSpan.FromMilliseconds(GameInitTime))
								{
									Log.Info("Closing Rockstar launcher");
									ProcessUtil.Kill("GTAVLauncher");
									ProcessUtil.Kill("LauncherPatcher");
									ProcessUtil.Kill("Launcher");
									break;
								}
							}
							else Thread.Sleep(GameCheckInterval);

							if(Environment.TickCount - start > GameWaitTimeout)
							{
								Log.Warn("The game wasn't launched properly.");
								break;
							}
						}
					}

					this.CloseLauncher();
				}
			}
			else
			{
				Log.Info("Aborting game launch.");
				this.UiManager.Working = false;
				this.UiManager.UIEnabled = true;
			}
		}

		private void CreateNewProfile(object sender, EventArgs e)
		{
			PopupCreate popup = new PopupCreate(this.Window);
			popup.ShowDialog();
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

				if(this.Config != null)
				{
					report.Append("Install: " + this.Config.SelectedInstall);
					report.Append("Current profile: " + this.Config.Profile);
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
					report.Append("Is working: " + this.UiManager.Working + '\n');
					report.Append("Are buttons enabled: " + this.UiManager.UIEnabled + '\n');
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

		private void OnWindowClosed(object sender, EventArgs e)
		{
			this.Window.News.Dispose();

			if(this.Config.Dirty)
				this.Config.Save();
		}
	}
}
