using System;
using LZ4.Tests.Helpers;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;

#if !X64
using LZOHelper;
#endif

namespace LZ4.Tests.Continuous
{
	// ReSharper disable InconsistentNaming

	class Program
	{
		#region Main

		static void Main(string[] args)
		{
			try
			{
				if (args.Length != 4)
				{
					Console.WriteLine("LZ4.Infinity <results> <path> <length> <codec>");
					Console.WriteLine("  <results>: XML file with results (will be created if does not exist)");
					Console.WriteLine("  <path>: Folders with files to test compression/decompression (separated by semicolon)");
					Console.WriteLine("  <length>: Number od MiB (1024*1024 bytes) to use for test");
					Console.WriteLine("  <codec>: codec to use");
					Console.WriteLine("Available codecs");
					Console.WriteLine("  MixedModeX, CppCliX, UnsafeX, SafeX, LZ4SharpX");
					Console.WriteLine("  where X is 32 or 64 depending which version should be used");
				}

				string results = args[0];
				string path = args[1];
				long bytes = long.Parse(args[2]) * 1024 * 1024;
				string codec = args[3];
				string architecture = IntPtr.Size == 4 ? "32" : "64";

				Run(results, path, bytes, codec, architecture);
			}
			catch (Exception e)
			{
				Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
			}
		}

		#endregion

		#region Run

		private static void Warmup(TimedMethod compressor, TimedMethod decompressor)
		{
			Console.WriteLine("Warming up...");

			const int length = 1 * 1024 * 1024;
			var data = new byte[length];
			var gen = new Random(0);
			gen.NextBytes(data);
			var compressed = compressor.Warmup(data, length);
			decompressor.Warmup(compressed, length);
		}

		private static void Run(string resultsFile, string path, long limit, string codec, string architecture)
		{
			var provider = new FileDataProvider(path.Split(';'));
			TimedMethod compressor;
			TimedMethod decompressor;

			var codecCode = SelectCodec(codec, architecture, out compressor, out decompressor);

			Warmup(compressor, decompressor);

			long total = 0;
			var pct = 0;

			while (total < limit)
			{
				var original = provider.GetBytes();
				var length = original.Length;
				var compressed = compressor.Run(original, length);
				var decompressed = decompressor.Run(compressed, length);
				AssertEqual(original, decompressed);
				total += length;

				var new_pct = (int)(total * 100 / limit);
				if (new_pct > pct)
				{
					Console.WriteLine("{0}%", new_pct);
					pct = new_pct;
				}
			}

			Console.WriteLine("{0}: {1:0.00} / {2:0.00}", codecCode, compressor.Speed, decompressor.Speed);

			UpdateResults(resultsFile, codecCode, compressor, decompressor);
		}

		#endregion

		#region SelectCodec

