using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PPTech.Builder
{
	public sealed class BuilderPackageAssetExpanderForAttribute : System.Attribute
	{
		public Type Target { get; set; }
		
		public BuilderPackageAssetExpanderForAttribute(Type target)
		{
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			this.Target = target;
		}
	}

	public abstract class BuilderPackageAssetExpander
	{
		private static Dictionary<Type, BuilderPackageAssetExpander> _map; 
		
		public static List<KeyValuePair<UnityEngine.Object, BuilderPackageAssetExpander>> ResolveExpanders(UnityEngine.Object obj)
		{
			if (obj == null)
			{
				return null;
			}
			
			if (_map == null)
			{
				LoadMap();
			}

			List<KeyValuePair<UnityEngine.Object, BuilderPackageAssetExpander>> result = null;

			var expander = Resolve(obj);
			if (expander != null)
			{
				result = new List<KeyValuePair<UnityEngine.Object, BuilderPackageAssetExpander>>
				{
					new KeyValuePair<UnityEngine.Object, BuilderPackageAssetExpander>(obj, expander)
				};
			}

			GameObject go;
			if ((go = obj as GameObject) != null)
			{
				foreach (Component c in go.GetComponents<Component>())
				{
					expander = Resolve(c);
					if (expander == null)
					{
						continue;
					}

					if (result == null)
					{
						result = new List<KeyValuePair<UnityEngine.Object, BuilderPackageAssetExpander>>();
					}
					result.Add(new KeyValuePair<UnityEngine.Object, BuilderPackageAssetExpander>(c, expander));
				}
			}
			
			return result;
		}

		private static BuilderPackageAssetExpander Resolve(UnityEngine.Object obj)
		{
			var t = obj.GetType();
			while (t != typeof(UnityEngine.Object))
			{
				BuilderPackageAssetExpander result;
				if (_map.TryGetValue(t, out result))
				{
					return result;
				}
				t = t.BaseType;
			}

			return null;
		}

		private static void LoadMap()
		{
			_map = new Dictionary<Type, BuilderPackageAssetExpander>();
			
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
						if (typeof(BuilderPackageAssetExpander).IsAssignableFrom(t) && t.IsPublic && !t.IsAbstract)
						{
							foreach (BuilderPackageAssetExpanderForAttribute attr in t.GetCustomAttributes(typeof(BuilderPackageAssetExpanderForAttribute), false))
							{
								if (typeof(UnityEngine.Object).IsAssignableFrom(attr.Target))
								{
									_map[attr.Target] = (BuilderPackageAssetExpander)Activator.CreateInstance(t);
								}
							}
						}
					}
				}
			}
		}	

		public abstract void FillGuids(UnityEngine.Object obj, ICollection<string> target);
	}
}

