#region license

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

#endregion

using System;
using System.Diagnostics;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace LZ4s
{
	/// <summary>Safe LZ4 codec.</summary>
	public static partial class LZ4Codec
	{
		#region Byte manipulation

		// ReSharper disable RedundantCast

		internal static void Poke2(byte[] buffer, int offset, ushort value)
		{
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 8);
		}

		internal static ushort Peek2(byte[] buffer, int offset)
		{
			// NOTE: It's faster than BitConverter.ToUInt16 (suprised? me too)
			return (ushort)(((uint)buffer[offset]) | ((uint)buffer[offset + 1] << 8));
		}

		internal static uint Peek4(byte[] buffer, int offset)
		{
			// NOTE: It's faster than BitConverter.ToUInt32 (suprised? me too)
			return
				((uint)buffer[offset]) |
				((uint)buffer[offset + 1] << 8) |
				((uint)buffer[offset + 2] << 16) |
				((uint)buffer[offset + 3] << 24);
		}

		private static uint Xor4(byte[] buffer, int offset1, int offset2)
		{
			// return Peek4(buffer, offset1) ^ Peek4(buffer, offset2);
			var value1 =
				((uint)buffer[offset1]) |
				((uint)buffer[offset1 + 1] << 8) |
				((uint)buffer[offset1 + 2] << 16) |
				((uint)buffer[offset1 + 3] << 24);
			var value2 =
				((uint)buffer[offset2]) |
				((uint)buffer[offset2 + 1] << 8) |
				((uint)buffer[offset2 + 2] << 16) |
				((uint)buffer[offset2 + 3] << 24);
			return value1 ^ value2;
		}

		private static ulong Xor8(byte[] buffer, int offset1, int offset2)
		{
			// return Peek8(buffer, offset1) ^ Peek8(buffer, offset2);
			var value1 =
				((ulong)buffer[offset1]) |
				((ulong)buffer[offset1 + 1] << 8) |
				((ulong)buffer[offset1 + 2] << 16) |
				((ulong)buffer[offset1 + 3] << 24) |
				((ulong)buffer[offset1 + 4] << 32) |
				((ulong)buffer[offset1 + 5] << 40) |
				((ulong)buffer[offset1 + 6] << 48) |
				((ulong)buffer[offset1 + 7] << 56);
			var value2 =
				((ulong)buffer[offset2]) |
				((ulong)buffer[offset2 + 1] << 8) |
				((ulong)buffer[offset2 + 2] << 16) |
				((ulong)buffer[offset2 + 3] << 24) |
				((ulong)buffer[offset2 + 4] << 32) |
				((ulong)buffer[offset2 + 5] << 40) |
				((ulong)buffer[offset2 + 6] << 48) |
				((ulong)buffer[offset2 + 7] << 56);
			return value1 ^ value2;
		}

		private static bool Equal4(byte[] buffer, int offset1, int offset2)
		{
			// return Peek4(buffer, offset1) == Peek4(buffer, offset2);
			if (buffer[offset1] != buffer[offset2]) return false;
			if (buffer[offset1 + 1] != buffer[offset2 + 1]) return false;
			if (buffer[offset1 + 2] != buffer[offset2 + 2]) return false;
			return buffer[offset1 + 3] == buffer[offset2 + 3];
		}

		private static bool Equal2(byte[] buffer, int offset1, int offset2)
		{
			// return Peek2(buffer, offset1) == Peek2(buffer, offset2);
			if (buffer[offset1] != buffer[offset2]) return false;
			return buffer[offset1 + 1] == buffer[offset2 + 1];
		}

		private static void BlockCopy(byte[] src, int src0, byte[] dst, int dst0, int length)
		{
			Debug.Assert(src != dst, "BlockCopy does not handle copying to the same buffer");

			if (length >= BLOCK_COPY_LIMIT)
			{
				Buffer.BlockCopy(src, src0, dst, dst0, length);
			}
			else
			{
				while (length >= 4)
				{
					dst[dst0] = src[src0];
					dst[dst0 + 1] = src[src0 + 1];
					dst[dst0 + 2] = src[src0 + 2];
					dst[dst0 + 3] = src[src0 + 3];
					length -= 4; src0 += 4; dst0 += 4;
				}

				while (length > 0)
				{
					dst[dst0++] = src[src0++];
					length--;
				}
			}
		}

		private static int WildCopy(byte[] src, int src_0, byte[] dst, int dst_0, int dst_end)
		{
			Debug.Assert(src != dst);

			// deal with this quickly
			var length = dst_end - dst_0;
			if (length <= 8)
			{
				dst[dst_0] = src[src_0];
				dst[dst_0 + 1] = src[src_0 + 1];
				dst[dst_0 + 2] = src[src_0 + 2];
				dst[dst_0 + 3] = src[src_0 + 3];
				dst[dst_0 + 4] = src[src_0 + 4];
				dst[dst_0 + 5] = src[src_0 + 5];
				dst[dst_0 + 6] = src[src_0 + 6];
				dst[dst_0 + 7] = src[src_0 + 7];
				return 8;
			}

			length = (length + 7) & ~7; // round up to 8
			if (length < BLOCK_COPY_LIMIT)
			{
				do
				{
					dst[dst_0] = src[src_0];
					dst[dst_0 + 1] = src[src_0 + 1];
					dst[dst_0 + 2] = src[src_0 + 2];
					dst[dst_0 + 3] = src[src_0 + 3];
					dst[dst_0 + 4] = src[src_0 + 4];
					dst[dst_0 + 5] = src[src_0 + 5];
					dst[dst_0 + 6] = src[src_0 + 6];
					dst[dst_0 + 7] = src[src_0 + 7];
					src_0 += 8; dst_0 += 8;
				} while (dst_0 < dst_end);
			}
			else
			{
				Buffer.BlockCopy(src, src_0, dst, dst_0, length);
			}

			return length;
		}

		// ReSharper restore RedundantCast

		#endregion

		#region Byte block copy

		private static void Copy4(byte[] buffer, int src, int dst)
		{
			Debug.Assert(dst - src > 0, "Copying backwards is not implemented");
			buffer[dst + 3] = buffer[src + 3];
			buffer[dst + 2] = buffer[src + 2];
			buffer[dst + 1] = buffer[src + 1];
			buffer[dst] = buffer[src];
		}

		private static void Copy8(byte[] buffer, int src, int dst)
		{
			Debug.Assert(dst - src > 0, "Copying backwards is not implemented");
			buffer[dst + 7] = buffer[src + 7];
			buffer[dst + 6] = buffer[src + 6];
			buffer[dst + 5] = buffer[src + 5];
			buffer[dst + 4] = buffer[src + 4];
			buffer[dst + 3] = buffer[src + 3];
			buffer[dst + 2] = buffer[src + 2];
			buffer[dst + 1] = buffer[src + 1];
			buffer[dst] = buffer[src];
		}

		private static int WildCopy32(byte[] src, int src_p, byte[] dst, int dst_p, int dst_end)
		{
			Debug.Assert(src != dst, "This implementation of WildCopy is meant to copy between different buffers");

			// NOTE: WildCopy and BlindCopy are identical but some mysterious maintanance reason they should be kep separate
			var length = dst_end - dst_p;
			if (length <= 8)
			{
				// copy 8 bytes anyway
				dst[dst_p] = src[src_p];
				dst[dst_p + 1] = src[src_p + 1];
				dst[dst_p + 2] = src[src_p + 2];
				dst[dst_p + 3] = src[src_p + 3];
				dst[dst_p + 4] = src[src_p + 4];
				dst[dst_p + 5] = src[src_p + 5];
				dst[dst_p + 6] = src[src_p + 6];
				dst[dst_p + 7] = src[src_p + 7];
				return 8;
			}
			length = (length + 7) & ~7;
			BlockCopy(src, src_p, dst, dst_p, length);
			return length;
		}

		private static int WildCopy64(byte[] buffer, int src_p, int dst_p, int dst_end)
		{
			Debug.Assert(dst_p > src_p, "Copying backwards is not implemented");
			buffer[dst_p + 7] = buffer[src_p + 7];
			buffer[dst_p + 6] = buffer[src_p + 6];
			buffer[dst_p + 5] = buffer[src_p + 5];
			buffer[dst_p + 4] = buffer[src_p + 4];
			buffer[dst_p + 3] = buffer[src_p + 3];
			buffer[dst_p + 2] = buffer[src_p + 2];
			buffer[dst_p + 1] = buffer[src_p + 1];
			buffer[dst_p] = buffer[src_p];
			dst_p += 8;

			if (dst_p < dst_end)
			{
				src_p += 8;
				var diff = dst_p - src_p;
				var length = (dst_end - dst_p + 7) & ~0x7;

				if (diff >= BLOCK_COPY_LIMIT)
				{

				}
			}
			else
			{
				return 8;
			}


			// NOTE: WildCopy and BlindCopy are identical but some mysterious maintanance reason they should be kep separate
			var length = dst_end - dst_p;
			if (length <= 8)
			{
				// copy 8 bytes anyway
				buffer[dst_p] = buffer[src_p];
				buffer[dst_p + 1] = buffer[src_p + 1];
				buffer[dst_p + 2] = buffer[src_p + 2];
				buffer[dst_p + 3] = buffer[src_p + 3];
				buffer[dst_p + 4] = buffer[src_p + 4];
				buffer[dst_p + 5] = buffer[src_p + 5];
				buffer[dst_p + 6] = buffer[src_p + 6];
				buffer[dst_p + 7] = buffer[src_p + 7];
				return 8;
			}
			length = (length + 7) & ~7;
			BlockCopy(buffer, src_p, buffer, dst_p, length);
			return length;
		}

		private static int WildCopy64(byte[] src, int src_p, byte[] dst, int dst_p, int dst_end)
		{
			Debug.Assert(src != dst, "This implementation of WildCopy is meant to copy between different buffers");

			// NOTE: WildCopy and BlindCopy are identical but some mysterious maintanance reason they should be kep separate
			var length = dst_end - dst_p;
			if (length <= 8)
			{
				// copy 8 bytes anyway
				dst[dst_p] = src[src_p];
				dst[dst_p + 1] = src[src_p + 1];
				dst[dst_p + 2] = src[src_p + 2];
				dst[dst_p + 3] = src[src_p + 3];
				dst[dst_p + 4] = src[src_p + 4];
				dst[dst_p + 5] = src[src_p + 5];
				dst[dst_p + 6] = src[src_p + 6];
				dst[dst_p + 7] = src[src_p + 7];
				return 8;
			}
			length = (length + 7) & ~7;
			BlockCopy(src, src_p, dst, dst_p, length);
			return length;
		}

		// ReSharper disable UnusedMethodReturnValue.Local

		private static int BlindCopy32(byte[] src, int src_p, byte[] dst, int dst_p, int dst_end)
		{
			Debug.Assert(src != dst, "This implementation of BlindCopy is meant to copy between different buffers");

			// NOTE: WildCopy and BlindCopy are identical but some mysterious maintanance reason they should be kep separate
			var length = dst_end - dst_p;
			if (length <= 8)
			{
				// copy 8 bytes anyway
				dst[dst_p] = src[src_p];
				dst[dst_p + 1] = src[src_p + 1];
				dst[dst_p + 2] = src[src_p + 2];
				dst[dst_p + 3] = src[src_p + 3];
				dst[dst_p + 4] = src[src_p + 4];
				dst[dst_p + 5] = src[src_p + 5];
				dst[dst_p + 6] = src[src_p + 6];
				dst[dst_p + 7] = src[src_p + 7];
				return 8;
			}
			length = (length + 7) & ~7;
			BlockCopy(src, src_p, dst, dst_p, length);
			return length;
		}

		private static int BlindCopy64(byte[] src, int src_p, byte[] dst, int dst_p, int dst_end)
		{
			Debug.Assert(src != dst, "This implementation of BlindCopy is meant to copy between different buffers");

			// NOTE: WildCopy and BlindCopy are identical but some mysterious maintanance reason they should be kep separate
			var length = dst_end - dst_p;
			if (length <= 8)
			{
				// copy 8 bytes anyway
				dst[dst_p] = src[src_p];
				dst[dst_p + 1] = src[src_p + 1];
				dst[dst_p + 2] = src[src_p + 2];
				dst[dst_p + 3] = src[src_p + 3];
				dst[dst_p + 4] = src[src_p + 4];
				dst[dst_p + 5] = src[src_p + 5];
				dst[dst_p + 6] = src[src_p + 6];
				dst[dst_p + 7] = src[src_p + 7];
				return 8;
			}
			length = (length + 7) & ~7;
			BlockCopy(src, src_p, dst, dst_p, length);
			return length;
		}

		// ReSharper restore UnusedMethodReturnValue.Local

		private static int SecureCopy32(byte[] buffer, int src, int dst, int dst_end)
		{
			var diff = dst - src;

			Debug.Assert(diff > 0, "Copying backwards is not implemented");
			Debug.Assert(BLOCK_COPY_LIMIT > 4, "This method requires BLOCK_COPY_LIMIT > 4");

			var length = (dst_end - dst + 7) & ~7;
			if (length < 8) length = 8;
			dst_end = dst + length; // the 'real' end (as it copies in 8 byte chunks)
			var copied = length; // length can get modified, so we need a copy which is going to be returned

			if (diff >= BLOCK_COPY_LIMIT /* && BLOCK_COPY_LIMIT > 4 */)
			{
				if (diff >= length)
				{
					Buffer.BlockCopy(buffer, src, buffer, dst, length);
					return copied; // done
				}

				while (length >= diff)
				{
					Buffer.BlockCopy(buffer, src, buffer, dst, diff);
					src += diff; dst += diff; length -= diff;
				}

				// to be continued...
			}
			else if (diff < 4)
			{
				// NOTE
				// this is very unlikely to be executed
				// I even think this is impossible, but if I'm wrong it will save the day
				while (dst < dst_end)
				{
					buffer[dst + 3] = buffer[src + 3];
					buffer[dst + 2] = buffer[src + 2];
					buffer[dst + 1] = buffer[src + 1];
					buffer[dst] = buffer[src];
					src += 4; dst += 4;
				}

				return copied; // done
			}

			// ...it continues here

			while (dst < dst_end)
			{
				// x8
				buffer[dst] = buffer[src];
				buffer[dst + 1] = buffer[src + 1];
				buffer[dst + 2] = buffer[src + 2];
				buffer[dst + 3] = buffer[src + 3];
				buffer[dst + 4] = buffer[src + 4];
				buffer[dst + 5] = buffer[src + 5];
				buffer[dst + 6] = buffer[src + 6];
				buffer[dst + 7] = buffer[src + 7];
				dst += 8; src += 8;
			}

			return copied; // done
		}

		private static int SecureCopy64(byte[] buffer, int src, int dst, int dst_end)
		{
			var diff = dst - src;

			Debug.Assert(diff > 0, "Copying backwards is not implemented");
			Debug.Assert(BLOCK_COPY_LIMIT > 8, "This method requires BLOCK_COPY_LIMIT > 8");

			var length = (dst_end - dst + 7) & ~7;
			if (length < 8) length = 8;
			dst_end = dst + length; // the 'real' end (as it copies in 8 byte chunks)
			var copied = length; // length can get modified, so we need a copy which is going to be returned

			if (diff >= BLOCK_COPY_LIMIT /* && BLOCK_COPY_LIMIT > 8 */)
			{
				if (diff >= length)
				{
					Buffer.BlockCopy(buffer, src, buffer, dst, length);
					return copied; // done
				}

				while (length >= diff)
				{
					Buffer.BlockCopy(buffer, src, buffer, dst, diff);
					src += diff; dst += diff; length -= diff;
				}

				// to be continued...
			}
			else if (diff < 8)
			{
				while (dst < dst_end)
				{
					// NOTE
					// this is very unlikely to be executed
					// I even think this is impossible, but if I'm wrong it will save the day
					buffer[dst + 7] = buffer[src + 7];
					buffer[dst + 6] = buffer[src + 6];
					buffer[dst + 5] = buffer[src + 5];
					buffer[dst + 4] = buffer[src + 4];
					buffer[dst + 3] = buffer[src + 3];
					buffer[dst + 2] = buffer[src + 2];
					buffer[dst + 1] = buffer[src + 1];
					buffer[dst] = buffer[src];
					src += 8; dst += 8;
				}

				return copied; // done
			}

			// ...it continues here

			while (dst < dst_end)
			{
				// x8
				buffer[dst] = buffer[src];
				buffer[dst + 1] = buffer[src + 1];
				buffer[dst + 2] = buffer[src + 2];
				buffer[dst + 3] = buffer[src + 3];
				buffer[dst + 4] = buffer[src + 4];
				buffer[dst + 5] = buffer[src + 5];
				buffer[dst + 6] = buffer[src + 6];
				buffer[dst + 7] = buffer[src + 7];
				dst += 8; src += 8;
			}

			return copied; // done
		}

		#endregion

		#region Encode32

		/// <summary>Encodes the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="inputOffset">The input offset.</param>
		/// <param name="inputLength">Length of the input.</param>
		/// <param name="output">The output.</param>
		/// <param name="outputOffset">The output offset.</param>
		/// <param name="outputLength">Length of the output.</param>
		/// <returns>Number of bytes written.</returns>
		public static int Encode32(
			byte[] input,
			int inputOffset,
			int inputLength,
			byte[] output,
			int outputOffset,
			int outputLength)
		{
			CheckArguments(
				input, inputOffset, ref inputLength,
				output, outputOffset, ref outputLength);
			if (outputLength == 0) return 0;

			if (inputLength < LZ4_64KLIMIT)
			{
				var hashTable = new ushort[HASH64K_TABLESIZE];
				return LZ4_compress64kCtx_safe32(hashTable, input, output, inputOffset, outputOffset, inputLength, outputLength);
			}
			else
			{
				var hashTable = new int[HASH_TABLESIZE];
				return LZ4_compressCtx_safe32(hashTable, input, output, inputOffset, outputOffset, inputLength, outputLength);
			}
		}

		/// <summary>Encodes the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="inputOffset">The input offset.</param>
		/// <param name="inputLength">Length of the input.</param>
		/// <returns>Compressed buffer.</returns>
		public static byte[] Encode32(byte[] input, int inputOffset, int inputLength)
		{
			if (inputLength < 0) inputLength = input.Length - inputOffset;

			if (input == null) throw new ArgumentNullException("input");
			if (inputOffset < 0 || inputOffset + inputLength > input.Length)
				throw new ArgumentException("inputOffset and inputLength are invalid for given input");

			var result = new byte[MaximumOutputLength(inputLength)];
			var length = Encode32(input, inputOffset, inputLength, result, 0, result.Length);

			if (length != result.Length)
			{
				if (length < 0)
					throw new InvalidOperationException("Compression has been corrupted");
				var buffer = new byte[length];
				Buffer.BlockCopy(result, 0, buffer, 0, length);
				return buffer;
			}
			return result;
		}

		#endregion

		#region Encode64

		/// <summary>Encodes the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="inputOffset">The input offset.</param>
		/// <param name="inputLength">Length of the input.</param>
		/// <param name="output">The output.</param>
		/// <param name="outputOffset">The output offset.</param>
		/// <param name="outputLength">Length of the output.</param>
		/// <returns>Number of bytes written.</returns>
		public static int Encode64(
			byte[] input,
			int inputOffset,
			int inputLength,
			byte[] output,
			int outputOffset,
			int outputLength)
		{
			CheckArguments(
				input, inputOffset, ref inputLength,
				output, outputOffset, ref outputLength);
			if (outputLength == 0) return 0;

			if (inputLength < LZ4_64KLIMIT)
			{
				var hashTable = new ushort[HASH64K_TABLESIZE];
				return LZ4_compress64kCtx_safe64(hashTable, input, output, inputOffset, outputOffset, inputLength, outputLength);
			}
			else
			{
				var hashTable = new int[HASH_TABLESIZE];
				return LZ4_compressCtx_safe64(hashTable, input, output, inputOffset, outputOffset, inputLength, outputLength);
			}
		}

		/// <summary>Encodes the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="inputOffset">The input offset.</param>
		/// <param name="inputLength">Length of the input.</param>
		/// <returns>Compressed buffer.</returns>
		public static byte[] Encode64(byte[] input, int inputOffset, int inputLength)
		{
			if (inputLength < 0) inputLength = input.Length - inputOffset;

			if (input == null) throw new ArgumentNullException("input");
			if (inputOffset < 0 || inputOffset + inputLength > input.Length)
				throw new ArgumentException("inputOffset and inputLength are invalid for given input");

			var result = new byte[MaximumOutputLength(inputLength)];
			var length = Encode64(input, inputOffset, inputLength, result, 0, result.Length);

			if (length != result.Length)
			{
				if (length < 0)
					throw new InvalidOperationException("Compression has been corrupted");
				var buffer = new byte[length];
				Buffer.BlockCopy(result, 0, buffer, 0, length);
				return buffer;
			}
			return result;
		}

		#endregion

		#region Decode32

		/// <summary>Decodes the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="inputOffset">The input offset.</param>
		/// <param name="inputLength">Length of the input.</param>
		/// <param name="output">The output.</param>
		/// <param name="outputOffset">The output offset.</param>
		/// <param name="outputLength">Length of the output.</param>
		/// <param name="knownOutputLength">Set it to <c>true</c> if output length is known.</param>
		/// <returns>Number of bytes written.</returns>
		public static int Decode32(
			byte[] input,
			int inputOffset,
			int inputLength,
			byte[] output,
			int outputOffset,
			int outputLength,
			bool knownOutputLength)
		{
			CheckArguments(
				input, inputOffset, ref inputLength,
				output, outputOffset, ref outputLength);

			if (outputLength == 0) return 0;

			if (knownOutputLength)
			{
				var length = LZ4_uncompress_safe32(input, output, inputOffset, outputOffset, outputLength);
				if (length != inputLength)
					throw new ArgumentException("LZ4 block is corrupted, or invalid length has been given.");
				return outputLength;
			}
			else
			{
				var length = LZ4_uncompress_unknownOutputSize_safe32(input, output, inputOffset, outputOffset, inputLength, outputLength);
				if (length < 0)
					throw new ArgumentException("LZ4 block is corrupted, or invalid length has been given.");
				return length;
			}
		}

		/// <summary>Decodes the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="inputOffset">The input offset.</param>
		/// <param name="inputLength">Length of the input.</param>
		/// <param name="outputLength">Length of the output.</param>
		/// <returns>Decompressed buffer.</returns>
		public static byte[] Decode32(byte[] input, int inputOffset, int inputLength, int outputLength)
		{
			if (inputLength < 0) inputLength = input.Length - inputOffset;

			if (input == null) throw new ArgumentNullException("input");
			if (inputOffset < 0 || inputOffset + inputLength > input.Length)
				throw new ArgumentException("inputOffset and inputLength are invalid for given input");

			var result = new byte[outputLength];
			var length = Decode32(input, inputOffset, inputLength, result, 0, outputLength, true);
			if (length != outputLength)
				throw new ArgumentException("outputLength is not valid");
			return result;
		}

		#endregion

		#region Decode64

		/// <summary>Decodes the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="inputOffset">The input offset.</param>
		/// <param name="inputLength">Length of the input.</param>
		/// <param name="output">The output.</param>
		/// <param name="outputOffset">The output offset.</param>
		/// <param name="outputLength">Length of the output.</param>
		/// <param name="knownOutputLength">Set it to <c>true</c> if output length is known.</param>
		/// <returns>Number of bytes written.</returns>
		public static int Decode64(
			byte[] input,
			int inputOffset,
			int inputLength,
			byte[] output,
			int outputOffset,
			int outputLength,
			bool knownOutputLength)
		{
			CheckArguments(
				input, inputOffset, ref inputLength,
				output, outputOffset, ref outputLength);

			if (outputLength == 0) return 0;

			if (knownOutputLength)
			{
				var length = LZ4_uncompress_safe64(input, output, inputOffset, outputOffset, outputLength);
				if (length != inputLength)
					throw new ArgumentException("LZ4 block is corrupted, or invalid length has been given.");
				return outputLength;
			}
			else
			{
				var length = LZ4_uncompress_unknownOutputSize_safe64(input, output, inputOffset, outputOffset, inputLength, outputLength);
				if (length < 0)
					throw new ArgumentException("LZ4 block is corrupted, or invalid length has been given.");
				return length;
			}
		}

		/// <summary>Decodes the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="inputOffset">The input offset.</param>
		/// <param name="inputLength">Length of the input.</param>
		/// <param name="outputLength">Length of the output.</param>
		/// <returns>Decompressed buffer.</returns>
		public static byte[] Decode64(byte[] input, int inputOffset, int inputLength, int outputLength)
		{
			if (inputLength < 0) inputLength = input.Length - inputOffset;

			if (input == null) throw new ArgumentNullException("input");
			if (inputOffset < 0 || inputOffset + inputLength > input.Length)
				throw new ArgumentException("inputOffset and inputLength are invalid for given input");

			var result = new byte[outputLength];
			var length = Decode64(input, inputOffset, inputLength, result, 0, outputLength, true);
			if (length != outputLength)
				throw new ArgumentException("outputLength is not valid");
			return result;
		}

		#endregion
	}
}

// ReSharper restore InconsistentNaming
// ReSharper restore CheckNamespace
