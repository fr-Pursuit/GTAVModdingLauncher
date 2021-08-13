using PursuitLib.Windows.WPF.Modern;

namespace GTAVModdingLauncher.Ui
{
	public partial class MainWindow : ModernWindow
	{
		public MainWindow()
		{
			this.InitializeComponent();
			Launcher.Instance.InitUI(this);
			this.DataContext = Launcher.Instance.UiManager;
		}
	}
}
