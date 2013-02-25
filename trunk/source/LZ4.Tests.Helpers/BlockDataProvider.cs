using System;
using System.Collections.Generic;
using System.IO;

namespace LZ4.Tests.Helpers
{
	public class BlockDataProvider: DataProviderBase
	{
		#region fields

		private IEnumerator<byte[]> _provider;
		private byte[] _buffer;
		private int _length;
		private int _offset;

		#endregion

		#region constructor

		public BlockDataProvider()
		{
		}

		public BlockDataProvider(IEnumerable<string> folders)
		{
			foreach (var folder in folders) Folders.Add(folder);
		}

		public BlockDataProvider(params string[] folders)
			: this((IEnumerable<string>)folders)
		{
		}

		#endregion

		#region public interface

		public byte[] GetBytes(int length)
		{
			var result = new byte[length];
			GetBytes(result, 0, length);
			return result;
		}

		public void GetBytes(byte[] buffer, int offset, int length)
		{
			while (length > 0)
			{
				var left = _length - _offset;
				if (left == 0)
				{
					if (_provider == null)
						_provider = EnumerateBlocks().GetEnumerator();
					if (!_provider.MoveNext())
						throw new InvalidOperationException("Cannot read data from empty provider");
					_buffer = _provider.Current;
					_offset = 0;
					_length = _buffer.Length;
					left = _length;
				}
				var bytes = Math.Min(left, length);
				Buffer.BlockCopy(_buffer, _offset, buffer, offset, bytes);
				_offset += bytes;
				offset += bytes;
				length -= bytes;
			}
		}

		#endregion

		#region private implmentation

		private IEnumerable<byte[]> EnumerateBlocks()
		{
			var length = 0x10000;
			var result = new byte[length];
			var offset = 0;

			foreach (var file in CycleEnumerateFileNames())
			{
				Stream stream = null;

				try
				{
					stream = File.OpenRead(file);
					Console.WriteLine(file);
				}
				catch (IOException)
				{
					continue; // ignore - next file please
				}

				using (stream)
				{
					while (true)
					{
						if (offset >= length)
						{
							yield return result;
							result = new byte[length];
							offset = 0;
						}

						var read = stream.Read(result, offset, length - offset);
						offset += read;

						if (read == 0) break;
					}
				}
			}
		}

		#endregion
	}
}
