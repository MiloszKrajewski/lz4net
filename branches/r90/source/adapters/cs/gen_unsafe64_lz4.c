# 1 "lz4_cs_adapter.h"
# 1 "<command-line>"
# 1 "lz4_cs_adapter.h"
// externaly defined:
//   - GEN_SAFE: generate safe code
//   - GEN_X64: generate 64-bit version
# 17 "lz4_cs_adapter.h"
private const int MINMATCH = 4;
private const int HASH_MASK = HASHTABLESIZE - 1;
private const int SKIPSTRENGTH = NOTCOMPRESSIBLE_DETECTIONLEVEL > 2 ? NOTCOMPRESSIBLE_DETECTIONLEVEL : 2;
private const int STACKLIMIT = 13;
private const int COPYLENGTH = 8;
private const int LASTLITERALS = 5;
private const int MFLIMIT = COPYLENGTH + MINMATCH;
private const int MINLENGTH = MFLIMIT + 1;
private const int MAXD_LOG = 16;
private const int MAX_DISTANCE = (1 << MAXD_LOG) - 1;
private const int ML_BITS = 4;
private const int ML_MASK = (1u << ML_BITS) - 1;
private const int RUN_BITS = 8 - ML_BITS;
private const int RUN_MASK = (1u << RUN_BITS) - 1;
private const int STEPSIZE_64 = 8;
private const int STEPSIZE_32 = 4;

private const int LZ4_64KLIMIT = (1 << 16) + (MFLIMIT - 1);
private const int HASH_LOG = MEMORY_USAGE - 2;
private const int HASH_TABLESIZE = 1 << HASH_LOG;
private const int HASH_ADJUST = (MINMATCH * 8) - HASH_LOG;

private const int HASH64K_LOG = HASH_LOG + 1;
private const int HASH64K_TABLESIZE = 1 << HASH64K_LOG;
private const int HASH64K_ADJUST = (MINMATCH * 8) - HASH64K_LOG;

private static readonly int[] DECODER_TABLE_32 = new int[] { 0, 3, 2, 3, 0, 0, 0, 0 };
private static readonly int[] DECODER_TABLE_64 = new int[] { 0, 0, 0, -1, 0, 1, 2, 3 };

private static readonly int[] DEBRUIJN_TABLE_32 = new int[] {
    0, 0, 3, 0, 3, 1, 3, 0, 3, 2, 2, 1, 3, 2, 0, 1,
    3, 3, 1, 2, 2, 2, 2, 0, 3, 1, 2, 0, 1, 0, 1, 1
};

private static readonly int[] DEBRUIJN_TABLE_64 = new int[] {
    0, 0, 0, 0, 0, 1, 1, 2, 0, 3, 1, 3, 1, 4, 2, 7,
    0, 2, 3, 6, 1, 5, 3, 5, 1, 3, 4, 4, 2, 5, 6, 7,
    7, 0, 1, 2, 3, 3, 4, 6, 2, 6, 5, 5, 3, 4, 5, 6,
    7, 1, 2, 4, 6, 4, 4, 5, 7, 2, 6, 5, 7, 6, 7, 7
};
# 192 "lz4_cs_adapter.h"
// GOGOGO
# 1 "..\\..\\..\\original\\lz4.c" 1
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
# 260 "..\\..\\..\\original\\lz4.c"
//****************************
// Private functions
//****************************
# 340 "..\\..\\..\\original\\lz4.c"
//******************************
// Compression functions
//******************************

// LZ4_compressCtx :
// -----------------
// Compress 'isize' bytes from 'source' into an output buffer 'dest' of maximum size 'maxOutputSize'.
// If it cannot achieve it, compression will stop, and result of the function will be zero.
// return : the number of bytes written in buffer 'dest', or 0 if the compression fails

