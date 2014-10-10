using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;

namespace PPTech.Builder.Modules
{
	[Description("Include Configuration")]
	[Guid("2664821F-C9CB-4C8E-BA0E-62D4636DD259")]
	public class IncludeConfiguration : BuilderModule
	{
		public string configuration { get; set; }

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.FromJson(data);
			this.configuration = (data["configuration"] != null) ? (string)data["configuration"] : null;
		}

		public override void ToJson(JObject data)
		{
			base.ToJson(data);
			data["configuration"] = this.configuration;
		}

		public override void OnGUI()
		{
			var configs = BuilderWindow.GetConfigurations();
			int selected = -1;
			if (!string.IsNullOrEmpty(this.configuration))
			{
				selected = Array.IndexOf(configs, this.configuration);

				if (selected < 0)
				{
					this.configuration = EditorGUILayout.TextField("Configuration", this.configuration);
					return;
				}
			}

			selected = EditorGUILayout.Popup("Configuration", selected, configs);
			this.configuration = selected < 0 ? null : configs[selected];
		}

		public override void OnBeforeBuild(BuilderState config)
		{
			BuilderConfiguration include = null;
			if (!string.IsNullOrEmpty(this.configuration))
			{
				var path = BuilderWindow.BuildConfigurationsDir + this.configuration + ".json";
				if (File.Exists(path))
				{
					include = new BuilderConfiguration();
					include.name = this.configuration;
					include.FromJson(JObject.Parse(File.ReadAllText(path)));
				}
			}
			config.parameters[this] = include;

			if (include == null)
			{
				return;
			}

			if (include.packages.Count > 0)
			{
				foreach (var p in include.packages)
				{
					if (!config.includedPackages.Contains(p))
					{
						config.includedPackages.Add(p);
					}
				}
			}

			for (int i = 0; i < include.modules.Count; i++)
			{
				var m = include.modules[i];
				m.OnBeforeBuild(config);
			}
		}

		public override void OnBuild(BuilderState config)
		{
			var include = (BuilderConfiguration)config.parameters[this];
			if (include == null)
			{
				return;
			}
			for (int i = 0; i < include.modules.Count; i++)
			{
				var m = include.modules[i];
				m.OnBuild(config);
			}
		}

		public override void OnAfterBuild(BuilderState config)
		{
			var include = (BuilderConfiguration)config.parameters[this];
			if (include == null)
			{
				return;
			}
			for (int i = 0; i < include.modules.Count; i++)
			{
				var m = include.modules[i];
				m.OnAfterBuild(config);
			}
		}

		public override void OnFinishBuild(BuilderState config)
		{
			var include = (BuilderConfiguration)config.parameters[this];
			if (include == null)
			{
				return;
			}
			for (int i = 0; i < include.modules.Count; i++)
			{
				var m = include.modules[i];
				m.OnFinishBuild(config);
			}
		}

		public override void OnCleanupBuild(BuilderState config)
		{
			var include = (BuilderConfiguration)config.parameters[this];
			if (include == null)
			{
				return;
			}
			for (int i = include.modules.Count - 1; i >= 0; i--)
			{
				var m = include.modules[i];
				m.OnCleanupBuild(config);
			}
		}			
	}
}

