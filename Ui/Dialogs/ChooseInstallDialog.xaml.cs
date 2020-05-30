using PursuitLib;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GTAVModdingLauncher.Ui.Dialogs
{
	public partial class ChooseInstallDialog : UserControl
	{
		private readonly IHost host;


		public ChooseInstallDialog(IHost host)
		{
			this.host = host;
			this.InitializeComponent();
			this.RefreshList();
		}

		private void RefreshList()
		{
			this.List.Items.Clear();

			foreach(GTAInstall install in GTAInstall.FindInstalls())
				this.List.Items.Add(new InstallEntry(install));

			foreach(GTAInstall install in Launcher.Instance.Config.CustomInstalls)
				this.List.Items.Add(new InstallEntry(install));

			this.List.Items.Add(new InstallEntry()); //New button
		}

		private void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(e.ChangedButton == MouseButton.Left)
			{
				InstallEntry selected = this.List.SelectedItem as InstallEntry;

				if(selected?.Install != null)
				{
					if(selected.Install.Path != null && Directory.Exists(selected.Install.Path))
					{
						Launcher.Instance.Config.SelectedInstall = selected.Install;
						Launcher.Instance.Config.Save();
						this.host.Close();
						Launcher.Instance.UpdateGameInfo();
					}
				}
				else
				{
					new CustomInstallDialog(this.host.Window).Show();
					this.RefreshList();
				}
			}
		}

		private void OnItemRightClick(object sender, MouseButtonEventArgs e)
		{
			InstallEntry entry = ((FrameworkElement) e.OriginalSource).DataContext as InstallEntry;

			if(entry?.Install != null && entry.Install.IsCustom)
			{
				this.List.ContextMenu = new ContextMenu();
				MenuItem item;

				item = new MenuItem();
				item.Header = I18n.Localize("ContextMenu", "Delete");
				item.Click += (o, args) =>
				{
					Launcher.Instance.Config.CustomInstalls.Remove(entry.Install);
					Launcher.Instance.Config.Save();
					this.RefreshList();
				};

				this.List.ContextMenu.Items.Add(item);
			}
			else this.List.ContextMenu = null;
		}
	}
}
