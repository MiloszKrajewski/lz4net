using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Ionic.BZip2;
using NUnit.Framework;

namespace LZ4.Tests
{
	public class Utilities
	{
		private const string TEST_DATA_FOLDER = @".\Corpus";
		private const string SILESIA_CORPUS_URL = "http://sun.aei.polsl.pl/~sdeor/corpus";

		public const string LoremIpsum = 
			"Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod " +
			"tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, " +
			"quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo " +
			"consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse " +
			"cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat " +
			"non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

		public static void AssertEqual(byte[] expected, byte[] actual, string name)
		{
			var length = Math.Min(expected.Length, actual.Length);

			for (var i = 0; i < length; i++)
			{
				if (expected[i] != actual[i]) Assert.Fail("Buffer differ @ {0} ({1})", i, name);
			}

			Assert.AreEqual(expected.Length, actual.Length, string.Format("Buffers are different length ({0})", name));
		}

		public static int RandomLength(Random generator, int maximum)
		{
			return (int)Math.Exp(generator.NextDouble() * Math.Log(maximum));
		}

		public static string GetSilesiaCorpusFolder()
		{
			Download();
			return TEST_DATA_FOLDER;
		}

		public static void Download()
		{
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "dickens");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "mozilla");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "mr");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "nci");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "ooffice");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "osdb");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "reymont");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "samba");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "sao");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "webster");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "xml");
			Download(SILESIA_CORPUS_URL, TEST_DATA_FOLDER, "x-ray");
		}

		private static void Download(string source, string target, string filename)
		{
			var sourceUrl = source + "/" + filename + ".bz2";
			var targetZip = Path.Combine(target, filename + ".bz2");
			var targetFile = Path.Combine(target, filename);
			if (File.Exists(targetFile))
				return;

			// ReSharper disable once AssignNullToNotNullAttribute
			Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

			Console.WriteLine(string.Format("Downloading '{0}'", sourceUrl));

			if (!File.Exists(targetZip))
			{
				using (var wc = new WebClient())
				{
					wc.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
					wc.DownloadFile(sourceUrl, targetZip);
				}
			}

			using (var istream = File.OpenRead(targetZip))
			using (var zstream = new BZip2InputStream(istream))
			using (var ostream = File.OpenWrite(targetFile))
			{
				CopyStream(zstream, ostream);
			}

			File.Delete(targetZip);
		}

		private static void CopyStream(Stream istream, Stream ostream)
		{
			var buffer = new byte[0x10000];
			while (true)
			{
				var length = istream.Read(buffer, 0, buffer.Length);
				if (length <= 0)
					break;
				ostream.Write(buffer, 0, length);
			}
		}
	}
}
