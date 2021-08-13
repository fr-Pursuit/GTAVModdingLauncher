using PursuitLib.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;

namespace GTAVModdingLauncher.Ui
{
	/// <summary>
	/// Bridge between <see cref="Launcher"/> and <see cref="MainWindow"/>
	/// </summary>
	public class UIManager : INotifyPropertyChanged
	{
		private MainWindow window;

		public event PropertyChangedEventHandler PropertyChanged;

		private string windowTitle = "GTA V Modding Launcher";
		public string WindowTitle
		{
			get => this.windowTitle;
			set
			{
				if(this.windowTitle != value)
				{
					this.windowTitle = value;
					this.NotifyPropertyChanged(nameof(WindowTitle));
				}
			}
		}

		private bool uiEnabled = false;
		public bool UIEnabled
		{
			get => this.uiEnabled;
			set
			{
				if(this.uiEnabled != value)
				{
					this.uiEnabled = value;
					this.NotifyPropertyChanged(nameof(UIEnabled));
				}
			}
		}

		private string launcherVersion;
		public string LauncherVersion
		{
			get => this.launcherVersion;
			set
			{
				if(this.launcherVersion != value)
				{
					this.launcherVersion = value;
					this.NotifyPropertyChanged(nameof(LauncherVersion));
				}
			}
		}

		private string gtaVersion;
		public string GtaVersion
		{
			get => this.gtaVersion;
			set
			{
				if(this.gtaVersion != value)
				{
					this.gtaVersion = value;
					this.NotifyPropertyChanged(nameof(GtaVersion));
				}
			}
		}

		private string gtaType;
		public string GtaType
		{
			get => this.gtaType;
			set
			{
				if(this.gtaType != value)
				{
					this.gtaType = value;
					this.NotifyPropertyChanged(nameof(GtaType));
				}
			}
		}

		private bool newsVisible = false;
		public bool NewsVisible
		{
			get => this.newsVisible;
			set
			{
				if(this.newsVisible != value)
				{
					this.newsVisible = value;
					this.NotifyPropertyChanged(nameof(NewsVisibility));
				}
			}
		}
		public Visibility NewsVisibility => this.NewsVisible ? Visibility.Visible : Visibility.Hidden;

		private bool working;
		public bool Working
		{
			get => this.working;
			set
			{
				if(this.working != value)
				{
					this.working = value;
					this.NotifyPropertyChanged(nameof(ProgressBarVisibility));
				}
			}
		}
		public Visibility ProgressBarVisibility => this.Working ? Visibility.Visible : Visibility.Hidden;

		public UIManager(MainWindow window)
		{
			this.window = window;
		}

		public void UpdateProfiles()
		{
			if(this.window.CheckAccess())
			{
				this.window.Profiles.Entries.Clear();

				foreach(Profile profile in Launcher.Instance.Config.Profiles)
					this.window.Profiles.Entries.Add(new ProfileEntry(profile));

				this.UpdateActiveProfile();
			}
			else this.window.Dispatcher.Invoke(UpdateProfiles);
		}

		public void AddProfile(Profile profile)
		{
			if(this.window.CheckAccess())
				this.window.Profiles.Entries.Add(new ProfileEntry(profile));
			else this.window.Dispatcher.Invoke(() => this.AddProfile(profile));
		}

		public void RemoveProfile(ProfileEntry entry)
		{
			if(this.window.CheckAccess())
				this.window.Profiles.Entries.Remove(entry);
			else this.window.Dispatcher.Invoke(() => this.RemoveProfile(entry));
		}

		public void UpdateActiveProfile()
		{
			if(this.window.CheckAccess())
			{
				foreach(ProfileEntry entry in this.window.Profiles.Entries)
					entry.Update();
			}
			else this.window.Dispatcher.Invoke(UpdateActiveProfile);
		}

		public void RandomizeBackground()
		{
			List<ResourcePath> backgrounds = ResourceManager.GetAllResources("*.png");

			if(backgrounds.Count > 0)
			{
				using(Stream img = ResourceManager.GetResourceStream(backgrounds[new Random().Next(backgrounds.Count)]))
				{
					MemoryStream mem = new MemoryStream();
					IOUtil.CopyStream(img, mem);
					this.window.ImgBg.Source = BitmapFrame.Create(mem, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
				}
			}
		}

		private void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
