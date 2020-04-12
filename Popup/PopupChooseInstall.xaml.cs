using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PursuitLib;
using PursuitLib.Windows.WPF;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// Interaction logic for PopupChooseInstall.xaml
	/// </summary>
	public partial class PopupChooseInstall : Window
	{
		public PopupChooseInstall() : this(null) {}

		public PopupChooseInstall(Window parent)
		{
			this.InitializeComponent();

			if(parent != null)
				this.SetParent(parent);

			this.RefreshList();
		}

		private void RefreshList()
		{
			this.List.Items.Clear();

			foreach(GTAInstall install in GTAInstall.FindInstalls())
				this.List.Items.Add(new InstallEntry(install));

			foreach(GTAInstall install in Launcher.Instance.Installs.CustomInstalls)
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
					Launcher.Instance.Installs.Selected = selected.Install;
					Launcher.Instance.Installs.Save();
					this.Close();
					Launcher.Instance.UpdateGameInfo();
				}
				else
				{
					new PopupCustomInstall(this).ShowDialog();
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
					Launcher.Instance.Installs.CustomInstalls.Remove(entry.Install);
					Launcher.Instance.Installs.Save();
					this.RefreshList();
				};

				this.List.ContextMenu.Items.Add(item);
			}
			else this.List.ContextMenu = null;
		}
	}
}
