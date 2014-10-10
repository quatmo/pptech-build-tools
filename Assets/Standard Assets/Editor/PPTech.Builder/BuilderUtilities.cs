using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PPTech.Builder
{
	public static class BuilderUtilities
	{	
		public static string GetStreamingAssetsDirectory(this BuilderState config)
		{	
			var target = config.buildTarget;

			if (target == BuildTarget.iPhone)
			{
				return Path.Combine(config.buildPath, "Data/Raw/");
			}

			if (target == BuildTarget.StandaloneOSXIntel || target == BuildTarget.StandaloneOSXIntel64 || target == BuildTarget.StandaloneOSXUniversal)
			{
				return Path.Combine(config.buildPath, "Contents/Data/StreamingAssets/");
			}

			if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneLinux || target == BuildTarget.StandaloneLinux64 || target == BuildTarget.StandaloneLinuxUniversal)
			{
				var dir = Path.GetFileNameWithoutExtension(config.buildPath) + "_Data";
				var parent = Path.GetDirectoryName(config.buildPath);
				if (!string.IsNullOrEmpty(parent))
				{
					dir = Path.Combine(parent, dir);
				}
				return Path.Combine(dir, "StreamingAssets/");
			}

			if (target == BuildTarget.WP8Player)
			{
				return Path.Combine(config.buildPath, config.productName + "/Data/StreamingAssets/");
			}


			if (target == BuildTarget.Android && (config.buildOptions & BuildOptions.AcceptExternalModificationsToPlayer) != 0)
			{
				return 
					PlayerSettings.Android.useAPKExpansionFiles
					? Path.Combine(config.buildPath, config.productName + ".main/assets/")
					: Path.Combine(config.buildPath, config.productName + "/assets/");
			}

			return null;	
		}

		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);
			DirectoryInfo[] dirs = dir.GetDirectories();

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			// If the destination directory doesn't exist, create it. 
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location. 
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}

		public static string CalculateObbDigest(string file)
		{
			byte[] arrayOfByte = null;

			using (var localMessageDigest = MD5.Create())			
			using (var localFileInputStream = File.OpenRead(file))
			{
				long l1 = localFileInputStream.Length;
				localFileInputStream.Seek(l1 - Math.Min(l1, 65558L), SeekOrigin.Current);
				arrayOfByte = localMessageDigest.ComputeHash(localFileInputStream);			
			}

			var localStringBuffer = new StringBuilder(32);
			for (int i1 = 0; i1 < arrayOfByte.Length; i1++)
			{
				localStringBuffer.AppendFormat(arrayOfByte[i1].ToString("x2"));
			}
			return localStringBuffer.ToString();	
		}

		public static Rect Percent(this Rect rect, float from, float to)
		{
			return new Rect(
				rect.x + from * rect.width,
				rect.y,
				(to - from) * rect.width,
				rect.height
			);
		}

		public static void EnsureDirectory(string dir)
		{
			var parts = dir.Trim('/').Split('/');
			if (parts.Length < 2)
				return;
			string baseDir = parts[0];
			for (int i = 1; i < parts.Length; i++)
			{			
				var newDir = baseDir + "/" + parts[i];
				if (Directory.Exists(newDir))
				{
					baseDir = newDir;
					continue;
				}			
				AssetDatabase.CreateFolder(baseDir, parts[i]);
				baseDir = newDir;
			}
		}
	}
}