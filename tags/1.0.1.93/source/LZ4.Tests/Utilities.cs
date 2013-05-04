using System;
using NUnit.Framework;

namespace LZ4.Tests
{
	public class Utilities
	{
		public const string TEST_DATA_FOLDER = @"T:\Temp\Corpus";
		//public const string TEST_DATA_FOLDER = @"D:\Archive\Corpus";

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
	}
}
