#region LZ4 original

/*
   LZ4 - Fast LZ compression algorithm
   Copyright (C) 2011-2012, Yann Collet.
   BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions are
   met:

       * Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
       * Redistributions in binary form must reproduce the above
   copyright notice, this list of conditions and the following disclaimer
   in the documentation and/or other materials provided with the
   distribution.

   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
   A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
   OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
   SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
   LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
   DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
   THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
   OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

   You can contact the author at :
   - LZ4 homepage : http://fastcompression.blogspot.com/p/lz4.html
   - LZ4 source repository : http://code.google.com/p/lz4/
*/

#endregion

#region LZ4 port

/*
Ported by: Milosz A. Krajewski <miloszkrajewski@o2.pl>
*/

#endregion

// ReSharper disable InconsistentNaming
// ReSharper disable TooWideLocalVariableScope
// ReSharper disable JoinDeclarationAndInitializer

namespace LZ4n
{
	public static partial class LZ4Codec
	{
		#region LZ4_compressCtx_32

		private unsafe static int LZ4_compressCtx_32(
			byte** hash_table,
			byte* src,
			byte* dst,
			int src_len,
			int dst_maxlen)
		{
			fixed (int* debruijn32 = &DEBRUIJN_TABLE_32[0])
			{
				byte* src_p = src;
				const int src_base = 0;
				byte* src_anchor = src_p;
				byte* src_end = src_p + src_len;
				byte* src_mflimit = src_end - MFLIMIT;

				byte* dst_p = dst;
				byte* dst_end = dst_p + dst_maxlen;

				int len, length;
				uint h, fwd_h;

				byte* src_end_LASTLITERALS = src_end - LASTLITERALS;
				byte* src_end_LASTLITERALS_1 = src_end - LASTLITERALS - 1;
				byte* src_end_LASTLITERALS_STEPSIZE_1 = src_end - LASTLITERALS - STEPSIZE_32 + 1;

				byte* dst_end_LASTLITERALS_1 = dst_end - LASTLITERALS - 1;
				byte* dst_end_LASTLITERALS_3 = dst_end - LASTLITERALS - 1 - 2;

				if (src_len < MINLENGTH) goto _last_literals;

				hash_table[(*(uint*)src_p * 2654435761u) >> HASH_ADJUST] = src_p - src_base;
				src_p++; fwd_h = (*(uint*)src_p * 2654435761u) >> HASH_ADJUST;

				while (true)
				{
					int findMatchAttempts = (1 << SKIPSTRENGTH) + 3;
					byte* fwd_p = src_p;
					byte* src_ref;
					byte* token;

					do
					{
						h = fwd_h;
						int step = findMatchAttempts++ >> SKIPSTRENGTH;
						src_p = fwd_p;
						fwd_p = src_p + step;

						if (fwd_p > src_mflimit) goto _last_literals;

						fwd_h = (*(uint*)fwd_p * 2654435761u) >> HASH_ADJUST;
						src_ref = src_base + hash_table[h];
						hash_table[h] = src_p - src_base;
					} while (src_ref < src_p - MAX_DISTANCE || *(uint*)src_ref != *(uint*)src_p);

					while (src_p > src_anchor && src_ref > src && src_p[-1] == src_ref[-1]) { src_p--; src_ref--; }

					length = (int)(src_p - src_anchor);
					token = dst_p++;
					if (dst_p + length + (length >> 8) > dst_end_LASTLITERALS_3) return 0;
					if (length >= RUN_MASK)
					{
						*token = RUN_MASK << ML_BITS;
						len = length - RUN_MASK;
						for (; len > 254; len -= 255) *dst_p++ = 255;
						*dst_p++ = (byte)len;
					}
					else
					{
						*token = (byte)(length << ML_BITS);
					}

					byte* e = (dst_p) + (length);
					do
					{
						*(uint*)dst_p = *(uint*)src_anchor; dst_p += 4; src_anchor += 4;
						*(uint*)dst_p = *(uint*)src_anchor; dst_p += 4; src_anchor += 4;
					} while (dst_p < e);
					dst_p = e;

				_next_match:
					*(ushort*)dst_p = (ushort)(src_p - src_ref); dst_p += 2;

					src_p += MINMATCH; src_ref += MINMATCH;
					src_anchor = src_p;
					while (src_p < src_end_LASTLITERALS_STEPSIZE_1)
					{
						int diff = (*(int*)(src_ref)) ^ (*(int*)(src_p));
						if (diff == 0) { src_p += STEPSIZE_32; src_ref += STEPSIZE_32; continue; }
						src_p += debruijn32[(uint)(diff & -diff) * 0x077CB531u >> 27];
						goto _endCount;
					}
					if (src_p < src_end_LASTLITERALS_1 && *(ushort*)src_ref == *(ushort*)src_p) { src_p += 2; src_ref += 2; }
					if (src_p < src_end_LASTLITERALS && *src_ref == *src_p) src_p++;

				_endCount:
					len = (int)(src_p - src_anchor);

					if (dst_p + (len >> 8) > dst_end_LASTLITERALS_1) return 0;

					if (len >= ML_MASK)
					{
						*token += ML_MASK;
						len -= ML_MASK;
						for (; len > 509; len -= 510) { *dst_p++ = 255; *dst_p++ = 255; }
						if (len > 254) { len -= 255; *dst_p++ = 255; }
						*dst_p++ = (byte)len;
					}
					else
					{
						*token += (byte)len;
					}

					if (src_p > src_mflimit) { src_anchor = src_p; break; }

					hash_table[(*(uint*)(src_p - 2) * 2654435761u) >> HASH_ADJUST] = src_p - 2 - src_base;

					h = (*(uint*)src_p * 2654435761u) >> HASH_ADJUST;
					src_ref = src_base + hash_table[h];
					hash_table[h] = src_p - src_base;

					if (src_ref > src_p - MAX_DISTANCE - 1 && *(uint*)src_ref == *(uint*)src_p)
					{
						*(token = dst_p++) = 0;
						goto _next_match;
					}

					src_anchor = src_p++;
					fwd_h = (*(uint*)src_p * 2654435761u) >> HASH_ADJUST;
				}

			_last_literals:
				var lastRun = (int)(src_end - src_anchor);
				if (dst_p - dst + lastRun + 1 + (lastRun + 255 - RUN_MASK) / 255 > dst_maxlen) return 0;
				if (lastRun >= RUN_MASK)
				{
					*dst_p++ = (RUN_MASK << ML_BITS);
					lastRun -= RUN_MASK;
					for (; lastRun > 254; lastRun -= 255) *dst_p++ = 255;
					*dst_p++ = (byte)lastRun;
				}
				else
				{
					*dst_p++ = (byte)(lastRun << ML_BITS);
				}
				BlockCopy(src_anchor, dst_p, (int)(src_end - src_anchor));
				dst_p += src_end - src_anchor;

				return (int)(dst_p - dst);
			}
		}

