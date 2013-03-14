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

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable TooWideLocalVariableScope
// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable RedundantIfElseBlock

namespace LZ4s
{
	public static partial class LZ4Codec
	{
		#region LZ4_compressCtx

		private static int LZ4_compressCtx_safe32(
			int[] hash_table,
			byte[] src,
			byte[] dst,
			int src_0,
			int dst_0,
			int src_len,
			int dst_maxlen)
		{
			var debruijn32 = DEBRUIJN_TABLE_32;
			int _i;

			// ---- preprocessed source start here ----

			var src_p = src_0;
			var src_base = src_0;
			var src_anchor = src_p;
			var src_end = src_p + src_len;
			var src_mflimit = src_end - MFLIMIT;

			var dst_p = dst_0;
			var dst_end = dst_p + dst_maxlen;

			var src_LASTLITERALS = src_end - LASTLITERALS;
			var src_LASTLITERALS_1 = src_LASTLITERALS - 1;
			var src_LASTLITERALS_STEPSIZE_1 = src_LASTLITERALS - STEPSIZE_32 + 1;
			var dst_LASTLITERALS_1 = dst_end - 1 - LASTLITERALS;
			var dst_LASTLITERALS_3 = dst_end - 2 - 1 - LASTLITERALS;

			int len, length;
			uint h, h_fwd;

			if (src_len < MINLENGTH) goto _last_literals;

			hash_table[(Peek4(src, src_p) * 2654435761u) >> HASH_ADJUST] = src_p - src_base;
			src_p++;
			h_fwd = (Peek4(src, src_p) * 2654435761u) >> HASH_ADJUST;

			while (true)
			{
				var findMatchAttempts = (1 << SKIPSTRENGTH) + 3;
				var src_p_fwd = src_p;
				int src_ref;
				int dst_token;

				do
				{
					h = h_fwd;
					var step = findMatchAttempts++ >> SKIPSTRENGTH;
					src_p = src_p_fwd;
					src_p_fwd = src_p + step;

					if (src_p_fwd > src_mflimit) goto _last_literals;

					h_fwd = (Peek4(src, src_p_fwd) * 2654435761u) >> HASH_ADJUST;
					src_ref = src_base + hash_table[h];
					hash_table[h] = src_p - src_base;
				} while (src_ref < src_p - MAX_DISTANCE || !Equal4(src, src_ref, src_p));

				while (src_p > src_anchor && src_ref > src_0 && src[src_p - 1] == src[src_ref - 1])
				{
					src_p--;
					src_ref--;
				}

				length = src_p - src_anchor;
				dst_token = dst_p++;
				if (dst_p + length + (length >> 8) > dst_LASTLITERALS_3) return 0;

				if (length >= RUN_MASK)
				{
					len = length - RUN_MASK;
					dst[dst_token] = RUN_MASK << ML_BITS;
					if (len > 254)
					{
						do
						{
							dst[dst_p++] = 255;
							len -= 255;
						} while (len > 254);
						dst[dst_p++] = (byte)len;
						BlockCopy(src, src_anchor, dst, dst_p, length);
						dst_p += length;
						goto _next_match;
					}
					else
					{
						dst[dst_p++] = (byte)len;
					}
				}
				else
				{
					dst[dst_token] = (byte)(length << ML_BITS);
				}

				_i = dst_p + length; /* src_anchor += */
				WildCopy(src, src_anchor, dst, dst_p, _i);
				dst_p = _i;

			_next_match:
				Poke2(dst, dst_p, (ushort)(src_p - src_ref));
				dst_p += 2;

				src_p += MINMATCH;
				src_ref += MINMATCH;
				src_anchor = src_p;
				while (src_p < src_LASTLITERALS_STEPSIZE_1)
				{
					var diff = (int)Xor4(src, src_ref, src_p);
					if (diff == 0)
					{
						src_p += STEPSIZE_32;
						src_ref += STEPSIZE_32;
						continue;
					}
					src_p += debruijn32[(uint)(diff & -diff) * 0x077CB531u >> 27];
					goto _endCount;
				}
				if (src_p < src_LASTLITERALS_1 && Equal2(src, src_ref, src_p))
				{
					src_p += 2;
					src_ref += 2;
				}
				if (src_p < src_LASTLITERALS && src[src_ref] == src[src_p]) src_p++;

			_endCount:
				len = src_p - src_anchor;
				if (dst_p + (len >> 8) > dst_LASTLITERALS_1) return 0;

				if (len >= ML_MASK)
				{
					dst[dst_token] += ML_MASK;
					len -= ML_MASK;
					for (; len > 509; len -= 510)
					{
						dst[dst_p++] = 255;
						dst[dst_p++] = 255;
					}
					if (len > 254)
					{
						len -= 255;
						dst[dst_p++] = 255;
					}
					dst[dst_p++] = (byte)len;
				}
				else
				{
					dst[dst_token] += (byte)len;
				}

				if (src_p > src_mflimit)
				{
					src_anchor = src_p;
					break;
				}

				hash_table[(Peek4(src, src_p - 2) * 2654435761u) >> HASH_ADJUST] = src_p - 2 - src_base;

				h = (Peek4(src, src_p) * 2654435761u) >> HASH_ADJUST;
				src_ref = src_base + hash_table[h];
				hash_table[h] = src_p - src_base;

				if (src_ref > src_p - MAX_DISTANCE - 1 && Equal4(src, src_ref, src_p))
				{
					dst[dst_token = dst_p++] = 0;
					goto _next_match;
				}

				src_anchor = src_p++;
				h_fwd = (Peek4(src, src_p) * 2654435761u) >> HASH_ADJUST;
			}

		_last_literals:
			var lastRun = src_end - src_anchor;

			if (dst_p + lastRun + 1 + (lastRun + 255 - RUN_MASK) / 255 > dst_end) return 0;

			if (lastRun >= RUN_MASK)
			{
				dst[dst_p++] = RUN_MASK << ML_BITS;
				lastRun -= RUN_MASK;
				for (; lastRun > 254; lastRun -= 255) dst[dst_p++] = 255;
				dst[dst_p++] = (byte)lastRun;
			}
			else
			{
				dst[dst_p++] = (byte)(lastRun << ML_BITS);
			}
			BlockCopy(src, src_anchor, dst, dst_p, src_end - src_anchor);
			dst_p += src_end - src_anchor;

			return dst_p - dst_0;
		}

