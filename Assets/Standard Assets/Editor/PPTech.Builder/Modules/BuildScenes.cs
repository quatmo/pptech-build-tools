using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEditor;

namespace PPTech.Builder.Modules
{
	[Description("Build Scenes")]
	[Guid("33AD24D3-6437-4925-B2E9-4307AAFAB983")]
	public class BuildScenes : BuilderModule
	{
		public override void OnBeforeBuild(BuilderState config)
		{
			var scenes = EditorBuildSettings.scenes;
			if (scenes == null)
			{
				return;
			}
			foreach (var s in scenes)
			{
				if (s.enabled && !config.scenes.Contains(s.path))
				{
					config.scenes.Add(s.path);
				}
			}
		}
	}
}