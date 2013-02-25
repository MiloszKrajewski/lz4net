using System;
using System.Diagnostics;
using System.Threading;

namespace LZ4.Tests.Helpers
{
	public class TimedMethod
	{
		#region constructor

		public TimedMethod(string name, Func<byte[], int, byte[]> method, bool identical = true)
		{
			Name = name;
			Timer = new Stopwatch();
			Method = method;
			Identical = identical;
		}

		#endregion

		#region public interface

		public string Name { get; internal set; }
		public Stopwatch Timer { get; internal set; }
		public Func<byte[], int, byte[]> Method { get; internal set; }
		public long InputLength { get; internal set; }
		public long OutputLength { get; internal set; }
		public bool Identical {get; internal set; }

		public byte[] Run(byte[] input, int length)
		{
			byte[] output;
			var timer = Timer;

			Scan(input);
			InputLength += length;
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			Thread.Yield();
			try
			{
				timer.Start();
				output = Method(input, length);
				timer.Stop();
			}
			finally
			{
				Thread.CurrentThread.Priority = ThreadPriority.Normal;
			}
			OutputLength += output.Length;
			return output;
		}

		private static byte Scan(byte[] buffer)
		{
			byte sum8 = 0;
			var length = buffer.Length;
			for (int i = 0; i < length; i++) sum8 += buffer[i];
			return sum8;
		}

		public byte[] Warmup(byte[] buffer, int length)
		{
			return Method(buffer, length);
		}

		public double Speed
		{
			get { return ((double)InputLength / 1024 / 1024) / Timer.Elapsed.TotalSeconds; }
		}

		public double Ratio
		{
			get { return (double)OutputLength * 100 / InputLength; }
		}

		#endregion
	}
}
