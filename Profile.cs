using System;
using System.IO;
using PursuitLib.IO.Serialization;

namespace GTAVModdingLauncher
{
	public class Profile
	{
		public string Name { get; set; }
		public bool IsVanilla { get; set; }

		[IgnoreData]
		public string ExtFolder => Path.Combine(Launcher.Instance.UserDirectory, "Profiles", this.Name);

		public Profile(string name) : this(name, false) {}

		public Profile(string name, bool isVanilla)
		{
			this.Name = name;
			this.IsVanilla = isVanilla;
		}

		public override string ToString()
		{
			return this.Name;
		}

		public override bool Equals(object obj)
		{
			return obj is Profile profile && profile.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public static bool operator ==(Profile p1, Profile p2)
		{
			return p1 is null ? p2 is null : p1.Equals(p2);
		}

		public static bool operator !=(Profile p1, Profile p2)
		{
			return !(p1 == p2);
		}
	}
}
