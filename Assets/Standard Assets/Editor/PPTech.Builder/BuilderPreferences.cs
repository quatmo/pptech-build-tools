using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PPTech.Builder
{
	public class BuilderPreferences
	{
		private const string ConfigPath = "Builder/config.json";

		private static string _lastConfiguration;
		private static string _lastPackage;
		private static Dictionary<string, string> _lastOptions;

		static BuilderPreferences()
		{
			Load();
		}


		public static string lastConfiguration
		{
			get
			{
				return _lastConfiguration;
			}
			set
			{
				value = !string.IsNullOrEmpty(value) ? value : null;
				if (_lastConfiguration == value)
				{
					return;
				}

				_lastConfiguration = value;
				Save();
			}
		}
			
		public static string lastPackage
		{
			get
			{
				return _lastPackage;
			}
			set
			{
				value = !string.IsNullOrEmpty(value) ? value : null;
				if (_lastPackage == value)
				{
					return;
				}

				_lastPackage = value;
				Save();
			}
		}

		public static string GetLastOption(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}

			string val;
			if (_lastOptions == null || !_lastOptions.TryGetValue(name, out val) || string.IsNullOrEmpty(val))
			{
				return null;
			}
			return val;
		}

		public static void SetLastOption(string name, string value)
		{
			if (string.IsNullOrEmpty(name))
			{
				return;
			}

			if (string.IsNullOrEmpty(value))
			{
				if (_lastOptions != null && _lastOptions.ContainsKey(name))
				{
					_lastOptions.Remove(name);
					Save();
				}
				return;			
			}

			string val;
			if (_lastOptions == null)
			{
				_lastOptions = new Dictionary<string, string>(1);
			}
			else if (_lastOptions.TryGetValue(name, out val) && val == value)
			{
				return;
			}
			_lastOptions[name] = value;
			Save();
		}

		public static void Save()
		{
			JObject config;
			if (File.Exists(ConfigPath))
			{
				config = JObject.Parse(File.ReadAllText(ConfigPath));
			}
			else
			{
				Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
				config = new JObject();
			}

			if (_lastConfiguration == null)
			{
				config.Remove("lastConfiguration");
			}
			else
			{
				config["lastConfiguration"] = _lastConfiguration;
			}

			if (_lastPackage == null)
			{
				config.Remove("lastPackage");
			}
			else
			{
				config["lastPackage"] = _lastPackage;
			}

			if (_lastOptions == null || _lastOptions.Count == 0)
			{
				config.Remove("lastOptions");
			}
			else
			{
				config["lastOptions"] = JObject.FromObject(_lastOptions);
			}

			File.WriteAllText(ConfigPath, config.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
		}

		public static void Load()
		{
			JObject config;
			if (File.Exists(ConfigPath))
			{
				config = JObject.Parse(File.ReadAllText(ConfigPath));
			}
			else
			{
				config = new JObject();
			}

			JToken token;
			if (config.TryGetValue("lastConfiguration", out token) && token != null && token.Type == JTokenType.String)			
			{
				string s = (string)token;
				if (string.IsNullOrEmpty(s))
				{
					s = null;
				}
				_lastConfiguration = s;
			}
			else
			{
				_lastConfiguration = null;
			}

			if (config.TryGetValue("lastPackage", out token) && token != null && token.Type == JTokenType.String)			
			{
				string s = (string)token;
				if (string.IsNullOrEmpty(s))
				{
					s = null;
				}
				_lastPackage = s;
			}
			else
			{
				_lastPackage = null;
			}

			_lastOptions = null;
			if (config.TryGetValue("lastOptions", out token) && token != null && token.Type == JTokenType.Object)			
			{
				_lastOptions = token.ToObject<Dictionary<string, string>>();
			}
		}
	}
}