		private static string SelectCodec(
			string codec, string architecture, 
			out TimedMethod compressor, 
			out TimedMethod decompressor)
		{
			string codecCode;
			switch (codec.ToLower())
			{
				// compare implementation for LZ4 only

				case "mixedmode32":
					codecCode = string.Format("MixedMode32@{0}", architecture);
					compressor = new TimedMethod(codecCode, (b, l) => LZ4mm.LZ4Codec.Encode32(b, 0, l));
					decompressor = new TimedMethod(codecCode, (b, l) => LZ4mm.LZ4Codec.Decode32(b, 0, b.Length, l));
					break;
				case "mixedmode64":
					codecCode = string.Format("MixedMode64@{0}", architecture);
					compressor = new TimedMethod(codecCode, (b, l) => LZ4mm.LZ4Codec.Encode64(b, 0, l));
					decompressor = new TimedMethod(codecCode, (b, l) => LZ4mm.LZ4Codec.Decode64(b, 0, b.Length, l));
					break;
				case "cppcli32":
					codecCode = string.Format("C++/CLI32@{0}", architecture);
					compressor = new TimedMethod(codecCode, (b, l) => LZ4cc.LZ4Codec.Encode32(b, 0, l));
					decompressor = new TimedMethod(codecCode, (b, l) => LZ4cc.LZ4Codec.Decode32(b, 0, b.Length, l));
					break;
				case "cppcli64":
					codecCode = string.Format("C++/CLI64@{0}", architecture);
					compressor = new TimedMethod(codecCode, (b, l) => LZ4cc.LZ4Codec.Encode64(b, 0, l));
					decompressor = new TimedMethod(codecCode, (b, l) => LZ4cc.LZ4Codec.Decode64(b, 0, b.Length, l));
					break;
				case "unsafe64":
					codecCode = string.Format("Unsafe64@{0}", architecture);
					compressor = new TimedMethod(codecCode, (b, l) => LZ4n.LZ4Codec.Encode64(b, 0, l));
					decompressor = new TimedMethod(codecCode, (b, l) => LZ4n.LZ4Codec.Decode64(b, 0, b.Length, l));
					break;
				case "unsafe32":
					codecCode = string.Format("Unsafe32@{0}", architecture);
					compressor = new TimedMethod(codecCode, (b, l) => LZ4n.LZ4Codec.Encode32(b, 0, l));
					decompressor = new TimedMethod(codecCode, (b, l) => LZ4n.LZ4Codec.Decode32(b, 0, b.Length, l));
					break;
				case "safe64":
					codecCode = string.Format("Safe64@{0}", architecture);
					compressor = new TimedMethod(codecCode, (b, l) => LZ4s.LZ4Codec.Encode64(b, 0, l));
					decompressor = new TimedMethod(codecCode, (b, l) => LZ4s.LZ4Codec.Decode64(b, 0, b.Length, l));
					break;
				case "safe32":
					codecCode = string.Format("Safe32@{0}", architecture);
					compressor = new TimedMethod(codecCode, (b, l) => LZ4s.LZ4Codec.Encode32(b, 0, l));
					decompressor = new TimedMethod(codecCode, (b, l) => LZ4s.LZ4Codec.Decode32(b, 0, b.Length, l));
					break;

				// compare different algorithms (LZ4, LZO, Snappy, QuickLZ)

				case "lz4.native":
					codecCode = string.Format("LZ4.native@{0}", architecture);
					if (IntPtr.Size == 4)
					{
						compressor = new TimedMethod(codecCode, (b, l) => LZ4mm.LZ4Codec.Encode32(b, 0, l));
						decompressor = new TimedMethod(codecCode, (b, l) => LZ4mm.LZ4Codec.Decode32(b, 0, b.Length, l));
					}
					else
					{
						compressor = new TimedMethod(codecCode, (b, l) => LZ4mm.LZ4Codec.Encode64(b, 0, l));
						decompressor = new TimedMethod(codecCode, (b, l) => LZ4mm.LZ4Codec.Decode64(b, 0, b.Length, l));
					}
					break;

				case "lz4.unsafe":
					codecCode = string.Format("LZ4.unsafe@{0}", architecture);
					if (IntPtr.Size == 4)
					{
						compressor = new TimedMethod(codecCode, (b, l) => LZ4n.LZ4Codec.Encode32(b, 0, l));
						decompressor = new TimedMethod(codecCode, (b, l) => LZ4n.LZ4Codec.Decode64(b, 0, b.Length, l));
					}
					else
					{
						compressor = new TimedMethod(codecCode, (b, l) => LZ4n.LZ4Codec.Encode64(b, 0, l));
						decompressor = new TimedMethod(codecCode, (b, l) => LZ4n.LZ4Codec.Decode32(b, 0, b.Length, l));
					}
					break;

#if !X64
				// LZO is available only in 32-bit mode

				case "lzo1x.native":
					codecCode = string.Format("LZO1X.native@{0}", architecture);
					compressor = new TimedMethod(codecCode, NativeLZO1XCompressor);
					decompressor = new TimedMethod(codecCode, NativeLZODecompressor);
					break;

				case "lzo1x11.native":
					codecCode = string.Format("LZO1X11.native@{0}", architecture);
					compressor = new TimedMethod(codecCode, NativeLZO1X11Compressor);
					decompressor = new TimedMethod(codecCode, NativeLZODecompressor);
					break;

				case "lzo1x12.native":
					codecCode = string.Format("LZO1X12.native@{0}", architecture);
					compressor = new TimedMethod(codecCode, NativeLZO1X12Compressor);
					decompressor = new TimedMethod(codecCode, NativeLZODecompressor);
					break;

				case "lzo1x15.native":
					codecCode = string.Format("LZO1X15.native@{0}", architecture);
					compressor = new TimedMethod(codecCode, NativeLZO1X15Compressor);
					decompressor = new TimedMethod(codecCode, NativeLZODecompressor);
					break;

				case "lzo1x999.native":
					codecCode = string.Format("LZO1X999.native@{0}", architecture);
					compressor = new TimedMethod(codecCode, NativeLZO1X999Compressor);
					decompressor = new TimedMethod(codecCode, NativeLZODecompressor);
					break;

#endif

				case "lz4sharp.unsafe":
					codecCode = string.Format("LZ4Sharp.unsafe@{0}", architecture);
					if (IntPtr.Size == 4)
					{
						var lz4sharp_compressor32 = new LZ4Sharp.LZ4Compressor32();
						var lz4sharp_decompressor32 = new LZ4Sharp.LZ4Decompressor32();
						compressor = new TimedMethod(codecCode, (b, l) => lz4sharp_compressor32.Compress(b));
						decompressor = new TimedMethod(codecCode, (b, l) =>
							{
								var output = new byte[l];
								lz4sharp_decompressor32.DecompressKnownSize(b, output);
								return output;
							});
					}
					else
					{
						var lz4sharp_compressor64 = new LZ4Sharp.LZ4Compressor64();
						var lz4sharp_decompressor64 = new LZ4Sharp.LZ4Decompressor64();
						compressor = new TimedMethod(codecCode, (b, l) => lz4sharp_compressor64.Compress(b));
						decompressor = new TimedMethod(codecCode, (b, l) =>
							{
								var output = new byte[l];
								lz4sharp_decompressor64.DecompressKnownSize(b, output);
								return output;
							});
					}
					break;

				case "quicklz.native":
					codecCode = string.Format("QuickLZ.native@{0}", architecture);
					compressor = new TimedMethod(codecCode, NativeQuickLZCompressor);
					decompressor = new TimedMethod(codecCode, NativeQuickLZDecompressor);
					break;

				case "snappy.native":
					codecCode = string.Format("Snappy.native@{0}", architecture);
					compressor = new TimedMethod(codecCode, NativeSnappyCompressor);
					decompressor = new TimedMethod(codecCode, NativeSnappyDecompressor);
					break;

				// safe compressors

				case "lz4.safe":
					codecCode = string.Format("LZ4.safe@{0}", architecture);
					if (IntPtr.Size == 4)
					{
						compressor = new TimedMethod(codecCode, (b, l) => LZ4s.LZ4Codec.Encode32(b, 0, l));
						decompressor = new TimedMethod(codecCode, (b, l) => LZ4s.LZ4Codec.Decode64(b, 0, b.Length, l));
					}
					else
					{
						compressor = new TimedMethod(codecCode, (b, l) => LZ4s.LZ4Codec.Encode32(b, 0, l));
						decompressor = new TimedMethod(codecCode, (b, l) => LZ4s.LZ4Codec.Decode64(b, 0, b.Length, l));
					}
					break;

				case "lzf.safe":
					codecCode = string.Format("LZF.safe@{0}", architecture);
					compressor = new TimedMethod(codecCode, SafeLZFCompressor);
					decompressor = new TimedMethod(codecCode, SafeLZFDecompressor);
					break;

				case "quicklz1.safe":
					codecCode = string.Format("QuickLZ1.safe@{0}", architecture);
					compressor = new TimedMethod(codecCode, SafeQuickLZ1Compressor);
					decompressor = new TimedMethod(codecCode, SafeQuickLZDecompressor);
					break;

				case "quicklz3.safe":
					codecCode = string.Format("QuickLZ3.safe@{0}", architecture);
					compressor = new TimedMethod(codecCode, SafeQuickLZ3Compressor);
					decompressor = new TimedMethod(codecCode, SafeQuickLZDecompressor);
					break;

				case "deflate.safe":
					codecCode = string.Format("Deflate.safe@{0}", architecture);
					compressor = new TimedMethod(codecCode, DeflateCompressor);
					decompressor = new TimedMethod(codecCode, DeflateDecompressor);
					break;

				default:
					throw new ArgumentException(string.Format("Unknown codec: {0}", codec));
			}
			return codecCode;
		}

