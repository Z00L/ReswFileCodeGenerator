using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ReswCodeGen.CustomTool
{
	public static class VisualStudioHelper
	{
		public static VisualStudioVersion GetVersion()
		{
			var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
			var vsVersion = VisualStudioVersion.Vs2012;
			if (dte != null)
			{
				Version dteVersion;
				if (Version.TryParse(dte.Version, out dteVersion))
				{
					if (dteVersion >= new Version(14, 0))
						vsVersion = VisualStudioVersion.Vs2015;
					else if (dteVersion >= new Version(12, 0))
						vsVersion = VisualStudioVersion.Vs2013;
				}
			}
			return vsVersion;
		}
	}
}