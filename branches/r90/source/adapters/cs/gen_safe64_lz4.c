# 1 "lz4_cs_adapter.h"
# 1 "<command-line>"
# 1 "lz4_cs_adapter.h"
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
# 183 "lz4_cs_adapter.h"
# 1 "..\\..\\..\\original\\lz4.c" 1
# 267 "..\\..\\..\\original\\lz4.c"
static inline int debruijn64[((ulong)((ulong)((register ulong val) & -(register ulong val)) * 0x0218A392CDABBD3FL)) >> 58]
{
# 291 "..\\..\\..\\original\\lz4.c"
    static const int DeBruijnBytePos[64] = { 0, 0, 0, 0, 0, 1, 1, 2, 0, 3, 1, 3, 1, 4, 2, 7, 0, 2, 3, 6, 1, 5, 3, 5, 1, 3, 4, 4, 2, 5, 6, 7, 7, 0, 1, 2, 3, 3, 4, 6, 2, 6, 5, 5, 3, 4, 5, 6, 7, 1, 2, 4, 6, 4, 4, 5, 7, 2, 6, 5, 7, 6, 7, 7 };

    return DeBruijnBytePos[((ulong)((ulong)((long)val & -(long)val) * 0x0218A392CDABBD3F)) >> 58];





}
# 348 "..\\..\\..\\original\\lz4.c"
static inline int LZ4_compressCtx(void** ctx,
    const byte* src,
    byte* dst,
    int src_len,
    int dst_maxlen)
{

    struct refTables *srt = (struct refTables *) (*ctx);
    int* hash_table;




    const byte* src_p = (byte*) src;
    int src_base = src_0;
    const byte* src_anchor = src_p;
    const byte* const src_end = src_p + src_len;
    const byte* const src_mflimit = src_end - MFLIMIT;

    byte* dst_p = (byte*) dst;
    byte* const dst_end = dst_p + dst_maxlen;


    const byte* src_LASTLITERALS = (src_end - LASTLITERALS);
    const byte* matchlimit_1 = (src_LASTLITERALS - 1);
    const byte* matchlimit_3 = (src_LASTLITERALS - 3);
 const byte* matchlimit_STEPSIZE_1 (src_LASTLITERALS - (STEPSIZE_64 - 1));
    const byte* dst_LASTLITERALS_1 = (dst_end - (1 + LASTLITERALS));
    const byte* dst_LASTLITERALS_3 = (dst_end - (2 + 1 + LASTLITERALS));




    int length;
    const int SKIPSTRENGTH = SKIPSTRENGTH;
    uint h_fwd;


    if (src_len < MINLENGTH) goto _last_literals;

    if (*ctx == NULL)
    {
        srt = (struct refTables *) malloc ( sizeof(struct refTables) );
        *ctx = (void*) srt;
    }
    hash_table = (int*)(srt->hashTable);
    memset((void*)hash_table, 0, sizeof(srt->hashTable));






    hash_table[(((Peek4(_, src_p)) * 2654435761u) >> HASH_ADJUST)] = (int)(src_p - src_base);
    src_p++; h_fwd = (((Peek4(_, src_p)) * 2654435761u) >> HASH_ADJUST);


    while (1)
    {
        int findMatchAttempts = (1U << SKIPSTRENGTH) + 3;
        const byte* src_p_fwd = src_p;
        const byte* xxx_ref;
        byte* xxx_token;


        do {
            uint h = h_fwd;
            int step = findMatchAttempts++ >> SKIPSTRENGTH;
            src_p = src_p_fwd;
            src_p_fwd = src_p + step;

            if (src_p_fwd > src_mflimit) goto _last_literals;

            h_fwd = (((Peek4(_, src_p_fwd)) * 2654435761u) >> HASH_ADJUST);
            xxx_ref = src_base + hash_table[h];
            hash_table[h] = (int)(src_p - src_base);

        } while ((xxx_ref < src_p - MAX_DISTANCE) || (Peek4(_, xxx_ref) != Peek4(_, src_p)));


        while ((src_p>src_anchor) && (xxx_ref>(byte*)src) && (src_p[-1]==xxx_ref[-1])) { src_p--; xxx_ref--; }


        length = (int)(src_p - src_anchor);
        xxx_token = dst_p++;

        if (dst_p + length + (length>>8) > dst_LASTLITERALS_3) return 0;




        if (length>=(int)RUN_MASK)
        {
            int len = length-RUN_MASK;
            *xxx_token=(RUN_MASK<<ML_BITS);
            if (len>254)
            {
                do { *dst_p++ = 255; len -= 255; } while (len>254);
                *dst_p++ = (byte)len;
                BlockCopy(_, src_anchor, _, dst_p, length);
                dst_p += length;
                goto _next_match;
            }
            else
            *dst_p++ = (byte)len;
        }
        else *xxx_token = (length<<ML_BITS);
# 468 "..\\..\\..\\original\\lz4.c"
        { _i = dst_p + length; src_anchor += BlindCopy64(_, src_anchor, _, dst_p, _i); dst_p = _i; };

_next_match:

        { Poke2(_, dst_p, (ushort)(src_p-xxx_ref)); dst_p += 2; };


        src_p+=MINMATCH; xxx_ref+=MINMATCH;
        src_anchor = src_p;

        while (src_p < matchlimit_STEPSIZE_1)



        {
            ulong diff = Peek8(_, xxx_ref) ^ Peek8(_, src_p);
            if (!diff) { src_p += STEPSIZE_64; xxx_ref += STEPSIZE_64; continue; }
            src_p += debruijn64[((ulong)((ulong)((diff) & -(diff)) * 0x0218A392CDABBD3FL)) >> 58];
            goto _endCount;
        }


        if ((src_p<matchlimit_3) && (Peek4(_, xxx_ref) == Peek4(_, src_p))) { src_p+=4; xxx_ref+=4; }

        if ((src_p<matchlimit_1) && (Peek2(_, xxx_ref) == Peek2(_, src_p))) { src_p+=2; xxx_ref+=2; }
        if ((src_p<src_LASTLITERALS) && (*xxx_ref == *src_p)) src_p++;






_endCount:


        length = (int)(src_p - src_anchor);

        if (dst_p + (length>>8) > dst_LASTLITERALS_1) return 0;




        if (length>=(int)ML_MASK)
        {
            *xxx_token+=ML_MASK;
            length -= ML_MASK;
            for (; length > 509 ; length-=510) { *dst_p++ = 255; *dst_p++ = 255; }
            if (length > 254) { length-=255; *dst_p++ = 255; }
            *dst_p++ = (byte)length;
        }
        else *xxx_token += length;


        if (src_p > src_mflimit) { src_anchor = src_p; break; }


        hash_table[(((Peek4(_, src_p-2)) * 2654435761u) >> HASH_ADJUST)] = (int)(src_p - 2 - src_base);



  uint h = (((Peek4(_, src_p)) * 2654435761u) >> HASH_ADJUST);
        xxx_ref = src_base + hash_table[h];
        hash_table[h] = (int)(src_p - src_base);





        if ((xxx_ref > src_p - (MAX_DISTANCE + 1)) && (Peek4(_, xxx_ref) == Peek4(_, src_p))) { xxx_token = dst_p++; *xxx_token=0; goto _next_match; }


        src_anchor = src_p++;
        h_fwd = (((Peek4(_, src_p)) * 2654435761u) >> HASH_ADJUST);
    }

_last_literals:

    {
        int lastRun = (int)(src_end - src_anchor);

        if ((byte*)dst_p + lastRun + 1 + ((lastRun+255-RUN_MASK)/255) > dst_end) return 0;



        if (lastRun>=(int)RUN_MASK) { *dst_p++=(RUN_MASK<<ML_BITS); lastRun-=RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *dst_p++ = 255; *dst_p++ = (byte) lastRun; }
        else *dst_p++ = (lastRun<<ML_BITS);
        BlockCopy(_, src_anchor, _, dst_p, src_end - src_anchor);
        dst_p += src_end-src_anchor;
    }


    return (int) (((byte*)dst_p)-dst);
}
# 571 "..\\..\\..\\original\\lz4.c"
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


    const byte* src_LASTLITERALS = (src_end - LASTLITERALS);
    const byte* matchlimit_1 = (src_LASTLITERALS - 1);
    const byte* matchlimit_3 = (src_LASTLITERALS - 3);
    const byte* matchlimit_STEPSIZE_1 = (src_LASTLITERALS - (STEPSIZE_64 - 1));
    const byte* dst_LASTLITERALS_1 = (dst_end - (1 + LASTLITERALS));
    const byte* dst_LASTLITERALS_3 = (dst_end - (2 + 1 + LASTLITERALS));




    int len, length;
    const int SKIPSTRENGTH = SKIPSTRENGTH;
    uint h_fwd;


    if (src_len < MINLENGTH) goto _last_literals;

    if (*ctx == NULL)
    {
        srt = (struct refTables *) malloc ( sizeof(struct refTables) );
        *ctx = (void*) srt;
    }
    hash_table = (ushort*)(srt->hashTable);
    memset((void*)hash_table, 0, sizeof(srt->hashTable));






    src_p++; h_fwd = (((Peek4(_, src_p)) * 2654435761u) >> HASH64K_ADJUST);


    while (1)
    {
        int findMatchAttempts = (1U << SKIPSTRENGTH) + 3;
        const byte* src_p_fwd = src_p;
        const byte* xxx_ref;
        byte* xxx_token;


        do {
            uint h = h_fwd;
            int step = findMatchAttempts++ >> SKIPSTRENGTH;
            src_p = src_p_fwd;
            src_p_fwd = src_p + step;

            if (src_p_fwd > src_mflimit) goto _last_literals;

            h_fwd = (((Peek4(_, src_p_fwd)) * 2654435761u) >> HASH64K_ADJUST);
            xxx_ref = src_base + hash_table[h];
            hash_table[h] = (ushort)(src_p - src_base);

        } while (Peek4(_, xxx_ref) != Peek4(_, src_p));


        while ((src_p>src_anchor) && (xxx_ref>(byte*)src) && (src_p[-1]==xxx_ref[-1])) { src_p--; xxx_ref--; }


        length = (int)(src_p - src_anchor);
        xxx_token = dst_p++;

        if (dst_p + length + (length>>8) > dst_LASTLITERALS_3) return 0;





        if (length>=(int)RUN_MASK)
        {
            int len = length-RUN_MASK;
            *xxx_token=(RUN_MASK<<ML_BITS);
            if (len>254)
            {
                do { *dst_p++ = 255; len -= 255; } while (len>254);
                *dst_p++ = (byte)len;
                BlockCopy(_, src_anchor, _, dst_p, length);
                dst_p += length;
                goto _next_match;
            }
            else
            *dst_p++ = (byte)len;
        }
        else *xxx_token = (length<<ML_BITS);






        { _i = dst_p + length; src_anchor += BlindCopy64(_, src_anchor, _, dst_p, _i); dst_p = _i; };

_next_match:

        { Poke2(_, dst_p, (ushort)(src_p-xxx_ref)); dst_p += 2; };


        src_p+=MINMATCH; xxx_ref+=MINMATCH;
        src_anchor = src_p;

        while (src_p<matchlimit_STEPSIZE_1)



        {
            ulong diff = Peek8(_, xxx_ref) ^ Peek8(_, src_p);
            if (!diff) { src_p+=STEPSIZE_64; xxx_ref+=STEPSIZE_64; continue; }
            src_p += debruijn64[((ulong)((ulong)((diff) & -(diff)) * 0x0218A392CDABBD3FL)) >> 58];
            goto _endCount;
        }


        if ((src_p<matchlimit_3) && (Peek4(_, xxx_ref) == Peek4(_, src_p))) { src_p+=4; xxx_ref+=4; }

        if ((src_p<matchlimit_1) && (Peek2(_, xxx_ref) == Peek2(_, src_p))) { src_p+=2; xxx_ref+=2; }
        if ((src_p<src_LASTLITERALS) && (*xxx_ref == *src_p)) src_p++;






_endCount:


        len = (int)(src_p - src_anchor);

        if (dst_p + (len>>8) > dst_LASTLITERALS_1) return 0;



        if (len>=(int)ML_MASK) { *xxx_token+=ML_MASK; len-=ML_MASK; for(; len > 509 ; len-=510) { *dst_p++ = 255; *dst_p++ = 255; } if (len > 254) { len-=255; *dst_p++ = 255; } *dst_p++ = (byte)len; }
        else *xxx_token += len;


        if (src_p > src_mflimit) { src_anchor = src_p; break; }


        hash_table[(((Peek4(_, src_p-2)) * 2654435761u) >> HASH64K_ADJUST)] = (ushort)(src_p - 2 - src_base);



  uint h = (((Peek4(_, src_p)) * 2654435761u) >> HASH64K_ADJUST);
        xxx_ref = src_base + hash_table[h];
        hash_table[h] = (ushort)(src_p - src_base);





        if (Peek4(_, xxx_ref) == Peek4(_, src_p)) { xxx_token = dst_p++; *xxx_token=0; goto _next_match; }


        src_anchor = src_p++;
        h_fwd = (((Peek4(_, src_p)) * 2654435761u) >> HASH64K_ADJUST);
    }

