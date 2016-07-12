using System.Windows;
using PursuitLib.Wpf;

namespace GTAVModdingLauncher
{
	/// <summary>
	/// Utility class used to show localized MessageBoxes easily
	/// </summary>
	public static class Messages
	{
		private delegate void MessageCallback();

		public static MessageBoxResult Show(Window window, string text, string title, MessageBoxButton buttons, MessageBoxImage icon)
		{
			return MessageBox.Show(window, I18n.Localize("Popup", text), I18n.Localize("Popup.Title", title), buttons, icon);
		}

		public static MessageBoxResult Show(Window window, string text, string title, MessageBoxButton buttons, MessageBoxImage icon, params object[] format)
		{
			return MessageBox.Show(window, I18n.Localize("Popup", text, format), I18n.Localize("Popup.Title", title), buttons, icon);
		}

		public static void NoGTA()
		{
			if(!Launcher.Instance.Window.Dispatcher.CheckAccess())
				Launcher.Instance.Window.Dispatcher.Invoke(new MessageCallback(NoGTA));
			else
			{
				Show(Launcher.Instance.Window, "NoGTAV", "FatalError", MessageBoxButton.OK, MessageBoxImage.Error);
				Launcher.Instance.CloseLauncher();
			}
		}

		public static void GTANotFound()
		{
			if(Launcher.Instance.Window.Dispatcher.CheckAccess())
				Launcher.Instance.Window.Dispatcher.Invoke(new MessageCallback(GTANotFound));
			else
			{
				Show(Launcher.Instance.Window, "GTAVNotFound", "FatalError", MessageBoxButton.OK, MessageBoxImage.Error);
				Launcher.Instance.CloseLauncher();
			}
		}

		public static void UnableToReadProfiles()
		{
			if(Launcher.Instance.Window.Dispatcher.CheckAccess())
				Launcher.Instance.Window.Dispatcher.Invoke(new MessageCallback(UnableToReadProfiles));
			else Show(Launcher.Instance.Window, "ReadProfilesError", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		public static void CantMoveProfiles()
		{
			if(Launcher.Instance.Window.Dispatcher.CheckAccess())
				Launcher.Instance.Window.Dispatcher.Invoke(new MessageCallback(CantMoveProfiles));
			else Show(Launcher.Instance.Window, "CantMoveProfiles", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}
