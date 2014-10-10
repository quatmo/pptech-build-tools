using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PPTech.Builder
{
	[InitializeOnLoad]
	public class BuilderPackagesState
	{
		private const string RecoverFileDir = "Builder/";
		private const string RecoverFile = "Builder/packages-recover-data.json";
		private const string ExcludeFolder = "Assets/Editor/_ExcludedAssets/";

		private struct RecoverData
		{
			public string guid;
			public string path;
		}

		private List<string> _ignore = new List<string>();
		private List<string> _guids = new List<string>();
		private List<RecoverData> _recover = new List<RecoverData>();

		static BuilderPackagesState()
		{
			// ensure auto recovery
			var dummy = new BuilderPackagesState();
			dummy.LoadRecoverData();
			dummy.Recover(null);
		}

		public BuilderPackagesState()
		{
		}

		public void Configure(List<string> availablePackages)
		{
			this._ignore.Clear();

			var allPackages = BuilderPackagesWindow.GetPackages();
			foreach (var name in allPackages)
			{
				var p = BuilderPackage.GetPackage(name);
				if (p == null)
				{
					continue;
				}

				p.FillGuids(this._ignore);
			}
			if (this._ignore.Count == 0 || availablePackages == null || availablePackages.Count == 0)
			{
				return;
			}

			foreach (var name in availablePackages)
			{
				var p = BuilderPackage.GetPackage(name);
				if (p == null)
				{
					continue;
				}

				p.FillGuids(this._guids);
				foreach (var guid in this._guids)
				{
					this._ignore.Remove(guid);
				}
				this._guids.Clear();
			}
		}

		public int Apply(BuilderState state)
		{
			int errors = 0;
			this._recover.Clear();

			if (this._ignore.Count == 0)
			{
				DeleteRecoverData();
				return errors;
			}

			for (int i = this._ignore.Count - 1; i >= 0; i--)
			{
				var path = AssetDatabase.GUIDToAssetPath(this._ignore[i]);
				if (string.IsNullOrEmpty(path))
				{
					this._ignore.RemoveAt(i);
					continue;
				}

				this._recover.Add(new RecoverData {
					guid = this._ignore[i],
					path = path
				});			
			}
			this.SaveRecoverData();

			EnsureDirectory(ExcludeFolder.TrimEnd('/'));

			foreach (var guid in this._ignore)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path) || path.StartsWith(ExcludeFolder))
				{
					this._recover.RemoveAll(x => x.guid == guid);
					continue;
				}

				if (state != null)
				{
					state.Log("Excluding " + path);
				}
				var error = AssetDatabase.MoveAsset(path, ExcludeFolder + guid + Path.GetExtension(path));
				if (!string.IsNullOrEmpty(error))
				{
					errors++;
					if (state != null)
					{
						state.Log(error);
					}
					Debug.LogError(error);				
				}
			}
			this.SaveRecoverData();

			return errors;
		}

		public void Recover(BuilderState state)
		{
			foreach (var d in this._recover)
			{
				var path = AssetDatabase.GUIDToAssetPath(d.guid);
				if (string.IsNullOrEmpty(path) || string.Equals(path, d.path, StringComparison.InvariantCultureIgnoreCase) || !path.StartsWith(ExcludeFolder))
				{
					continue;
				}

				var targetParent = Path.GetDirectoryName(d.path);
				if (!Directory.Exists(targetParent))
				{
					continue;
				}

				if (state != null)
				{
					state.Log("Recovering " + d.path + " from " + path);
				}
				string error = AssetDatabase.MoveAsset(path, d.path);
				if (!string.IsNullOrEmpty(error))
				{
					if (state != null)
					{
						state.Log(error);
					}
					Debug.LogError(error);
				}
			}

			if (Directory.Exists(ExcludeFolder) && Directory.GetFileSystemEntries(ExcludeFolder).Length == 0)
			{
				AssetDatabase.DeleteAsset(ExcludeFolder.TrimEnd('/'));
				AssetDatabase.SaveAssets();
			}

			this._recover.Clear();
			DeleteRecoverData();
		}

		private static void EnsureDirectory(string path/*, ref List<string> dirs */)
		{
			if (Directory.Exists(path))
			{
				return;
			}

			var parentDir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
			{
				EnsureDirectory(parentDir);
			}
				
			AssetDatabase.CreateFolder(parentDir, Path.GetFileName(path));	
		}


		private void SaveRecoverData()
		{
			var recover = new JArray();
			foreach (var d in this._recover)
			{
				recover.Add(new JObject {
					{ "guid", d.guid },
					{ "path", d.path }
				});
			}
			Directory.CreateDirectory(RecoverFileDir);
			File.WriteAllText(RecoverFile, recover.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
		}

		private void LoadRecoverData()
		{
			this._recover.Clear();
			if (!File.Exists(RecoverFile))
			{
				return;
			}

			var recover = JArray.Parse(File.ReadAllText(RecoverFile));
			foreach (JObject obj in recover)
			{
				this._recover.Add(new RecoverData {
					guid = (string)obj["guid"],
					path = (string)obj["path"]
				});
			}
		}

		private void DeleteRecoverData()
		{
			if (File.Exists(RecoverFile))
			{
				File.Delete(RecoverFile);
			}
		}
	}
}

