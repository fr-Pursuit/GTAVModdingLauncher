using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PursuitLib;
using System.Security.Cryptography;

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
		private static List<string> vanillaEntries = new List<string>();
		/// <summary>
		/// A list of all x64.rpf files and their hashes
		/// </summary>
		private static Dictionary<string, string> fileHashes = new Dictionary<string, string>();

		public static int CheckableFileCount
		{
			get { return fileHashes.Count; }
		}

		public static void Init()
		{
			Log.Info("Initializing the game scanner...");
			Log.Info("Registering common files...");

			vanillaEntries.Add("readme");
			vanillaEntries.Add("update");
			vanillaEntries.Add("x64");
			vanillaEntries.Add("bink2w64.dll");
			vanillaEntries.Add("common.rpf");
			vanillaEntries.Add("d3dcompiler_46.dll");
			vanillaEntries.Add("d3dcsx_46.dll");
			vanillaEntries.Add("gfsdk_shadowlib.win64.dll");
			vanillaEntries.Add("gfsdk_txaa.win64.dll");
			vanillaEntries.Add("gfsdk_txaa_alpharesolve.win64.dll");
			vanillaEntries.Add("gpuperfapidx11-x64.dll");
			vanillaEntries.Add("gta5.exe");
			vanillaEntries.Add("gtavlauncher.exe");
			vanillaEntries.Add("nvpmapi.core.win64.dll");
			vanillaEntries.Add("playgtav.exe");
			vanillaEntries.Add("version.txt");
			vanillaEntries.Add("x64a.rpf");
			vanillaEntries.Add("x64b.rpf");
			vanillaEntries.Add("x64c.rpf");
			vanillaEntries.Add("x64d.rpf");
			vanillaEntries.Add("x64e.rpf");
			vanillaEntries.Add("x64f.rpf");
			vanillaEntries.Add("x64g.rpf");
			vanillaEntries.Add("x64h.rpf");
			vanillaEntries.Add("x64i.rpf");
			vanillaEntries.Add("x64j.rpf");
			vanillaEntries.Add("x64k.rpf");
			vanillaEntries.Add("x64l.rpf");
			vanillaEntries.Add("x64m.rpf");
			vanillaEntries.Add("x64n.rpf");
			vanillaEntries.Add("x64o.rpf");
			vanillaEntries.Add("x64p.rpf");
			vanillaEntries.Add("x64q.rpf");
			vanillaEntries.Add("x64r.rpf");
			vanillaEntries.Add("x64s.rpf");
			vanillaEntries.Add("x64t.rpf");
			vanillaEntries.Add("x64u.rpf");
			vanillaEntries.Add("x64v.rpf");
			vanillaEntries.Add("x64w.rpf");

			if(Launcher.Instance.IsSteamVersion())
			{
				Log.Info("Registering Steam files...");

				vanillaEntries.Add("_commonredist");
				vanillaEntries.Add("installers");
				vanillaEntries.Add("installscript.vdf");
				vanillaEntries.Add("steam_api64.dll");
			}

			Process process = Process.GetCurrentProcess();
			DirectoryInfo parent = Directory.GetParent(process.MainModule.FileName);

			if(parent != null && parent.ToString() == Launcher.Instance.GtaPath)
			{
				Log.Info("The launcher is running in the GTA V directory. Registering it...");
				vanillaEntries.Add("pursuitlib.dll");
				vanillaEntries.Add("license.txt");
				vanillaEntries.Add(Path.GetFileName(process.MainModule.FileName.ToLower()));
			}

			fileHashes.Add("x64a.rpf", "683610e269ba60c5fcc7a9f6d1a8bfd5");
			fileHashes.Add("x64b.rpf", "70af24cd4fe2c8ee58edb902f018a558");
			fileHashes.Add("x64c.rpf", "2a0f6f1c35ad567fe8e56b9c9cc4e4c6");
			fileHashes.Add("x64d.rpf", "c8757b052ab5079c7749bcce02538b2e");
			fileHashes.Add("x64e.rpf", "e5416c0b0000dad4014e0c5e9b878ff9");
			fileHashes.Add("x64f.rpf", "5c6fc965d56ae6d422cd6cbe5a65a3a5");
			fileHashes.Add("x64g.rpf", "1d8a64b337c3e07dffec0f53530cdb8e");
			fileHashes.Add("x64h.rpf", "fe657d9282df303b080c3a2f6771c9ea");
			fileHashes.Add("x64i.rpf", "bb271d313467465d62c75e208236487b");
			fileHashes.Add("x64j.rpf", "143deee4c7699b9f07ef21d43ae0915b");
			fileHashes.Add("x64k.rpf", "da2c88b4ca69c99a86868a9433084a9d");
			fileHashes.Add("x64l.rpf", "f4307b005a3e90192f235959621781d1");
			fileHashes.Add("x64m.rpf", "a1304d84875747aa7405465d37d3c6fb");
			fileHashes.Add("x64n.rpf", "c48a14fe1c301360a16e8b0c5472fd1d");
			fileHashes.Add("x64o.rpf", "6715a4eabbbc8868f15630bf917db49a");
			fileHashes.Add("x64p.rpf", "6ad56befada1db7cccd9cea7834c825b");
			fileHashes.Add("x64q.rpf", "ff6d09527d7fdc005d3fa78435e09c8a");
			fileHashes.Add("x64r.rpf", "1465c9da5cc17b68f14915b6c1d815bc");
			fileHashes.Add("x64s.rpf", "2c6e61201eb4f60d5c3c1e9ae6d67a32");
			fileHashes.Add("x64t.rpf", "4c15a54a4c9573d7a0bcfa4689d9d1ed");
			fileHashes.Add("x64u.rpf", "2c9cff0cc5f99ad2218e4c4de39881b7");
			fileHashes.Add("x64v.rpf", "db647120263d0282b6f6c555f6112a1c");
			fileHashes.Add("x64w.rpf", "46a4abe50bfc78c30c0173d888cf2c4a");
		}

		public static bool IsGTAModded()
		{
			if(Launcher.Instance.GtaPath != null)
			{
				if(Directory.Exists(Launcher.Instance.GtaPath))
				{
					foreach(string file in Directory.GetFileSystemEntries(Launcher.Instance.GtaPath))
					{
						if(!vanillaEntries.Contains(Path.GetFileName(file).ToLower()))
							return true;
					}

					if(Directory.Exists(Path.Combine(Launcher.Instance.GtaPath, "update\\x64\\dlcpacks")))
					{
						string filename;

						foreach(string file in Directory.GetFileSystemEntries(Path.Combine(Launcher.Instance.GtaPath, "update\\x64\\dlcpacks")))
						{
							if(Directory.Exists(file))
							{
								filename = Path.GetFileName(file).ToLower();
								if(!filename.StartsWith("mp") && !filename.StartsWith("patchday"))
									return true;
							}
						}
					}

					return false;
				}
				throw new ApplicationException("GtaPath doesn't exist.");
			}
			else throw new ApplicationException("GtaPath is not set.");
		}

		/// <summary>
		/// List all mods located at the root of the game's directory
		/// </summary>
		public static void ListRootMods(out List<string> files, out List<string> dirs)
		{
			if(Launcher.Instance.GtaPath != null)
			{
				if(Directory.Exists(Launcher.Instance.GtaPath))
				{
					files = new List<string>();
					dirs = new List<string>();

					foreach(string file in Directory.GetFileSystemEntries(Launcher.Instance.GtaPath))
					{
						if(!vanillaEntries.Contains(Path.GetFileName(file).ToLower()))
						{
							if(File.Exists(file))
								files.Add(file);
							else
							{
								dirs.Add(file);
								dirs.AddRange(Directory.GetDirectories(file, "*", SearchOption.AllDirectories));
								files.AddRange(Directory.GetFiles(file, "*", SearchOption.AllDirectories));
							}
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
			if(Launcher.Instance.GtaPath != null)
			{
				if(Directory.Exists(Launcher.Instance.GtaPath))
				{
					List<string> mods = new List<string>();

					if(Directory.Exists(Path.Combine(Launcher.Instance.GtaPath, "update\\x64\\dlcpacks")))
					{
						string filename;

						foreach(string file in Directory.GetFileSystemEntries(Path.Combine(Launcher.Instance.GtaPath, "update\\x64\\dlcpacks")))
						{
							if(Directory.Exists(file))
							{
								filename = Path.GetFileName(file).ToLower();
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

		/// <summary>
		/// Checks whether a file's integrity can be verified or not
		/// </summary>
		/// <param name="file">The file that needs to be verified</param>
		/// <returns>true if the file's integrity can be verified, false otherwise</returns>
		public static bool CanFileBeVerified(string file)
		{
			return fileHashes.ContainsKey(file);
		}

		/// <summary>
		/// Get a file's vanilla MD5 hash
		/// </summary>
		/// <param name="file">The file that needs to be verified</param>
		/// <returns>The file's vanilla MD5 hash</returns>
		public static string GetVanillaFileHash(string file)
		{
			return fileHashes[file];
		}

		/// <summary>
		/// Get a file's current MD5 hash
		/// </summary>
		/// <param name="file">The file that needs to be verified</param>
		/// <returns>The file's current MD5 hash</returns>
		public static string CalculateFileHash(string file)
		{
			using(MD5 md5 = MD5.Create())
			{
				using(Stream stream = File.OpenRead(Path.Combine(Launcher.Instance.GtaPath, file)))
				{
					return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
		}
	}
}
