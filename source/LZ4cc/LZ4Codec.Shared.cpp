#include "stdafx.h"

#include "LZ4Codec.h"

namespace NAMESPACE {

void LZ4Codec::CheckArguments(
	array<Byte>^ input, int inputOffset, int% inputLength,
	array<Byte>^ output, int outputOffset, int% outputLength)
{
	if (inputLength < 0) inputLength = input->Length - inputOffset;
	if (inputLength == 0)
	{
		outputLength = 0;
		return;
	}

	if (input == nullptr) throw gcnew ArgumentNullException("input");
	if (inputOffset < 0 || inputOffset + inputLength > input->Length)
		throw gcnew ArgumentException("inputOffset and inputLength are invalid for given input");

	if (outputLength < 0) outputLength = output->Length - outputOffset;
	if (output == nullptr) throw gcnew ArgumentNullException("output");
	if (outputOffset < 0 || outputOffset + outputLength > output->Length)
		throw gcnew ArgumentException("outputOffset and outputLength are invalid for given output");
}

}