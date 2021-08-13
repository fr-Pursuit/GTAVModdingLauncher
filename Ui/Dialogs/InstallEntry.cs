using PursuitLib;

namespace GTAVModdingLauncher.Ui.Dialogs
{
	public class InstallEntry
	{
		public GTAInstall Install { get; private set; }

		public string Icon
		{
			get
			{
				if(this.Install != null)
				{
					if(this.Install.Type == InstallType.Steam)
						return "/GTAVModdingLauncher;component/resources/steam.png";
					else if(this.Install.Type == InstallType.Retail)
						return "/GTAVModdingLauncher;component/resources/retail.png";
					else if(this.Install.Type == InstallType.Epic)
						return "/GTAVModdingLauncher;component/resources/epic.png";
					else return "";
				}
				else return "/GTAVModdingLauncher;component/resources/new.png";
			}
		}

		public string Text
		{
			get
			{
				if(this.Install != null)
					return this.Install.Path;
				else return I18n.Localize("Random", "NewInstall");
			}
		}

		public InstallEntry() : this(null) {}

		public InstallEntry(GTAInstall install)
		{
			this.Install = install;
		}
	}
}