		#endregion

		#region LZ4_compress64kCtx

		private static int LZ4_compress64kCtx_safe32(
			ushort[] hash_table,
			byte[] src,
			byte[] dst,
			int src_0,
			int dst_0,
			int src_len,
			int dst_maxlen)
		{
			var debruijn32 = DEBRUIJN_TABLE_32;
			int _i;

			// ---- preprocessed source start here ----

			var src_p = src_0;
			var src_anchor = src_p;
			var src_base = src_p;
			var src_end = src_p + src_len;
			var src_mflimit = src_end - MFLIMIT;
			var dst_p = dst_0;
			var dst_end = dst_p + dst_maxlen;


			var src_LASTLITERALS = src_end - LASTLITERALS;
			var src_LASTLITERALS_1 = src_LASTLITERALS - 1;
			var src_LASTLITERALS_STEPSIZE_1 = src_LASTLITERALS - STEPSIZE_32 + 1;
			var dst_LASTLITERALS_1 = dst_end - 1 - LASTLITERALS;
			var dst_LASTLITERALS_3 = dst_end - 2 - 1 - LASTLITERALS;

			int len, length;
			uint h, h_fwd;

			if (src_len < MINLENGTH) goto _last_literals;

			src_p++;
			h_fwd = (Peek4(src, src_p) * 2654435761u) >> HASH64K_ADJUST;

			while (true)
			{
				var findMatchAttempts = (1 << SKIPSTRENGTH) + 3;
				var src_p_fwd = src_p;
				int src_ref;
				int dst_token;

				do
				{
					h = h_fwd;
					var step = findMatchAttempts++ >> SKIPSTRENGTH;
					src_p = src_p_fwd;
					src_p_fwd = src_p + step;

					if (src_p_fwd > src_mflimit) goto _last_literals;

					h_fwd = (Peek4(src, src_p_fwd) * 2654435761u) >> HASH64K_ADJUST;
					src_ref = src_base + hash_table[h];
					hash_table[h] = (ushort)(src_p - src_base);
				} while (!Equal4(src, src_ref, src_p));


				while (src_p > src_anchor && src_ref > src_0 && src[src_p - 1] == src[src_ref - 1])
				{
					src_p--;
					src_ref--;
				}


				length = src_p - src_anchor;
				dst_token = dst_p++;
				if (dst_p + length + (length >> 8) > dst_LASTLITERALS_3) return 0;

				if (length >= RUN_MASK)
				{
					len = length - RUN_MASK;
					dst[dst_token] = (RUN_MASK << ML_BITS);
					if (len > 254)
					{
						do
						{
							dst[dst_p++] = 255;
							len -= 255;
						} while (len > 254);
						dst[dst_p++] = (byte)len;
						BlockCopy(src, src_anchor, dst, dst_p, length);
						dst_p += length;
						goto _next_match;
					}
					else
					{
						dst[dst_p++] = (byte)len;
					}
				}
				else
				{
					dst[dst_token] = (byte)(length << ML_BITS);
				}

				_i = dst_p + length; /* src_anchor += */
				WildCopy(src, src_anchor, dst, dst_p, _i);
				dst_p = _i;

			_next_match:
				Poke2(dst, dst_p, (ushort)(src_p - src_ref));
				dst_p += 2;


				src_p += MINMATCH;
				src_ref += MINMATCH;
				src_anchor = src_p;
				while (src_p < src_LASTLITERALS_STEPSIZE_1)
				{
					var diff = (int)Xor4(src, src_ref, src_p);
					if (diff == 0)
					{
						src_p += STEPSIZE_32;
						src_ref += STEPSIZE_32;
						continue;
					}
					src_p += debruijn32[(uint)(diff & -diff) * 0x077CB531u >> 27];
					goto _endCount;
				}
				if (src_p < src_LASTLITERALS_1 && Equal2(src, src_ref, src_p))
				{
					src_p += 2;
					src_ref += 2;
				}
				if (src_p < src_LASTLITERALS && src[src_ref] == src[src_p]) src_p++;

			_endCount:
				len = src_p - src_anchor;
				if (dst_p + (len >> 8) > dst_LASTLITERALS_1) return 0;
				if (len >= ML_MASK)
				{
					dst[dst_token] += ML_MASK;
					len -= ML_MASK;
					for (; len > 509; len -= 510)
					{
						dst[dst_p++] = 255;
						dst[dst_p++] = 255;
					}
					if (len > 254)
					{
						len -= 255;
						dst[dst_p++] = 255;
					}
					dst[dst_p++] = (byte)len;
				}
				else
				{
					dst[dst_token] += (byte)len;
				}

				if (src_p > src_mflimit)
				{
					src_anchor = src_p;
					break;
				}

				hash_table[(Peek4(src, src_p - 2) * 2654435761u) >> HASH64K_ADJUST] = (ushort)(src_p - 2 - src_base);


				h = (Peek4(src, src_p) * 2654435761u) >> HASH64K_ADJUST;
				src_ref = src_base + hash_table[h];
				hash_table[h] = (ushort)(src_p - src_base);
				if (Equal4(src, src_ref, src_p))
				{
					dst[dst_token = dst_p++] = 0;
					goto _next_match;
				}

				src_anchor = src_p++;
				h_fwd = (Peek4(src, src_p) * 2654435761u) >> HASH64K_ADJUST;
			}

		_last_literals:
			var lastRun = src_end - src_anchor;
			if (dst_p + lastRun + 1 + (lastRun - RUN_MASK + 255) / 255 > dst_end) return 0;
			if (lastRun >= RUN_MASK)
			{
				dst[dst_p++] = RUN_MASK << ML_BITS;
				lastRun -= RUN_MASK;
				for (; lastRun > 254; lastRun -= 255) dst[dst_p++] = 255;
				dst[dst_p++] = (byte)lastRun;
			}
			else
			{
				dst[dst_p++] = (byte)(lastRun << ML_BITS);
			}
			BlockCopy(src, src_anchor, dst, dst_p, src_end - src_anchor);
			dst_p += src_end - src_anchor;

			return dst_p - dst_0;
		}