		#region adapters

		#region LZO adapters

#if !X64

		private static byte[] NativeLZO1XCompressor(byte[] input, int length)
		{
			return LZOCompressor.Compress(input, CompressionAlgorithm.LZO1X);
		}

		private static byte[] NativeLZO1X11Compressor(byte[] input, int length)
		{
			return LZOCompressor.Compress(input, CompressionAlgorithm.LZO1X11);
		}

		private static byte[] NativeLZO1X12Compressor(byte[] input, int length)
		{
			return LZOCompressor.Compress(input, CompressionAlgorithm.LZO1X12);
		}

		private static byte[] NativeLZO1X15Compressor(byte[] input, int length)
		{
			return LZOCompressor.Compress(input, CompressionAlgorithm.LZO1X15);
		}

		private static byte[] NativeLZO1X999Compressor(byte[] input, int length)
		{
			return LZOCompressor.Compress(input, CompressionAlgorithm.LZO1X999);
		}

		private static byte[] NativeLZODecompressor(byte[] input, int outputLength)
		{
			return LZOCompressor.Decompress(input);
		}

#endif

		#endregion

		#region QuickLZ adapters

		private static readonly QuickLZ.PI.Codec NativeQuickLZCodec = new QuickLZ.PI.Codec();

		private static byte[] NativeQuickLZCompressor(byte[] input, int length)
		{
			return NativeQuickLZCodec.Compress(input);
		}

