using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace PPTech.Builder
{
	[Browsable(false)]
	public sealed class BuilderMissingModule : BuilderModule
	{
		private JObject _data = new JObject();
		private string _name;

		public override string name
		{
			get
			{
				return this._name;
			}
		}

		public BuilderMissingModule(string name)
		{
			this._name = name;
		}

		public override void FromJson(JObject data)
		{
			base.FromJson(data);
			foreach (var p in data)
			{
				this._data[p.Key] = p.Value;
			}
		}

		public override void ToJson(JObject data)
		{
			base.ToJson(data);
			foreach (var p in this._data)
			{
				data[p.Key] = p.Value;
			}
		}
	}

	// Fucking workaround because original BuildOptions is full of crap!
	[Flags]
	public enum BuilderBuildOptions
	{
		Development = 1,
		Unknown = 2,
		AutoRunPlayer = 4,
		ShowBuiltPlayer = 8,
		BuildAdditionalStreamedScenes = 16,
		AcceptExternalModificationsToPlayer = 32,
		InstallInBuildFolder = 64,
		WebPlayerOfflineDeployment = 128,
		ConnectWithProfiler = 256,
		AllowDebugging = 512,
		SymlinkLibraries = 1024,
		UncompressedAssetBundle = 2048,
		ConnectToHost = 4096,
		DeployOnline = 8192,
		EnableHeadlessMode = 16384
	}

	public class BuilderConfiguration
	{
		private static bool _packagesExpanded;
		private readonly List<BuilderModule> _modules = new List<BuilderModule>();		

		public string name { get; set; }
		public string option { get; set; }
		public string buildPath { get; set; }
		public BuildTarget buildTarget { get; set; }
		public BuilderBuildOptions buildOptions { get; set; }
		public string productName { get; set; }
		public string bundleIdentifier { get; set; }
		public string version { get; set; }
		public ReadOnlyCollection<BuilderModule> modules { get; private set; }
		public List<string> packages { get; private set; }

		public BuilderConfiguration()
		{
			this.modules = this._modules.AsReadOnly();
			this.packages = new List<string>();
		}

		public void InitializeNew()
		{
			this.buildPath = "Builds/" + this.name + "/";
		}

		public void ToJson(JObject json)
		{
			if (string.IsNullOrEmpty(this.option))
			{
				json.Remove("option");
			}
			else
			{
				json["option"] = this.option;
			}

			json["path"] = this.buildPath ?? "";
			json["buildTarget"] = this.buildTarget.ToString();
			json["productName"] = this.productName ?? "";
			json["bundleIdentifier"] = this.bundleIdentifier ?? "";
			json["version"] = this.version ?? "";

			if (this.buildOptions != 0)
			{
				var options = new JObject();

				foreach (BuilderBuildOptions o in System.Enum.GetValues(typeof(BuilderBuildOptions)))
				{
					if ((this.buildOptions & o) != 0)
					{
						var name = new StringBuilder(o.ToString());
						name[0] = char.ToLowerInvariant(name[0]);
						options[name.ToString()] = true;
					}
				}
				json["buildOptions"] = options;
			}

			if (this.packages.Count > 0)
			{
				json["packages"] = JArray.FromObject(this.packages);
			}
			else
			{
				json.Remove("packages");
			}

			if (this._modules.Count > 0)
			{
				var modules = new JArray();
				foreach (var m in this._modules)
				{
					var obj = new JObject();
					if (m.guid != null)
					{
						obj["_guid"] = m.guid;
					}
					else if (m.name != null)
					{
						obj["_name"] = m.name;
					}
					m.ToJson(obj);
					modules.Add(obj);
				}
				json["modules"] = modules;
			}
			else
			{
				json.Remove("modules");
			}				
		}

		public void FromJson(JObject json)
		{
			this.option = null;
			this.buildPath = null;
			this.buildTarget = (BuildTarget)0;
			this.buildOptions = 0;

			if (json == null)
			{
				return;
			}

			JToken obj;

			if (json.TryGetValue("option", out obj))
			{
				this.option = (string)obj;
			}

			if (json.TryGetValue("path", out obj))
			{
				this.buildPath = (string)obj;
			}

			if (json.TryGetValue("buildTarget", out obj))
			{
				try
				{
					this.buildTarget = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), (string)obj, true);
				}
				catch
				{
					this.buildTarget = (BuildTarget)0;
				}
			}

			if (json.TryGetValue("productName", out obj))
			{
				this.productName = (string)obj;
			}

			if (json.TryGetValue("bundleIdentifier", out obj))
			{
				this.bundleIdentifier = (string)obj;
			}

			if (json.TryGetValue("version", out obj))
			{
				this.version = (string)obj;
			}

			if (json.TryGetValue("buildOptions", out obj))
			{
				var options = (JObject)obj;

				foreach (var p in options)
				{
					if ((bool)p.Value)
					{
						try
						{
							this.buildOptions |= (BuilderBuildOptions)System.Enum.Parse(typeof(BuilderBuildOptions), p.Key, true);
						}
						catch
						{
						}
					}
				}
			}

			this.packages.Clear();
			if (json.TryGetValue("packages", out obj))
			{
				foreach (var o in (JArray)obj)
				{
					if (o == null || o.Type != JTokenType.String)
					{
						continue;
					}

					string name = (string)o;
					if (string.IsNullOrEmpty(name))
					{
						continue;
					}

					if (!this.packages.Contains(name))
					{
						this.packages.Add(name);
					}
				}
			}

			this._modules.Clear();
			if (json.TryGetValue("modules", out obj))
			{
				foreach (JObject o in (JArray)obj)
				{
					string guid = (string)o["_guid"];
					o.Remove("_guid");
					string name = (string)o["_name"];
					o.Remove("_name");
					var info = BuilderModule.GetModule(guid ?? name);
					var module = (info != null) ? info.Instantiate() : new BuilderMissingModule(name);
					module.FromJson(o);
					this._modules.Add(module);
				}
			}
		}

		public bool OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			this.name = EditorGUILayout.TextField("Name", this.name);
			this.option = EditorGUILayout.TextField("Option", this.option);

			GUI.backgroundColor = Color.gray;
			this.buildPath = EditorGUILayout.TextField("Path", this.buildPath);
			this.buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Type", this.buildTarget);
			this.productName = EditorGUILayout.TextField("Product Name", this.productName);
			this.bundleIdentifier = EditorGUILayout.TextField("Bundle Identifier", this.bundleIdentifier);
			this.version = EditorGUILayout.TextField("Version", this.version);
			this.buildOptions = (BuilderBuildOptions)EditorGUILayout.EnumMaskField("Options", this.buildOptions);

			bool dirty = EditorGUI.EndChangeCheck();

			dirty |= this.PackagesGUI();

			EditorGUILayout.Separator();

			BuilderModule moduleToRemove = null;
			BuilderModule moduleUp = null;
			BuilderModule moduleDown = null;
			foreach (var m in this._modules)
			{
				var info = BuilderModule.GetModule(m.name);
				EditorGUILayout.BeginHorizontal();
				if (info == null)
				{
					EditorGUILayout.LabelField(m.name != null ? "Missing module: " + m.name : "Missing module");
				}
				else
				{
					m.isCollapsed = !EditorGUILayout.Foldout(!m.isCollapsed, info.description);
				}
				EditorGUI.BeginChangeCheck();

				if (GUILayout.Button("Up", GUILayout.ExpandWidth(false)))
				{
					moduleUp = m;
				}
				if (GUILayout.Button("Down", GUILayout.ExpandWidth(false)))
				{
					moduleDown = m;
				}
				if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
				{
					moduleToRemove = m;
				}
				EditorGUILayout.EndHorizontal();

				if (info != null && !m.isCollapsed)
				{
					EditorGUI.indentLevel++;
					m.OnGUI();
					EditorGUI.indentLevel--;
				}

				dirty |= EditorGUI.EndChangeCheck();
			}

			if (moduleToRemove != null)
			{
				this._modules.Remove(moduleToRemove);
			}
			if (moduleUp != null)
			{
				int i = this._modules.IndexOf(moduleUp);
				if (i > 0)
				{
					this._modules.RemoveAt(i);
					this._modules.Insert(i - 1, moduleUp);
				}
			}
			if (moduleDown != null)
			{
				int i = this._modules.IndexOf(moduleDown);
				if (i >= 0 && i < this._modules.Count - 1)
				{
					this._modules.RemoveAt(i);
					this._modules.Insert(i + 1, moduleDown);
				}
			}

			return dirty;
		}

		public void AddModule(BuilderModuleInfo module)
		{
			if (module == null)
			{
				return;
			}
			this._modules.Add(module.Instantiate());
		}

		public void Build(Dictionary<string, string> options)
		{
			if (BuildPipeline.isBuildingPlayer)
			{
				return;
			}

			Debug.Log("Build started...");
			EditorApplication.delayCall += () => {
				this.DoBuild(options);
			};
		}

		private bool PackagesGUI()
		{		
			string title;
			if (_packagesExpanded || this.packages.Count == 0)
			{
				title = "Packages";
			}
			else
			{
				title = "Packages: " + string.Join(", ", this.packages.ToArray());
			}

			EditorGUILayout.BeginHorizontal();
			_packagesExpanded = EditorGUILayout.Foldout(_packagesExpanded, title);
			if (GUILayout.Button("Manage Packages", GUILayout.ExpandWidth(false)))
			{
				BuilderPackagesWindow.OpenWindow();
			}
			EditorGUILayout.EndHorizontal();
			if (!_packagesExpanded)
			{
				return false;
			}

			var allPackages = BuilderPackagesWindow.GetPackages();

			bool dirty = false;
			foreach (var package in allPackages)
			{
				bool oldValue = this.packages.Contains(package);
				bool newValue = EditorGUILayout.ToggleLeft(package, oldValue);
				if (newValue != oldValue)
				{
					dirty = true;
					if (newValue)
					{
						this.packages.Add(package);
					}
					else
					{
						this.packages.Remove(package);
					}
				}
			}

			// missing packages
			int i = 0;
			while (i < this.packages.Count)
			{
				var package = this.packages[i];
				if (Array.IndexOf(allPackages, package) < 0)
				{
					if (!EditorGUILayout.ToggleLeft("[" + package + "]", true))
					{
						dirty = true;
						this.packages.RemoveAt(i);
						continue;
					}
				}
				i++;
			}

			return dirty;
		}

		private void DoBuild(Dictionary<string, string> options)
		{
			if (BuildPipeline.isBuildingPlayer)
			{
				Debug.LogWarning("Build is already running");
				return;
			}

			EditorApplication.LockReloadAssemblies();
			
			AssetDatabase.SaveAssets();
			
			bool hasErrors = false;

			using (var config = new State(this, options))
			{
				config.Log("Build started");

				config.buildPath = this.buildPath;
				config.buildTarget = this.buildTarget;
				config.buildOptions = (BuildOptions)this.buildOptions;

				if (config.buildTarget == BuildTarget.iPhone && (config.buildOptions & BuildOptions.AcceptExternalModificationsToPlayer) != 0)
				{
					if (!File.Exists(Path.Combine(config.buildPath, "Unity-iPhone.xcodeproj/project.pbxproj")))
					{
						config.buildOptions &= ~BuildOptions.AcceptExternalModificationsToPlayer;
					}
				}

				if (this.packages.Count > 0)
				{
					config.includedPackages.AddRange(this.packages);
				}

				string oldBundleIdentifier = null;
				if (!string.IsNullOrEmpty(this.bundleIdentifier))
				{
					oldBundleIdentifier = PlayerSettings.bundleIdentifier;
					PlayerSettings.bundleIdentifier = this.bundleIdentifier;
				}

				string oldVersion = null;
				int oldBundleVersion = 0;
				if (!string.IsNullOrEmpty(this.version))
				{				
					oldVersion = PlayerSettings.bundleVersion;
					PlayerSettings.bundleVersion = this.version;
					if (config.buildTarget == BuildTarget.Android)
					{
						try
						{
							var version = new Version(this.version);
							if (version.Revision > 0)
							{							
								PlayerSettings.bundleVersion = (version.Build == 0) ? version.ToString(2) : version.ToString(3);							
								oldBundleVersion = PlayerSettings.Android.bundleVersionCode;
								PlayerSettings.Android.bundleVersionCode = version.Revision;
							}
						}
						catch
						{
						}
					}
				}

				string oldProductName = null;
				if (!string.IsNullOrEmpty(this.productName))
				{
					oldProductName = PlayerSettings.productName;
					PlayerSettings.productName = this.productName;
				}

				config.bundleIdentifier = PlayerSettings.bundleIdentifier;
				config.version = PlayerSettings.bundleVersion;
				config.productName = PlayerSettings.productName;

				var buildTargetGroup = BuilderWindow.GetBuildTargetGroup(config.buildTarget);
				string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, "");

				foreach (var m in config.modules)
				{
					m.OnBeforeBuild(config);
				}

				foreach (var m in config.modules)
				{
					m.OnBuild(config);
				}

				if (!string.IsNullOrEmpty(config.bundleIdentifier) && config.bundleIdentifier != PlayerSettings.bundleIdentifier)
				{
					if (oldBundleIdentifier == null)
					{
						oldBundleIdentifier = PlayerSettings.bundleIdentifier;
					}
					PlayerSettings.bundleIdentifier = config.bundleIdentifier;
				}

				// complex android version patch 
				if (config.buildTarget == BuildTarget.Android)
				{
					bool updateVersion = !string.IsNullOrEmpty(config.version);
					Version newVersion = null;
					if (updateVersion)
					{
						try
						{
							newVersion = new Version(config.version);
						}
						catch
						{
							updateVersion = false;
						}
					}

					if (updateVersion)
					{
						try
						{
							if (!string.IsNullOrEmpty(PlayerSettings.bundleVersion))
							{
								var oldV = new Version(PlayerSettings.bundleVersion);
								oldV = new Version(oldV.Major, oldV.Minor, oldV.Build, PlayerSettings.Android.bundleVersionCode);

								if (newVersion == oldV)
								{
									updateVersion = false;
								}
							}
						}
						catch
						{
						}
					}

					if (updateVersion)
					{
						if (oldVersion == null)
						{
							oldVersion = PlayerSettings.bundleVersion;
						}
						if (newVersion.Revision > 0)
						{
							PlayerSettings.bundleVersion = (newVersion.Build == 0) ? newVersion.ToString(2) : newVersion.ToString(3);

							if (oldBundleVersion == 0)
							{
								oldBundleVersion = PlayerSettings.Android.bundleVersionCode;
							}
							PlayerSettings.Android.bundleVersionCode = newVersion.Revision;
						}
						else
						{
							PlayerSettings.bundleVersion = newVersion.ToString();
						}
					}
				}
				else
				{
					if (!string.IsNullOrEmpty(config.version) && config.version != PlayerSettings.bundleVersion)
					{
						if (oldVersion == null)
						{
							oldVersion = PlayerSettings.bundleVersion;
						}
						PlayerSettings.bundleVersion = config.version;
					}
				}

				if (!string.IsNullOrEmpty(config.productName) && config.productName != PlayerSettings.productName)
				{
					if (oldProductName == null)
					{
						oldProductName = PlayerSettings.productName;
					}
					PlayerSettings.productName = config.productName;
				}

				var dir = Path.GetDirectoryName(config.buildPath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}

				AssetDatabase.SaveAssets();

				string obbDir = null;
				string newObbName = null;
				string oldDigest = null;
				try
				{
					if (config.buildTarget == BuildTarget.Android && (config.buildOptions & BuildOptions.AcceptExternalModificationsToPlayer) != 0)
					{
						var assetsDir = Path.Combine(config.buildPath, PlayerSettings.productName + "/assets");
						if (Directory.Exists(assetsDir))
						{
							Directory.Delete(assetsDir, true);
						}
					}

					var packagesState = new BuilderPackagesState();
					try
					{					
						packagesState.Configure(config.includedPackages);
						int errors = packagesState.Apply(config);
						AssetDatabase.SaveAssets();

						if (errors > 0)
						{
							config.error = "Package exclusion completed with errors. See build log for details";
							config.Log(config.error);
							Debug.LogError(config.error);
							hasErrors = true;
						}

						if (!hasErrors)
						{
							string error = BuildPipeline.BuildPlayer(config.scenes.ToArray(), config.buildPath, config.buildTarget, config.buildOptions);
							if (!string.IsNullOrEmpty(error))
							{
								config.error = error;
								config.Log(error);
								hasErrors = true;
							}
						}
					}
					finally
					{
						try
						{
							packagesState.Recover(config);
						}
						catch
						{
						}

						AssetDatabase.SaveAssets();
					}
						

					if (!hasErrors && config.buildTarget == BuildTarget.Android && (config.buildOptions & BuildOptions.AcceptExternalModificationsToPlayer) != 0 && PlayerSettings.Android.useAPKExpansionFiles)
					{
						try
						{
							EditorUtility.DisplayProgressBar("Extracting OBB", "", 0.0f);

							var expFile = Path.Combine(config.buildPath, config.productName + ".main.obb");
							if (File.Exists(expFile))
							{
								obbDir = Path.Combine(config.buildPath, config.productName + ".main");
								newObbName = Path.Combine(config.buildPath, "main." + PlayerSettings.Android.bundleVersionCode + "." + PlayerSettings.bundleIdentifier + ".obb");
								if (Directory.Exists(obbDir))
								{
									Directory.Delete(obbDir, true);
								}							

								int count;
								using (var zipFileStream = File.OpenRead(expFile))
								using (var zipStream = new ZipInputStream(zipFileStream))
								{
									count = this.CountZipEntries(zipStream);
								}

								using (var zipFileStream = File.OpenRead(expFile))
								using (var zipStream = new ZipInputStream(zipFileStream))
								{
									this.UncompressFolder(obbDir, zipStream, count);
								}

								oldDigest = BuilderUtilities.CalculateObbDigest(expFile);
							}
						}
						finally
						{
							EditorUtility.ClearProgressBar();
						}
					}

				}
				catch (Exception ex)
				{
					config.error = ex.Message;
					config.LogException(ex);
					hasErrors = true;
					Debug.LogException(ex);
				}

				if (!hasErrors)
				{
					try
					{
						foreach (var m in config.modules)
						{
							m.OnAfterBuild(config);
						}
					}
					catch (Exception ex)
					{
						config.error = ex.Message;
						config.LogException(ex);
						hasErrors = true;
						Debug.LogException(ex);
					}
				}

				if (obbDir != null)
				{
					try
					{
						EditorUtility.DisplayProgressBar("Compressing OBB", "", 0.0f);

						var fileName = obbDir + ".zip";
						if (File.Exists(fileName))
						{
							File.Delete(fileName);
						}
						
						using (var zipFile = File.Create(fileName))
						using (var zip = new ZipOutputStream(zipFile))
						{
							zip.IsStreamOwner = true;
							zip.SetLevel(6);
							int counter = 0;
							this.CompressFolder(obbDir, zip, obbDir.Length + 1, Directory.GetFiles(obbDir, "*.*", SearchOption.AllDirectories).Length, ref counter);
						}
						Directory.Delete(obbDir, true);

						var obbName = obbDir + ".obb";
						if (File.Exists(obbName))
						{
							File.Delete(obbName);
						}

						if (File.Exists(newObbName))
						{
							File.Delete(newObbName);
						}
						File.Move(fileName, newObbName);		
		
						var settingsPath = Path.Combine(config.buildPath, config.productName + "/assets/bin/Data/settings.xml");
						if (File.Exists(settingsPath))
						{
							string digest = BuilderUtilities.CalculateObbDigest(newObbName);
							var doc = XDocument.Load(settingsPath);
							doc.Root.Elements("bool").Where(x => x.Attribute("name").Value == oldDigest).Remove();
							doc.Root.Add(new XElement("bool", "True", new XAttribute("name", digest)));
							doc.Save(settingsPath);
						}
					}
					catch (Exception ex)
					{
						config.error = ex.Message;
						config.LogException(ex);
						hasErrors = true;
						Debug.LogException(ex);
					}
					finally
					{
						EditorUtility.ClearProgressBar();
					}
				}

				if (!hasErrors)
				{
					try
					{
						foreach (var m in config.modules)
						{
							m.OnFinishBuild(config);
						}
					}
					catch (Exception ex)
					{
						config.error = ex.Message;
						config.LogException(ex);
						hasErrors = true;
						Debug.LogException(ex);
					}
				}
				
				for (int i = config.modules.Count - 1; i >= 0; i--)
				{
					try
					{
						config.modules[i].OnCleanupBuild(config);
					}
					catch (Exception ex)
					{					
						hasErrors = true;
						config.LogException(ex);
						Debug.LogException(ex);
					}
				}

				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);

				if (oldProductName != null)
				{
					PlayerSettings.productName = oldProductName;
				}
				if (oldVersion != null)
				{
					PlayerSettings.bundleVersion = oldVersion;
				}
				if (oldBundleVersion != 0)
				{
					PlayerSettings.Android.bundleVersionCode = oldBundleVersion;
				}
				if (oldBundleIdentifier != null)
				{
					PlayerSettings.bundleIdentifier = oldBundleIdentifier;
				}

				config.Log("Build is finished");
			}

			AssetDatabase.SaveAssets();		

			if (!hasErrors)
			{
				Debug.Log(string.Format("Building '{0}' is successfuly completed!", this.name));
			}
			else
			{
				Debug.LogWarning(string.Format("Building '{0}' is completed with errors!!!", this.name));
			}

			EditorApplication.UnlockReloadAssemblies();
		}

		private int CountZipEntries(ZipInputStream zipStream)
		{
			int count = 0;
			var zipEntry = zipStream.GetNextEntry();
			while (zipEntry != null) 
			{
				count++;
				zipEntry = zipStream.GetNextEntry();
			}
			return count;
		}

		private void UncompressFolder(string path, ZipInputStream zipStream, int count)
		{
			int cur = 0;

			var zipEntry = zipStream.GetNextEntry();
			while (zipEntry != null) 
			{
				String entryFileName = ZipEntry.CleanName(zipEntry.Name);

				if (count > 0)
				{
					EditorUtility.DisplayProgressBar("Extracting OBB", entryFileName, (float)cur / count);
					cur++;
				}

				byte[] buffer = new byte[4096];     // 4K is optimum

				string fullZipToPath = Path.Combine(path, entryFileName);
				string directoryName = Path.GetDirectoryName(fullZipToPath);
				if (directoryName.Length > 0)
				{
					Directory.CreateDirectory(directoryName);
				}
					
				using (FileStream streamWriter = File.Create(fullZipToPath)) 
				{
					StreamUtils.Copy(zipStream, streamWriter, buffer);
				}				

				zipEntry = zipStream.GetNextEntry();
			}
		}

		private void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset, int count, ref int counter)
		{
			string[] files = Directory.GetFiles(path);
			Array.Sort(files, (x, y) => string.Compare(x, y, true));

			foreach (string filename in files)
			{
				FileInfo fi = new FileInfo(filename);

				string entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
				entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction			

				if (count > 0)
				{
					EditorUtility.DisplayProgressBar("Compressing OBB", entryName, (float)counter / count);
					counter++;
				}

				ZipEntry newEntry = new ZipEntry(entryName);			
				newEntry.DateTime = DateTime.MinValue; // Do not store date. Note the zip format stores 2 second granularity
				newEntry.DosTime = 0;			
				zipStream.UseZip64 = UseZip64.Off;
				newEntry.Size = 
					entryName.StartsWith("assets/bin/") && !entryName.EndsWith(".resS") 
					? -1 
					: fi.Length;
				newEntry.CompressionMethod = 
					entryName.StartsWith("assets/bin/") && !entryName.EndsWith(".resS") 
					? CompressionMethod.Deflated 
					: CompressionMethod.Stored;
				zipStream.PutNextEntry(newEntry);

				// Zip the file in buffered chunks
				// the "using" will close the stream even if an exception occurs
				byte[] buffer = new byte[4096];
				using (FileStream streamReader = File.OpenRead(filename))
				{
					StreamUtils.Copy(streamReader, zipStream, buffer);
				}
				zipStream.CloseEntry();
			}
			string[] folders = Directory.GetDirectories(path);
			Array.Sort(folders, (x, y) => string.Compare(x, y, true));
			foreach (string folder in folders)
			{
				this.CompressFolder(folder, zipStream, folderOffset, count, ref counter);
			}
		}

		private sealed class State : BuilderState
		{
			public List<BuilderModule> modules { get; private set; }

			private Dictionary<string, string> _options;

			public State(BuilderConfiguration config, Dictionary<string, string> options)
			{
				this._options = options;

				this.modules = new List<BuilderModule>(config.modules.Count);
				foreach (var m in config.modules)
				{
					var info = BuilderModule.GetModule(m.name);
					if (info == null)
					{
						continue;
					}

					var module = info.Instantiate();
					var o = new JObject();
					m.ToJson(o);
					module.FromJson(o);
					this.modules.Add(module);
				}
			}

			public override BuilderConfiguration ResolveOption(string name)
			{
				if (this._options == null || string.IsNullOrEmpty(name))
				{
					return base.ResolveOption(name);
				}

				string config;
				if (!this._options.TryGetValue(name, out config))
				{
					return base.ResolveOption(name);
				}

				if (string.IsNullOrEmpty(config))
				{
					return base.ResolveOption(name);
				}

				var path = BuilderWindow.BuildConfigurationsDir + config + ".json";
				if (!File.Exists(path))
				{
					return base.ResolveOption(name);
				}
						
				var result = new BuilderConfiguration();
				result.name = config;
				result.FromJson(JObject.Parse(File.ReadAllText(path)));
				return result;
			}
		}
	}
}