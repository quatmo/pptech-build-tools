using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;

namespace PPTech.Builder.Modules
{
	[Description("Custom Defines")]
	[Guid("0A8E6C5D-74FB-4D47-A242-BE985C2798CB")]
	public class CustomDefines : BuilderModule
	{
		private List<string> _defines;

		public List<string> defines
		{ 
			get
			{
				return this._defines ?? (this._defines = new List<string>());
			}
			set
			{
				this._defines = value;
			}
		}

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.FromJson(data);
			if (data["defines"] != null)
			{
				this.defines = data["defines"].ToObject<List<string>>();
			}
		}

		public override void ToJson(JObject data)
		{
			base.ToJson(data);
			data["defines"] = JToken.FromObject(this.defines);
		}

		public override void OnBuild(BuilderState config)
		{
			var targetGroup = BuilderWindow.GetBuildTargetGroup(config.buildTarget);
			var scriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
			config.parameters[this] = new State 
			{
				targetGroup = targetGroup,
				originalDefines = scriptingDefines
			};

			var newDefines = new List<string>();
			
			foreach (var d in scriptingDefines.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
			{
				if (!newDefines.Contains(d))
				{
					newDefines.Add(d);
				}
			}			

			foreach (var s in this.defines)
			{
				if (!newDefines.Contains(s))
				{
					newDefines.Add(s);
				}
			}

			PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", newDefines.ToArray()));
		}

		public override void OnCleanupBuild(BuilderState config)
		{
			var state = (State)config.parameters[this];
			PlayerSettings.SetScriptingDefineSymbolsForGroup(state.targetGroup, state.originalDefines);
		}

		public override void OnGUI()
		{
			Rotorz.ReorderableList.ReorderableListGUI.ListField(
				this.defines,
				(pos, value) => EditorGUI.TextField(pos, value)
			);			
		}

		private class State
		{
			public BuildTargetGroup targetGroup;
			public string originalDefines;
		}
	}
}