		#endregion

		#region LZ4_compress64kCtx_32

		private unsafe static int LZ4_compress64kCtx_32(
			ushort* hash_table,
			byte* src,
			byte* dst,
			int src_len,
			int dst_maxlen)
		{
			fixed (int* debruijn32 = &DEBRUIJN_TABLE_32[0])
			{
				byte* src_p = src;
				byte* src_anchor = src_p;
				byte* src_base = src_p;
				byte* src_end = src_p + src_len;
				byte* src_mflimit = src_end - MFLIMIT;

				byte* dst_p = dst;
				byte* dst_end = dst_p + dst_maxlen;

				byte* src_end_LASTLITERALS = src_end - LASTLITERALS;
				byte* src_end_LASTLITERALS_1 = src_end - LASTLITERALS - 1;
				byte* src_end_LASTLITERALS_STEPSIZE_1 = src_end - LASTLITERALS - STEPSIZE_32 + 1;
				byte* dst_end_LASTLITERALS_1 = dst_end - 1 - LASTLITERALS;
				byte* dst_end_LASTLITERALS_3 = dst_end - 2 - 1 - LASTLITERALS;

				int len, length;
				uint h, fwd_h;

				if (src_len < MINLENGTH) goto _last_literals;

				src_p++; fwd_h = (*(uint*)src_p * 2654435761u) >> HASH64K_ADJUST;

				while (true)
				{
					int findMatchAttempts = (1 << SKIPSTRENGTH) + 3;
					byte* fwd_p = src_p;
					byte* src_ref;
					byte* token;

					do
					{
						h = fwd_h;
						int step = findMatchAttempts++ >> SKIPSTRENGTH;
						src_p = fwd_p;
						fwd_p = src_p + step;

						if (fwd_p > src_mflimit) goto _last_literals;

						fwd_h = (*(uint*)fwd_p * 2654435761u) >> HASH64K_ADJUST;
						src_ref = src_base + hash_table[h];
						hash_table[h] = (ushort)(src_p - src_base);

					} while (*(uint*)src_ref != *(uint*)src_p);


					while (src_p > src_anchor && src_ref > src && src_p[-1] == src_ref[-1]) { src_p--; src_ref--; }

					length = (int)(src_p - src_anchor);
					token = dst_p++;

					if (dst_p + length + (length >> 8) > dst_end_LASTLITERALS_3) return 0;
					if (length >= RUN_MASK)
					{
						*token = (RUN_MASK << ML_BITS);
						len = length - RUN_MASK;
						for (; len > 254; len -= 255) *dst_p++ = 255;
						*dst_p++ = (byte)len;
					}
					else
					{
						*token = (byte)(length << ML_BITS);
					}

					byte* e = (dst_p) + (length);
					do
					{
						*(uint*)dst_p = *(uint*)src_anchor; dst_p += 4; src_anchor += 4;
						*(uint*)dst_p = *(uint*)src_anchor; dst_p += 4; src_anchor += 4;
					} while (dst_p < e);
					dst_p = e;

				_next_match:
					*(ushort*)dst_p = (ushort)(src_p - src_ref); dst_p += 2;

					src_p += MINMATCH; src_ref += MINMATCH;
					src_anchor = src_p;
					while (src_p < src_end_LASTLITERALS_STEPSIZE_1)
					{
						int diff = *(int*)src_ref ^ *(int*)src_p;
						if (diff == 0) { src_p += STEPSIZE_32; src_ref += STEPSIZE_32; continue; }
						src_p += debruijn32[(uint)(diff & -diff) * 0x077CB531u >> 27];
						goto _endCount;
					}
					if (src_p < src_end_LASTLITERALS_1 && *(ushort*)src_ref == *(ushort*)src_p) { src_p += 2; src_ref += 2; }
					if (src_p < src_end_LASTLITERALS && *src_ref == *src_p) src_p++;

				_endCount:
					len = (int)(src_p - src_anchor);
					if (dst_p + (len >> 8) > dst_end_LASTLITERALS_1) return 0;
					if (len >= ML_MASK)
					{
						*token += ML_MASK;
						len -= ML_MASK;
						for (; len > 509; len -= 510) { *dst_p++ = 255; *dst_p++ = 255; }
						if (len > 254) { len -= 255; *dst_p++ = 255; }
						*dst_p++ = (byte)len;
					}
					else
					{
						*token += (byte)len;
					}

					if (src_p > src_mflimit) { src_anchor = src_p; break; }

					hash_table[(*(uint*)(src_p - 2) * 2654435761u) >> HASH64K_ADJUST] = (ushort)(src_p - 2 - src_base);


					h = (*(uint*)src_p * 2654435761u) >> HASH64K_ADJUST;
					src_ref = src_base + hash_table[h];
					hash_table[h] = (ushort)(src_p - src_base);
					if (*(uint*)src_ref == *(uint*)src_p) { *(token = dst_p++) = 0; goto _next_match; }


					src_anchor = src_p++;
					fwd_h = (*(uint*)src_p * 2654435761u) >> HASH64K_ADJUST;
				}

			_last_literals:
				var lastRun = (int)(src_end - src_anchor);
				if (dst_p + lastRun + 1 + (lastRun - RUN_MASK + 255) / 255 > dst_end) return 0;
				if (lastRun >= RUN_MASK)
				{
					*dst_p++ = (RUN_MASK << ML_BITS);
					lastRun -= RUN_MASK;
					for (; lastRun > 254; lastRun -= 255) *dst_p++ = 255;
					*dst_p++ = (byte)lastRun;
				}
				else
				{
					*dst_p++ = (byte)(lastRun << ML_BITS);
				}
				BlockCopy(src_anchor, dst_p, (int)(src_end - src_anchor));
				dst_p += src_end - src_anchor;

				return (int)(dst_p - dst);
			}
		}

