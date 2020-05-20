using PursuitLib;
using PursuitLib.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAPICodePack.Dialogs;
using PursuitLib.Windows.WPF.Dialogs;

namespace GTAVModdingLauncher
{
	/// <summary>
	/// This class is used to scan the game for mods, and to perform integrity checks
	/// </summary>
	public static class GameScanner
	{
		/// <summary>
		/// The list of all files and folders located at the root of a vanilla copy of GTA V
		/// </summary>
		private static List<GameFile> gameManifest = new List<GameFile>();

		public static void Init()
		{
			Log.Info("Initializing the game scanner...");

			if(File.Exists("GameManifest.xml"))
				gameManifest = new XMLFile<List<GameFile>>("GameManifest.xml").Content;
			else
			{
				Log.Error("Fatal error: Unable to find game manifest");
				LocalizedMessage.Show("ManifestNotFound", "FatalError", TaskDialogStandardIcon.Error, TaskDialogStandardButtons.Ok);
				Environment.Exit(1);
			}
		}

		public static bool IsGTAModded()
		{
			GTAInstall install = Launcher.Instance.Config.SelectedInstall;

			foreach(string file in Directory.GetFileSystemEntries(install.Path))
			{
				if(!IsVanillaEntry(Path.GetFileName(file)))
					return true;
			}

			if(Directory.Exists(Path.Combine(install.Path, "update\\x64\\dlcpacks")))
			{
				foreach(string file in Directory.GetFileSystemEntries(Path.Combine(install.Path, "update\\x64\\dlcpacks")))
				{
					if(Directory.Exists(file))
					{
						string filename = Path.GetFileName(file).ToLower();
						if(!filename.StartsWith("mp") && !filename.StartsWith("patchday"))
							return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// List all mods located at the root of the game's directory
		/// </summary>
		public static void ListRootMods(out List<string> files, out List<string> dirs)
		{
			GTAInstall install = Launcher.Instance.Config.SelectedInstall;

			if(install.Path != null)
			{
				if(Directory.Exists(install.Path))
				{
					files = new List<string>();
					dirs = new List<string>();

					foreach(string file in Directory.EnumerateFileSystemEntries(install.Path))
					{
						if(!IsVanillaEntry(Path.GetFileName(file)))
						{
							if(File.Exists(file))
								files.Add(file);
							else dirs.Add(file);
						}
					}
				}
				else throw new ApplicationException("GtaPath doesn't exist.");
			}
			else throw new ApplicationException("GtaPath is not set.");
		}

		/// <summary>
		/// List all mods located in update\x64\dlcpacks\
		/// </summary>
		public static List<string> ListDlcMods()
		{
			GTAInstall install = Launcher.Instance.Config.SelectedInstall;

			if(install.Path != null)
			{
				if(Directory.Exists(install.Path))
				{
					List<string> mods = new List<string>();

					if(Directory.Exists(Path.Combine(install.Path, "update\\x64\\dlcpacks")))
					{
						foreach(string file in Directory.GetFileSystemEntries(Path.Combine(install.Path, "update\\x64\\dlcpacks")))
						{
							if(Directory.Exists(file))
							{
								string filename = Path.GetFileName(file).ToLower();
								if(!filename.StartsWith("mp") && !filename.StartsWith("patchday"))
									mods.Add(file);
							}
						}
					}

					return mods;
				}
				throw new ApplicationException("GtaPath doesn't exist.");
			}
			else throw new ApplicationException("GtaPath is not set.");
		}

		private static bool IsVanillaEntry(string name)
		{
			return gameManifest.FirstOrDefault(f => (f.Type == null || f.Type.Value == Launcher.Instance.Config.SelectedInstall.Type) && String.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) != null;
		}
	}
}
