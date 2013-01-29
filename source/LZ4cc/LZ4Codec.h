#pragma once

using namespace System;

namespace NAMESPACE {

	public ref class LZ4Codec
	{
    private:
        typedef unsigned char byte;

    private:
		static void CheckArguments(
			array<Byte>^ input, int inputOffset, int% inputLength,
			array<Byte>^ output, int outputOffset, int% outputLength);

    public:
        static inline int MaximumOutputLength(int inputLength)
        {
            return (inputLength + (inputLength/255) + 16);
        }

        // region: 32-bit

        static int Encode32(
			byte* input, int inputLength, 
			byte* output, int outputLength);

		static int Encode32(
			array<Byte>^ input, int inputOffset, int inputLength,
			array<Byte>^ output, int outputOffset, int outputLength);

		static array<Byte>^ Encode32(
			array<Byte>^ input, int inputOffset, int inputLength);

		static int Decode32(
			byte* input, int inputLength,
			byte* output, int outputLength,
			bool knownOutputLength);

		static int Decode32(
			array<Byte>^ input, int inputOffset, int inputLength,
			array<Byte>^ output, int outputOffset, int outputLength,
			bool knownOutputLength);

		static array<Byte>^ Decode32(
			array<Byte>^ input, int inputOffset, int inputLength, 
			int outputLength);

        // region: 64-bit

        static int Encode64(
			byte* input, int inputLength, 
			byte* output, int outputLength);

		static int Encode64(
			array<Byte>^ input, int inputOffset, int inputLength,
			array<Byte>^ output, int outputOffset, int outputLength);

		static array<Byte>^ Encode64(
			array<Byte>^ input, int inputOffset, int inputLength);

		static int Decode64(
			byte* input, int inputLength,
			byte* output, int outputLength,
			bool knownOutputLength);

		static int Decode64(
			array<Byte>^ input, int inputOffset, int inputLength,
			array<Byte>^ output, int outputOffset, int outputLength,
			bool knownOutputLength);

		static array<Byte>^ Decode64(
			array<Byte>^ input, int inputOffset, int inputLength, 
			int outputLength);
	};

}
