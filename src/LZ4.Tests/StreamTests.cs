using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LZ4.Tests.Helpers;
using NUnit.Framework;
using System.Text;

namespace LZ4.Tests
{
	[TestFixture]
	public class StreamTests
	{
		//const long TOTAL_SIZE = 1L * 1024 * 1024 * 1024;
		const long TOTAL_SIZE = 256 * 1024 * 1024;
		const int CHUNK_SIZE = 2 * 1024 * 1024;

		[Test]
		public void CopyTo()
		{
			var tempFileName = Path.GetTempFileName();
			var builder = new StringBuilder();
			for (var i = 0; i < 1000; i++)
			{
				builder.AppendLine(Utilities.LoremIpsum);
			}
			var data = Encoding.UTF8.GetBytes(builder.ToString());

			using (var ostream = File.Create(tempFileName))
			using (var zstream = new LZ4Stream(ostream, CompressionMode.Compress))
			{
				zstream.Write(data, 0, data.Length);
			}

			using (var istream = File.OpenRead(tempFileName))
			using (var zstream = new LZ4Stream(istream, CompressionMode.Decompress))
			using (var ostream = File.Create(tempFileName + ".orig"))
			{
				zstream.CopyTo(ostream);
			}
		}

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
					FullBlockRead(s, buffer, 0, buffer.Length);
					Utilities.AssertEqual(b, buffer, "Read");
				},
				true);
		}

		private void FullBlockRead(Stream stream, byte[] buffer, int offset, int length)
		{
			while (length > 0)
			{
				var read = stream.Read(buffer, offset, length);
				length -= read;
				offset += read;
				if (read == 0) 
					throw new EndOfStreamException();
			}
		}

		[Test]
		public void TcpClientServer()
		{
			const int port = 54856;
			Parallel.Invoke(
				() => TcpServer(port),
				() => TcpClient(port));
		}

		public void TcpClient(int port)
		{
			var client = new TcpClient();
			client.Connect("127.0.0.1", port);

			Console.WriteLine("Connected...");

			using (var tcpStream = client.GetStream())
			using (var lz4Stream = new LZ4Stream(tcpStream, CompressionMode.Decompress))
			using (var reader = new BinaryReader(lz4Stream))
			{
				while (true)
				{
					var file = reader.ReadString();
					if (file.Length == 0) break;
					Console.WriteLine("client: {0}", file);
					var length = reader.ReadInt32();
					var bytes = reader.ReadBytes(length);
					Assert.AreEqual(length, bytes.Length);
				}
			}
		}

		public void TcpServer(int port)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();

			try
			{
				Console.WriteLine("Waiting for client...");
				var client = listener.AcceptTcpClient();
				using (var tcpStream = client.GetStream())
				using (var lz4Stream = new LZ4Stream(tcpStream, CompressionMode.Compress, blockSize : 128 * 1024))
				using (var writer = new BinaryWriter(lz4Stream))
				{
					foreach (var file in Directory.GetFiles(Utilities.GetSilesiaCorpusFolder(), "*", SearchOption.AllDirectories))
					{
						Console.WriteLine("server: {0}", file);

						writer.Write(file);
						var bytes = File.ReadAllBytes(file);
						writer.Write(bytes.Length);
						writer.Write(bytes);
						Thread.Sleep(500); // pause to force client to wait
					}

					writer.Write(string.Empty);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
			}
			finally
			{
				listener.Stop();
			}
		}

		// ReSharper disable InconsistentNaming

		private static void DoAction(Action<byte[], Stream> action, bool read)
		{
			var provider = new BlockDataProvider(Utilities.GetSilesiaCorpusFolder());
			var r = new Random(0);

			Console.WriteLine("Architecture: {0}bit", IntPtr.Size * 8);
			Console.WriteLine("CodecName: {0}", LZ4Codec.CodecName);

			var fileName = Path.Combine(Path.GetTempPath(), "BlockCompressionStream.dat");

			using (var stream = new LZ4Stream(
				read ? File.OpenRead(fileName) : File.Create(fileName),
				read ? CompressionMode.Decompress : CompressionMode.Compress,
				LZ4StreamFlags.HighCompression))
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
					var pct = (int)((double)total * 100 / limit);
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