static inline int LZ4_compressCtx(void** ctx,
    const byte* src,
    byte* dst,
    int src_len,
    int dst_maxlen)
{

    struct refTables *srt = (struct refTables *) (*ctx);
    uint* hash_table;




    const byte* src_p = (byte*) src;
    byte* src_base = src_p;
    const byte* src_anchor = src_p;
    const byte* const src_end = src_p + src_len;
    const byte* const src_mflimit = src_end - MFLIMIT;

    byte* dst_p = (byte*) dst;
    byte* const dst_end = dst_p + dst_maxlen;


    const byte* src_LASTLITERALS = src_end - LASTLITERALS;
    const byte* src_LASTLITERALS_1 = src_LASTLITERALS - 1;
    const byte* src_LASTLITERALS_3 = src_LASTLITERALS - 3;
    const byte* src_LASTLITERALS_STEPSIZE_1 = src_LASTLITERALS - (STEPSIZE_64 - 1);
    const byte* dst_LASTLITERALS_1 = dst_end - (1 + LASTLITERALS);
    const byte* dst_LASTLITERALS_3 = dst_end - (2 + 1 + LASTLITERALS);




    int length;



    uint h_fwd;

    // Init
    if (src_len < MINLENGTH) goto _last_literals;
# 405 "..\\..\\..\\original\\lz4.c"
    // First Byte
    hash_table[((((*(uint*)(src_p))) * 2654435761u) >> HASH_ADJUST)] = (uint)(src_p - src_base);
    src_p++; h_fwd = ((((*(uint*)(src_p))) * 2654435761u) >> HASH_ADJUST);

    // Main Loop
    while (1)
    {
        int findMatchAttempts = (1U << SKIPSTRENGTH) + 3;
        const byte* src_p_fwd = src_p;
        const byte* xxx_ref;
        byte* xxx_token;

        // Find a match
        do {
            uint h = h_fwd;
            int step = findMatchAttempts++ >> SKIPSTRENGTH;
            src_p = src_p_fwd;
            src_p_fwd = src_p + step;

            if (src_p_fwd > src_mflimit) goto _last_literals;

            h_fwd = ((((*(uint*)(src_p_fwd))) * 2654435761u) >> HASH_ADJUST);
            xxx_ref = src_base + hash_table[h];
            hash_table[h] = (uint)(src_p - src_base);

        } while ((xxx_ref < src_p - MAX_DISTANCE) || ((*(uint*)(xxx_ref)) != (*(uint*)(src_p))));

        // Catch up
        while ((src_p>src_anchor) && (xxx_ref>(byte*)src) && (src_p[-1]==xxx_ref[-1])) { src_p--; xxx_ref--; }

        // Encode Literal length
        length = (int)(src_p - src_anchor);
        xxx_token = dst_p++;

        if (dst_p + length + (length>>8) > dst_LASTLITERALS_3) return 0; // Check output limit




        if (length>=(int)RUN_MASK)
        {
            int len = length-RUN_MASK;
            *xxx_token=(RUN_MASK<<ML_BITS);
            if (len>254)
            {
                do { *dst_p++ = 255; len -= 255; } while (len>254);
                *dst_p++ = (byte)len;
                BlockCopy(src_anchor, dst_p, (int)(length));
                dst_p += length;
                goto _next_match;
            }
            else
            *dst_p++ = (byte)len;
        }
        else *xxx_token = (length<<ML_BITS);
# 472 "..\\..\\..\\original\\lz4.c"
        // Copy Literals
        { _p = dst_p + (length); { do { *(ulong*)dst_p = *(ulong*)src_anchor; dst_p += 8; src_anchor += 8; } while (dst_p < e); }; dst_p = e; };

_next_match:
        // Encode Offset
        { *(ushort)dst_p = (ushort)(src_p-xxx_ref); dst_p += 2; };

        // Start Counting
        src_p+=MINMATCH; xxx_ref+=MINMATCH; // MinMatch already verified
        src_anchor = src_p;

        while (src_p < src_LASTLITERALS_STEPSIZE_1)



        {
            ulong diff = (*(ulong*)(xxx_ref)) ^ (*(ulong*)(src_p));
            if (!diff) { src_p += STEPSIZE_64; xxx_ref += STEPSIZE_64; continue; }
            src_p += debruijn64[((ulong)((ulong)((diff) & -(diff)) * 0x0218A392CDABBD3FL)) >> 58];
            goto _endCount;
        }


        if ((src_p<src_LASTLITERALS_3) && ((*(uint*)(xxx_ref)) == (*(uint*)(src_p)))) { src_p+=4; xxx_ref+=4; }

        if ((src_p<src_LASTLITERALS_1) && ((*(ushort*)(xxx_ref)) == (*(ushort*)(src_p)))) { src_p+=2; xxx_ref+=2; }
        if ((src_p<src_LASTLITERALS) && (*xxx_ref == *src_p)) src_p++;






_endCount:

        // Encode MatchLength
        length = (int)(src_p - src_anchor);

        if (dst_p + (length>>8) > dst_LASTLITERALS_1) return 0; // Check output limit




        if (length>=(int)ML_MASK)
        {
            *xxx_token+=ML_MASK;
            length -= ML_MASK;
            for (; length > 509 ; length-=510) { *dst_p++ = 255; *dst_p++ = 255; }
            if (length > 254) { length-=255; *dst_p++ = 255; }
            *dst_p++ = (byte)length;
        }
        else *xxx_token += length;

        // Test end of chunk
        if (src_p > src_mflimit) { src_anchor = src_p; break; }

        // Fill table
        hash_table[((((*(uint*)(src_p-2))) * 2654435761u) >> HASH_ADJUST)] = (uint)(src_p - 2 - src_base);

        // Test next position

        uint h = ((((*(uint*)(src_p))) * 2654435761u) >> HASH_ADJUST);
        xxx_ref = src_base + hash_table[h];
        hash_table[h] = (uint)(src_p - src_base);





        if ((xxx_ref > src_p - (MAX_DISTANCE + 1)) && ((*(uint*)(xxx_ref)) == (*(uint*)(src_p)))) { xxx_token = dst_p++; *xxx_token=0; goto _next_match; }

        // Prepare next loop
        src_anchor = src_p++;
        h_fwd = ((((*(uint*)(src_p))) * 2654435761u) >> HASH_ADJUST);
    }

_last_literals:
    // Encode Last Literals
    {
        int lastRun = (int)(src_end - src_anchor);

        if ((byte*)dst_p + lastRun + 1 + ((lastRun+255-RUN_MASK)/255) > dst_end) return 0;



        if (lastRun>=(int)RUN_MASK) { *dst_p++=(RUN_MASK<<ML_BITS); lastRun-=RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *dst_p++ = 255; *dst_p++ = (byte) lastRun; }
        else *dst_p++ = (lastRun<<ML_BITS);
        BlockCopy(src_anchor, dst_p, (int)(src_end - src_anchor));
        dst_p += src_end-src_anchor;
    }

    // End
    return (int) (((byte*)dst_p)-dst);
}

