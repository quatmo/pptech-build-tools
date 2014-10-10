using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace PPTech.Builder
{
	public class BuilderState : IDisposable
	{
		private Dictionary<BuilderModule, object> _parameters;
		private List<string> _includedPackages;
		private List<string> _scenes = new List<string>();
		private TextWriter _log;

		public string error { get; set; }
		public Dictionary<BuilderModule, object> parameters 
		{
			get
			{
				return this._parameters ?? (this._parameters = new Dictionary<BuilderModule, object>());
			}
		}
		public List<string> includedPackages
		{
			get
			{
				return this._includedPackages ?? (this._includedPackages = new List<string>());
			}
		}
		public List<string> scenes
		{
			get
			{
				return this._scenes;
			}
		}
		public string buildPath { get; set; }
		public BuildTarget buildTarget { get; set; }
		public BuildOptions buildOptions { get; set; }
		public string productName { get; set; }
		public string bundleIdentifier { get; set; }
		public string version { get; set; }


		public void Log(string message)
		{
			if (this._log == null)
			{
				this._log = new StreamWriter(string.Format("Build {0:yyyy-MM-dd HH-mm-ss}.log", DateTime.Now), false, Encoding.UTF8);
			}
			this._log.WriteLine("[{0:HH:mm:ss.fff}] {1}", DateTime.Now, message);
		}

		public void LogException(Exception ex)
		{
			if (this._log == null)
			{
				this._log = new StreamWriter(string.Format("Build {0:yyyy-MM-dd HH-mm-ss}.log", DateTime.Now), false, Encoding.UTF8);
			}
			this._log.WriteLine("[{0:HH:mm:ss.fff}] {1}", DateTime.Now, ex.ToString());
		}

		public virtual BuilderConfiguration ResolveOption(string name)
		{
			return null;
		}

	#region IDisposable Members

		public virtual void Dispose()
		{
			if (this._log != null)
			{
				this._log.Dispose();
				this._log = null;
			}
		}

	#endregion
		
	}
}