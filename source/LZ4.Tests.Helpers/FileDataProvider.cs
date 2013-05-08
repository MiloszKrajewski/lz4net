using System;
using System.Collections.Generic;
using System.IO;

namespace LZ4.Tests.Helpers
{
	public class FileDataProvider: DataProviderBase
	{
		#region consts

		private const int LARGE_FILE = 1 * 1024 * 1024 * 1024; // 1GB

		#endregion

		#region fields

		private IEnumerator<byte[]> _provider;

		#endregion

		#region constructor

		public FileDataProvider()
		{
		}

		public FileDataProvider(IEnumerable<string> folders)
		{
			foreach (var folder in folders) Folders.Add(folder);
		}

		public FileDataProvider(params string[] folders)
			: this((IEnumerable<string>)folders)
		{
		}

		#endregion

		#region public interface

		public byte[] GetBytes()
		{
			if (_provider == null)
				_provider = EnumerateBlocks().GetEnumerator();
			if (!_provider.MoveNext())
				throw new InvalidOperationException("Cannot read data from empty provider");
			return _provider.Current;
		}

		#endregion

		#region private implmentation

		private IEnumerable<byte[]> EnumerateBlocks()
		{
			foreach (var file in CycleEnumerateFileNames())
			{
				Stream stream = null;

				// skip large files
				long length = new FileInfo(file).Length;
				if (length > LARGE_FILE)
				{
					Console.WriteLine("Skipped (too large): {0}", file);
					continue;
				}
				if (length <= 0)
				{
					Console.WriteLine("Skipped (zero length): {0}", file);
					continue;
				}

				try
				{
					stream = File.OpenRead(file);
					Console.WriteLine(file);
				}
				catch (IOException)
				{
					Console.WriteLine("Skipped (cannot read): {0}", file);
					continue; // ignore - next file please
				}

				using (stream)
				{
					var result = new byte[stream.Length];
					stream.Read(result, 0, result.Length);
					yield return result;
				}
			}
		}

		#endregion
	}
}