// Note : this function is valid only if isize < LZ4_64KLIMIT
# 576 "..\\..\\..\\original\\lz4.c"
static inline int LZ4_compress64kCtx(void** ctx,
                 const byte* src,
                 byte* dst,
                 int src_len,
                 int dst_maxlen)
{

    struct refTables *srt = (struct refTables *) (*ctx);
    ushort* hash_table;




    const byte* src_p = (byte*) src;
    const byte* src_anchor = src_p;
    const byte* const src_base = src_p;
    const byte* const src_end = src_p + src_len;
    const byte* const src_mflimit = src_end - MFLIMIT;

    byte* dst_p = (byte*) dst;
    byte* const dst_end = dst_p + dst_maxlen;


    const byte* src_LASTLITERALS = src_end - LASTLITERALS;
    const byte* src_LASTLITERALS_1 = src_LASTLITERALS - 1;
    const byte* src_LASTLITERALS_3 = src_LASTLITERALS - 3;
    const byte* src_LASTLITERALS_STEPSIZE_1 = src_LASTLITERALS - (STEPSIZE_64 - 1);
    const byte* dst_LASTLITERALS_1 = dst_end - (1 + LASTLITERALS);
    const byte* dst_LASTLITERALS_3 = dst_end - (2 + 1 + LASTLITERALS);




    int len, length;



    uint h_fwd;

    // Init
    if (src_len < MINLENGTH) goto _last_literals;
# 631 "..\\..\\..\\original\\lz4.c"
    // First Byte
    src_p++; h_fwd = ((((*(uint*)(src_p))) * 2654435761u) >> HASH64K_ADJUST);

    // Main Loop
    while (1)
    {
        int findMatchAttempts = (1U << SKIPSTRENGTH) + 3;
        const byte* src_p_fwd = src_p;
        const byte* xxx_ref;
        byte* xxx_token;

        // Find a match
        do {
            uint h = h_fwd;
            int step = findMatchAttempts++ >> SKIPSTRENGTH;
            src_p = src_p_fwd;
            src_p_fwd = src_p + step;

            if (src_p_fwd > src_mflimit) goto _last_literals;

            h_fwd = ((((*(uint*)(src_p_fwd))) * 2654435761u) >> HASH64K_ADJUST);
            xxx_ref = src_base + hash_table[h];
            hash_table[h] = (ushort)(src_p - src_base);

        } while ((*(uint*)(xxx_ref)) != (*(uint*)(src_p)));

        // Catch up
        while ((src_p>src_anchor) && (xxx_ref>(byte*)src) && (src_p[-1]==xxx_ref[-1])) { src_p--; xxx_ref--; }

        // Encode Literal length
        length = (int)(src_p - src_anchor);
        xxx_token = dst_p++;

        if (dst_p + length + (length>>8) > dst_LASTLITERALS_3) return 0; // Check output limit





        if (length>=(int)RUN_MASK)
        {
            int len = length-RUN_MASK;
            *xxx_token=(RUN_MASK<<ML_BITS);
            if (len>254)
            {
                do { *dst_p++ = 255; len -= 255; } while (len>254);
                *dst_p++ = (byte)len;
                BlockCopy(src_anchor, dst_p, (int)(length));
                dst_p += length;
                goto _next_match;
            }
            else
            *dst_p++ = (byte)len;
        }
        else *xxx_token = (length<<ML_BITS);





        // Copy Literals
        { _p = dst_p + (length); { do { *(ulong*)dst_p = *(ulong*)src_anchor; dst_p += 8; src_anchor += 8; } while (dst_p < e); }; dst_p = e; };

_next_match:
        // Encode Offset
        { *(ushort)dst_p = (ushort)(src_p-xxx_ref); dst_p += 2; };

        // Start Counting
        src_p+=MINMATCH; xxx_ref+=MINMATCH; // MinMatch verified
        src_anchor = src_p;

        while (src_p<src_LASTLITERALS_STEPSIZE_1)



        {
            ulong diff = (*(ulong*)(xxx_ref)) ^ (*(ulong*)(src_p));
            if (!diff) { src_p+=STEPSIZE_64; xxx_ref+=STEPSIZE_64; continue; }
            src_p += debruijn64[((ulong)((ulong)((diff) & -(diff)) * 0x0218A392CDABBD3FL)) >> 58];
            goto _endCount;
        }


        if ((src_p<src_LASTLITERALS_3) && ((*(uint*)(xxx_ref)) == (*(uint*)(src_p)))) { src_p+=4; xxx_ref+=4; }

        if ((src_p<src_LASTLITERALS_1) && ((*(ushort*)(xxx_ref)) == (*(ushort*)(src_p)))) { src_p+=2; xxx_ref+=2; }
        if ((src_p<src_LASTLITERALS) && (*xxx_ref == *src_p)) src_p++;






_endCount:

        // Encode MatchLength
        len = (int)(src_p - src_anchor);

        if (dst_p + (len>>8) > dst_LASTLITERALS_1) return 0; // Check output limit



        if (len>=(int)ML_MASK) { *xxx_token+=ML_MASK; len-=ML_MASK; for(; len > 509 ; len-=510) { *dst_p++ = 255; *dst_p++ = 255; } if (len > 254) { len-=255; *dst_p++ = 255; } *dst_p++ = (byte)len; }
        else *xxx_token += len;

        // Test end of chunk
        if (src_p > src_mflimit) { src_anchor = src_p; break; }

        // Fill table
        hash_table[((((*(uint*)(src_p-2))) * 2654435761u) >> HASH64K_ADJUST)] = (ushort)(src_p - 2 - src_base);

        // Test next position

        uint h = ((((*(uint*)(src_p))) * 2654435761u) >> HASH64K_ADJUST);
        xxx_ref = src_base + hash_table[h];
        hash_table[h] = (ushort)(src_p - src_base);





        if ((*(uint*)(xxx_ref)) == (*(uint*)(src_p))) { xxx_token = dst_p++; *xxx_token=0; goto _next_match; }

        // Prepare next loop
        src_anchor = src_p++;
        h_fwd = ((((*(uint*)(src_p))) * 2654435761u) >> HASH64K_ADJUST);
    }

_last_literals:
    // Encode Last Literals
    {
        int lastRun = (int)(src_end - src_anchor);
        if (dst_p + lastRun + 1 + (lastRun-RUN_MASK+255)/255 > dst_end) return 0;
        if (lastRun>=(int)RUN_MASK) { *dst_p++=(RUN_MASK<<ML_BITS); lastRun-=RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *dst_p++ = 255; *dst_p++ = (byte) lastRun; }
        else *dst_p++ = (lastRun<<ML_BITS);
        BlockCopy(src_anchor, dst_p, (int)(src_end - src_anchor));
        dst_p += src_end-src_anchor;
    }

    // End
    return (int) (((byte*)dst_p)-dst);
}


