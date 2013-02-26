# 1 "lz4hc_cs_adapter.h"
# 1 "<command-line>"
# 1 "lz4hc_cs_adapter.h"
# 17 "lz4hc_cs_adapter.h"
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
private const int ML_MASK = (1 << ML_BITS) - 1;
private const int RUN_BITS = 8 - ML_BITS;
private const int RUN_MASK = (1 << RUN_BITS) - 1;
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


private const int DICTIONARY_LOGSIZE = 16;
private const int MAXD = 1 << DICTIONARY_LOGSIZE;
private const int MAXD_MASK = MAXD - 1;
private const int MAX_DISTANCE = MAXD - 1;
private const int HASH_LOG_HC = DICTIONARY_LOGSIZE - 1;
private const int HASH_TABLESIZE_HC = 1 << HASH_LOG_HC;
private const int HASH_MASK_HC = HASH_TABLESIZE_HC - 1;
private const int MAX_NB_ATTEMPTS = 256;
private const int OPTIMAL_ML = (ML_MASK - 1) + MINMATCH;
# 203 "lz4hc_cs_adapter.h"
private class LZ4HC_Data_Structure
{
 public byte[] src_base;
 public int hashTable[HASH_TABLESIZE_HC];
 public ushort chainTable[MAXD];
 public int nextToUpdate;
};



# 1 "..\\..\\..\\original\\lz4hc.c" 1
# 311 "..\\..\\..\\original\\lz4hc.c"
inline static int LZ4HC_Init (LZ4HC_Data_Structure* hc4, const byte* src_base)
{
 BlockSet((void*)hc4->hashTable, sizeof(hc4->hashTable), 0);
 BlockSet(hc4->chainTable, sizeof(hc4->chainTable), 0xFF);
 hc4->nextToUpdate = src_base + 1;
 hc4->src_base = src_base;
 return 1;
}


inline static void* LZ4HC_Create (const byte* src_base)
{
 void* hc4 = (new byte[sizeof(LZ4HC_Data_Structure)]);

    LZ4HC_Init ((LZ4HC_Data_Structure*)hc4, src_base);
 return hc4;
}


inline static int LZ4HC_Free (void** LZ4HC_Data)
{
 (*LZ4HC_Data);
 *LZ4HC_Data = NULL;
 return (1);
}



forceinline static void LZ4HC_Insert (LZ4HC_Data_Structure* hc4, const byte* src_p)
{
 ushort* chainTable = hc4->chainTable;
 int* hash_table = hc4->hashTable;
 int src_base = hc4->src_base;

 while(hc4->nextToUpdate < src_p)
 {
        const byte* p = hc4->nextToUpdate;
        int delta = (p) - (hash_table[(((Peek4(_, p)) * 2654435761u) >> HASH_ADJUST)] + src_base);
        if (delta>MAX_DISTANCE) delta = MAX_DISTANCE;
        chainTable[(int)(p) & MAXD_MASK] = (ushort)delta;
        hash_table[HASH_VALUE(p)] = (p) - src_base;
  hc4->nextToUpdate++;
 }
}


forceinline static int LZ4HC_CommonLength (const byte* p1, const byte* p2, const byte* const src_LASTLITERALS)
{
    const byte* p1t = p1;

    while (p1t<src_LASTLITERALS-(STEPSIZE_64-1))
    {
        ulong diff = Peek8(_, p2) ^ Peek8(_, p1t);
        if (!diff) { p1t+=STEPSIZE_64; p2+=STEPSIZE_64; continue; }
        p1t += debruijn64[((ulong)((ulong)((diff) & -(diff)) * 0x0218A392CDABBD3FL)) >> 58];
        return (p1t - p1);
    }
    if (1) if ((p1t<(src_LASTLITERALS-3)) && (Peek4(_, p2) == Peek4(_, p1t))) { p1t+=4; p2+=4; }
    if ((p1t<(src_LASTLITERALS-1)) && (Peek2(_, p2) == Peek2(_, p1t))) { p1t+=2; p2+=2; }
    if ((p1t<src_LASTLITERALS) && (*p2 == *p1t)) p1t++;
    return (p1t - p1);
}