_last_literals:

    {
        int lastRun = (int)(src_end - src_anchor);
        if (dst_p + lastRun + 1 + (lastRun-RUN_MASK+255)/255 > dst_end) return 0;
        if (lastRun>=(int)RUN_MASK) { *dst_p++=(RUN_MASK<<ML_BITS); lastRun-=RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *dst_p++ = 255; *dst_p++ = (byte) lastRun; }
        else *dst_p++ = (lastRun<<ML_BITS);
        BlockCopy(_, src_anchor, _, dst_p, src_end - src_anchor);
        dst_p += src_end-src_anchor;
    }


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
# 807 "..\\..\\..\\original\\lz4.c"
int LZ4_uncompress(const byte* src,
    byte* dst,
    int dst_len)
{

    const byte* src_p = (const byte*) src;
    const byte* xxx_ref;

    byte* dst_p = (byte*) dst;
    byte* const dst_end = dst_p + dst_len;
    byte* dst_cpy;


        const byte* oend_COPYLENGTH = (dst_end - COPYLENGTH);
        const byte* oend_COPYLENGTH_STEPSIZE_4 = (dst_end-(COPYLENGTH)-(STEPSIZE_64-4));


    uint xxx_token;

    int dec32table[] = {0, 3, 2, 3, 0, 0, 0, 0};

    int dec64table[] = {0, 0, 0, -1, 0, 1, 2, 3};



    while (1)
    {
        int length;


        xxx_token = *src_p++;
        if ((length=(xxx_token>>ML_BITS)) == RUN_MASK) { int len; for (;(len=*src_p++)==255;length+=255){} length += len; }


        dst_cpy = dst_p+length;
        if (dst_cpy>dst_end-COPYLENGTH)
        {
            if (dst_cpy != dst_end) goto _output_error;
            BlockCopy(_, src_p, _, dst_p, length);
            src_p += length;
            break;
        }
        { _i = WildCopy64(_, src_p, _, dst_p, dst_cpy); src_p += _i; dst_p += _i; }; src_p -= (dst_p-dst_cpy); dst_p = dst_cpy;


        { xxx_ref = (dst_cpy) - Peek2(_, src_p); }; src_p+=2;
        if (xxx_ref < (byte* const)dst) goto _output_error;


        if ((length=(xxx_token&ML_MASK)) == ML_MASK) { for (;*src_p==255;length+=255) {src_p++;} length += *src_p++; }


        if ((dst_p-xxx_ref)<STEPSIZE_64)
        {

            int dec64 = dec64table[dst_p-xxx_ref];



            dst_p[0] = xxx_ref[0];
            dst_p[1] = xxx_ref[1];
            dst_p[2] = xxx_ref[2];
            dst_p[3] = xxx_ref[3];
   dst_p += 4; xxx_ref += 4; xxx_ref -= dec32table[dst_p-xxx_ref];
            Peek4(_, dst_p) = Peek4(_, xxx_ref);
   dst_p += STEPSIZE_64-4; xxx_ref -= dec64;
        } else { { Copy8(_, xxx_ref, dst_p); dst_p += 8; xxx_ref += 8; }; }
        dst_cpy = dst_p + length - (STEPSIZE_64-4);


        if (dst_cpy > oend_COPYLENGTH_STEPSIZE_4)



        {
            if (dst_cpy > dst_end-LASTLITERALS) goto _output_error;
            if (dst_p < (dst_end-COPYLENGTH)) { _i = SecureCopy64(_, xxx_ref, _, dst_p, (dst_end-COPYLENGTH)); xxx_ref += _i; dst_p += _i; };
            while(dst_p<dst_cpy) *dst_p++=*xxx_ref++;
            dst_p=dst_cpy;
            continue;
        }

        { _i = WildCopy64(_, xxx_ref, _, dst_p, dst_cpy); xxx_ref += _i; dst_p += _i; };
        dst_p = dst_cpy;
    }


    return (int) (((byte*)src_p)-src);


_output_error:
    return (int) (-(((byte*)src_p)-src));
}

