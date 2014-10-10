using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEditor;

namespace PPTech.Builder.Modules
{
	[Description("Include Option")]
	[Guid("E2F8A7C1-90B3-42F4-BFCA-F41DCEA7A6F1")]
	public class IncludeOption : BuilderModule
	{
		public string option { get; set; }

		private BuilderConfiguration _config;

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.FromJson(data);
			this.option = (data["option"] != null) ? (string)data["option"] : null;
		}

		public override void ToJson(JObject data)
		{
			base.ToJson(data);
			data["option"] = this.option;
		}

		public override void OnGUI()
		{
			var options = BuilderWindow.GetOptions();
			int selected = -1;
			if (!string.IsNullOrEmpty(this.option))
			{
				selected = Array.IndexOf(options, this.option);

				if (selected < 0)
				{
					this.option = EditorGUILayout.TextField("Options", this.option);
					return;
				}
			}

			selected = EditorGUILayout.Popup("Options", selected, options);
			this.option = selected < 0 ? null : options[selected];
		}

		public override void OnBeforeBuild(BuilderState state)
		{
			if (this._config == null)
			{
				this._config = state.ResolveOption(this.option);
			}

			if (this._config == null)
			{
				return;
			}

			if (this._config.packages.Count > 0)
			{
				foreach (var p in this._config.packages)
				{
					if (!state.includedPackages.Contains(p))
					{
						state.includedPackages.Add(p);
					}
				}
			}

			for (int i = 0; i < this._config.modules.Count; i++)
			{
				var m = this._config.modules[i];
				m.OnBeforeBuild(state);
			}
		}

		public override void OnBuild(BuilderState state)
		{
			if (this._config == null)
			{
				return;
			}
			for (int i = 0; i < this._config.modules.Count; i++)
			{
				var m = this._config.modules[i];
				m.OnBuild(state);
			}
		}

		public override void OnAfterBuild(BuilderState state)
		{
			if (this._config == null)
			{
				return;
			}
			for (int i = 0; i < this._config.modules.Count; i++)
			{
				var m = this._config.modules[i];
				m.OnAfterBuild(state);
			}
		}

		public override void OnFinishBuild(BuilderState state)
		{
			if (this._config == null)
			{
				return;
			}
			for (int i = 0; i < this._config.modules.Count; i++)
			{
				var m = this._config.modules[i];
				m.OnFinishBuild(state);
			}
		}

		public override void OnCleanupBuild(BuilderState state)
		{
			if (this._config == null)
			{
				return;
			}
			for (int i = this._config.modules.Count - 1; i >= 0; i--)
			{
				var m = this._config.modules[i];
				m.OnCleanupBuild(state);
			}
		}			
	}
}

