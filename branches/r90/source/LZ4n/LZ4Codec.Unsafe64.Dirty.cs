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

// ReSharper disable InconsistentNaming
// ReSharper disable TooWideLocalVariableScope
// ReSharper disable JoinDeclarationAndInitializer

namespace LZ4n
{
	public static partial class LZ4Codec
	{
		#region LZ4_compressCtx_64

		private static unsafe int LZ4_compressCtx_64(
			uint* hash_table,
			byte* src,
			byte* dst,
			int src_len,
			int dst_maxlen)
		{
			// r90
			fixed (int* debruijn64 = &DEBRUIJN_TABLE_64[0])
			{
				var src_p = src;
				var src_base = src_p;
				var src_anchor = src_p;
				var src_end = src_p + src_len;
				var src_mflimit = src_end - MFLIMIT;

				var dst_p = dst;
				var dst_end = dst_p + dst_maxlen;

				var src_LASTLITERALS = (src_end - LASTLITERALS);
				var matchlimit_1 = (src_LASTLITERALS - 1);
				var matchlimit_3 = (src_LASTLITERALS - 3);
				var matchlimit_STEPSIZE_1 = (src_LASTLITERALS - (STEPSIZE_64 - 1));
				var dst_LASTLITERALS_1 = (dst_end - (1 + LASTLITERALS));
				var dst_LASTLITERALS_3 = (dst_end - (2 + 1 + LASTLITERALS));

				int length;
				uint h, h_fwd;

				if (src_len < MINLENGTH) goto _last_literals;

				hash_table[(*(uint*)src_p * 2654435761u) >> HASH_ADJUST] = (uint)(src_p - src_base);
				src_p++;
				h_fwd = (*(uint*)src_p * 2654435761u) >> HASH_ADJUST;

				while (true)
				{
					var findMatchAttempts = (1 << SKIPSTRENGTH) + 3;
					var src_p_fwd = src_p;
					byte* src_ref;
					byte* dst_token;

					do
					{
						h = h_fwd;
						var step = findMatchAttempts++ >> SKIPSTRENGTH;
						src_p = src_p_fwd;
						src_p_fwd = src_p + step;

						if (src_p_fwd > src_mflimit) goto _last_literals;

						h_fwd = ((((*(uint*)(src_p_fwd))) * 2654435761u) >> HASH_ADJUST);
						src_ref = src_base + hash_table[h];
						hash_table[h] = (uint)(src_p - src_base);
					} while ((src_ref < src_p - MAX_DISTANCE) || ((*(uint*)(src_ref)) != (*(uint*)(src_p))));

					while ((src_p > src_anchor) && (src_ref > src) && (src_p[-1] == src_ref[-1]))
					{
						src_p--;
						src_ref--;
					}

					length = (int)(src_p - src_anchor);
					dst_token = dst_p++;

					if (dst_p + length + (length >> 8) > dst_LASTLITERALS_3) return 0;

					if (length >= RUN_MASK)
					{
						var len = length - RUN_MASK;
						*dst_token = (RUN_MASK << ML_BITS);
						if (len > 254)
						{
							do
							{
								*dst_p++ = 255;
								len -= 255;
							} while (len > 254);
							*dst_p++ = (byte)len;
							BlockCopy(src_anchor, dst_p, (length));
							dst_p += length;
							goto _next_match;
						}
						*dst_p++ = (byte)len;
					}
					else *dst_token = (byte)(length << ML_BITS);
					{
						var e = (dst_p) + (length);
						{
							do
							{
								(*(ulong*)(dst_p)) = (*(ulong*)(src_anchor));
								dst_p += 8;
								src_anchor += 8;
							} while (dst_p < e);
						}
						dst_p = e;
					}

				_next_match:
					{
						(*(ushort*)(dst_p)) = (ushort)(src_p - src_ref);
						dst_p += 2;
					}

					src_p += MINMATCH;
					src_ref += MINMATCH;
					src_anchor = src_p;

					while (src_p < matchlimit_STEPSIZE_1)
					{
						var diff = (*(long*)(src_ref)) ^ (*(long*)(src_p));
						if (diff == 0)
						{
							src_p += STEPSIZE_64;
							src_ref += STEPSIZE_64;
							continue;
						}
						src_p += debruijn64[(((ulong)((diff) & -(diff)) * 0x0218A392CDABBD3FL)) >> 58];
						goto _endCount;
					}

					if ((src_p < matchlimit_3) && ((*(uint*)(src_ref)) == (*(uint*)(src_p))))
					{
						src_p += 4;
						src_ref += 4;
					}
					if ((src_p < matchlimit_1) && ((*(ushort*)(src_ref)) == (*(ushort*)(src_p))))
					{
						src_p += 2;
						src_ref += 2;
					}
					if ((src_p < src_LASTLITERALS) && (*src_ref == *src_p)) src_p++;

				_endCount:
					length = (int)(src_p - src_anchor);

					if (dst_p + (length >> 8) > dst_LASTLITERALS_1) return 0;

					if (length >= ML_MASK)
					{
						*dst_token += ML_MASK;
						length -= ML_MASK;
						for (; length > 509; length -= 510)
						{
							*dst_p++ = 255;
							*dst_p++ = 255;
						}
						if (length > 254)
						{
							length -= 255;
							*dst_p++ = 255;
						}
						*dst_p++ = (byte)length;
					}
					else *dst_token += (byte)length;

					if (src_p > src_mflimit)
					{
						src_anchor = src_p;
						break;
					}

					hash_table[(((*(uint*)(src_p - 2))) * 2654435761u) >> HASH_ADJUST] = (uint)(src_p - 2 - src_base);

					h = (((*(uint*)(src_p))) * 2654435761u) >> HASH_ADJUST;
					src_ref = src_base + hash_table[h];
					hash_table[h] = (uint)(src_p - src_base);

					if ((src_ref > src_p - (MAX_DISTANCE + 1)) && ((*(uint*)(src_ref)) == (*(uint*)(src_p))))
					{
						dst_token = dst_p++;
						*dst_token = 0;
						goto _next_match;
					}

					src_anchor = src_p++;
					h_fwd = ((((*(uint*)(src_p))) * 2654435761u) >> HASH_ADJUST);
				}

			_last_literals:
				{
					var lastRun = (int)(src_end - src_anchor);

					if (dst_p + lastRun + 1 + ((lastRun + 255 - RUN_MASK) / 255) > dst_end) return 0;

					if (lastRun >= RUN_MASK)
					{
						*dst_p++ = (RUN_MASK << ML_BITS);
						lastRun -= RUN_MASK;
						for (; lastRun > 254; lastRun -= 255) *dst_p++ = 255;
						*dst_p++ = (byte)lastRun;
					}
					else *dst_p++ = (byte)(lastRun << ML_BITS);
					BlockCopy(src_anchor, dst_p, (int)(src_end - src_anchor));
					dst_p += src_end - src_anchor;
				}

				return (int)((dst_p) - dst);
			}
		}

