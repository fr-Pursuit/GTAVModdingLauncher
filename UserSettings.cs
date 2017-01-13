using PursuitLib.Wpf;
using System;
using System.Globalization;

namespace GTAVModdingLauncher
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
		public bool IntegrityVerified { get; set; }
		public string CustomFolder { get; set; }
		public string CustomGTAFolder { get; set; }
		public string Language { get; set; }
		public string GtaLanguage { get; set; }

		public UserSettings()
		{
			this.UseRph = true;
			this.DeleteLogs = false;
			this.OfflineMode = true;
			this.CheckUpdates = true;
			this.UseLogFile = true;
			this.IntegrityVerified = false;
			this.CustomFolder = null;
			this.CustomGTAFolder = null;

			this.Language = CultureInfo.CurrentCulture.ToString();
			if(!I18n.SupportedLanguages.Contains(this.Language))
				this.Language = "en-US";

			this.GtaLanguage = this.GetDefaultGtaLanguage();
		}

		public string GetProfileFolder()
		{
			return this.CustomFolder != null ? this.CustomFolder : Launcher.Instance.UserDirPath;
		}

		public string GetGtaLanguage()
		{
			return this.GtaLanguage != null ? this.GtaLanguage : this.GetDefaultGtaLanguage();
		}

		private string GetDefaultGtaLanguage()
		{
			string cultureName = CultureInfo.CurrentCulture.ToString();

			if(cultureName == "es_ES")
				return "spanish";

			switch(cultureName.Split('-')[0])
			{
				case "en": return "american";
				case "fr": return "french";
				case "it": return "italian";
				case "de": return "german";
				case "ja": return "japanese";
				case "ru": return "russian";
				case "pl": return "polish";
				case "pt": return "portuguese";
				case "zh": return "chinese";
				case "es": return "mexican";
				case "ko": return "korean";
				default: return "american";
			}
		}
	}
}