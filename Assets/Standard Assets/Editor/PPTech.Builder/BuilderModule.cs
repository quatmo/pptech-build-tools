using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PPTech.Builder
{
	public sealed class BuilderModuleInfo
	{
		public string guid { get; private set; }
		public string name { get; private set; }
		public string description { get; private set; }
		public Type type { get; private set; }

		public BuilderModuleInfo(Type type)
		{
			this.type = type;
			this.name = type.FullName;
			this.description = this.name;

			var attrs = type.GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (attrs.Length > 0)
			{
				this.description = ((DescriptionAttribute)attrs[0]).Description;
			}

			attrs = type.GetCustomAttributes(typeof(GuidAttribute), false);
			if (attrs.Length > 0)
			{
				this.guid = ((GuidAttribute)attrs[0]).Value;
			}
		}

		public BuilderModule Instantiate()
		{
			return (BuilderModule)Activator.CreateInstance(this.type);
		}
	}

	public abstract class BuilderModule
	{
		private static readonly Dictionary<string, BuilderModuleInfo> _modulesByName;
		private static readonly Dictionary<string, BuilderModuleInfo> _modulesByGuid;

		public virtual string name
		{
			get
			{
				return this.GetType().FullName;
			}
		}
		public string guid
		{
			get
			{
				var attrs = this.GetType().GetCustomAttributes(typeof(GuidAttribute), false);
				if (attrs.Length > 0)
				{
					return ((GuidAttribute)attrs[0]).Value;
				}
				return null;
			}
		}
		public virtual bool isCollapsed { get; set; }

		static BuilderModule()
		{
			_modulesByName = new Dictionary<string, BuilderModuleInfo>();
			_modulesByGuid = new Dictionary<string, BuilderModuleInfo>();

			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (a.GlobalAssemblyCache)
				{
					continue;
				}
				
				Type[] types;
				try
				{
					types = a.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					types = ex.Types;
				}
				
				if (types != null)
				{
					foreach (var t in types)
					{
						if (t == null)
						{
							continue;
						}
						if (typeof(BuilderModule).IsAssignableFrom(t) && t.IsPublic && !t.IsAbstract)
						{
							var attrs = t.GetCustomAttributes(typeof(BrowsableAttribute), true);
							if (attrs.Length > 0 && !((BrowsableAttribute)attrs[0]).Browsable)
							{
								continue;
							}

							var info = new BuilderModuleInfo(t);
							_modulesByName[t.FullName] = new BuilderModuleInfo(t);
							if (info.guid != null)
							{
								_modulesByGuid[info.guid] = info;
							}
						}
					}
				}				
			}
		}

		public static List<BuilderModuleInfo> GetModules()
		{
			return new List<BuilderModuleInfo>(_modulesByName.Values);
		}
		public static BuilderModuleInfo GetModule(string id)
		{
			if (id == null)
			{
				return null;
			}
			BuilderModuleInfo module;
			if (_modulesByGuid.TryGetValue(id, out module))
			{
				return module;
			}
			if (_modulesByName.TryGetValue(id, out module))
			{
				return module;
			}
			return null;
		}

		public virtual void FromJson(JObject data)
		{
			this.isCollapsed = false;
			JToken obj;
			if (data.TryGetValue("_isCollapsed", out obj))
			{
				this.isCollapsed = (bool)obj;
				data.Remove("_isCollapsed");
			}
		}

		public virtual void ToJson(JObject data)
		{
			if (this.isCollapsed)
			{
				data["_isCollapsed"] = true;
			}
		}

		public virtual void OnGUI()
		{
		}

		public virtual void OnBeforeBuild(BuilderState state)
		{
		}

		public virtual void OnBuild(BuilderState state)
		{
		}

		public virtual void OnAfterBuild(BuilderState state)
		{
		}

		public virtual void OnFinishBuild(BuilderState state)
		{
		}

		public virtual void OnCleanupBuild(BuilderState state)
		{
		}
	}
}