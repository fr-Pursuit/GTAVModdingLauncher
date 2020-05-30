using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Modern.Dialogs.Base;
using System.Windows;
using System.Windows.Controls;

namespace GTAVModdingLauncher.Ui.Dialogs
{
	public partial class HostDialog : ModernDialogBase, IHost
	{
		public new IWindow Window => this;

		public new Control Content
		{
			get => this.MainContent.Children.Count > 0 ? (Control)this.MainContent.Children[0] : null;
			set
			{
				this.MainContent.Children.Clear();
				if(value != null)
					this.MainContent.Children.Add(value);
			}
		}

		public HostDialog(Window parent)
		{
			this.Parent = (WPFWindow)parent;
			this.InitializeComponent();
		}

		private void CloseDialog(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
