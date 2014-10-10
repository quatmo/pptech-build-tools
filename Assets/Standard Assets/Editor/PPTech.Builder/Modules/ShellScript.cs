using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PPTech.Builder.Modules
{
	[Description("Shell Script")]
	[Guid("79264718-4C7D-49DF-B112-92E98BCCDA57")]
	public class ShellScript : BuilderModule
	{
		private static readonly char[] escapes = new[] {'"', '\\'};

		public string path { get; set; }
		public bool runAfterBuild { get; set; }

		public ShellScript()
		{
			this.runAfterBuild = true;
		}

		public override void FromJson(Newtonsoft.Json.Linq.JObject data)
		{
			base.FromJson(data);
			if (data["path"] != null)
			{
				this.path = (string)data["path"];
			}
			if (data["runAfterBuild"] != null)
			{
				this.runAfterBuild = (bool)data["runAfterBuild"];
			}
		}

		public override void ToJson(JObject data)
		{
			base.ToJson(data);
			data["path"] = this.path;
			data["runAfterBuild"] = this.runAfterBuild;
		}

		public override void OnBuild(BuilderState config)
		{
			if (this.runAfterBuild || string.IsNullOrEmpty(this.path))
			{
				return;
			}

			this.Run(this.path, config);
		}

		public override void OnAfterBuild(BuilderState config)
		{
			if (!this.runAfterBuild || string.IsNullOrEmpty(this.path))
			{
				return;
			}

			this.Run(this.path, config);
		}

		public override void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			{
				this.path = EditorGUILayout.TextField("Script", this.path);
				if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
				{
					var dir = Environment.CurrentDirectory;

					var file = EditorUtility.OpenFilePanel("Select shell script", dir, "");
					if (!string.IsNullOrEmpty(file))
					{
						if (file.StartsWith(dir))
						{
							file = file.Substring(dir.Length).TrimStart('/');
						}
						this.path = file;
					}
				}
			}
			EditorGUILayout.EndHorizontal();
			this.runAfterBuild = EditorGUILayout.Toggle("Run After Build", this.runAfterBuild);
		}

		private void Run(string path, BuilderState config)
		{
			if (!path.StartsWith("/"))
			{
				path = Path.Combine(Environment.CurrentDirectory, path);
			}

			if (!File.Exists(path))
			{
				UnityEngine.Debug.LogWarning("Shell script " + path + " doesn't exist");
				return;
			}

			string tool = "sh";
			using (var file = File.OpenText(path))
			{
				string line = file.ReadLine();
				if (line != null && line.StartsWith("#!"))
				{
					tool = line.Substring(2);
				}
			}				

			var psi = new ProcessStartInfo();
			psi.WorkingDirectory = Environment.CurrentDirectory;
			psi.RedirectStandardOutput = true;
			psi.FileName = tool;

			psi.Arguments = MakeArgs(
				path,

				// 0
				Path.Combine(Environment.CurrentDirectory, config.buildPath),

				// 1
				// The type of player built:
				// "dashboard", "standaloneWin32", "standaloneOSXIntel", "standaloneOSXPPC", "standaloneOSXUniversal", "webplayer"
				GetPlayerTypeString(config.buildTarget),

				// 2
				// What optimizations are applied. At the moment either "" or "strip" when Strip debug symbols is selected.
				(config.buildOptions & BuildOptions.Development) != 0 ? "strip" : "",

				// 3
				// The name of the company set in the project settings
				PlayerSettings.companyName,	

				// 4 
				// The name of the product set in the project settings
				PlayerSettings.productName,

				// 5
				// The default screen width of the player.
				PlayerSettings.defaultScreenWidth.ToString(),

				// 6
				// The default screen height of the player 
				PlayerSettings.defaultScreenHeight.ToString()
			);

			psi.EnvironmentVariables["BUILD_VERSION"] = config.version;
			psi.EnvironmentVariables["BUILD_BUNDLEID"] = config.bundleIdentifier;
			psi.UseShellExecute = false;

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

		private static string GetPlayerTypeString(BuildTarget buildTarget)
		{
			switch(buildTarget)
			{
			case BuildTarget.StandaloneOSXIntel:
			case BuildTarget.StandaloneOSXIntel64:
				return "standaloneOSXIntel";
			case BuildTarget.StandaloneOSXUniversal:
				return "standaloneOSXUniversal";
			case BuildTarget.StandaloneWindows:			
				return "standaloneWin32";
			case BuildTarget.WebPlayer:
			case BuildTarget.WebPlayerStreamed:
				return "webplayer";
			default:
				var sb = new StringBuilder(buildTarget.ToString());
				sb[0] = Char.ToLowerInvariant(sb[0]);
				string result = sb.ToString();
				sb.Length = 0;
				return result;
			}
		}

		private static string MakeArgs(params string[] args)
		{
			if (args == null || args.Length == 0)
			{
				return "";
			}

			int size = 0;
			for (int i = 0; i < args.Length; i++)
			{
				if (i > 0)
				{
					size += 1;
				}

				size += 2;

				string a = args[i];
				if (a != null)
				{
					for (int j = 0; j < a.Length; j++)
					{
						if (Array.IndexOf(escapes, a[j]) >= 0)
						{
							size += 1;
						}
					}
				}
			}

			var text = new StringBuilder(size);
			for (int i = 0; i < args.Length; i++)
			{
				if (i > 0)
				{
					text.Append(" ");
				}

				text.Append("\"");

				string a = args[i];
				if (a != null)
				{
					int start = 0;
					while (start < a.Length)
					{
						int ind = a.IndexOfAny(escapes, start);
						int end = ind < 0 ? a.Length : ind;

						text.Append(a, start, end - start);

						if (ind >= 0)
						{
							text.Append('\\');
							text.Append(a[ind]);
						}
						start = end + 1;
					}
				}

				text.Append("\"");
			}

			var result = text.ToString();
			text.Length = 0;
			return result;
		}
	}
}