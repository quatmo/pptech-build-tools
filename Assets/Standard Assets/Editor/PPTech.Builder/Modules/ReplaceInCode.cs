using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace PPTech.Builder.Modules
{
	[Description("Replace In Code")]	
	[Guid("83AD9618-FFC3-4805-8A82-14692A0CD8CC")]
	public class ReplaceInCode : BuilderModule
	{
		private Dictionary<string, string> _replacements;
		
		public List<ReplaceInCodeItem> items { get; private set; }

		public ReplaceInCode()
		{
			this.items = new List<ReplaceInCodeItem>();
		}

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.FromJson(data);

			this.items = (data["items"] != null ? data["items"].ToObject<List<ReplaceInCodeItem>>() : null) ?? new List<ReplaceInCodeItem>();
		}

		public override void ToJson(JObject data)
		{
			base.ToJson(data);
			data["items"] = JArray.FromObject(this.items);
		}

		public override void OnBuild(BuilderState config)
		{
			if (this.items.Count == 0)
			{
				return;
			}

			var index = new Dictionary<string, List<ReplaceInCodeItem>>();
			foreach (var item in this.items)
			{
				if (item == null || string.IsNullOrEmpty(item.replacement) || string.IsNullOrEmpty(item.guid))
				{
					continue;
				}
				List<ReplaceInCodeItem> list;
				if (!index.TryGetValue(item.guid, out list))
				{
					index[item.guid] = list = new List<ReplaceInCodeItem>();
				}

				list.Add(item);
			}

			foreach (var p in index)
			{
				string path = AssetDatabase.GUIDToAssetPath(p.Key);
				if (string.IsNullOrEmpty(path) || !File.Exists(path))
				{
					config.Log(string.Format("No file found for asset '{0}'", p.Key));
					continue;
				}

				string input = File.ReadAllText(path);
				string output = input;
				foreach (var item in p.Value)
				{						
					string prev = output;
					output = Regex.Replace(
						prev,
						string.Format(@"\/\*\+{0}\+\*\/(.*?)\/\*\-{0}\-\*\/", Regex.Escape(item.replacement)),
						string.Format(@"/*+{0}+*/{1}/*-{0}-*/", item.replacement, item.value),
						RegexOptions.Singleline
					);

					if (output == prev)
					{
						config.Log(string.Format("No replacement for '{0}' found in '{1}'", item.replacement, path));
					}
				}

				if (input != output)
				{
					if (this._replacements == null)
					{
						this._replacements = new Dictionary<string, string>();
					}
					if (!this._replacements.ContainsKey(path))
					{
						this._replacements.Add(p.Key, input);
					}
					
					File.WriteAllText(path, output, Encoding.UTF8);				
				}
			}
		}

		public override void OnCleanupBuild(BuilderState state)
		{		
			if (this._replacements == null)
			{
				return;
			}

			foreach (var p in this._replacements)
			{		
				string path = AssetDatabase.GUIDToAssetPath(p.Key);
				if (string.IsNullOrEmpty(path))
				{
					state.Log(string.Format("Unable to restore asset '{0}'", p.Key));
					continue;
				}
				File.WriteAllText(path, p.Value, Encoding.UTF8);
			}

			this._replacements.Clear();
			this._replacements = null;
		}

		public override void OnGUI()
		{
			Rotorz.ReorderableList.ReorderableListGUI.ListField(this.items,	(pos, item) => 
			{
				if (item == null)
				{
					item = new ReplaceInCodeItem();
				}
							
				MonoScript script = null;
				if (!string.IsNullOrEmpty(item.guid))
				{
					string path = AssetDatabase.GUIDToAssetPath(item.guid);
					if (!string.IsNullOrEmpty(path))
					{
						script = (MonoScript)AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));
					}
				}
				script = (MonoScript)EditorGUI.ObjectField(pos.Percent(0.0f, 0.33f), script, typeof(MonoScript), false);
				item.guid = script != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script)) : null;

				item.replacement = EditorGUI.TextField(pos.Percent(0.34f, 0.66f), item.replacement);
				item.value = EditorGUI.TextField(pos.Percent(0.67f, 1.0f), item.value);

				return item;
			});
		}
	}

	public sealed class ReplaceInCodeItem
	{
		public string guid { get; set; }
		public string replacement { get; set; }
		public string value { get; set; }
	}
}