		private static byte[] NativeQuickLZDecompressor(byte[] input, int outputLength)
		{
			return NativeQuickLZCodec.Decompress(input);
		}

		#endregion

		#region Snappy adapters

		private static byte[] NativeSnappyCompressor(byte[] input, int length)
		{
			return SnappyPI.SnappyCodec.Compress(input, 0, length);
		}

		private static byte[] NativeSnappyDecompressor(byte[] input, int outputLength)
		{
			return SnappyPI.SnappyCodec.Uncompress(input, 0, input.Length);
		}

		#endregion

		#region LZF adapters

		private static byte[] SafeLZFCompressor(byte[] input, int length)
		{
			var output = new byte[length * 2];
			length = LZF.LZF.Compress(input, length, output, length * 2);
			var result = new byte[length];
			Buffer.BlockCopy(output, 0, result, 0, length);
			return result;
		}

		private static byte[] SafeLZFDecompressor(byte[] input, int outputLength)
		{
			var result = new byte[outputLength];
			LZF.LZF.Decompress(input, input.Length, result, outputLength);
			return result;
		}

		#endregion

		#region Safe QuickLZ adapters

		private static byte[] SafeQuickLZ1Compressor(byte[] input, int length)
		{
			return QuickLZ.S.Codec.compress(input, 1);
		}

		private static byte[] SafeQuickLZ3Compressor(byte[] input, int length)
		{
			return QuickLZ.S.Codec.compress(input, 3);
		}

		private static byte[] SafeQuickLZDecompressor(byte[] input, int outputLength)
		{
			return QuickLZ.S.Codec.decompress(input);
		}

		#endregion

		#region Defalte adapters

		private static byte[] DeflateCompressor(byte[] input, int length)
		{
			using (var stream = new MemoryStream())
			{
				using (var deflate = new DeflateStream(stream, CompressionMode.Compress))
				{
					deflate.Write(input, 0, length);
				}
				return stream.ToArray();
			}
		}

		private static byte[] DeflateDecompressor(byte[] input, int outputLength)
		{
			using (var stream = new MemoryStream(input))
			using (var inflate = new DeflateStream(stream, CompressionMode.Decompress))
			{
				var temp = new byte[outputLength];
				inflate.Read(temp, 0, outputLength);
				return temp;
			}
		}

		#endregion

		#endregion

		#endregion

		#region UpdateResults

		private static void UpdateResults(string resultsFile, string codecCode, TimedMethod compressor, TimedMethod decompressor)
		{
			Console.WriteLine("Updating results");

			var serializer = new XmlSerializer(typeof(Results));
			Results results;

			if (File.Exists(resultsFile))
			{
				using (var stream = File.OpenRead(resultsFile)) results = (Results)serializer.Deserialize(stream);
			}
			else
			{
				results = new Results();
			}

			if (results.Update(codecCode, compressor.Speed, decompressor.Speed, compressor.InputLength, compressor.OutputLength))
			{
				Console.WriteLine("IMPROVED!!!");
			}

			using (var stream = File.Create(resultsFile)) serializer.Serialize(stream, results);
			results.SaveAsCSV(resultsFile + ".csv");

			Console.WriteLine("---- Latest results ----");
			foreach (var item in results.Items)
			{
				Console.WriteLine("  {0}: {1:0.00} / {2:0.00}", item.Codec, item.CompressionSpeed, item.DecompressionSpeed);
			}
			Console.WriteLine("------------------------");
		}

		#endregion

		#region AssertEqual

		private static void AssertEqual(byte[] expected, byte[] actual)
		{
			if (expected.Length != actual.Length)
				throw new ArgumentException("Not equal buffer sizes");

			var length = expected.Length;

			for (var i = 0; i < length; i++)
			{
				if (expected[i] != actual[i])
					throw new ArgumentException(string.Format("Buffer are different @ {0}", i));
			}
		}

		#endregion
	}
}

// ReSharper restore InconsistentNaming
