using PursuitLib.Windows.WPF.Modern;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using PursuitLib.Windows.WPF;

namespace GTAVModdingLauncher.Ui
{
	public partial class HostWindow : ModernWindow, IHost
	{
		public IWindow Window => (WPFWindow)this;

		public new Control Content
		{
			get => (Control)base.Content;
			set => base.Content = value;
		}

		public HostWindow()
		{
			this.InitializeComponent();
			StyleUtil.SetFlyoutTheme(this, FlyoutTheme.Accent);
		}
	}
}
