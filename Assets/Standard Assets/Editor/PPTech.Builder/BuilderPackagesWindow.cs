using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PPTech.Builder
{
	public class BuilderPackagesWindow : EditorWindow
	{
		[SerializeField]
		private bool _initialized;
		[SerializeField]
		private string _currentPackageName;
		[SerializeField]
		private string _currentPackageSerialized;
		[SerializeField]
		private bool _currentPackageDirty;

		private BuilderPackage _currentPackage;
		private Vector2 _scrollPos;

		public static string[] GetPackages()
		{
			if (!Directory.Exists(BuilderPackage.PackagesDir))
			{
				return new string[0];
			}

			return Directory
				.GetFiles(BuilderPackage.PackagesDir, "*.json")
				.Select(x => Path.GetFileNameWithoutExtension(x))
				.OrderBy(x => x)
				.ToArray();
		}

		[MenuItem("Window/PPTools/Builder Packages")]
		public static void OpenWindow()
		{
			var window = EditorWindow.GetWindow<BuilderPackagesWindow>(true, "Builder Packages");
			window.Show();
			window.Focus();
		}

		private void OnEnable()
		{
			if (!this._initialized)
			{
				this._currentPackageName = BuilderPreferences.lastPackage;
				this._initialized = true;
				return;
			}

			if (!string.IsNullOrEmpty(this._currentPackageSerialized))
			{
				this._currentPackage = new BuilderPackage();
				this._currentPackage.name = this._currentPackageName;
				this._currentPackage.FromJson(JObject.Parse(this._currentPackageSerialized));
			}
			else
			{
				this._currentPackage = null;
			}

			this._currentPackageSerialized = null;
		}

		// workaround because OnDisable happens after serialization
		private void OnLostFocus()
		{
			if (this._currentPackage != null)
			{					
				var obj = new JObject();
				this._currentPackage.ToJson(obj);
				this._currentPackageSerialized = obj.ToString();
			}
			else
			{
				this._currentPackageSerialized = null;
			}
		}

		private void OnGUI()
		{		
			var packages = GetPackages();

			int oldConfigIndex = this._currentPackageName != null ? System.Array.IndexOf(packages, this._currentPackageName) : -1;
			int newConfigIndex = EditorGUILayout.Popup(
				oldConfigIndex,
				packages
			);
			if (newConfigIndex != oldConfigIndex)
			{
				this._currentPackageName = newConfigIndex != -1 ? packages[newConfigIndex] : null;
				this._currentPackage = null;
			}

			BuilderPreferences.lastPackage = this._currentPackageName;

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("New"))
			{
				this.CreateNewPackage();
			}
		
			var bg = GUI.backgroundColor;
			if (this._currentPackage != null && this._currentPackageDirty)
			{
				GUI.backgroundColor = Color.yellow;
			}
			if (GUILayout.Button(this._currentPackage != null && this._currentPackageDirty ? "Save*" : "Save"))
			{
				this.SaveCurrentPackage();
			}
			GUI.backgroundColor = bg;

			EditorGUILayout.EndHorizontal();

			if (this._currentPackage == null && this._currentPackageName != null)
			{
				this._currentPackage = this.LoadPackage(this._currentPackageName);
				this._currentPackageDirty = false;
			}				

			this._scrollPos = EditorGUILayout.BeginScrollView(this._scrollPos);
			if (this._currentPackage != null)
			{
				this._currentPackageDirty |= this._currentPackage.OnGUI();
			}
			EditorGUILayout.EndScrollView();			   
		}

		private BuilderPackage LoadPackage(string name)
		{
			var config = new BuilderPackage();

			string path = BuilderPackage.PackagesDir + name + ".json";
			config.name = name;
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

		private void CopyPackage()
		{
			if (this._currentPackage == null)
			{
				return;
			}
			var obj = new JObject();
			this._currentPackage.ToJson(obj);

			string nameBase = this._currentPackage.name;
			string name = nameBase;
			string path = null;
			int i = 0;
			while (true)
			{
				path = BuilderPackage.PackagesDir + name + ".json";
				if (!File.Exists(path))
				{
					break;
				}

				name = nameBase + (++i).ToString();
			}

			var package = new BuilderPackage();
			package.name = name;
			package.FromJson(obj);
			File.WriteAllText(path, obj.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);

			this._currentPackageName = name;
			this._currentPackage = package;
			this._currentPackageDirty = false;
		}

		private void CreateNewPackage()
		{
			string nameBase = "Package";
			string name = nameBase;
			string path = null;
			int i = 0;
			while (true)
			{
				path = BuilderPackage.PackagesDir + name + ".json";
				if (!File.Exists(path))
				{
					break;
				}

				name = nameBase + (++i).ToString();
			}

			var package = new BuilderPackage();
			package.name = name;
			package.InitializeNew();
			var obj = new JObject();
			package.ToJson(obj);
			File.WriteAllText(path, obj.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);

			this._currentPackageName = name;
			this._currentPackage = package;
			this._currentPackageDirty = false;
		}

		private void SaveCurrentPackage()
		{
			if (this._currentPackage == null || string.IsNullOrEmpty(this._currentPackage.name))
			{
				return;
			}

			if (this._currentPackageName != this._currentPackage.name)
			{
				string oldPath = BuilderPackage.PackagesDir + this._currentPackageName + ".json";
				if (File.Exists(oldPath))
				{
					File.Delete(oldPath);
				}
			}

			this._currentPackageName = this._currentPackage.name;
			string newPath = BuilderPackage.PackagesDir + this._currentPackageName + ".json";
			var obj = new JObject();
			this._currentPackage.ToJson(obj);
			string dir = Path.GetDirectoryName(newPath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			File.WriteAllText(newPath, obj.ToString(Formatting.Indented), Encoding.UTF8);
			this._currentPackageDirty = false;
		}
	}
}