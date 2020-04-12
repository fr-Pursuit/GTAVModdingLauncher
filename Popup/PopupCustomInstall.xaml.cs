using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using PursuitLib;
using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Dialogs;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// Interaction logic for PopupCustomInstall.xaml
	/// </summary>
	public partial class PopupCustomInstall : Window
	{
		public PopupCustomInstall(Window parent)
		{
			this.InitializeComponent();
			this.SetParent(parent);
		}

		private void Save(object sender, EventArgs e)
		{
			Launcher.Instance.Installs.CustomInstalls.Add(new GTAInstall(this.InstallPath.Text, this.SteamVersion.IsChecked.Value ? InstallType.Steam : InstallType.Retail));
			Launcher.Instance.Installs.Save();
			this.Close();
		}

		private void Cancel(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Return)
				this.Save(null, null);
		}

		private void Browse(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog(I18n.Localize("Dialog.Caption", "ChooseFolder"));
			dialog.IsFolderPicker = true;

			if(dialog.ShowDialog(this) == CommonFileDialogResult.Ok)
				this.InstallPath.Text = dialog.FileName;
		}

		private void UpdateSaveButton(object sender, RoutedEventArgs e)
		{
			this.SaveButton.IsEnabled = Directory.Exists(this.InstallPath.Text) && (this.SteamVersion.IsChecked.Value || this.RetailVersion.IsChecked.Value);
		}
	}
}
