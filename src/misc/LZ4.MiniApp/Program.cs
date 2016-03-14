using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace LZ4.MiniApp
{
	class Program
	{
		static int Main(string[] args)
		{
			Trace.Listeners.Add(new ConsoleTraceListener());

			return new Func<int>(() => {
				Console.WriteLine("Version 7");
				Console.WriteLine("Architecture: {0}", IntPtr.Size);
				Console.WriteLine(LZ4Codec.CodecName);

				if (args.Length == 0)
				{
					Console.WriteLine("No filenames given");
					return -1;
				}

				var success = args.All(TestFile);

				if (!success)
				{
					Console.WriteLine("FAIL!");
					return -1;
				}

				return 0;
			})();
		}

		private static bool TestFile(string fn)
		{
			try
			{
				var hash1 = Copy(fn);
				Console.WriteLine("Hash: {0}", hash1);
				var hash2 = Check(fn);
				Console.WriteLine("Hash: {0}", hash2);
				if (hash1 != hash2)
				{
					Console.WriteLine("ERROR! Hash mismatch");
					return false;
				}
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine("ERROR: {0}", e);
				return false;
			}
		}

		private static string Copy(string fn)
		{
			Console.WriteLine("Compressing {0}", fn);
			var md5 = MD5.Create();
			var buffer = new byte[0x10000];
			using (var finput = File.OpenRead(fn))
			using (var foutput = File.Create(fn + ".lz4"))
			using (var zoutput = new LZ4Stream(foutput, LZ4StreamMode.Compress))
			{
				while (true)
				{
					var length = finput.Read(buffer, 0, buffer.Length);
					if (length == 0) break;
					md5.TransformBlock(buffer, 0, length, buffer, 0);
					zoutput.Write(buffer, 0, length);
				}
				md5.TransformFinalBlock(buffer, 0, 0);
				return Convert.ToBase64String(md5.Hash);
			}
		}

		private static string Check(string fn)
		{
			Console.WriteLine("Testing {0}", fn);
			var md5 = MD5.Create();
			var buffer = new byte[0x10000];
			using (var finput = File.OpenRead(fn + ".lz4"))
			using (var zinput = new LZ4Stream(finput, LZ4StreamMode.Decompress))
			{
				while (true)
				{
					var length = zinput.Read(buffer, 0, buffer.Length);
					if (length == 0) break;
					md5.TransformBlock(buffer, 0, length, buffer, 0);
				}
				md5.TransformFinalBlock(buffer, 0, 0);
				return Convert.ToBase64String(md5.Hash);
			}
		}
	}
}
