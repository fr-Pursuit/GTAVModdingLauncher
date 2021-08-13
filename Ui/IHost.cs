using System.Windows.Controls;
using PursuitLib.Windows.WPF;

namespace GTAVModdingLauncher.Ui
{
	public interface IHost
	{
		IWindow Window { get; }

		Control Content { get; set; }

		void Close();
	}
}
