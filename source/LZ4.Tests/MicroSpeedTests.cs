using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace LZ4.Tests
{
	[TestFixture]
	public class MicroSpeedTests
	{
		[Test]
		public void LoopVsBlock()
		{
			var buffer1 = new byte[1024];
			var buffer2 = new byte[1024];

			Buffer.BlockCopy(buffer2, 0, buffer1, 0, buffer1.Length);
			Thread.Sleep(1000);

			for (int step = 4; step < 512; step += 4)
			{
				LoopVsBlock(buffer1, buffer2, step);
			}
		}

		[Test]
		public void BlockCopyTest()
		{
			const int limit = 100000;

			var buffer1 = new byte[1024];
			var buffer2 = new byte[1024];
			Buffer.BlockCopy(buffer2, 0, buffer1, 0, buffer1.Length);
			Thread.Sleep(1000);

			for (var r = 0; r < 10; r++)
			{
				var copy1Timer = new Stopwatch();
				var copy2Timer = new Stopwatch();

				copy1Timer.Restart();
				for (var i = 0; i < limit; i++)
				{
					BlockCopy1(buffer2, buffer1, 0, 0, 1024);
				}
				copy1Timer.Stop();

				copy2Timer.Restart();
				for (var i = 0; i < limit; i++)
				{
					BlockCopy2(buffer2, buffer1, 0, 0, 1024);
				}
				copy2Timer.Stop();

				Console.WriteLine("Copy1: {0:0.0000}ms", copy1Timer.Elapsed.TotalMilliseconds);
				Console.WriteLine("Copy2: {0:0.0000}ms", copy2Timer.Elapsed.TotalMilliseconds);
			}
		}

		public void BlockCopy1(byte[] src, byte[] dst, int src0, int dst0, int length)
		{
			while (length >= 4)
			{
				dst[dst0++] = src[src0++];
				dst[dst0++] = src[src0++];
				dst[dst0++] = src[src0++];
				dst[dst0++] = src[src0++];
				length -= 4;
			}

			while (length > 0)
			{
				dst[dst0++] = src[src0++];
				length--;
			}
		}

		public void BlockCopy2(byte[] src, byte[] dst, int src0, int dst0, int length)
		{
			while (length >= 4)
			{
				dst[dst0] = src[src0];
				dst[dst0 + 1] = src[src0 + 1];
				dst[dst0 + 2] = src[src0 + 2];
				dst[dst0 + 3] = src[src0 + 3];
				dst0 += 4;
				src0 += 4;
				length -= 4;
			}

			while (length > 0)
			{
				dst[dst0++] = src[src0++];
				length--;
			}
		}

		private void LoopVsBlock(byte[] buffer1, byte[] buffer2, int step)
		{
			var count = 1000000 / step;
			var loopTimer = Stopwatch.StartNew();
			loopTimer.Restart();
			for (int i = 0; i < count; i++)
			{
				int s = 0;
				int d = 0;
				while (s < step)
				{
					buffer1[d++] = buffer2[s++];
					buffer1[d++] = buffer2[s++];
					buffer1[d++] = buffer2[s++];
					buffer1[d++] = buffer2[s++];
				}
			}
			loopTimer.Stop();

			var blockTimer = Stopwatch.StartNew();
			blockTimer.Restart();
			for (int i = 0; i < count; i++)
			{
				Buffer.BlockCopy(buffer2, 0, buffer1, 0, step);
			}
			blockTimer.Stop();

			var blockTicks = blockTimer.ElapsedTicks;
			var loopTicks = loopTimer.ElapsedTicks;
			var rel = loopTicks > blockTicks ? ">" : "<";

			Console.WriteLine("@{3} --- {0:0.0000}ms {1} {2:0.0000}ms", loopTimer.Elapsed.TotalMilliseconds, rel, blockTimer.Elapsed.TotalMilliseconds, step);
		}
	}
}
