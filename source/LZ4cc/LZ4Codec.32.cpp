#include "stdafx.h"

#include "LZ4Codec.h"
#include "lz4_32.h"

namespace NAMESPACE {

int LZ4Codec::Encode32(
	byte* input, int inputLength,
	byte* output, int outputLength)
{
    return LZ4_FUNC(LZ4_compress_limitedOutput)((char*)input, (char*)output, inputLength, outputLength);
}

int LZ4Codec::Encode32(
	array<Byte>^ input, int inputOffset, int inputLength,
	array<Byte>^ output, int outputOffset, int outputLength)
{
	CheckArguments(
		input, inputOffset, inputLength, 
		output, outputOffset, outputLength);

	if (outputLength == 0) return 0;

    pin_ptr<Byte> inputPtr = &input[inputOffset];
    pin_ptr<Byte> outputPtr = &output[outputOffset];

    byte* i = (byte*)inputPtr;
    byte* o = (byte*)outputPtr;

    return Encode32(i, inputLength, o, outputLength);
}

array<Byte>^ LZ4Codec::Encode32(
	array<Byte>^ input, int inputOffset, int inputLength)
{
	if (inputLength < 0) inputLength = input->Length - inputOffset;

	if (input == nullptr) throw gcnew ArgumentNullException("input");
	if (inputOffset < 0 || inputOffset + inputLength > input->Length)
		throw gcnew ArgumentException("inputOffset and inputLength are invalid for given input");

    int outputLength = MaximumOutputLength(inputLength);
    array<Byte>^ result = gcnew array<Byte>(outputLength);
	int length = Encode32(input, inputOffset, inputLength, result, 0, outputLength);

	if (length != outputLength)
	{
		if (length < 0)
			throw gcnew InvalidOperationException("Compression has been corrupted");
		array<Byte>^ buffer = gcnew array<Byte>(length);
		Buffer::BlockCopy(result, 0, buffer, 0, length);
		return buffer;
	}
	return result;
}

int LZ4Codec::Decode32(
	byte* input, int inputLength,
	byte* output, int outputLength,
	bool knownOutputLength)
{
	if (knownOutputLength)
	{
		int length = LZ4_FUNC(LZ4_uncompress)((char*)input, (char*)output, outputLength);
		if (length != inputLength) 
			throw gcnew ArgumentException("LZ4 block is corrupted, or invalid length has been given.");
		return outputLength;
	}
	else
	{
		int length = LZ4_FUNC(LZ4_uncompress_unknownOutputSize)((char*)input, (char*)output, inputLength, outputLength);
		if (length < 0) 
			throw gcnew ArgumentException("LZ4 block is corrupted, or invalid length has been given.");
		return length;
	}
}

int LZ4Codec::Decode32(
	array<Byte>^ input, int inputOffset, int inputLength,
	array<Byte>^ output, int outputOffset, int outputLength,
	bool knownOutputLength)
{
	CheckArguments(
		input, inputOffset, inputLength,
		output, outputOffset, outputLength);

	if (outputLength == 0) return 0;

	pin_ptr<Byte> inputPtr = &input[inputOffset];
	pin_ptr<Byte> outputPtr = &output[outputOffset];
            
    byte* i = inputPtr;
    byte* o = outputPtr;

	return Decode32(i, inputLength, o, outputLength, knownOutputLength);
}

array<Byte>^ LZ4Codec::Decode32(
	array<Byte>^ input, int inputOffset, int inputLength, int outputLength)
{
	if (inputLength < 0) inputLength = input->Length - inputOffset;

	if (input == nullptr) throw gcnew ArgumentNullException("input");
	if (inputOffset < 0 || inputOffset + inputLength > input->Length)
		throw gcnew ArgumentException("inputOffset and inputLength are invalid for given input");

    array<Byte>^ result = gcnew array<Byte>(outputLength);
	int length = Decode32(input, inputOffset, inputLength, result, 0, outputLength, true);
	if (length != outputLength)
		throw gcnew ArgumentException("outputLength is not valid");

	return result;
}

}