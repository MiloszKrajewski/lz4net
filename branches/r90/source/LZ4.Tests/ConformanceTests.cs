using System;
using System.Collections.Generic;
using NUnit.Framework;
using LZ4.Tests.Helpers;

// ReSharper disable InconsistentNaming

namespace LZ4.Tests
{
	[TestFixture]
	public class ConformanceTests
	{
		private const int MAXIMUM_LENGTH = 1 * 10 * 1024 * 1024; // 10MB
		private const string TEST_DATA_FOLDER = @"T:\Temp\Corpus";
		//private const string TEST_DATA_FOLDER = @"D:\Archive\Corpus";

		[Test]
		public void TestCompressionConformance()
		{
			var provider = new BlockDataProvider(TEST_DATA_FOLDER);
			
			var r = new Random(0);

			Console.WriteLine("Architecture: {0}bit", IntPtr.Size * 8);

			var compressors = new[] {
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
				new TimedMethod("MixedMode 64", (b, l) => LZ4mm.LZ4Codec.Decode64(b, 0, b.Length, l)),
				//new TimedMethod("MixedMode 32", (b, l) => LZ4mm.LZ4Codec.Decode32(b, 0, b.Length, l)),
				//new TimedMethod("C++/CLI 64", (b, l) => LZ4cc.LZ4Codec.Decode64(b, 0, b.Length, l)),
				//new TimedMethod("C++/CLI 32", (b, l) => LZ4cc.LZ4Codec.Decode32(b, 0, b.Length, l)),
				//new TimedMethod("Unsafe 64", (b, l) => LZ4n.LZ4Codec.Decode64(b, 0, b.Length, l)),
				//new TimedMethod("Unsafe 32", (b, l) => LZ4n.LZ4Codec.Decode32(b, 0, b.Length, l)),
				new TimedMethod("Safe 64", (b, l) => LZ4s.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("Safe 32", (b, l) => LZ4s.LZ4Codec.Decode32(b, 0, b.Length, l)),
			};

			var total = 0;
			const long limit = 1L * 1024 * 1024 * 1024;
			var last_pct = 0;

			while (total < limit)
			{
				var length = RandomLength(r, MAXIMUM_LENGTH);
				var block = provider.GetBytes(length);
				TestData(block, compressors, decompressors);
				total += block.Length;
				var pct = (int)((double)total * 100 / limit);
				if (pct > last_pct)
				{
					Console.WriteLine("{0}%...", pct);
					last_pct = pct;
				}
			}

			/*
			
			The performance results from this test are completely unreliable 
			Too much garbage collection and caching.
			So, no need to mislead anyone.
			
			Console.WriteLine("Compression:");
			foreach (var compressor in compressors)
			{
				Console.WriteLine("  {0}: {1:0.00}MB/s", compressor.Name, compressor.Speed);
			}

			Console.WriteLine("Decompression:");
			foreach (var decompressor in decompressors)
			{
				Console.WriteLine("  {0}: {1:0.00}MB/s", decompressor.Name, decompressor.Speed);
			}
			*/
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

		private int RandomLength(Random generator, int maximum)
		{
			return (int)Math.Exp(generator.NextDouble() * Math.Log(maximum));
		}

		private static void TestData(
			byte[] original, 
			IEnumerable<TimedMethod> compressors, 
			IEnumerable<TimedMethod> decompressors)
		{
			int length = original.Length;
			var compressed = new Dictionary<string, byte[]>();
			byte[] compressed0 = null;

			foreach (var compressor in compressors)
			{
				var buffer = compressor.Run(original, length);
				compressed[compressor.Name] = buffer;
				if (compressed0 == null)
				{
					compressed0 = buffer;
				}
				else if (compressor.Identical)
				{
					AssertEqual(compressed0, buffer, compressor.Name);
				}
			}

			foreach (var decompressor in decompressors)
			{
				try
				{
					var temp = decompressor.Run(compressed[decompressor.Name], length);
					AssertEqual(original, temp, decompressor.Name);
				}
				catch
				{
					Console.WriteLine("Failed in: {0}", decompressor.Name);
					throw;
				}
			}
		}
	}
}

// ReSharper restore InconsistentNaming
