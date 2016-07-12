using PursuitLib.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

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
					this.NotifyPropertyChanged("WindowTitle");
				}
			}
		}

		private string windowBackground = "/GTAVModdingLauncher;component/resources/bg" + new Random().Next(16) + ".png";
		public string WindowBackground
		{
			get { return this.windowBackground; }
			set
			{
				if(this.windowBackground != value)
				{
					this.windowBackground = value;
					this.NotifyPropertyChanged("WindowBackground");
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
					this.NotifyPropertyChanged("ButtonsEnabled");
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
					this.NotifyPropertyChanged("CanPlayOnline");
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
					this.NotifyPropertyChanged("SelectedProfile");
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
					this.NotifyPropertyChanged("LauncherVersion");
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
					this.NotifyPropertyChanged("GtaVersion");
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
					this.NotifyPropertyChanged("GtaType");
				}
			}
		}

		public bool Working
		{
			get { return this.window.Progress.Visibility == Visibility.Visible; }
			set { this.window.Progress.Visibility = value ? Visibility.Visible : Visibility.Hidden; }
		}

		public double ProgressMaximum
		{
			get { return this.window.Progress.Maximum; }
			set { this.window.Progress.Maximum = value; }
		}

		public double Progress
		{
			get { return this.window.Progress.Value; }
			set { this.window.Progress.Value = value; }
		}

		public bool ProgressIndeterminate
		{
			get { return this.window.Progress.IsIndeterminate; }
			set { this.window.Progress.IsIndeterminate = value; }
		}

		private void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public UIManager(MainWindow window)
		{
			this.window = window;
			this.Profiles.Add(I18n.Localize("Random", "Loading"));
		}
	}
}
