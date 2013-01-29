namespace LZ4
{
	internal interface ILZ4Service
	{
		string CodecName { get; }
		int Encode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength);
		int Decode(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength, bool knownOutputLength);
	}
}