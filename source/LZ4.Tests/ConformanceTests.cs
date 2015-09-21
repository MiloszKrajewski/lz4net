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
		#region consts

		private const int MAXIMUM_LENGTH = 1 * 10 * 1024 * 1024; // 10MB

		#endregion

		#region utilities

		private void TestConformance(TimedMethod[] compressors, TimedMethod[] decompressors)
		{
			var provider = new BlockDataProvider(Utilities.GetSilesiaCorpusFolder());

			var r = new Random(0);

			Console.WriteLine("Architecture: {0}bit", IntPtr.Size * 8);

			var total = 0;
			const long limit = 1L * 1024 * 1024 * 1024;
			var last_pct = 0;

			while (total < limit)
			{
				var length = Utilities.RandomLength(r, MAXIMUM_LENGTH);
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
		}

		#endregion

		[TestFixtureSetUp]
		public void Setup()
		{
			Utilities.Download();
		}

		[Test]
		public void TestCompressionConformance()
		{
			var compressors = new[] {
				new TimedMethod("MixedMode 64", (b, l) => LZ4mm.LZ4Codec.Encode64(b, 0, l)),
				new TimedMethod("MixedMode 32", (b, l) => LZ4mm.LZ4Codec.Encode32(b, 0, l)),
				new TimedMethod("C++/CLI 64", (b, l) => LZ4cc.LZ4Codec.Encode64(b, 0, l)),
				new TimedMethod("C++/CLI 32", (b, l) => LZ4cc.LZ4Codec.Encode32(b, 0, l)),
				new TimedMethod("Unsafe 64", (b, l) => LZ4pn.LZ4Codec.Encode64(b, 0, l)),
				new TimedMethod("Unsafe 32", (b, l) => LZ4pn.LZ4Codec.Encode32(b, 0, l)),
				new TimedMethod("Safe 64", (b, l) => LZ4ps.LZ4Codec.Encode64(b, 0, l)),
				new TimedMethod("Safe 32", (b, l) => LZ4ps.LZ4Codec.Encode32(b, 0, l)),
			};

			var decompressors = new[] {
				new TimedMethod("MixedMode 64", (b, l) => LZ4mm.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("MixedMode 32", (b, l) => LZ4mm.LZ4Codec.Decode32(b, 0, b.Length, l)),
				new TimedMethod("C++/CLI 64", (b, l) => LZ4cc.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("C++/CLI 32", (b, l) => LZ4cc.LZ4Codec.Decode32(b, 0, b.Length, l)),
				new TimedMethod("Unsafe 64", (b, l) => LZ4pn.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("Unsafe 32", (b, l) => LZ4pn.LZ4Codec.Decode32(b, 0, b.Length, l)),
				new TimedMethod("Safe 64", (b, l) => LZ4ps.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("Safe 32", (b, l) => LZ4ps.LZ4Codec.Decode32(b, 0, b.Length, l)),
			};

			TestConformance(compressors, decompressors);
		}

		[Test]
		public void TestCompressionConformanceHC()
		{
			var compressors = new[] {
				new TimedMethod("MixedMode 64", (b, l) => LZ4mm.LZ4Codec.Encode64HC(b, 0, l)),
				new TimedMethod("MixedMode 32", (b, l) => LZ4mm.LZ4Codec.Encode32HC(b, 0, l)),
				new TimedMethod("C++/CLI 64", (b, l) => LZ4cc.LZ4Codec.Encode64HC(b, 0, l)),
				new TimedMethod("C++/CLI 32", (b, l) => LZ4cc.LZ4Codec.Encode32HC(b, 0, l)),
				new TimedMethod("Unsafe 64", (b, l) => LZ4pn.LZ4Codec.Encode64HC(b, 0, l)),
				new TimedMethod("Unsafe 32", (b, l) => LZ4pn.LZ4Codec.Encode32HC(b, 0, l)),
				new TimedMethod("Safe 64", (b, l) => LZ4ps.LZ4Codec.Encode64HC(b, 0, l)),
				new TimedMethod("Safe 32", (b, l) => LZ4ps.LZ4Codec.Encode32HC(b, 0, l)),
			};

			var decompressors = new[] {
				new TimedMethod("MixedMode 64", (b, l) => LZ4mm.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("MixedMode 32", (b, l) => LZ4mm.LZ4Codec.Decode32(b, 0, b.Length, l)),
				new TimedMethod("C++/CLI 64", (b, l) => LZ4cc.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("C++/CLI 32", (b, l) => LZ4cc.LZ4Codec.Decode32(b, 0, b.Length, l)),
				new TimedMethod("Unsafe 64", (b, l) => LZ4pn.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("Unsafe 32", (b, l) => LZ4pn.LZ4Codec.Decode32(b, 0, b.Length, l)),
				new TimedMethod("Safe 64", (b, l) => LZ4ps.LZ4Codec.Decode64(b, 0, b.Length, l)),
				new TimedMethod("Safe 32", (b, l) => LZ4ps.LZ4Codec.Decode32(b, 0, b.Length, l)),
			};

			TestConformance(compressors, decompressors);
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
					Utilities.AssertEqual(compressed0, buffer, compressor.Name);
				}
			}

			foreach (var decompressor in decompressors)
			{
				try
				{
					var temp = decompressor.Run(compressed[decompressor.Name], length);
					Utilities.AssertEqual(original, temp, decompressor.Name);
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
