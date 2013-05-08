using System;
using System.Reflection;
using System.IO;

namespace LZ4.MixedModeAutoLoad
{
	public static class AutoLoad3264
	{
		// http://scottbilas.com/blog/automatically-choose-32-or-64-bit-mixed-mode-dlls/
		public static void Register(string assemblyName, string executableFolder = null)
		{
			if (executableFolder == null)
			{
				var assembly = Assembly.GetEntryAssembly() ?? typeof(AutoLoad3264).Assembly;
				string fileName = assembly.Location;
				executableFolder = Path.GetDirectoryName(fileName);
			}

			AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
				var n = new AssemblyName(e.Name);
				if (string.Compare(assemblyName, n.Name, true) == 0)
				{
					string fileName = Path.Combine(
						executableFolder,
						string.Format("{0}.{1}.dll", assemblyName, (IntPtr.Size == 4) ? "x86" : "x64"));
					return Assembly.LoadFile(fileName);
				}
				return null;
			};
		}
	}
}