		#endregion

		#region LZ4_uncompress

		private static int LZ4_uncompress_safe32(
			byte[] src,
			byte[] dst,
			int src_0,
			int dst_0,
			int dst_len)
		{
			var dec32table = DECODER_TABLE_32;
			int _i;

			// ---- preprocessed source start here ----

			var src_p = src_0;
			int dst_ref;

			var dst_p = dst_0;
			var dst_end = dst_p + dst_len;
			int dst_cpy;

			var oend_COPYLENGTH = dst_end - COPYLENGTH;

			byte token;

			int len, length;

			while (true)
			{
				token = src[src_p++];
				if ((length = (token >> ML_BITS)) == RUN_MASK)
				{
					for (; (len = src[src_p++]) == 255; length += 255)
					{
					}
					length += len;
				}

				dst_cpy = dst_p + length;
				if (dst_cpy > oend_COPYLENGTH)
				{
					if (dst_cpy != dst_end) goto _output_error;
					BlockCopy(src, src_p, dst, dst_p, length);
					src_p += length;
					break;
				}
				_i = WildCopy(src, src_p, dst, dst_p, dst_cpy);
				src_p += _i;
				dst_p += _i;
				src_p -= dst_p - dst_cpy;
				dst_p = dst_cpy;

				dst_ref = dst_cpy - Peek2(src, src_p);
				src_p += 2;
				if (dst_ref < dst_0) goto _output_error;

				if ((length = (token & ML_MASK)) == ML_MASK)
				{
					for (; src[src_p] == 255; length += 255) src_p++;
					length += src[src_p++];
				}

				if (dst_p - dst_ref < STEPSIZE_32)
				{
					const int dec64 = 0;
					dst[dst_p] = dst[dst_ref];
					dst[dst_p + 1] = dst[dst_ref + 1];
					dst[dst_p + 2] = dst[dst_ref + 2];
					dst[dst_p + 3] = dst[dst_ref + 3];
					dst_p += 4;
					dst_ref += 4;
					dst_ref -= dec32table[dst_p - dst_ref];
					Copy4(dst, dst_ref, dst_p);
					dst_p += STEPSIZE_32 - 4;
					dst_ref -= dec64;
				}
				else
				{
					Copy4(dst, dst_ref, dst_p);
					dst_p += 4;
					dst_ref += 4;
				}
				dst_cpy = dst_p + length - (STEPSIZE_32 - 4);
				if (dst_cpy > oend_COPYLENGTH)
				{
					if (dst_cpy > dst_end) goto _output_error;
					_i = WildCopy32(dst, dst_ref, dst_p, oend_COPYLENGTH);
					dst_ref += _i;
					dst_p += _i;
					while (dst_p < dst_cpy) dst[dst_p++] = dst[dst_ref++];
					dst_p = dst_cpy;
					if (dst_p == dst_end) goto _output_error;
					continue;
				}
				/* _i = */
				WildCopy32(dst, dst_ref, dst_p, dst_cpy); /* dst_ref += _i; dst_p += _i; */
				dst_p = dst_cpy;
			}

			return src_p - src_0;

		_output_error:
			return -(src_p - src_0);
		}