forceinline static int LZ4HC_InsertAndFindBestMatch (LZ4HC_Data_Structure* hc4, const byte* src_p, const byte* const src_LASTLITERALS, const byte** matchpos)
{
 ushort* const chainTable = hc4->chainTable;
 int* const hash_table = hc4->hashTable;
 const byte* xxx_ref;
 int src_base = hc4->src_base;
 int nbAttempts=MAX_NB_ATTEMPTS;
    int ml=0;


 LZ4HC_Insert(hc4, src_p);
 xxx_ref = (hash_table[(((Peek4(_, src_p)) * 2654435761u) >> HASH_ADJUST)] + src_base);


    if (xxx_ref >= src_p-4)
    {
        if (Peek4(_, xxx_ref) == Peek4(_, src_p))
        {
            const ushort delta = (ushort)(src_p-xxx_ref);
            const byte* ptr = src_p;
            const byte* end;
            ml = LZ4HC_CommonLength(src_p+MINMATCH, xxx_ref+MINMATCH, src_LASTLITERALS) + MINMATCH;
            end = src_p + ml - (MINMATCH-1);
            while(ptr < end-delta)
            {
                chainTable[(int)(ptr) & MAXD_MASK] = delta;
                ptr++;
            }
            do
            {
                chainTable[(int)(ptr) & MAXD_MASK] = delta;
                hash_table[HASH_VALUE(ptr)] = (ptr) - src_base;
                ptr++;
            } while(ptr < end);
            hc4->nextToUpdate = end;
            *matchpos = xxx_ref;
        }
        xxx_ref = ((xxx_ref) - (int)chainTable[(int)(xxx_ref) & MAXD_MASK]);
    }


 while ((xxx_ref >= (src_p-MAX_DISTANCE)) && (nbAttempts))
 {
  nbAttempts--;
  if (*(xxx_ref+ml) == *(src_p+ml))
        if (Peek4(_, xxx_ref) == Peek4(_, src_p))
  {
            int mlt = LZ4HC_CommonLength(src_p+MINMATCH, xxx_ref+MINMATCH, src_LASTLITERALS) + MINMATCH;
            if (mlt > ml) { ml = mlt; *matchpos = xxx_ref; }
  }
  xxx_ref = ((xxx_ref) - (int)chainTable[(int)(xxx_ref) & MAXD_MASK]);
 }

    return (int)ml;
}


forceinline static int LZ4HC_InsertAndGetWiderMatch (LZ4HC_Data_Structure* hc4, const byte* src_p, const byte* startLimit, const byte* src_LASTLITERALS, int longest, const byte** matchpos, const byte** startpos)
{
 ushort* const chainTable = hc4->chainTable;
 int* const hash_table = hc4->hashTable;
 int src_base = hc4->src_base;
 const byte* xxx_ref;
 int nbAttempts = MAX_NB_ATTEMPTS;
 int delta = (int)(src_p-startLimit);


 LZ4HC_Insert(hc4, src_p);
 xxx_ref = (hash_table[(((Peek4(_, src_p)) * 2654435761u) >> HASH_ADJUST)] + src_base);

 while ((xxx_ref >= src_p-MAX_DISTANCE) && (xxx_ref >= hc4->src_base) && (nbAttempts))
 {
  nbAttempts--;
  if (*(startLimit + longest) == *(xxx_ref - delta + longest))
        if (Peek4(_, xxx_ref) == Peek4(_, src_p))
  {

   const byte* reft = xxx_ref+MINMATCH;
   const byte* ipt = src_p+MINMATCH;
   const byte* startt = src_p;

   while (ipt<src_LASTLITERALS-(STEPSIZE_64-1))
   {
    ulong diff = Peek8(_, reft) ^ Peek8(_, ipt);
    if (!diff) { ipt+=STEPSIZE_64; reft+=STEPSIZE_64; continue; }
    ipt += debruijn64[((ulong)((ulong)((diff) & -(diff)) * 0x0218A392CDABBD3FL)) >> 58];
    goto _endCount;
   }
   if (1) if ((ipt<(src_LASTLITERALS-3)) && (Peek4(_, reft) == Peek4(_, ipt))) { ipt+=4; reft+=4; }
   if ((ipt<(src_LASTLITERALS-1)) && (Peek2(_, reft) == Peek2(_, ipt))) { ipt+=2; reft+=2; }
   if ((ipt<src_LASTLITERALS) && (*reft == *ipt)) ipt++;
_endCount:
   reft = xxx_ref;







   while ((startt>startLimit) && (reft > hc4->src_base) && (startt[-1] == reft[-1])) {startt--; reft--;}

   if ((ipt-startt) > longest)
   {
    longest = (int)(ipt-startt);
    *matchpos = reft;
    *startpos = startt;
   }
  }
  xxx_ref = ((xxx_ref) - (int)chainTable[(int)(xxx_ref) & MAXD_MASK]);
 }

 return longest;
}


