using System;
using System.Windows.Controls;
using System.Windows.Threading;
using GTANews;
using PursuitLib.Windows.WPF;

namespace GTAVModdingLauncher.Ui
{
	public partial class NewsBox : UserControl, IDisposable
	{
		private const int ReadTime = 30000;

		public bool CycleStarted => this.timer != null;
		public ImageLoader ImgLoader { get; private set; }
		private DispatcherTimer timer = null;
		private NewsList list = null;
		private NewsEntry next;
		private int index;

		public NewsBox()
		{
			this.InitializeComponent();
			this.ImgLoader = new ImageLoader(this);
		}

		public void StartCycle()
		{
			if(!this.CycleStarted)
			{
				if(this.CheckAccess())
					Launcher.Instance.WorkManager.StartWork(Initialize);
				else this.Dispatcher.Invoke(StartCycle);
			}
		}

		public void StopCycle()
		{
			if(this.CycleStarted)
			{
				if(this.CheckAccess())
				{
					this.timer?.Stop();
					this.timer = null;
				}
				else this.Dispatcher.Invoke(StopCycle);
			}
		}

		private void Initialize()
		{
			if(this.list == null)
			{
				this.list = new NewsList();

				try
				{
					this.list.Load();
					this.index = 0;

				}
				catch(Exception) {}
			}

			this.LoadNext();

			this.Dispatcher.Invoke(Cycle);
			this.Dispatcher.Invoke(StartTimer);
		}

		private void LoadNext()
		{
			if(this.list != null && this.list.Count > 0)
			{
				try
				{
					News news = this.list[this.index];
					news.Load(Launcher.Instance.Config.Language.Split('-')[0]);
					this.Dispatcher.Invoke(() => this.next = new NewsEntry(this, news));
					this.index = (this.index + 1) % this.list.Count;
				}
				catch(Exception) { }
			}
		}

		private void StartTimer()
		{
			this.timer = new DispatcherTimer(TimeSpan.FromMilliseconds(ReadTime), DispatcherPriority.Background, (s, a) => this.Cycle(), this.Dispatcher);
			this.timer.Start();
		}

		private void Cycle()
		{
			if(!Launcher.Instance.UiManager.NewsVisible)
				Launcher.Instance.UiManager.NewsVisible = true;

			if(this.next != null)
				this.Content.Content = this.next;
			Launcher.Instance.WorkManager.StartWork(LoadNext);
		}

		public void Dispose()
		{
			this.timer?.Stop();
			this.ImgLoader?.Dispose();
		}
	}
}
