using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace PPTech.Builder.Modules
{
	[Description("Clear Meta")]
	[Guid("46D64A05-DB99-4B4A-BB48-AE8F9B2FFBD3")]
	public class ClearMeta : BuilderModule
	{	
		public override void OnAfterBuild(BuilderState config)
		{
			if (!Directory.Exists(config.buildPath))
			{
				return;
			}

			var log = new StringBuilder();
			foreach (var file in Directory.GetFiles(config.buildPath, "*.meta", SearchOption.AllDirectories))
			{
				log.AppendLine(file);
				File.Delete(file);
			}
			if (log.Length > 0)
			{
				log.Insert(0, "Meta files removed" + Environment.NewLine);
				Debug.Log(log.ToString());
				log.Length = 0;
			}
		}
	}
}