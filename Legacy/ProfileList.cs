using System;
using System.Collections.Generic;

namespace GTAVModdingLauncher
{
	/// <summary>
	/// The profile list, saved in profiles.dat
	/// </summary>
	[Serializable]
	public class ProfileList : List<string>
	{
		public int CurrentProfile { get; set; } = 0;

		public bool ProfileExists(string profile)
		{
			profile = profile.ToLower();

			foreach(string name in this)
			{
				if(name.ToLower() == profile)
					return true;
			}

			return false;
		}
	}
}