		#endregion

		#region LZ4_uncompress_32

		private unsafe static int LZ4_uncompress_32(
			byte* src,
			byte* dst,
			int dst_len)
		{
			fixed (int* dec32table = &DECODER_TABLE_32[0])
			{
				byte* src_p = src;
				byte* dst_ref;

				byte* dst_p = dst;
				byte* dst_end = dst_p + dst_len;
				byte* dst_cpy;

				byte token;
				int len, length;

				byte* dst_end_COPYLENGTH = dst_end - COPYLENGTH;

				while (true)
				{
					token = *src_p++;
					if ((length = (token >> ML_BITS)) == RUN_MASK)
					{
						for (; (len = *src_p++) == 255; length += 255) { }
						length += len;
					}

					dst_cpy = dst_p + length;
					if (dst_cpy > dst_end_COPYLENGTH)
					{
						if (dst_cpy != dst_end) goto _output_error;
						BlockCopy(src_p, dst_p, length);
						src_p += length;
						break;
					}

					do
					{
						*(uint*)dst_p = *(uint*)src_p; dst_p += 4; src_p += 4;
						*(uint*)dst_p = *(uint*)src_p; dst_p += 4; src_p += 4;
					} while (dst_p < dst_cpy);
					src_p -= dst_p - dst_cpy;
					dst_p = dst_cpy;


					dst_ref = dst_cpy - *(ushort*)src_p; src_p += 2;
					if (dst_ref < dst) goto _output_error;

					if ((length = (token & ML_MASK)) == ML_MASK)
					{
						for (; *src_p == 255; length += 255) { src_p++; }
						length += *src_p++;
					}


					if (dst_p - dst_ref < STEPSIZE_32)
					{
						const int dec64 = 0;
						dst_p[0] = dst_ref[0];
						dst_p[1] = dst_ref[1];
						dst_p[2] = dst_ref[2];
						dst_p[3] = dst_ref[3];
						dst_p += 4; dst_ref += 4;
						dst_ref -= dec32table[dst_p - dst_ref];
						*(uint*)dst_p = *(uint*)dst_ref;
						dst_p += STEPSIZE_32 - 4; dst_ref -= dec64;
					}
					else
					{
						*(uint*)dst_p = *(uint*)dst_ref; dst_p += 4; dst_ref += 4;
					}
					dst_cpy = dst_p + length - (STEPSIZE_32 - 4);
					if (dst_cpy > dst_end_COPYLENGTH)
					{
						if (dst_cpy > dst_end) goto _output_error;
						do
						{
							*(uint*)dst_p = *(uint*)dst_ref; dst_p += 4; dst_ref += 4;
							*(uint*)dst_p = *(uint*)dst_ref; dst_p += 4; dst_ref += 4;
						} while (dst_p < dst_end_COPYLENGTH);
						while (dst_p < dst_cpy) *dst_p++ = *dst_ref++;
						dst_p = dst_cpy;
						if (dst_p == dst_end) goto _output_error;
						continue;
					}
					do
					{
						*(uint*)dst_p = *(uint*)dst_ref; dst_p += 4; dst_ref += 4;
						*(uint*)dst_p = *(uint*)dst_ref; dst_p += 4; dst_ref += 4;
					} while (dst_p < dst_cpy);
					dst_p = dst_cpy;
				}


				return (int)(src_p - src);


			_output_error:
				return (int)-(src_p - src);
			}
		}

