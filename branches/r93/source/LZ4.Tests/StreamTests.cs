using System;
using System.IO;
using System.IO.Compression;
using LZ4.Tests.Helpers;
using NUnit.Framework;

namespace LZ4.Tests
{
	[TestFixture]
	public class StreamTests
	{
		const long TOTAL_SIZE = 1L * 1024 * 1024 * 1024;
		const int CHUNK_SIZE = 2 * 1024 * 1024;

		[Test]
		public void ReadAndWrite()
		{
			DoAction(
				(b, s) => {
					s.WriteByte(b[0]);
					s.Write(b, 1, b.Length - 1);
				},
				false);

			DoAction(
				(b, s) => {
					var buffer = new byte[b.Length];
					buffer[0] = (byte)s.ReadByte();
					s.Read(buffer, 1, buffer.Length - 1);
					Utilities.AssertEqual(b, buffer, "Read");
				},
				true);
		}

		// ReSharper disable InconsistentNaming

		private static void DoAction(Action<byte[], Stream> action, bool read)
		{
			var provider = new BlockDataProvider(Utilities.TEST_DATA_FOLDER);
			var r = new Random(0);

			Console.WriteLine("Architecture: {0}bit", IntPtr.Size*8);
			Console.WriteLine("CodecName: {0}", LZ4Codec.CodecName);

			var fileName = Path.Combine(Path.GetTempPath(), "BlockCompressionStream.dat");

			using (var stream = new LZ4Stream(
				read ? File.OpenRead(fileName) : File.Create(fileName),
				read ? CompressionMode.Decompress : CompressionMode.Compress))
			{
				var total = 0;
				const long limit = TOTAL_SIZE;
				var last_pct = 0;

				while (total < limit)
				{
					var length = Utilities.RandomLength(r, CHUNK_SIZE);
					var block = provider.GetBytes(length);
					action(block, stream);
					total += block.Length;
					var pct = (int)((double)total*100/limit);
					if (pct > last_pct)
					{
						Console.WriteLine("{0}%...", pct);
						last_pct = pct;
					}
				}
			}
		}

		// ReSharper restore InconsistentNaming
	}
}