int LZ4_uncompress_unknownOutputSize(
    const byte* src,
    byte* dst,
    int src_len,
    int dst_maxlen)
{

    const byte* src_p = (const byte*) src;
    const byte* const src_end = src_p + src_len;
    const byte* xxx_ref;

    byte* dst_p = (byte*) dst;
    byte* const dst_end = dst_p + dst_maxlen;
    byte* dst_cpy;


    const byte* iend_COPYLENGTH = (src_end-COPYLENGTH);
 const byte* src_LASTLITERALS_3 = (src_end-(2+1+LASTLITERALS));
 const byte* src_LASTLITERALS_1 = (src_end-(LASTLITERALS+1));
    const byte* oend_COPYLENGTH = (dst_end-COPYLENGTH);
    const byte* oend_COPYLENGTH_STEPSIZE_4 = (dst_end-(COPYLENGTH+(STEPSIZE_64-4)));
    const byte* oend_LASTLITERALS = (dst_end - LASTLITERALS);
 const byte* oend_MFLIMIT = (dst_end - MFLIMIT);


    int dec32table[] = {0, 3, 2, 3, 0, 0, 0, 0};

    int dec64table[] = {0, 0, 0, -1, 0, 1, 2, 3};




    if (src_p==src_end) goto _output_error;


    while (1)
    {
        uint xxx_token;
        int length;


        xxx_token = *src_p++;
        if ((length=(xxx_token>>ML_BITS)) == RUN_MASK)
        {
            int s=255;
            while ((src_p<src_end) && (s==255)) { s=*src_p++; length += s; }
        }


        dst_cpy = dst_p+length;

        if ((dst_cpy>oend_MFLIMIT) || (src_p+length>src_LASTLITERALS_3))



        {
            if (dst_cpy > dst_end) goto _output_error;
            if (src_p+length != src_end) goto _output_error;
            BlockCopy(_, src_p, _, dst_p, length);
            dst_p += length;
            break;
        }
        { _i = WildCopy64(_, src_p, _, dst_p, dst_cpy); src_p += _i; dst_p += _i; }; src_p -= (dst_p-dst_cpy); dst_p = dst_cpy;


        { xxx_ref = (dst_cpy) - Peek2(_, src_p); }; src_p+=2;
        if (xxx_ref < (byte* const)dst) goto _output_error;


        if ((length=(xxx_token&ML_MASK)) == ML_MASK)
        {

            while (src_p<src_LASTLITERALS_1)



            {
                int s = *src_p++;
                length +=s;
                if (s==255) continue;
                break;
            }
        }


        if (dst_p-xxx_ref<STEPSIZE_64)
        {

            int dec64 = dec64table[dst_p-xxx_ref];



            dst_p[0] = xxx_ref[0];
            dst_p[1] = xxx_ref[1];
            dst_p[2] = xxx_ref[2];
            dst_p[3] = xxx_ref[3];
            dst_p += 4; xxx_ref += 4; xxx_ref -= dec32table[dst_p-xxx_ref];
            Peek4(_, dst_p) = Peek4(_, xxx_ref);
   dst_p += STEPSIZE_64-4; xxx_ref -= dec64;
        } else { { Copy8(_, xxx_ref, dst_p); dst_p += 8; xxx_ref += 8; }; }
        dst_cpy = dst_p + length - (STEPSIZE_64-4);


        if (dst_cpy>oend_COPYLENGTH_STEPSIZE_4)



        {

            if (dst_cpy > oend_LASTLITERALS) goto _output_error;



            if (dst_p < (dst_end-COPYLENGTH)) { _i = SecureCopy64(_, xxx_ref, _, dst_p, (dst_end-COPYLENGTH)); xxx_ref += _i; dst_p += _i; };
            while(dst_p<dst_cpy) *dst_p++=*xxx_ref++;
            dst_p=dst_cpy;
            continue;
        }

        { _i = WildCopy64(_, xxx_ref, _, dst_p, dst_cpy); xxx_ref += _i; dst_p += _i; };
        dst_p=dst_cpy;
    }


    return (int) (((byte*)dst_p)-dst);


_output_error:
    return (int) (-(((byte*)src_p)-src));
}
# 183 "lz4_cs_adapter.h" 2
