﻿using GTAVModdingLauncher.Ui;
using GTAVModdingLauncher.Ui.Dialogs;
using GTAVModdingLauncher.Task;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PursuitLib;
using PursuitLib.Extensions;
using PursuitLib.IO;
using PursuitLib.IO.PPF;
using PursuitLib.Threading.Tasks;
using PursuitLib.Windows;
using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Dialogs;
using PursuitLib.Windows.WPF.Modern;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
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
		public ProgressManager ProgressManager { get; private set; }


		private bool windowInitialized = false;

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
			this.Config = new UserConfig();

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
			this.Window.Activated += OnWindowActivated;
			this.Window.Closing += OnWindowClosing;
			this.Window.Closed += OnWindowClosed;
			this.Window.AboutButton.Click += ShowAboutPopup;
			this.Window.SettingsButton.Click += (s, a) => this.Window.Settings.Open();
			this.Window.CreateButton.Click += (s, a) => new CreateDialog(this.Window).Show();
			this.UiManager = new UIManager(this.Window);
			this.UiManager.WindowTitle = this.DisplayName;

			this.ProgressManager = new ProgressManager(this.TaskManager);
			this.ProgressManager.AddMonitor((WPFProgressBar)this.Window.Progress);

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
				this.TaskManager.Run(CheckUpdates);
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
					LocalizedMessage.Show("GTANotFound", "Info", DialogIcon.Information, DialogButtons.Ok);
					HostWindow host = new HostWindow();
					host.Content = new ChooseInstallDialog(host);
					host.ShowDialog();

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
				LocalizedMessage.Show("InstalledInGTA", "Error", DialogIcon.Error, DialogButtons.Ok);
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

				using HttpWebResponse response = request.GetResponse() as HttpWebResponse;

				if(response.StatusCode == HttpStatusCode.OK)
				{
					using StreamReader streamReader = new StreamReader(response.GetResponseStream());
					using JsonReader reader = new JsonTextReader(streamReader);
					JObject obj = JObject.Load(reader);

					if(new Version(obj["tag_name"].ToString()) > Version)
					{
						Log.Info("New update found (" + obj["name"] + ')');
						return obj;
					}
				}
				else Log.Warn("Unable to check for updates. Response code was " + response.StatusCode + " (" + response.StatusDescription + ')');
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
				if(MessageDialog.Show(window, I18n.Localize("Dialog", "Update", obj["name"], obj["body"].ToString().Replace("\\", "")), I18n.Localize("Dialog.Caption", "Update"), DialogIcon.Information, DialogButtons.Yes | DialogButtons.No) == DialogStandardResult.Yes)
				{
					ProcessUtil.Execute(obj["assets"][0]["browser_download_url"].ToString());
					this.CloseLauncher();
				}
			}
			else this.Window.Dispatcher.Invoke(() => ShowUpdatePopup(window, obj));
		}

		/// <summary>
		/// Called whenever mods are present in the vanilla profile.
		/// Asks the user if they want to delete the mods, or create a new profile with these mods
		/// </summary>
		/// <returns>true if the user chose a valid outcome, false if the user chose to cancel</returns>
		private bool HandleVanillaMods()
		{
			Log.Warn("GTA V is modded, but the vanilla profile is selected!");

			DialogStandardResult result = LocalizedMessage.Show(this.Window, "ModsOnVanilla", "Warn", DialogIcon.Warning, DialogButtons.Yes | DialogButtons.No | DialogButtons.Cancel);

			if(result == DialogStandardResult.Yes)
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
			else if(result == DialogStandardResult.No)
			{
				new PerformTaskDialog(this.ProgressManager, new DeleteMods()).Show(this.ProgressManager);
				return true;
			}
			else return false;
		}

		/// <summary>
		/// Ensure the vanilla profile is unmodded, and update the current profile's files
		/// </summary>
		/// <returns>true if the game can be launched, false otherwise</returns>
		private bool UpdateCurrentProfile()
		{
			Profile currentProfile = this.Config.Profile;

			if(currentProfile.IsVanilla && GameScanner.IsGTAModded())
			{
				if(this.HandleVanillaMods())
					currentProfile = this.Config.Profile;
				else return false;
			}
			
			if(!currentProfile.IsVanilla && Directory.Exists(currentProfile.ExtFolder))
			{
				string path = currentProfile.ExtFolder;

				if(Directory.GetFileSystemEntries(path).GetLength(0) != 0)
				{
					DialogStandardResult result = LocalizedMessage.Show(this.Window, "UpdateProfile", "UpdateProfile", DialogIcon.Information, DialogButtons.Yes | DialogButtons.No);

					if(result == DialogStandardResult.Yes)
					{
						GameScanner.ListRootMods(out List<string> modFiles, out List<string> modDirs);
						List<string> dlcMods = GameScanner.ListDlcMods();

						TaskSequence sequence = new TaskSequence();

						foreach(string dir in modDirs)
						{
							sequence.AddTask(new MoveTask(dir, Path.Combine(path, IOUtil.GetRelativePath(dir, this.Config.SelectedInstall.Path))));
						}
						foreach(string file in modFiles)
						{
							if(this.Config.DeleteLogs && file.EndsWith(".log"))
								sequence.AddTask(new DeleteTask(file));
							else sequence.AddTask(new MoveTask(file, Path.Combine(path, IOUtil.GetRelativePath(file, this.Config.SelectedInstall.Path))));
						}
						foreach(string mod in dlcMods)
							sequence.AddTask(new MoveTask(mod, Path.Combine(path, IOUtil.GetRelativePath(mod, this.Config.SelectedInstall.Path))));

						new PerformTaskDialog(this.ProgressManager, sequence).Show();
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
					LocalizedMessage.Show(this.Window, "NoExecutable", "Error", DialogIcon.Error, DialogButtons.Ok);
					return false;
				}
			}
			else if(this.Config.SelectedInstall.Type == InstallType.Steam)
			{
				if(SteamHelper.IsAvailable)
					return true;
				else
				{
					LocalizedMessage.Show(this.Window, "NoSteam", "Error", DialogIcon.Error, DialogButtons.Ok);
					return false;
				}
			}
			else if(this.Config.SelectedInstall.Type == InstallType.Epic)
				return true;
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
				int i = 1;
				while(this.Config.ProfileExists(baseName + ' ' + i))
					i++;
				return baseName + ' ' + i;
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
			else if(this.Config.SelectedInstall.Type == InstallType.Epic)
				this.UiManager.GtaType = I18n.Localize("Label", "EpicVersion");
			else this.UiManager.GtaType = "";

			if(Directory.Exists(this.Config.SelectedInstall.Path))
			{
				DirectoryInfo dir = new DirectoryInfo(this.Config.SelectedInstall.Path);
				DirectorySecurity sec = dir.GetAccessControl();

				IdentityReference id = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
				bool hasPerms = false;

				foreach(FileSystemAccessRule rule in sec.GetAccessRules(true, true, typeof(SecurityIdentifier)))
				{
					if(rule.IdentityReference == id && rule.FileSystemRights == FileSystemRights.FullControl && rule.InheritanceFlags == (InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit) && rule.AccessControlType == AccessControlType.Allow)
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
						sec.SetAccessRule(new FileSystemAccessRule(id, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
						dir.SetAccessControl(sec);
					}
					else
					{
						Log.Info("Asking for elevated privileges...");

						if(LocalizedCMessage.Show(this.Window, "LauncherNeedsPerms", "Info", DialogIcon.Shield, new DialogButton("Ok", true), "Cancel") == "Ok")
						{
							ProcessBuilder builder = new ProcessBuilder(Process.GetCurrentProcess().MainModule.FileName);
							builder.UseShell = true;
							builder.LaunchAsAdmin = true;
							builder.StartProcess();
						}

						Environment.Exit(0);
					}
				}
			}
			else
			{
				HostDialog host = new HostDialog(this.Window);
				host.Content = new ChooseInstallDialog(host);
				host.Show();
			}
		}

		/// <summary>
		/// Change the active profile and move the mods accordingly
		/// </summary>
		/// <param name="selected"></param>
		/// <returns>True if the profile was successfully changed, false otherwise</returns>
		public bool SwitchProfileTo(Profile selected)
		{
			if(this.Config.Profile != selected)
			{
				Profile oldProfile = this.Config.Profile;

				this.UiManager.Working = true;

				if(oldProfile.IsVanilla && GameScanner.IsGTAModded())
				{
					if(this.HandleVanillaMods())
						oldProfile = this.Config.Profile;
					else return false;
				}

				Log.Info("Switching from profile '" + oldProfile + "' to '" + selected + "'.");

				try
				{
					TaskSequence sequence = new TaskSequence();

					if(!oldProfile.IsVanilla)
					{
						string profilePath = oldProfile.ExtFolder;
						GameScanner.ListRootMods(out List<string> modFiles, out List<string> modDirs);
						List<string> dlcMods = GameScanner.ListDlcMods();

						foreach(string dir in modDirs)
							sequence.AddTask(new MoveTask(dir, Path.Combine(profilePath, IOUtil.GetRelativePath(dir, this.Config.SelectedInstall.Path))));
						foreach(string file in modFiles)
						{
							if(this.Config.DeleteLogs && file.EndsWith(".log"))
								sequence.AddTask(new DeleteTask(file));
							else sequence.AddTask(new MoveTask(file, Path.Combine(profilePath, IOUtil.GetRelativePath(file, this.Config.SelectedInstall.Path))));
						}
						foreach(string mod in dlcMods)
							sequence.AddTask(new MoveTask(mod, Path.Combine(profilePath, IOUtil.GetRelativePath(mod, this.Config.SelectedInstall.Path))));
					}

					if(!selected.IsVanilla)
					{
						string profilePath = selected.ExtFolder;

						foreach(string entry in Directory.GetFileSystemEntries(profilePath))
							sequence.AddTask(new MoveTask(entry, Path.Combine(this.Config.SelectedInstall.Path, Path.GetFileName(entry))));
					}

					this.ProgressManager.Run(sequence);

					this.Config.Profile = selected;
					this.Config.Save();
					this.UiManager.UpdateActiveProfile();
				}
				catch(IOException e)
				{
					Log.Error(e.ToString());
					LocalizedMessage.Show(this.Window, "ProfileSwitchError", "FatalError", DialogIcon.Error, DialogButtons.Ok);
					Process.GetCurrentProcess().Kill();
				}

				this.UiManager.Working = false;
				return true;
			}
			else return true;
		}

		public void LaunchGame(bool online)
		{
			Log.Info("Launching game...");
			Profile profile = this.Config.Profile;

			if(this.CanLaunchGame() && this.UpdateCurrentProfile())
			{
				if(online && !profile.IsVanilla)
				{
					LocalizedMessage.Show(this.Window, "CantPlayOnline", "Impossible", DialogIcon.Error, DialogButtons.Ok);
					this.UiManager.Working = false;
					this.UiManager.UIEnabled = true;
				}
				else
				{
					ProcessBuilder builder = new ProcessBuilder();
					builder.UseShell = true;
					builder.WorkingDirectory = this.Config.SelectedInstall.Path;

					if(this.Config.UseRph && this.Config.SelectedInstall.Type != InstallType.Epic && File.Exists(Path.Combine(this.Config.SelectedInstall.Path, "RAGEPluginHook.exe")))
					{
						Log.Info("Starting RAGE Plugin Hook process...");
						builder.FilePath = Path.Combine(this.Config.SelectedInstall.Path, "RAGEPluginHook.exe");
					}
					else if(this.Config.SelectedInstall.Type == InstallType.Steam)
					{
						Log.Info("Starting steam game process...");
						builder.FilePath = SteamHelper.ExecutablePath;
						builder.AddArgument("-applaunch 271590");

						if(!File.Exists(builder.FilePath))
						{
							builder.FilePath = null;
							Log.Error("Error: Steam.exe not found");
							LocalizedMessage.Show(this.Window, "SteamNotFound", "Error", DialogIcon.Error, DialogButtons.Ok);
						}
					}
					else if(this.Config.SelectedInstall.Type == InstallType.Retail)
					{
						Log.Info("Starting retail game process...");
						builder.FilePath = Path.Combine(this.Config.SelectedInstall.Path, "PlayGTAV.exe");

						if(!File.Exists(builder.FilePath))
						{
							builder.FilePath = null;
							Log.Error("Error: PlayGTAV.exe not found");
							LocalizedMessage.Show(this.Window, "PlayGTANotFound", "Error", DialogIcon.Error, DialogButtons.Ok);
						}
					}
					else if(this.Config.SelectedInstall.Type == InstallType.Epic)
					{
						Log.Info("Starting epic game process...");
						builder.FilePath = "com.epicgames.launcher://apps/9d2d0eb64d5c44529cece33fe2a46482?action=launch&silent=true";
					}

					if(builder.FilePath != null)
					{
						Log.Info("Setting game language to " + this.Config.GtaLanguage);
						builder.AddArgument("-uilanguage " + this.Config.GtaLanguage);

						if(online)
							builder.AddArgument("-StraightIntoFreemode");

						Log.Info("Executing " + builder);
						builder.StartProcess();

						this.Window.Dispatcher.Invoke(() => this.Window.Visibility = Visibility.Hidden);

						if(this.Config.KillLauncher)
						{
							Log.Info("Waiting for game to launch...");
							long start = Environment.TickCount;

							while(true)
							{
								Process[] processes = Process.GetProcessesByName("GTA5");

								if(processes.Length > 0)
								{
									Process process = processes[0];
									if(DateTime.Now - process.StartTime > TimeSpan.FromMilliseconds(GameInitTime))
									{
										Log.Info("Closing Rockstar launcher");
										ProcessUtil.Kill("PlayGTAV");
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
					else
					{
						this.UiManager.Working = false;
						this.UiManager.UIEnabled = true;
					}
				}
			}
			else
			{
				Log.Info("Aborting game launch.");
				this.UiManager.Working = false;
				this.UiManager.UIEnabled = true;
			}
		}

		public void CloseLauncher()
		{
			this.closeRequested = true;

			if(Application.Current.CheckAccess())
				Application.Current.Shutdown();
			else Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
		}

		private void OnWindowActivated(object sender, EventArgs e)
		{
			if(!this.windowInitialized)
			{
				this.ProgressManager.AddMonitor(new TaskBarProgress());
				this.windowInitialized = true;
			}
		}

		private void ShowAboutPopup(object sender, EventArgs e)
		{
			MessageDialog.Show(this.Window, I18n.Localize("Dialog", "About", this.Version), I18n.Localize("Dialog.Caption", "About"), DialogIcon.Information, DialogButtons.Ok);
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
					report.Append("Install: " + this.Config.SelectedInstall + '\n');
					report.Append("Current profile: " + this.Config.Profile + '\n');
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
			if(!this.closeRequested && this.ProgressManager.HasAnyTask)
			{
				e.Cancel = true;
				LocalizedMessage.Show(this.Window, "LauncherWorking", "Impossible", DialogIcon.Information, DialogButtons.Ok);
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
