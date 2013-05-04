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

#pragma once

using namespace System;

namespace NAMESPACE {

	public ref class LZ4Codec
	{
	private:
		typedef unsigned char byte;

	private:
		static void CheckArguments(
			array<Byte>^ input, int inputOffset, int% inputLength);

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

		static int Encode32HC(
			byte* input, int inputLength, 
			byte* output, int outputLength);

		static int Encode32HC(
			array<Byte>^ input, int inputOffset, int inputLength,
			array<Byte>^ output, int outputOffset, int outputLength);

		static array<Byte>^ Encode32HC(
			array<Byte>^ input, int inputOffset, int inputLength);

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

		static int Encode64HC(
			byte* input, int inputLength, 
			byte* output, int outputLength);

		static int Encode64HC(
			array<Byte>^ input, int inputOffset, int inputLength,
			array<Byte>^ output, int outputOffset, int outputLength);

		static array<Byte>^ Encode64HC(
			array<Byte>^ input, int inputOffset, int inputLength);
	};

}
