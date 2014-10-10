using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;

namespace PPTech.Builder.Modules
{
	[Description("Android Configuration")]
	[Guid("43ABA82F-8621-40D7-BE15-50C87430649F")]
	public class AndroidConfiguration : BuilderModule
	{
		private bool _createObbOriginal;

		public bool createObb { get; set; }
		public bool usePlugins { get; set; }

		public override void ToJson(Newtonsoft.Json.Linq.JObject data)
		{
			data["createObb"] = this.createObb;
			data["usePlugins"] = this.usePlugins;
		}

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			JToken tok;
			this.createObb = 
				(data.TryGetValue("createObb", out tok) && tok != null && tok.Type == JTokenType.Boolean)
				? (bool)tok
				: false;

			this.usePlugins = 
				(data.TryGetValue("usePlugins", out tok) && tok != null && tok.Type == JTokenType.Boolean)
				? (bool)tok
				: false;
		}

		public override void OnGUI()
		{
			this.createObb = EditorGUILayout.Toggle("Create Expansion File", this.createObb);
			this.usePlugins = EditorGUILayout.Toggle("Use PPTech Plugins", this.usePlugins);
		}

		public override void OnBeforeBuild(BuilderState state)
		{
			this._createObbOriginal = PlayerSettings.Android.useAPKExpansionFiles;
			PlayerSettings.Android.useAPKExpansionFiles = this.createObb;
		}

		public override void OnFinishBuild(BuilderState state)
		{
			if (this.usePlugins)
			{
				const string classPattern = "public class UnityPlayerNativeActivity extends NativeActivity";
				string path = Path.Combine(state.buildPath, state.productName + "/src/" + state.bundleIdentifier.Replace(".", "/") + "/UnityPlayerNativeActivity.java");
				if (File.Exists(path))
				{
					var content = File.ReadAllLines(path);
					int i = Array.IndexOf(content, classPattern);
					if (i >= 0)
					{
						content[i] = "public class UnityPlayerNativeActivity extends com.playpanic.tech.core.CoreActivity";
						File.WriteAllLines(path, content, new UTF8Encoding(false));
					}
				}
			}
		}

		public override void OnCleanupBuild(BuilderState state)
		{
			PlayerSettings.Android.useAPKExpansionFiles = this._createObbOriginal;
		}
	}
}

