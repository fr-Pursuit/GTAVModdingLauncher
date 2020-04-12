using PursuitLib;
using PursuitLib.Windows.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Mime;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PursuitLib.IO;

namespace GTAVModdingLauncher
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
			get { return this.windowTitle; }
			set
			{
				if(this.windowTitle != value)
				{
					this.windowTitle = value;
					this.NotifyPropertyChanged(nameof(WindowTitle));
				}
			}
		}

		private bool buttonsEnabled = false;
		public bool ButtonsEnabled
		{
			get { return this.buttonsEnabled; }
			set
			{
				if(this.buttonsEnabled != value)
				{
					this.buttonsEnabled = value;
					this.NotifyPropertyChanged(nameof(ButtonsEnabled));
				}
			}
		}

		private bool canPlayOnline = false;
		public bool CanPlayOnline
		{
			get { return this.canPlayOnline; }
			set
			{
				if(this.canPlayOnline != value)
				{
					this.canPlayOnline = value;
					this.NotifyPropertyChanged(nameof(CanPlayOnline));
				}
			}
		}

		private bool canApplyChanges = false;
		public bool CanApplyChanges
		{
			get { return this.canApplyChanges; }
			set
			{
				if(this.canApplyChanges != value)
				{
					this.canApplyChanges = value;
					this.NotifyPropertyChanged(nameof(CanApplyChanges));
				}
			}
		}

		public ObservableCollection<string> Profiles { get; internal set; } = new ObservableCollection<string>();

		private int selectedProfile = 0;
		public int SelectedProfile
		{
			get { return this.selectedProfile; }
			set
			{
				if(this.selectedProfile != value)
				{
					this.selectedProfile = value;
					this.NotifyPropertyChanged(nameof(SelectedProfile));
				}
			}
		}

		public int LastSelectedProfile { get; set; } = 0;

		private string launcherVersion;
		public string LauncherVersion
		{
			get { return this.launcherVersion; }
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
			get { return this.gtaVersion; }
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
			get { return this.gtaType; }
			set
			{
				if(this.gtaType != value)
				{
					this.gtaType = value;
					this.NotifyPropertyChanged(nameof(GtaType));
				}
			}
		}

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
			this.Profiles.Add(I18n.Localize("Random", "Loading"));
		}

		public void randomizeBackground()
		{
			List<ResourcePath> backgrounds = ResourceManager.GetAllResources("*.png");
			using(Stream img = ResourceManager.GetResourceStream(backgrounds[new Random().Next(backgrounds.Count)]))
			{
				MemoryStream mem = new MemoryStream();
				IOUtil.CopyStream(img, mem);
				this.window.ImgBg.Source = BitmapFrame.Create(mem, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
			}
		}

		private void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
