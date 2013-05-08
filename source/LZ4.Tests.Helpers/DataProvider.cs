using System.Collections.Generic;
using System.IO;

namespace LZ4.Tests.Helpers
{
	public class DataProviderBase
	{
		#region fields

		private readonly List<string> _folders = new List<string>();

		#endregion

		#region public interface

		public IList<string> Folders { get { return _folders; } }

		#endregion

		#region protected interface

		protected IEnumerable<string> CycleEnumerateFileNames()
		{
			return Cycle(EnumerateFileNames());
		}

		#endregion

		#region private implementation

		// ReSharper disable FunctionNeverReturns
		// ReSharper disable PossibleMultipleEnumeration

		private static IEnumerable<T> Cycle<T>(IEnumerable<T> collection)
		{
			while (true)
			{
				foreach (var item in collection) yield return item;
			}
		}

		// ReSharper restore PossibleMultipleEnumeration
		// ReSharper restore FunctionNeverReturns

		private IEnumerable<string> EnumerateFileNames()
		{
			foreach (var folder in _folders)
			{
				foreach (var file in EnumerateFileNames(folder))
					yield return file;
			}
		}

		private static IEnumerable<string> EnumerateFileNames(string folder)
		{
			foreach (var file in Directory.GetFiles(folder))
			{
				yield return file;
			}

			foreach (var subfolder in Directory.GetDirectories(folder))
			{
				foreach (var file in EnumerateFileNames(Path.Combine(folder, subfolder)))
				{
					yield return file;
				}
			}
		}

		#endregion
	}
}
