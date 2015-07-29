using System;
using System.Text;
using NUnit.Framework;

namespace LZ4.Tests
{
	[TestFixture]
	public class WrapTests
	{
		[Test]
		public void WrapLorem()
		{
			const string longLorem = Utilities.LoremIpsum + Utilities.LoremIpsum + Utilities.LoremIpsum + Utilities.LoremIpsum;

			var buffer = Encoding.UTF8.GetBytes(longLorem);

			var compressed = LZ4Codec.Wrap(buffer);
			var decompressed = LZ4Codec.Unwrap(compressed);

			Assert.AreEqual(longLorem, Encoding.UTF8.GetString(decompressed));
		}

		[Test]
		public void WrapRandom()
		{
			var buffer = new byte[2048];
			var random = new Random(0);
			random.NextBytes(buffer);

			var compressed = LZ4Codec.Wrap(buffer);
			var decompressed = LZ4Codec.Unwrap(compressed);

			Assert.AreEqual(
				Convert.ToBase64String(buffer), 
				Convert.ToBase64String(decompressed));
		}

		[Test]
		public void Wrap1B()
		{
			LZ4Codec.Wrap(new byte[1]);
		}

		[Test]
		public void Wrap1B_HC()
		{
			LZ4Codec.WrapHC(new byte[1]);
		}
	}
}