		#endregion

		#region LZ4_uncompress_unknownOutputSize

		private static int LZ4_uncompress_unknownOutputSize_safe32(
			byte[] src,
			byte[] dst,
			int src_0,
			int dst_0,
			int src_len,
			int dst_maxlen)
		{
			var dec32table = DECODER_TABLE_32;
			int _i;

			// ---- preprocessed source start here ----

			var src_p = src_0;
			var src_end = src_p + src_len;
			int dst_ref;

			var dst_p = dst_0;
			var dst_end = dst_p + dst_maxlen;
			int dst_cpy;

			var iend_COPYLENGTH = src_end - COPYLENGTH;
			var oend_COPYLENGTH = dst_end - COPYLENGTH;

			while (src_p < src_end)
			{
				byte token;
				int len, length;

				token = src[src_p++];
				if ((length = (token >> ML_BITS)) == RUN_MASK)
				{
					len = 255;
					while (src_p < src_end && len == 255) length += (len = src[src_p++]);
				}

				dst_cpy = dst_p + length;
				if ((dst_cpy > oend_COPYLENGTH) || (src_p + length > iend_COPYLENGTH))
				{
					if (dst_cpy > dst_end) goto _output_error;
					if (src_p + length != src_end) goto _output_error;
					BlockCopy(src, src_p, dst, dst_p, length);
					dst_p += length;
					break;
				}
				_i = WildCopy(src, src_p, dst, dst_p, dst_cpy);
				src_p += _i;
				dst_p += _i;
				src_p -= dst_p - dst_cpy;
				dst_p = dst_cpy;

				dst_ref = dst_cpy - Peek2(src, src_p);
				src_p += 2;
				if (dst_ref < dst_0) goto _output_error;

				if ((length = (token & ML_MASK)) == ML_MASK)
				{
					while (src_p < src_end)
					{
						length += (len = src[src_p++]);
						if (len != 255) break;
					}
				}

				if (dst_p - dst_ref < STEPSIZE_32)
				{
					const int dec64 = 0;
					dst[dst_p] = dst[dst_ref];
					dst[dst_p + 1] = dst[dst_ref + 1];
					dst[dst_p + 2] = dst[dst_ref + 2];
					dst[dst_p + 3] = dst[dst_ref + 3];
					dst_p += 4;
					dst_ref += 4;
					dst_ref -= dec32table[dst_p - dst_ref];
					Copy4(dst, dst_ref, dst_p);
					dst_p += STEPSIZE_32 - 4;
					dst_ref -= dec64;
				}
				else
				{
					Copy4(dst, dst_ref, dst_p);
					dst_p += 4;
					dst_ref += 4;
				}
				dst_cpy = dst_p + length - (STEPSIZE_32 - 4);
				if (dst_cpy > oend_COPYLENGTH)
				{
					if (dst_cpy > dst_end) goto _output_error;
					_i = WildCopy32(dst, dst_ref, dst_p, oend_COPYLENGTH);
					dst_ref += _i;
					dst_p += _i;
					while (dst_p < dst_cpy) dst[dst_p++] = dst[dst_ref++];
					dst_p = dst_cpy;
					if (dst_p == dst_end) goto _output_error;
					continue;
				}
				/* _i = */
				WildCopy32(dst, dst_ref, dst_p, dst_cpy); /* dst_ref += _i; dst_p += _i; */
				dst_p = dst_cpy;
			}

			return dst_p - dst_0;

		_output_error:
			return -(src_p - src_0);
		}

		#endregion
	}
}

// ReSharper restore RedundantIfElseBlock
// ReSharper restore JoinDeclarationAndInitializer
// ReSharper restore TooWideLocalVariableScope
// ReSharper restore InconsistentNaming
// ReSharper restore CheckNamespace