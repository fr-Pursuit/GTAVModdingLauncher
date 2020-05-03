using System;

namespace GTAVModdingLauncher.Legacy
{
	/// <summary>
	/// The launcher settings, saved in settings.dat
	/// </summary>
	[Serializable]
	public class UserSettings
	{
		public bool UseRph { get; set; }
		public bool DeleteLogs { get; set; }
		public bool OfflineMode { get; set; }
		public bool CheckUpdates { get; set; }
		public bool UseLogFile { get; set; }
		public string CustomFolder { get; set; }
		public string CustomGTAFolder { get; set; }
		public string Language { get; set; }
		public string GtaLanguage { get; set; }
	}
}