forceinline static int LZ4_encodeSequence(const byte** src_p, byte** dst_p, const byte** src_anchor, int ml, const byte* xxx_ref)
{
 int length, len;
 byte* xxx_token;


 length = (int)(*src_p - *src_anchor);
 xxx_token = (*dst_p)++;
 if (length>=(int)RUN_MASK) { *xxx_token=(RUN_MASK<<ML_BITS); len = length-RUN_MASK; for(; len > 254 ; len-=255) *(*dst_p)++ = 255; *(*dst_p)++ = (byte)len; }
 else *xxx_token = (length<<ML_BITS);


 { _i = *dst_p + length; *src_anchor += BlindCopy64(_, *src_anchor, _, *dst_p, _i); *dst_p = _i; };


 { Poke2(_, *dst_p, (ushort)(*src_p-xxx_ref)); *dst_p += 2; };


 len = (int)(ml-MINMATCH);
 if (len>=(int)ML_MASK) { *xxx_token+=ML_MASK; len-=ML_MASK; for(; len > 509 ; len-=510) { *(*dst_p)++ = 255; *(*dst_p)++ = 255; } if (len > 254) { len-=255; *(*dst_p)++ = 255; } *(*dst_p)++ = (byte)len; }
 else *xxx_token += len;


 *src_p += ml;
 *src_anchor = *src_p;

 return 0;
}






