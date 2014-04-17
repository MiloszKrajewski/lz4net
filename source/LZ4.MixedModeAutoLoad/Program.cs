using System;
using System.Text;

namespace LZ4.MixedModeAutoLoad
{
	class Program
	{
		private const string LoremIpsum =
			"Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut " +
			"labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco " +
			"laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in " +
			"voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat " +
			"non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

		static void Main()
		{
			AutoLoad3264.Register("LZ4mm");
			AutoLoad3264.Register("LZ4cc");

			var buffer = Encoding.UTF8.GetBytes(LoremIpsum);
			Console.WriteLine("Codec: {0}", LZ4Codec.CodecName);
			var encoded = LZ4Codec.Encode(buffer, 0, buffer.Length);
			var decoded = LZ4Codec.Decode(encoded, 0, encoded.Length, buffer.Length);
			var actual = Encoding.UTF8.GetString(decoded);

			Console.WriteLine(actual == LoremIpsum ? "Success." : "Failure.");

			Console.WriteLine("Press <enter>");
			Console.ReadLine();
		}
	}
}
