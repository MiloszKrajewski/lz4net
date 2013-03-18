using System;
using System.Linq;
using System.Threading;
using LZ4.Tests.Helpers;
using NUnit.Framework;

namespace LZ4.Tests
{
	[TestFixture]
	public class PerformanceTests
	{
		private const string TEST_DATA_FOLDER = @"T:\Temp\Corpus";
		//private const string TEST_DATA_FOLDER = @"D:\Archive\Corpus";

		[Test]
		public void TestCompressionPerformance()
		{
			var compressors = new[] {
				//new TimedMethod("Copy", Copy),
				new TimedMethod("MixedMode 64", (b, l) => LZ4mm.LZ4Codec.Encode64(b, 0, l)),
				//new TimedMethod("MixedMode 32", (b, l) => LZ4mm.LZ4Codec.Encode32(b, 0, l)),
				//new TimedMethod("C++/CLI 64", (b, l) => LZ4cc.LZ4Codec.Encode64(b, 0, l)),
				//new TimedMethod("C++/CLI 32", (b, l) => LZ4cc.LZ4Codec.Encode32(b, 0, l)),
				//new TimedMethod("Unsafe 64", (b, l) => LZ4n.LZ4Codec.Encode64(b, 0, l)),
				//new TimedMethod("Unsafe 32", (b, l) => LZ4n.LZ4Codec.Encode32(b, 0, l)),
				new TimedMethod("Safe 64", (b, l) => LZ4s.LZ4Codec.Encode64(b, 0, l)),
				new TimedMethod("Safe 32", (b, l) => LZ4s.LZ4Codec.Encode32(b, 0, l)),
			};

			var decompressors = new[] {
				//new TimedMethod("Copy", Copy),
				new TimedMethod("MixedMode 64", (b, l) => LZ4mm.LZ4Codec.Decode64(b, 0, b.Length, l)),
				//new TimedMethod("MixedMode 32", (b, l) => LZ4mm.LZ4Codec.Decode32(b, 0, b.Length, l)),
				//new TimedMethod("C++/CLI 64", (b, l) => LZ4cc.LZ4Codec.Decode64(b, 0, b.Length, l)),
				//new TimedMethod("C++/CLI 32", (b, l) => LZ4cc.LZ4Codec.Decode32(b, 0, b.Length, l)),
				//new TimedMethod("Unsafe 64", (b, l) => LZ4n.LZ4Codec.Decode64(b, 0, b.Length, l)),
				//new TimedMethod("Unsafe 32", (b, l) => LZ4n.LZ4Codec.Decode32(b, 0, b.Length, l)),
				new TimedMethod("Safe 64", (b, l) => LZ4s.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("Safe 32", (b, l) => LZ4s.LZ4Codec.Decode64(b, 0, b.Length, l)),
			};

			var names = compressors.Select(c => c.Name).ToArray();

			foreach (var name in names)
			{
				var compressor = compressors.First(c => c.Name == name);
				var decompressor = decompressors.First(d => d.Name == name);

				Console.WriteLine("---- {0} ----", name);

				Warmup(compressor, decompressor);

				var provider = new FileDataProvider(TEST_DATA_FOLDER);
				long total = 0;
				const long limit = 1L * 1024 * 1024 * 1024;
				var last_pct = 0;

				while (total < limit)
				{
					var block = provider.GetBytes();
					TestSpeed(block, compressor, decompressor);
					total += block.Length;
					var pct = (int)((double)total * 100 / limit);
					if (pct > last_pct)
					{
						Console.WriteLine("{0}%...", pct);
						last_pct = pct;
					}
				}

				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
				Thread.Sleep(1000);
			}

			Console.WriteLine("---- Results ----");

			Console.WriteLine("Architecture: {0}bit", IntPtr.Size * 8);
			Console.WriteLine("Compression:");
			foreach (var compressor in compressors)
			{
				Console.WriteLine("  {0}: {1:0.00}MB/s ({2:0.00}%)", compressor.Name, compressor.Speed, compressor.Ratio);
			}

			Console.WriteLine("Decompression:");
			foreach (var decompressor in decompressors)
			{
				Console.WriteLine("  {0}: {1:0.00}MB/s", decompressor.Name, decompressor.Speed);
			}
		}

		private static byte[] Copy(byte[] b, int l)
		{
			var result = new byte[l];
			Buffer.BlockCopy(b, 0, result, 0, l);
			return result;
		}

		private static void Warmup(TimedMethod compressor, TimedMethod decompressor)
		{
			const int length = 1 * 1024 * 1024;
			var data = new byte[length];
			var gen = new Random(0);
			gen.NextBytes(data);

			var compressed = compressor.Warmup(data, length);
			AssertEqual(data, decompressor.Warmup(compressed, length), compressor.Name);
		}

		private static void TestSpeed(byte[] original, TimedMethod compressor, TimedMethod decompressor)
		{
			int length = original.Length;
			byte[] compressed = compressor.Run(original, length);
			AssertEqual(original, decompressor.Run(compressed, length), compressor.Name);
		}

		private static void AssertEqual(byte[] expected, byte[] actual, string name)
		{
			Assert.AreEqual(expected.Length, actual.Length, string.Format("Buffers are different length ({0})", name));
			var length = expected.Length;

			for (int i = 0; i < length; i++)
			{
				if (expected[i] != actual[i]) Assert.Fail("Buffer differ @ {0} ({1})", i, name);
			}
		}
	}
}