using System;

namespace LZ4.Services
{
	internal class CppCC32LZ4Service: ILZ4Service
	{
		public string CodecName
		{
			get { return string.Format("C++/CLI {0}", IntPtr.Size == 4 ? "32" : "64"); }
		}

		public int Encode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
		{
			return LZ4cc.LZ4Codec.Encode32(input, inputOffset, inputLength, output, outputOffset, outputLength);
		}

		public int Decode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength, bool knownOutputLength)
		{
			return LZ4cc.LZ4Codec.Decode32(input, inputOffset, inputLength, output, outputOffset, outputLength, knownOutputLength);
		}
	}
}
