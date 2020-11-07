using PursuitLib;
using PursuitLib.IO;
using PursuitLib.IO.Serialization;
using PursuitLib.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GTAVModdingLauncher
{
	public class UserConfig : ConfigFile
	{
		public bool KillLauncher { get; set; } = true;
		public bool UseRph { get; set; } = true;
		public bool DeleteLogs { get; set; } = false;
		public bool CheckUpdates { get; set; } = true;
		public bool DisplayNews { get; set; } = true;
		public bool UseLogFile { get; set; } = false;
		public Theme Theme { get; set; } = Theme.Light;
		public string Language { get; set; }
		public string GtaLanguage { get; set; }
		public List<GTAInstall> CustomInstalls { get; set; } = new List<GTAInstall>();
		public GTAInstall SelectedInstall { get; set; }
		public List<Profile> Profiles { get; set; } = new List<Profile>();
		public string CurrentProfile { get; set; }

		[IgnoreData]
		public Profile VanillaProfile => this.Profiles.FirstOrDefault(p => p.IsVanilla);

		[IgnoreData]
		public Profile Profile
		{
			get => this.Profiles.FirstOrDefault(i => i.Name.Equals(this.CurrentProfile, StringComparison.OrdinalIgnoreCase));
			set => this.CurrentProfile = value.Name;
		}

		public UserConfig() : base(Launcher.Instance.UserConfigPath, true)
		{
			if(this.Language == null)
			{
				this.Language = CultureInfo.CurrentCulture.ToString();
				if(!I18n.SupportedLanguages.Contains(this.Language))
					this.Language = "en-US";
			}

			if(this.GtaLanguage == null)
				this.GtaLanguage = this.GetDefaultGtaLanguage();
		}

		public bool ProfileExists(string name)
		{
			return this.Profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) != null;
		}

		public string GetGtaLanguage()
		{
			return this.GtaLanguage ?? this.GetDefaultGtaLanguage();
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
