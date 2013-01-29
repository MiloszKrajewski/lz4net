namespace LZ4.Services
{
	internal class Unsafe32LZ4Service: ILZ4Service
	{
		#region ILZ4Service Members

		public string CodecName
		{
			get { return "Unsafe 32"; }
		}

		public int Encode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
		{
			return LZ4n.LZ4Codec.Encode32(input, inputOffset, inputLength, output, outputOffset, outputLength);
		}

		public int Decode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength, bool knownOutputLength)
		{
			return LZ4n.LZ4Codec.Decode32(input, inputOffset, inputLength, output, outputOffset, outputLength, knownOutputLength);
		}

		#endregion
	}
}