int LZ4_compress_limitedOutput(const byte* src,
    byte* dst,
    int src_len,
    int dst_maxlen)
{

    void* ctx = malloc(sizeof(struct refTables));
    int result;
    if (src_len < LZ4_64KLIMIT)
        result = LZ4_compress64kCtx(&ctx, src, dst, src_len, dst_maxlen);
    else result = LZ4_compressCtx(&ctx, src, dst, src_len, dst_maxlen);
    free(ctx);
    return result;




}

int LZ4_compress(const byte* src,
    byte* dst,
    int src_len)
{
    return LZ4_compress_limitedOutput(src, dst, src_len, LZ4_compressBound(src_len));
}

//****************************
// Decompression functions
//****************************

// Note : The decoding functions LZ4_uncompress() and LZ4_uncompress_unknownOutputSize()
//      are safe against "buffer overflow" attack type.
//      They will never write nor read outside of the provided output buffers.
//      LZ4_uncompress_unknownOutputSize() also insures that it will never read outside of the input buffer.
//      A corrupted input will produce an error result, a negative int, indicating the position of the error within input stream.

int LZ4_uncompress(const byte* src,
    byte* dst,
    int dst_len)
{
    // Local Variables
    const byte* src_p = (const byte*) src;
    const byte* xxx_ref;

    byte* dst_p = (byte*) dst;
    byte* const dst_end = dst_p + dst_len;
    byte* dst_cpy;


  const byte* dst_LASTLITERALS = dst_end - LASTLITERALS;
        const byte* dst_COPYLENGTH = dst_end - COPYLENGTH;
        const byte* dst_COPYLENGTH_STEPSIZE_4 = dst_end - COPYLENGTH - (STEPSIZE_64 - 4);


    uint xxx_token;

    int dec32table[] = {0, 3, 2, 3, 0, 0, 0, 0};

    int dec64table[] = {0, 0, 0, -1, 0, 1, 2, 3};


    // Main Loop
    while (1)
    {
        int length;

        // get runlength
        xxx_token = *src_p++;
        if ((length=(xxx_token>>ML_BITS)) == RUN_MASK) { int len; for (;(len=*src_p++)==255;length+=255) { } length += len; }

        // copy literals
        dst_cpy = dst_p+length;

        if (dst_cpy>dst_COPYLENGTH)



        {
            if (dst_cpy != dst_end) goto _output_error; // Error : not enough place for another match (min 4) + 5 literals
            BlockCopy(src_p, dst_p, (int)(length));
            src_p += length;
            break; // EOF
        }
        { do { *(ulong*)dst_p = *(ulong*)src_p; dst_p += 8; src_p += 8; } while (dst_p < dst_cpy); }; src_p -= (dst_p-dst_cpy); dst_p = dst_cpy;

        // get offset
        { xxx_ref = (dst_cpy) - (*(ushort*)(src_p)); }; src_p+=2;
        if (xxx_ref < (byte* const)dst) goto _output_error; // Error : offset outside destination buffer

        // get matchlength
        if ((length=(xxx_token&ML_MASK)) == ML_MASK) { for (;*src_p==255;length+=255) {src_p++;} length += *src_p++; }

        // copy repeated sequence
        if ((dst_p-xxx_ref)<STEPSIZE_64)
        {

            int dec64 = dec64table[dst_p-xxx_ref];



            dst_p[0] = xxx_ref[0];
            dst_p[1] = xxx_ref[1];
            dst_p[2] = xxx_ref[2];
            dst_p[3] = xxx_ref[3];
            dst_p += 4; xxx_ref += 4; xxx_ref -= dec32table[dst_p-xxx_ref];
            (*(uint*)(dst_p)) = (*(uint*)(xxx_ref));
            dst_p += STEPSIZE_64-4; xxx_ref -= dec64;
        } else { *(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8;; }
        dst_cpy = dst_p + length - (STEPSIZE_64-4);


        if (dst_cpy > dst_COPYLENGTH_STEPSIZE_4)



        {

            if (dst_cpy > dst_LASTLITERALS) goto _output_error; // Error : last 5 bytes must be literals
            if (dst_p < dst_COPYLENGTH) { do { *(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8; } while (dst_p < dst_COPYLENGTH); };




            while(dst_p<dst_cpy) *dst_p++=*xxx_ref++;
            dst_p=dst_cpy;
            continue;
        }

        { do { *(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8; } while (dst_p < dst_cpy); };
        dst_p = dst_cpy; // correction
    }

    // end of decoding
    return (int) (((byte*)src_p)-src);

    // write overflow error detected
_output_error:
    return (int) (-(((byte*)src_p)-src));
}

int LZ4_uncompress_unknownOutputSize(
    const byte* src,
    byte* dst,
    int src_len,
    int dst_maxlen)
{
    // Local Variables
    const byte* src_p = (const byte*) src;
    const byte* const src_end = src_p + src_len;
    const byte* xxx_ref;

    byte* dst_p = (byte*) dst;
    byte* const dst_end = dst_p + dst_maxlen;
    byte* dst_cpy;


    const byte* src_COPYLENGTH = (src_end-COPYLENGTH);
    const byte* src_LASTLITERALS_3 = (src_end-(2+1+LASTLITERALS));
    const byte* src_LASTLITERALS_1 = (src_end-(LASTLITERALS+1));
    const byte* dst_COPYLENGTH = (dst_end-COPYLENGTH);
    const byte* dst_COPYLENGTH_STEPSIZE_4 = (dst_end-(COPYLENGTH+(STEPSIZE_64-4)));
    const byte* dst_LASTLITERALS = (dst_end - LASTLITERALS);
    const byte* dst_MFLIMIT = (dst_end - MFLIMIT);


    int dec32table[] = {0, 3, 2, 3, 0, 0, 0, 0};

    int dec64table[] = {0, 0, 0, -1, 0, 1, 2, 3};


    // Special case
    if (src_p==src_end) goto _output_error; // A correctly formed null-compressed LZ4 must have at least one byte (token=0)

    // Main Loop
    while (1)
    {
        uint xxx_token;
        int length;

        // get runlength
        xxx_token = *src_p++;
        if ((length=(xxx_token>>ML_BITS)) == RUN_MASK)
        {
            int s=255;
            while ((src_p<src_end) && (s==255)) { s=*src_p++; length += s; }
        }

        // copy literals
        dst_cpy = dst_p+length;

        if ((dst_cpy>dst_MFLIMIT) || (src_p+length>src_LASTLITERALS_3))



        {
            if (dst_cpy > dst_end) goto _output_error; // Error : writes beyond output buffer
            if (src_p+length != src_end) goto _output_error; // Error : LZ4 format requires to consume all input at this stage (no match within the last 11 bytes, and at least 8 remaining input bytes for another match+literals)
            BlockCopy(src_p, dst_p, (int)(length));
            dst_p += length;
            break; // Necessarily EOF, due to parsing restrictions
        }
        { do { *(ulong*)dst_p = *(ulong*)src_p; dst_p += 8; src_p += 8; } while (dst_p < dst_cpy); }; src_p -= (dst_p-dst_cpy); dst_p = dst_cpy;

        // get offset
        { xxx_ref = (dst_cpy) - (*(ushort*)(src_p)); }; src_p+=2;
        if (xxx_ref < (byte* const)dst) goto _output_error; // Error : offset outside of destination buffer

        // get matchlength
        if ((length=(xxx_token&ML_MASK)) == ML_MASK)
        {

            while (src_p<src_LASTLITERALS_1) // Error : a minimum input bytes must remain for LASTLITERALS + token



            {
                int s = *src_p++;
                length +=s;
                if (s==255) continue;
                break;
            }
        }

        // copy repeated sequence
        if (dst_p-xxx_ref<STEPSIZE_64)
        {

            int dec64 = dec64table[dst_p-xxx_ref];



            dst_p[0] = xxx_ref[0];
            dst_p[1] = xxx_ref[1];
            dst_p[2] = xxx_ref[2];
            dst_p[3] = xxx_ref[3];
            dst_p += 4; xxx_ref += 4; xxx_ref -= dec32table[dst_p-xxx_ref];
            (*(uint*)(dst_p)) = (*(uint*)(xxx_ref));
            dst_p += STEPSIZE_64-4; xxx_ref -= dec64;
        } else { *(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8;; }
        dst_cpy = dst_p + length - (STEPSIZE_64-4);


        if (dst_cpy>dst_COPYLENGTH_STEPSIZE_4)



        {

            if (dst_cpy > dst_LASTLITERALS) goto _output_error; // Error : last 5 bytes must be literals
            if (dst_p < dst_COPYLENGTH) { do { *(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8; } while (dst_p < dst_COPYLENGTH); };




            while(dst_p<dst_cpy) *dst_p++=*xxx_ref++;
            dst_p=dst_cpy;
            continue;
        }

        { do { *(ulong*)dst_p = *(ulong*)xxx_ref; dst_p += 8; xxx_ref += 8; } while (dst_p < dst_cpy); };
        dst_p=dst_cpy; // correction
    }

    // end of decoding
    return (int) (((byte*)dst_p)-dst);

    // write overflow error detected
_output_error:
    return (int) (-(((byte*)src_p)-src));
}
# 193 "lz4_cs_adapter.h" 2
