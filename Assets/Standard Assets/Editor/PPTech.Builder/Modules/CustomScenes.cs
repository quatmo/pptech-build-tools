using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace PPTech.Builder.Modules
{
	[Description("Custom Scenes")]
	[Guid("836D78E0-FA29-4D66-BA6D-E2F52F3B95BB")]
	public class CustomScenes : BuilderModule
	{		
		public List<UObject> scenes = new List<UObject>();

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{			
			base.FromJson(data);
			this.scenes.Clear();
			if (data["scenes"] != null)
			{
				var ids = data["scenes"].ToObject<List<string>>();

				foreach (var id in ids)
				{
					if (string.IsNullOrEmpty(id))
					{
						this.scenes.Add(null);
						continue;
					}

					string path = id;
					if (!path.EndsWith(".unity"))
					{
						path = AssetDatabase.GUIDToAssetPath(path);
					}

					this.scenes.Add(AssetDatabase.LoadAssetAtPath(path, typeof(UObject)));
				}
			}
		}

		public override void ToJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.ToJson(data);
			data["scenes"] = JToken.FromObject(this.scenes.ConvertAll(x =>
			{
				if (x == null)
				{
					return null;
				}
				return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x));
			}));
		}

		public override void OnBeforeBuild(BuilderState config)
		{
			if (this.scenes != null)
			{
				foreach (var s in this.scenes)
				{
					if (s == null)
					{
						return;
					}
					var path = AssetDatabase.GetAssetPath(s);
					if (!config.scenes.Contains(path))
					{
						config.scenes.Add(path);
					}
				}
			}
		}

		public override void OnGUI()
		{
			Rotorz.ReorderableList.ReorderableListGUI.ListField(
				this.scenes,
				(pos, value) =>
				{
					var obj = EditorGUI.ObjectField(pos, value, typeof(UObject), false);
					if (obj == null || !(AssetDatabase.GetAssetPath(obj) ?? "").EndsWith(".unity"))
					{
						return null;
					}
					return obj;
				}
			);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Append From Build Settings"))
			{
				var scenes = EditorBuildSettings.scenes;
				if (scenes == null)
				{
					return;
				}
				
				foreach (var s in scenes)
				{
					if (s.enabled)
					{
						var asset = AssetDatabase.LoadAssetAtPath(s.path, typeof(UnityEngine.Object));
						if (asset != null && !this.scenes.Contains(asset))
						{
							this.scenes.Add(asset);
						}
					}
				}
			}
			if (GUILayout.Button("Append To Build Settings"))
			{
				var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
				
				foreach (var a in this.scenes)
				{
					if (!a)
					{
						continue;
					}
					var path = AssetDatabase.GetAssetPath(a);
					if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity", StringComparison.InvariantCultureIgnoreCase))
					{
						return;
					}

					var s = scenes.Find(x => x.path == path);
					if (s != null)
					{
						s.enabled = true;
					}
					else
					{
						scenes.Add(new EditorBuildSettingsScene
						{
							path = path,
							enabled = true
						});
					}
				}

				EditorBuildSettings.scenes = scenes.ToArray();
			}
			if (GUILayout.Button("Clear"))
			{
				this.scenes.Clear();
			}
			EditorGUILayout.EndHorizontal();
			//ReorderableList
		}
	}
}

