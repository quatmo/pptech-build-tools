using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PPTech.Builder
{
	public class BuilderPackageGroup
	{
		public string name;
		[DefaultValue(false)]
		public bool collapsed;
		public List<string> assets;
	}

	public class BuilderPackage
	{
		public const string PackagesDir = "Builder/Packages/";
		private List<UnityEngine.Object> _guiList;
		private string _newGroup;

		public string name { get; set; }
		public List<BuilderPackageGroup> assetGroups { get; private set; }
		public List<string> assetGuids { get; private set; }


		public BuilderPackage()
		{
			this.name = name;
			this.assetGuids = new List<string>();
			this.assetGroups = new List<BuilderPackageGroup>();
			this._guiList = new List<UnityEngine.Object>();
		}

		public static BuilderPackage GetPackage(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			var path = PackagesDir + name + ".json";
			if (!File.Exists(path))
			{
				return null;
			}

			var package = new BuilderPackage();
			package.name = name;
			package.FromJson(JObject.Parse(File.ReadAllText(path)));
			return package;
		}

		public void InitializeNew()
		{
			this.assetGuids.Clear();
		}

		public void FromJson(JObject obj)
		{
			this.assetGuids.Clear();

			JToken token;
			if (obj.TryGetValue("assets", out token) && token != null && token.Type == JTokenType.Array)
			{
				this.assetGuids.AddRange(token.ToObject<string[]>());
			}

			if (obj.TryGetValue("groups", out token) && token != null && token.Type == JTokenType.Array)
			{
				this.assetGroups.AddRange(token.ToObject<BuilderPackageGroup[]>());
			}
		}

		public void ToJson(JObject obj)
		{
			obj["groups"] = JToken.FromObject(this.assetGroups);
			obj["assets"] = JToken.FromObject(this.assetGuids);
		}

		public bool OnGUI()
		{
			bool dirty = false;

			EditorGUILayout.BeginHorizontal();
			this._newGroup = EditorGUILayout.TextField("New Group", this._newGroup);

			if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)) && !string.IsNullOrEmpty(this._newGroup))
			{
				this.assetGroups.Add(new BuilderPackageGroup
				{
					name = this._newGroup
				});
				this._newGroup = "";
				dirty = true;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUI.BeginChangeCheck();
			this.name = EditorGUILayout.TextField("Name", this.name);

			EditorGUILayout.LabelField("Assets");
			EditGuidsList(this.assetGuids);


			dirty |= EditorGUI.EndChangeCheck();

			BuilderPackageGroup toRemove = null;
			foreach (var g in this.assetGroups)
			{
				if (g == null)
				{
					continue;
				}

				EditorGUILayout.BeginHorizontal();
				g.collapsed = !EditorGUILayout.Foldout(!g.collapsed, g.name ?? "");
				if (!g.collapsed && GUILayout.Button("Add Selection", GUILayout.ExpandWidth(false)))
				{
					var objs = Selection.objects;
					if (objs != null && objs.Length > 0)
					{
						foreach (var obj in objs)
						{
							if (!EditorUtility.IsPersistent(obj))
							{
								continue;
							}
							string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
							if (!string.IsNullOrEmpty(guid) && !g.assets.Contains(guid))
							{
								g.assets.Add(guid);
								dirty = true;
							}
						}
					}
				}
				if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
				{
					toRemove = g;
				}
				EditorGUILayout.EndHorizontal();
				if (!g.collapsed)
				{
					if (g.assets == null)
					{
						g.assets = new List<string>();
					}
					EditorGUI.BeginChangeCheck();
					EditGuidsList(g.assets);
					dirty |= EditorGUI.EndChangeCheck();
				}
			}

			if (toRemove != null && this.assetGroups.Remove(toRemove))
			{
				dirty = true;
			}					

			return dirty;
		}

		public void FillGuids(ICollection<string> target)
		{
			var result = new List<string>();
			var guids = new List<string>();

			foreach (var guid in this.assetGuids)
			{			
				if (string.IsNullOrEmpty(guid) || result.Contains(guid))
				{
					continue;
				}
				result.Add(guid);
			}
			foreach (var group in this.assetGroups)
			{
				if (group.assets == null)
				{
					continue;
				}
				foreach (var guid in group.assets)
				{			
					if (string.IsNullOrEmpty(guid) || result.Contains(guid))
					{
						continue;
					}
					result.Add(guid);
				}
			}

			for (int i = 0; i < result.Count; i++)
			{
				string guid = result[i];
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path))
				{
					continue;
				}

				var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
				if (!asset)
				{
					continue;
				}

				var expanders = BuilderPackageAssetExpander.ResolveExpanders(asset);
				if (expanders != null)
				{
					foreach (var p in expanders)
					{
						guids.Clear();
						p.Value.FillGuids(p.Key, guids);
						foreach (var g in guids)
						{
							if (!string.IsNullOrEmpty(g) && !result.Contains(g))
							{
								result.Add(g);
							}
						}
					}
					continue;
				}
			}		

			foreach (var r in result)
			{			
				if (!target.Contains(r))
				{
					target.Add(r);
				}
			}
		}

		private void EditGuidsList(List<string> guids)
		{
			this._guiList.Clear();
			if (this._guiList.Capacity < guids.Capacity)
			{
				this._guiList.Capacity = guids.Capacity;
			}
			
			for (int i = 0; i < guids.Count; i++)
			{
				if (string.IsNullOrEmpty(guids[i]))
				{
					this._guiList.Add(null);
					continue;
				}
				this._guiList.Add(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(UnityEngine.Object)));
			}
			
		
			Rotorz.ReorderableList.ReorderableListGUI.ListField(
				this._guiList,
				(pos, value) => EditorGUI.ObjectField(pos, value, typeof(UnityEngine.Object), false)
			);
			
			guids.Clear();
			if (guids.Capacity < this._guiList.Capacity)
			{
				guids.Capacity = this._guiList.Capacity;
			}

			for (int i = 0; i < this._guiList.Count; i++)
			{
				if (this._guiList[i] == null)
				{
					guids.Add(null);
					continue;
				}
				guids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this._guiList[i])));
			}
		}
	
		private string GetPath()
		{
			return PackagesDir + this.name + ".json";
		}
	}
}