		#endregion

		#region LZ4_compress64kCtx_64

		private static unsafe int LZ4_compress64kCtx_64(
			ushort* hash_table,
			byte* src,
			byte* dst,
			int src_len,
			int dst_maxlen)
		{
			// r90
			fixed (int* debruijn64 = &DEBRUIJN_TABLE_64[0])
			{
				var src_p = src;
				var src_anchor = src_p;
				var src_base = src_p;
				var src_end = src_p + src_len;
				var src_mflimit = src_end - MFLIMIT;

				var dst_p = dst;
				var dst_end = dst_p + dst_maxlen;

				var src_LASTLITERALS = (src_end - LASTLITERALS);
				var matchlimit_1 = (src_LASTLITERALS - 1);
				var matchlimit_3 = (src_LASTLITERALS - 3);
				var matchlimit_STEPSIZE_1 = (src_LASTLITERALS - (STEPSIZE_64 - 1));
				var dst_LASTLITERALS_1 = (dst_end - (1 + LASTLITERALS));
				var dst_LASTLITERALS_3 = (dst_end - (2 + 1 + LASTLITERALS));

				int len, length;
				uint h, h_fwd;

				if (src_len < MINLENGTH) goto _last_literals;

				src_p++;
				h_fwd = (*(uint*)src_p * 2654435761u) >> HASH64K_ADJUST;

				while (true)
				{
					var findMatchAttempts = (1 << SKIPSTRENGTH) + 3;
					var src_p_fwd = src_p;
					byte* src_ref;
					byte* dst_token;

					do
					{
						h = h_fwd;
						var step = findMatchAttempts++ >> SKIPSTRENGTH;
						src_p = src_p_fwd;
						src_p_fwd = src_p + step;

						if (src_p_fwd > src_mflimit) goto _last_literals;

						h_fwd = (*(uint*)src_p_fwd * 2654435761u) >> HASH64K_ADJUST;
						src_ref = src_base + hash_table[h];
						hash_table[h] = (ushort)(src_p - src_base);
					} while (*(uint*)src_ref != *(uint*)src_p);

					while (src_p > src_anchor && src_ref > src && src_p[-1] == src_ref[-1])
					{
						src_p--;
						src_ref--;
					}

					length = (int)(src_p - src_anchor);
					dst_token = dst_p++;

					if (dst_p + length + (length >> 8) > dst_LASTLITERALS_3) return 0;

					if (length >= RUN_MASK)
					{
						len = length - RUN_MASK;
						*dst_token = (RUN_MASK << ML_BITS);
						if (len > 254)
						{
							do
							{
								*dst_p++ = 255;
								len -= 255;
							} while (len > 254);
							*dst_p++ = (byte)len;
							BlockCopy(src_anchor, dst_p, (length));
							dst_p += length;
							goto _next_match;
						}
						*dst_p++ = (byte)len;
					}
					else *dst_token = (byte)(length << ML_BITS);

					var e = dst_p + length;
					{
						do
						{
							*(ulong*)dst_p = *(ulong*)src_anchor;
							dst_p += 8;
							src_anchor += 8;
						} while (dst_p < e);
					}
					dst_p = e;

				_next_match:
					*(ushort*)dst_p = (ushort)(src_p - src_ref);
					dst_p += 2;

					src_p += MINMATCH;
					src_ref += MINMATCH;
					src_anchor = src_p;

					while (src_p < matchlimit_STEPSIZE_1)
					{
						var diff = *(long*)src_ref ^ *(long*)src_p;
						if (diff == 0)
						{
							src_p += STEPSIZE_64;
							src_ref += STEPSIZE_64;
							continue;
						}
						src_p += debruijn64[((ulong)(diff & -diff) * 0x0218A392CDABBD3FL) >> 58];
						goto _endCount;
					}

					if (src_p < matchlimit_3 && *(uint*)src_ref == *(uint*)src_p)
					{
						src_p += 4;
						src_ref += 4;
					}
					if (src_p < matchlimit_1 && *(ushort*)src_ref == *(ushort*)src_p)
					{
						src_p += 2;
						src_ref += 2;
					}
					if (src_p < src_LASTLITERALS && *src_ref == *src_p) src_p++;

				_endCount:
					len = (int)(src_p - src_anchor);
					if (dst_p + (len >> 8) > dst_LASTLITERALS_1) return 0;

					if (len >= ML_MASK)
					{
						*dst_token += ML_MASK;
						len -= ML_MASK;
						for (; len > 509; len -= 510)
						{
							*dst_p++ = 255;
							*dst_p++ = 255;
						}
						if (len > 254)
						{
							len -= 255;
							*dst_p++ = 255;
						}
						*dst_p++ = (byte)len;
					}
					else *dst_token += (byte)len;

					if (src_p > src_mflimit)
					{
						src_anchor = src_p;
						break;
					}

					hash_table[(*(uint*)(src_p - 2) * 2654435761u) >> HASH64K_ADJUST] = (ushort)(src_p - 2 - src_base);

					h = (*(uint*)src_p * 2654435761u) >> HASH64K_ADJUST;
					src_ref = src_base + hash_table[h];
					hash_table[h] = (ushort)(src_p - src_base);

					if (*(uint*)src_ref == *(uint*)src_p)
					{
						dst_token = dst_p++;
						*dst_token = 0;
						goto _next_match;
					}

					src_anchor = src_p++;
					h_fwd = (*(uint*)src_p * 2654435761u) >> HASH64K_ADJUST;
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
				else *dst_p++ = (byte)(lastRun << ML_BITS);
				BlockCopy(src_anchor, dst_p, (int)(src_end - src_anchor));
				dst_p += src_end - src_anchor;

				return (int)(dst_p - dst);
			}
		}

		#endregion

		#region LZ4_uncompress_64

		private static unsafe int LZ4_uncompress_64(
			byte* src,
			byte* dst,
			int dst_len)
		{
			fixed (int* dec32table = &DECODER_TABLE_32[0])
			fixed (int* dec64table = &DECODER_TABLE_64[0])
			{
				byte* src_p = src;
				byte* xxx_ref;

				byte* dst_p = (byte*)dst;
				byte* dst_end = dst_p + dst_len;
				byte* dst_cpy;

				// byte* oend_COPYLENGTH = (dst_end - COPYLENGTH);
				byte* oend_COPYLENGTH_STEPSIZE_4 = (dst_end-(COPYLENGTH)-(STEPSIZE_64-4));

				int src_token;

				while (true)
				{
					int length;

					src_token = *src_p++;
					if ((length = (src_token >> ML_BITS)) == RUN_MASK)
					{
						int len;
						for (; (len = *src_p++) == 255; length += 255) /* do nothing */;
						length += len;
					}

					dst_cpy = dst_p + length;
					if (dst_cpy > dst_end-COPYLENGTH)
					{
						if (dst_cpy != dst_end) goto _output_error;
						BlockCopy(src_p, dst_p, (int)(length));
						src_p += length;
						break;
					}
					{ do { (*(ulong*)(dst_p)) = (*(ulong*)(src_p)); dst_p += 8; src_p += 8; } while (dst_p < dst_cpy); }; src_p -= (dst_p-dst_cpy); dst_p = dst_cpy;

					{ xxx_ref = (dst_cpy) - (*(ushort*)(src_p)); }; src_p+=2;
					if (xxx_ref < (byte*)dst) goto _output_error;

					if ((length=(src_token&ML_MASK)) == ML_MASK) { for (; *src_p==255; length+=255) { src_p++; } length += *src_p++; }

					if ((dst_p-xxx_ref)<STEPSIZE_64)
					{
						int dec64 = dec64table[dst_p-xxx_ref];

						dst_p[0] = xxx_ref[0];
						dst_p[1] = xxx_ref[1];
						dst_p[2] = xxx_ref[2];
						dst_p[3] = xxx_ref[3];
						dst_p += 4; xxx_ref += 4; xxx_ref -= dec32table[dst_p-xxx_ref];
						*(uint*)dst_p = *(uint*)xxx_ref;
						dst_p += STEPSIZE_64-4; xxx_ref -= dec64;
					}
					else
					{
						*(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8;
					}
					dst_cpy = dst_p + length - (STEPSIZE_64-4);

					if (dst_cpy > oend_COPYLENGTH_STEPSIZE_4)
					{
						if (dst_cpy > dst_end-LASTLITERALS) goto _output_error;
						if (dst_p < (dst_end-COPYLENGTH))
						{
							do
							{
								*(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8;
							} while (dst_p < dst_end - COPYLENGTH);
						}
						while (dst_p<dst_cpy) *dst_p++=*xxx_ref++;
						dst_p=dst_cpy;
						continue;
					}

					{
						do
						{
							*(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8;
						} while (dst_p < dst_cpy);
					}
					dst_p = dst_cpy;
				}

				return (int)((byte*)src_p - src);

			_output_error:
				return -(int)((byte*)src_p - src);
			}
		}

		#endregion

		#region LZ4_uncompress_unknownOutputSize_64

		private static unsafe int LZ4_uncompress_unknownOutputSize_64(
			byte* src,
			byte* dst,
			int src_len,
			int dst_maxlen)
		{
			fixed (int* dec32table = &DECODER_TABLE_32[0])
			fixed (int* dec64table = &DECODER_TABLE_64[0])
			{
				var src_p = src;
				var src_end = src_p + src_len;
				byte* dst_ref;

				var dst_p = dst;
				var dst_end = dst_p + dst_maxlen;
				byte* dst_cpy;

				var dst_end_COPYLENGTH = dst_end - COPYLENGTH;
				var src_end_COPYLENGTH = src_end - COPYLENGTH;

				while (src_p < src_end)
				{
					byte token;
					int len, length;

					token = *src_p++;
					if ((length = (token >> ML_BITS)) == RUN_MASK)
					{
						len = 255;
						while (src_p < src_end && len == 255) length += (len = *src_p++);
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
					do
					{
						*(ulong*)dst_p = *(ulong*)src_p;
						dst_p += 8;
						src_p += 8;
					} while (dst_p < dst_cpy);
					src_p -= (dst_p - dst_cpy);
					dst_p = dst_cpy;

					dst_ref = (dst_cpy) - (*(ushort*)(src_p));
					src_p += 2;
					if (dst_ref < dst) goto _output_error;

					if ((length = (token & ML_MASK)) == ML_MASK)
					{
						while (src_p < src_end)
						{
							length += (len = *src_p++);
							if (len != 255) break;
						}
					}

					if (dst_p - dst_ref < STEPSIZE_64)
					{
						var dec64 = dec64table[dst_p - dst_ref];
						dst_p[0] = dst_ref[0];
						dst_p[1] = dst_ref[1];
						dst_p[2] = dst_ref[2];
						dst_p[3] = dst_ref[3];
						dst_p += 4;
						dst_ref += 4;
						dst_ref -= dec32table[dst_p - dst_ref];
						*(uint*)dst_p = *(uint*)dst_ref;
						dst_p += STEPSIZE_64 - 4;
						dst_ref -= dec64;
					}
					else
					{
						*(ulong*)dst_p = *(ulong*)dst_ref;
						dst_p += 8;
						dst_ref += 8;
					}
					dst_cpy = dst_p + length - (STEPSIZE_64 - 4);
					if (dst_cpy > dst_end_COPYLENGTH)
					{
						if (dst_cpy > dst_end) goto _output_error;
						while (dst_p < dst_end_COPYLENGTH)
						{
							*(ulong*)dst_p = *(ulong*)dst_ref;
							dst_p += 8;
							dst_ref += 8;
						}
						while (dst_p < dst_cpy) *dst_p++ = *dst_ref++;
						dst_p = dst_cpy;
						if (dst_p == dst_end) goto _output_error;
						continue;
					}
					while (dst_p < dst_cpy)
					{
						*(ulong*)dst_p = *(ulong*)dst_ref;
						dst_p += 8;
						dst_ref += 8;
					}
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