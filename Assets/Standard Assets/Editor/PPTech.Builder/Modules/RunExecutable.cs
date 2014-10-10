using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace PPTech.Builder.Modules
{
	[Description("Run Executable")]
	[Guid("A1BF002E-68C8-440B-9060-F6F315A3572C")]
	public class RunExecutable : BuilderModule
	{
		public string path { get; set; }
		public string arguments { get; set; }
		public bool runAfterBuild { get; set; }

		public RunExecutable()
		{
			this.runAfterBuild = true;
		}

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.FromJson(data);			
			this.path = data["path"] != null ? (string)data["path"] : null;
			this.arguments = data["arguments"] != null ? (string)data["arguments"] : null;
			this.runAfterBuild = data["runAfterBuild"] != null ? (bool)data["runAfterBuild"] : false;			
		}

		public override void ToJson(JObject data)
		{
			base.ToJson(data);
			data["path"] = this.path;
			data["arguments"] = this.arguments;
			data["runAfterBuild"] = this.runAfterBuild;
		}

		public override void OnBuild(BuilderState config)
		{
			if (this.runAfterBuild || string.IsNullOrEmpty(this.path))
			{
				return;
			}

			this.Run(config);
		}

		public override void OnAfterBuild(BuilderState config)
		{
			if (!this.runAfterBuild || string.IsNullOrEmpty(this.path))
			{
				return;
			}

			this.Run(config);
		}

		public override void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			{
				this.path = EditorGUILayout.TextField("Executable", this.path);
				if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
				{					
					string dir = Environment.CurrentDirectory;						
					string file = EditorUtility.OpenFilePanel("Select Executable", dir, "");
					if (!string.IsNullOrEmpty(file))
					{
						if (dir[dir.Length - 1] != Path.DirectorySeparatorChar)
						{
							dir += Path.DirectorySeparatorChar;
						}
						if (file.StartsWith(dir))
						{
							file = file.Substring(dir.Length);
						}
						this.path = file;
					}
				}
			}
			EditorGUILayout.EndHorizontal();
			this.arguments = EditorGUILayout.TextField("Arguments", this.arguments);
			this.runAfterBuild = EditorGUILayout.Toggle("Run After Build", this.runAfterBuild);
		}

		private void Run(BuilderState config)
		{
			string path = this.path ?? "";
			if (!Path.IsPathRooted(path))
			{
				path = Path.Combine(Environment.CurrentDirectory, path);
			}

			if (!File.Exists(path))
			{
				UnityEngine.Debug.LogWarning("Executable " + path + " doesn't exist");
				return;
			}

			string arguments = this.arguments ?? "";
			arguments = arguments.Replace("{BuildPath}", config.buildPath);

			var psi = new ProcessStartInfo();
			psi.WorkingDirectory = Environment.CurrentDirectory;
			psi.RedirectStandardOutput = true;
			psi.UseShellExecute = false;
			psi.FileName = path;
			psi.Arguments = this.arguments;

			using (var p = Process.Start(psi))
			{
				string output = p.StandardOutput.ReadToEnd();
				p.WaitForExit();

				if (!string.IsNullOrEmpty(output))
				{
					config.Log(output);
				}
			}
		}
	}
}