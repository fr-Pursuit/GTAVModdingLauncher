using Microsoft.WindowsAPICodePack.Dialogs;
using PursuitLib;
using PursuitLib.Windows.WPF;
using PursuitLib.Windows.WPF.Modern;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GTAVModdingLauncher.Ui.Popup
{
	public partial class PopupCustomInstall : ModernWindow
	{
		public PopupCustomInstall(Window parent)
		{
			this.InitializeComponent();
			this.SetParent(parent);
		}

		private void Save(object sender, EventArgs e)
		{
			Launcher.Instance.Config.CustomInstalls.Add(new GTAInstall(this.InstallPath.Text, this.SteamVersion.IsChecked.Value ? InstallType.Steam : (this.RetailVersion.IsChecked.Value ? InstallType.Retail : InstallType.Epic)));
			Launcher.Instance.Config.Save();
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
			this.SaveButton.IsEnabled = Directory.Exists(this.InstallPath.Text) && (this.SteamVersion.IsChecked.Value || this.RetailVersion.IsChecked.Value || this.EpicVersion.IsChecked.Value);
		}
	}
}