int LZ4_compressHCCtx(LZ4HC_Data_Structure* ctx,
     const byte* src,
     byte* dst,
     int src_len)
{
 const byte* src_p = (const byte*) src;
 const byte* src_anchor = src_p;
 const byte* const src_end = src_p + src_len;
 const byte* const src_mflimit = src_end - MFLIMIT;
 const byte* const src_LASTLITERALS = (src_end - LASTLITERALS);

 byte* dst_p = (byte*) dst;

 int ml, ml2, ml3, ml0;
 const byte* xxx_ref=NULL;
 const byte* start2=NULL;
 const byte* ref2=NULL;
 const byte* start3=NULL;
 const byte* ref3=NULL;
 const byte* start0;
 const byte* ref0;

 src_p++;


 while (src_p < src_mflimit)
 {
  ml = LZ4HC_InsertAndFindBestMatch (ctx, src_p, src_LASTLITERALS, (&xxx_ref));
  if (!ml) { src_p++; continue; }


  start0 = src_p;
  ref0 = xxx_ref;
  ml0 = ml;

_Search2:
  if (src_p+ml < src_mflimit)
   ml2 = LZ4HC_InsertAndGetWiderMatch(ctx, src_p + ml - 2, src_p + 1, src_LASTLITERALS, ml, &ref2, &start2);
  else ml2=ml;

  if (ml2 == ml)
  {
   LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref);
   continue;
  }

  if (start0 < src_p)
  {
   if (start2 < src_p + ml0)
   {
    src_p = start0;
    xxx_ref = ref0;
    ml = ml0;
   }
  }


  if ((start2 - src_p) < 3)
  {
   ml = ml2;
   src_p = start2;
   xxx_ref =ref2;
   goto _Search2;
  }

_Search3:



  if ((start2 - src_p) < OPTIMAL_ML)
  {
   int correction;
   int new_ml = ml;
   if (new_ml > OPTIMAL_ML) new_ml = OPTIMAL_ML;
   if (src_p+new_ml > start2 + ml2 - MINMATCH) new_ml = (int)(start2 - src_p) + ml2 - MINMATCH;
   correction = new_ml - (int)(start2 - src_p);
   if (correction > 0)
   {
    start2 += correction;
    ref2 += correction;
    ml2 -= correction;
   }
  }


  if (start2 + ml2 < src_mflimit)
   ml3 = LZ4HC_InsertAndGetWiderMatch(ctx, start2 + ml2 - 3, start2, src_LASTLITERALS, ml2, &ref3, &start3);
  else ml3=ml2;

  if (ml3 == ml2)
  {

   if (start2 < src_p+ml)
   {
    if ((start2 - src_p) < OPTIMAL_ML)
    {
     int correction;
     if (ml > OPTIMAL_ML) ml = OPTIMAL_ML;
     if (src_p+ml > start2 + ml2 - MINMATCH) ml = (int)(start2 - src_p) + ml2 - MINMATCH;
     correction = ml - (int)(start2 - src_p);
     if (correction > 0)
     {
      start2 += correction;
      ref2 += correction;
      ml2 -= correction;
     }
    }
    else
    {
     ml = (int)(start2 - src_p);
    }
   }

   LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref);
   src_p = start2;
   LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml2, ref2);
   continue;
  }

  if (start3 < src_p+ml+3)
  {
   if (start3 >= (src_p+ml))
   {
    if (start2 < src_p+ml)
    {
     int correction = (int)(src_p+ml - start2);
     start2 += correction;
     ref2 += correction;
     ml2 -= correction;
     if (ml2 < MINMATCH)
     {
      start2 = start3;
      ref2 = ref3;
      ml2 = ml3;
     }
    }

    LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref);
    src_p = start3;
    xxx_ref = ref3;
    ml = ml3;

    start0 = start2;
    ref0 = ref2;
    ml0 = ml2;
    goto _Search2;
   }

   start2 = start3;
   ref2 = ref3;
   ml2 = ml3;
   goto _Search3;
  }



  if (start2 < src_p+ml)
  {
   if ((start2 - src_p) < (int)ML_MASK)
   {
    int correction;
    if (ml > OPTIMAL_ML) ml = OPTIMAL_ML;
    if (src_p + ml > start2 + ml2 - MINMATCH) ml = (int)(start2 - src_p) + ml2 - MINMATCH;
    correction = ml - (int)(start2 - src_p);
    if (correction > 0)
    {
     start2 += correction;
     ref2 += correction;
     ml2 -= correction;
    }
   }
   else
   {
    ml = (int)(start2 - src_p);
   }
  }
  LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref);

  src_p = start2;
  xxx_ref = ref2;
  ml = ml2;

  start2 = start3;
  ref2 = ref3;
  ml2 = ml3;

  goto _Search3;

 }


 {
  int lastRun = (int)(src_end - src_anchor);
  if (lastRun>=(int)RUN_MASK) { *dst_p++=(RUN_MASK<<ML_BITS); lastRun-=RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *dst_p++ = 255; *dst_p++ = (byte) lastRun; }
  else *dst_p++ = (lastRun<<ML_BITS);
  BlockCopy(_, src_anchor, _, dst_p, src_end - src_anchor);
  dst_p += src_end-src_anchor;
 }


 return (int) (((byte*)dst_p)-dst);
}


int LZ4_compressHC(const byte* src,
     byte* dst,
     int src_len)
{
 void* ctx = LZ4HC_Create((const byte*)src);
 int result = LZ4_compressHCCtx(ctx, src, dst, src_len);
 LZ4HC_Free (&ctx);

 return result;
}
# 214 "lz4hc_cs_adapter.h" 2