		#endregion

		#region LZ4_uncompress_unknownOutputSize_32

		private unsafe static int LZ4_uncompress_unknownOutputSize_32(
			byte* src,
			byte* dst,
			int src_len,
			int dst_maxlen)
		{
			fixed (int* dec32table = &DECODER_TABLE_32[0])
			{
				byte* src_p = src;
				byte* src_end = src_p + src_len;
				byte* dst_ref;

				byte* dst_p = dst;
				byte* dst_end = dst_p + dst_maxlen;
				byte* dst_cpy;

				byte* dst_end_COPYLENGTH = dst_end - COPYLENGTH;
				byte* src_end_COPYLENGTH = src_end - COPYLENGTH;

				while (src_p < src_end)
				{
					byte token;
					int len, length;

					token = *src_p++;
					if ((length = (token >> ML_BITS)) == RUN_MASK)
					{
						len = 255;
						while (src_p < src_end && len == 255) { len = *src_p++; length += len; }
					}

					dst_cpy = dst_p + length;
					if (dst_cpy > dst_end_COPYLENGTH || src_p + length > src_end_COPYLENGTH)
					{
						if (dst_cpy > dst_end) goto _output_error;
						if (src_p + length != src_end) goto _output_error;
						BlockCopy(src_p, dst_p, length);
						dst_p += length;
						break;
					}
					{
						do
						{
							*(uint*)dst_p = *(uint*)src_p; dst_p += 4; src_p += 4;
							*(uint*)dst_p = *(uint*)src_p; dst_p += 4; src_p += 4;
						} while (dst_p < dst_cpy);
					}
					src_p -= (dst_p - dst_cpy);
					dst_p = dst_cpy;


					dst_ref = dst_cpy - *(ushort*)src_p; src_p += 2;
					if (dst_ref < dst) goto _output_error;

					if ((length = (token & ML_MASK)) == ML_MASK)
					{
						while (src_p < src_end) { len = *src_p++; length += len; if (len != 255) break; }
					}

					if (dst_p - dst_ref < STEPSIZE_32)
					{
						const int dec64 = 0;
						dst_p[0] = dst_ref[0];
						dst_p[1] = dst_ref[1];
						dst_p[2] = dst_ref[2];
						dst_p[3] = dst_ref[3];
						dst_p += 4; dst_ref += 4;
						dst_ref -= dec32table[dst_p - dst_ref];
						*(uint*)dst_p = *(uint*)dst_ref;
						dst_p += STEPSIZE_32 - 4; dst_ref -= dec64;
					}
					else
					{
						*(uint*)dst_p = *(uint*)dst_ref; dst_p += 4; dst_ref += 4;
					}
					dst_cpy = dst_p + length - (STEPSIZE_32 - 4);
					if (dst_cpy > dst_end_COPYLENGTH)
					{
						if (dst_cpy > dst_end) goto _output_error;
						do
						{
							*(uint*)dst_p = *(uint*)dst_ref; dst_p += 4; dst_ref += 4;
							*(uint*)dst_p = *(uint*)dst_ref; dst_p += 4; dst_ref += 4;
						} while (dst_p < dst_end_COPYLENGTH);
						while (dst_p < dst_cpy) *dst_p++ = *dst_ref++;
						dst_p = dst_cpy;
						if (dst_p == dst_end) goto _output_error;
						continue;
					}
					do
					{
						*(uint*)(dst_p) = *(uint*)(dst_ref); dst_p += 4; dst_ref += 4;
						*(uint*)(dst_p) = *(uint*)(dst_ref); dst_p += 4; dst_ref += 4;
					} while (dst_p < dst_cpy);
					dst_p = dst_cpy;
				}


				return (int)(dst_p - dst);


			_output_error:
				return (int)-(src_p - src);
			}
		}

		#endregion
	}
}

// ReSharper restore JoinDeclarationAndInitializer
// ReSharper restore TooWideLocalVariableScope
// ReSharper restore InconsistentNaming
