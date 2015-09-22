/*
Copyright (c) 2013, Milosz Krajewski
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided 
that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions 
  and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
  and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED 
WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR 
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE 
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN 
IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

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

int LZ4Codec::Encode32HC(
	byte* input, int inputLength,
	byte* output, int outputLength)
{
	return LZ4_FUNC(LZ4_compressHC_limitedOutput)((char*)input, (char*)output, inputLength, outputLength);
}

int LZ4Codec::Encode32HC(
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

	return Encode32HC(i, inputLength, o, outputLength);
}

array<Byte>^ LZ4Codec::Encode32HC(
	array<Byte>^ input, int inputOffset, int inputLength)
{
	if (inputLength < 0) inputLength = input->Length - inputOffset;

	if (input == nullptr) throw gcnew ArgumentNullException("input");
	if (inputOffset < 0 || inputOffset + inputLength > input->Length)
		throw gcnew ArgumentException("inputOffset and inputLength are invalid for given input");

	int outputLength = MaximumOutputLength(inputLength);
	array<Byte>^ result = gcnew array<Byte>(outputLength);
	int length = Encode32HC(input, inputOffset, inputLength, result, 0, outputLength);

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

}