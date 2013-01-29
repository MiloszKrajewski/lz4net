namespace LZ4.Services
{
	internal class Unsafe64LZ4Service: ILZ4Service
	{
		#region ILZ4Service Members

		public string CodecName
		{
			get { return "Unsafe 64"; }
		}

		public int Encode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength)
		{
			return LZ4n.LZ4Codec.Encode64(input, inputOffset, inputLength, output, outputOffset, outputLength);
		}

		public int Decode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength, bool knownOutputLength)
		{
			return LZ4n.LZ4Codec.Decode64(input, inputOffset, inputLength, output, outputOffset, outputLength, knownOutputLength);
		}

		#endregion
	}
}
