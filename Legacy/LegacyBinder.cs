using System;
using System.Runtime.Serialization;

namespace GTAVModdingLauncher.Legacy
{
	public class LegacyBinder : SerializationBinder
	{
		public override Type BindToType(string assemblyName, string typeName)
		{
			if(typeName == "GTAVModdingLauncher.UserSettings")
				return typeof(UserSettings);
			else return null;
		}
	}
}