using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PPTech.Builder
{
	public class BuilderWindow : EditorWindow
	{
		public const string BuildConfigurationsDir = "Builder/Configurations/";		

		[SerializeField]
		private bool _initialized;
		[SerializeField]
		private string _currentConfigurationName;
		[SerializeField]
		private string _currentConfigurationSerialized;
		[SerializeField]
		private bool _currentConfigurationDirty;

		[NonSerialized]
		private static string[] _configurations;
		[NonSerialized]
		private static Dictionary<string, string[]> _options;
		[NonSerialized]
		private static string[] _optionKeys;


		private BuilderConfiguration _currentConfiguration;
		private Vector2 _scrollPos;

		public static BuildTargetGroup GetBuildTargetGroup(BuildTarget buildTarget)
		{
			if (buildTarget == BuildTarget.WebPlayer 
				|| buildTarget == BuildTarget.WebPlayerStreamed)
			{
				return BuildTargetGroup.WebPlayer;
			}

			if (buildTarget == BuildTarget.StandaloneOSXIntel 
				|| buildTarget == BuildTarget.StandaloneOSXIntel64
				|| buildTarget == BuildTarget.StandaloneOSXUniversal
				|| buildTarget == BuildTarget.StandaloneWindows
				|| buildTarget == BuildTarget.StandaloneWindows64
				|| buildTarget == BuildTarget.StandaloneLinux
				|| buildTarget == BuildTarget.StandaloneLinux64
				|| buildTarget == BuildTarget.StandaloneLinuxUniversal)
			{
				return BuildTargetGroup.Standalone;
			}

			if (buildTarget == BuildTarget.StandaloneGLESEmu)
			{
				return BuildTargetGroup.GLESEmu;
			}

			if (buildTarget == BuildTarget.iPhone)
			{
				return BuildTargetGroup.iPhone;
			}

			if (buildTarget == BuildTarget.PS3)
			{
				return BuildTargetGroup.PS3;
			}

			if (buildTarget == BuildTarget.XBOX360)
			{
				return BuildTargetGroup.XBOX360;
			}

			if (buildTarget == BuildTarget.Android)
			{
				return BuildTargetGroup.Android;
			}

			/*
			if (buildTarget == BuildTarget.Wii)
			{
				return BuildTargetGroup.Wii;
			}
			*/

			if (buildTarget == BuildTarget.NaCl)
			{
				return BuildTargetGroup.NaCl;
			}

			if (buildTarget == BuildTarget.FlashPlayer)
			{
				return BuildTargetGroup.FlashPlayer;
			}

			if (buildTarget == BuildTarget.MetroPlayer)
			{
				return BuildTargetGroup.Metro;
			}

			if (buildTarget == BuildTarget.WP8Player)
			{
				return BuildTargetGroup.WP8;
			}

			#if UNITY_4_5
			if (buildTarget == BuildTarget.BlackBerry)
			{
				return BuildTargetGroup.BlackBerry;
			}
			#else
			if (buildTarget == BuildTarget.BB10)
			{
				return BuildTargetGroup.BB10;
			}
			#endif

			return BuildTargetGroup.Unknown;
		}

		public static string[] GetConfigurations()
		{
			EnsureConfigurations();
			return _configurations;
		}

		public static string[] GetOptions()
		{
			EnsureOptions();
			return _optionKeys;
		}

		[MenuItem("Window/PPTools/Builder %&F6")]
		public static void OpenWindow()
		{
			var window = EditorWindow.GetWindow<BuilderWindow>(true, "Builder");
			window.Show();
			window.Focus();
		}		

		private static void EnsureConfigurations()
		{
			if (_configurations != null)
			{
				return;
			}
			
			if (!Directory.Exists(BuildConfigurationsDir))
			{
				_configurations = new string[0];
				return;
			}

			_configurations = 
				Directory.GetFiles(BuildConfigurationsDir, "*.json")
				.Select(x => Path.GetFileNameWithoutExtension(x))
				.OrderBy(x => x)
				.ToArray();
		}

		private static void EnsureOptions()
		{
			if (_options != null)
			{
				return;
			}

			var files = Directory.Exists(BuildConfigurationsDir) ? Directory.GetFiles(BuildConfigurationsDir, "*.json") : new string[0];
			if (files.Length == 0)
			{
				_options = new Dictionary<string, string[]>();
				_optionKeys = new string[0];
				return;
			}

			_options = 
				files
				.Select(x => 
				{
					var json = JObject.Parse(File.ReadAllText(x));
					string option = null;
					JToken token;
					if (json.TryGetValue("option", out token))
					{
						option = (string)token;
					}
					return new KeyValuePair<string, string>(option, Path.GetFileNameWithoutExtension(x));
				})
				.Where(x => !string.IsNullOrEmpty(x.Key))
				.GroupBy(
					x => x.Key,
					(x, y) => new KeyValuePair<string, string[]>(x, new[] { " " }.Union(y.Select(z => z.Value).OrderBy(z => z)).ToArray())
				)
				.ToDictionary(
					x => x.Key,
					x => x.Value
				);
			
			_optionKeys = _options.Keys.OrderBy(x => x).ToArray();
		}

		private static void ClearCache()
		{
			_configurations = null;
			_options = null;
			_optionKeys = null;
		}

		private void OnEnable()
		{
			ClearCache();

			if (!this._initialized)
			{
				this._currentConfigurationName = BuilderPreferences.lastConfiguration;
				this._initialized = true;
				return;
			}

			if (!string.IsNullOrEmpty(this._currentConfigurationSerialized))
			{
				this._currentConfiguration = new BuilderConfiguration();
				this._currentConfiguration.name = this._currentConfigurationName;
				this._currentConfiguration.FromJson(JObject.Parse(this._currentConfigurationSerialized));
			}
			else
			{
				this._currentConfiguration = null;
			}

			this._currentConfigurationSerialized = null;
		}

		private void OnFocus()
		{
			ClearCache();
		}

		// workaround because OnDisable happens after serialization
		private void OnLostFocus()
		{
			ClearCache();

			if (this._currentConfiguration != null)
			{					
				var obj = new JObject();
				this._currentConfiguration.ToJson(obj);
				this._currentConfigurationSerialized = obj.ToString();
			}
			else
			{
				this._currentConfigurationSerialized = null;
			}
		}

		private void OnGUI()
		{		
			var options = GetOptions();
			if (options.Length > 0)
			{
				for (int i = 0; i < options.Length; i++)
				{
					this.OnOptionGUI(options[i]);
				}
				EditorGUILayout.Space();
			}


			var configs = GetConfigurations();

			int oldConfigIndex = this._currentConfigurationName != null ? System.Array.IndexOf(configs, this._currentConfigurationName) : -1;
			int newConfigIndex = EditorGUILayout.Popup(
				oldConfigIndex,
				configs
			);
			if (newConfigIndex != oldConfigIndex)
			{
				this._currentConfigurationName = newConfigIndex != -1 ? configs[newConfigIndex] : null;
				this._currentConfiguration = null;
			}

			BuilderPreferences.lastConfiguration = this._currentConfigurationName;

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("New"))
			{
				this.CreateNewConfiguration();
			}
			if (GUILayout.Button("Copy"))
			{
				this.CopyConfiguration();
			}

			var bg = GUI.backgroundColor;
			if (this._currentConfiguration != null && this._currentConfigurationDirty)
			{
				GUI.backgroundColor = Color.yellow;
			}
			if (GUILayout.Button(this._currentConfiguration != null && this._currentConfigurationDirty ? "Save*" : "Save"))
			{
				this.SaveCurrentConfiguration();
			}
			GUI.backgroundColor = bg;

			EditorGUILayout.EndHorizontal();

			if (this._currentConfiguration == null && this._currentConfigurationName != null)
			{
				this._currentConfiguration = this.LoadConfiguration(this._currentConfigurationName);
				this._currentConfigurationDirty = false;
			}

			if (this._currentConfiguration != null)
			{
				bg = GUI.backgroundColor;
				GUI.backgroundColor = Color.green;
				if (GUILayout.Button("Build " + this._currentConfiguration.name))
				{				
					if (this._currentConfigurationDirty)
					{
						this.SaveCurrentConfiguration();
					}
					Debug.ClearDeveloperConsole();

					var currentOptions = this.GetCurrentOptions();
					EditorApplication.delayCall += () => {
						this._currentConfiguration.Build(currentOptions);
					};
				}
				GUI.backgroundColor = bg;
			}

			this._scrollPos = EditorGUILayout.BeginScrollView(this._scrollPos);
			if (this._currentConfiguration != null)
			{
				this._currentConfigurationDirty |= this._currentConfiguration.OnGUI();
			}
			EditorGUILayout.EndScrollView();
			   
			if (this._currentConfiguration != null)
			{
				var modules = BuilderModule.GetModules();
				int moduleIndex = EditorGUILayout.Popup("Add Module", -1, modules.ConvertAll(x => x.description).ToArray());
				var module = moduleIndex >= 0 ? modules[moduleIndex] : null;

				if (module != null)
				{
					this._currentConfiguration.AddModule(module);
					this._currentConfigurationDirty = true;
				}
			}

			if (GUILayout.Button("Player Settings"))
			{
				EditorApplication.ExecuteMenuItem("Edit/Project Settings/Player");
			}
		}

		private void OnOptionGUI(string name)
		{
			EnsureOptions();
			string[] values;
			if (!_options.TryGetValue(name, out values) || values.Length == 0)
			{
				return;
			}

			var option = BuilderPreferences.GetLastOption(name);

			int selected = -1;
			if (!string.IsNullOrEmpty(option))
			{
				selected = Array.IndexOf(values, option);				

				if (selected <= 0 && !string.IsNullOrEmpty(option))
				{
					BuilderPreferences.SetLastOption(name, EditorGUILayout.TextField(name, option));
					return;
				}
			}
			
			selected = EditorGUILayout.Popup(name, selected, values);
			BuilderPreferences.SetLastOption(name, selected > 0 ? values[selected] : null);
		}

		private Dictionary<string, string> GetCurrentOptions()
		{
			Dictionary<string, string> result = null;

			EnsureOptions();
			foreach (var p in _options)
			{
				var val = BuilderPreferences.GetLastOption(p.Key);
				if (val == null || Array.IndexOf(p.Value, val) < 0)
				{
					continue;
				}
				if (result == null)
				{
					result = new Dictionary<string, string>(_options.Count);
				}
				result[p.Key] = val;
			}
			return result;
		}

		private BuilderConfiguration LoadConfiguration(string name)
		{
			var config = new BuilderConfiguration();
			config.name = name;

			string path = BuildConfigurationsDir + name + ".json";
			if (File.Exists(path))
			{
				config.FromJson(JObject.Parse(File.ReadAllText(path)));
			}
			else
			{
				config.InitializeNew();
			}

			return config;
		}

		private void CopyConfiguration()
		{
			if (this._currentConfiguration == null)
			{
				return;
			}
			var obj = new JObject();
			this._currentConfiguration.ToJson(obj);

			string nameBase = this._currentConfiguration.name;
			string name = nameBase;
			string path = null;
			int i = 0;
			while (true)
			{
				path = BuildConfigurationsDir + name + ".json";
				if (!File.Exists(path))
				{
					break;
				}

				name = nameBase + (++i).ToString();
			}

			var config = new BuilderConfiguration();
			config.name = name;
			config.FromJson(obj);
			File.WriteAllText(path, obj.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);

			this._currentConfigurationName = name;
			this._currentConfiguration = config;
			this._currentConfigurationDirty = false;
		}

		private void CreateNewConfiguration()
		{
			Directory.CreateDirectory(BuildConfigurationsDir);

			string nameBase = "Configuration";
			string name = nameBase;
			string path = null;
			int i = 0;
			while (true)
			{
				path = BuildConfigurationsDir + name + ".json";
				if (!File.Exists(path))
				{
					break;
				}

				name = nameBase + (++i).ToString();
			}

			var config = new BuilderConfiguration();
			config.name = name;
			config.InitializeNew();
			var obj = new JObject();
			config.ToJson(obj);
			File.WriteAllText(path, obj.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);

			this._currentConfigurationName = name;
			this._currentConfiguration = config;
			this._currentConfigurationDirty = false;

			ClearCache();
		}

		private void SaveCurrentConfiguration()
		{
			if (this._currentConfiguration == null || string.IsNullOrEmpty(this._currentConfiguration.name))
			{
				return;
			}

			if (this._currentConfigurationName != this._currentConfiguration.name)
			{
				string oldPath = BuildConfigurationsDir + this._currentConfigurationName + ".json";
				if (File.Exists(oldPath))
				{
					File.Delete(oldPath);
				}
			}

			this._currentConfigurationName = this._currentConfiguration.name;
			string newPath = BuildConfigurationsDir + this._currentConfigurationName + ".json";
			var obj = new JObject();
			this._currentConfiguration.ToJson(obj);
			string dir = Path.GetDirectoryName(newPath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			File.WriteAllText(newPath, obj.ToString(Formatting.Indented), Encoding.UTF8);
			this._currentConfigurationDirty = false;

			ClearCache();
		}
	}
}