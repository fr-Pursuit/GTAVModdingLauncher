using System.Windows;

namespace GTAVModdingLauncher
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			Launcher launcher = new Launcher(this);
			this.InitializeComponent();
			launcher.InitUI();
			this.DataContext = launcher.UiManager;
		}
	}
}
