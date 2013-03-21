# 1 "lz4hc_cs_adapter.h"
# 1 "<command-line>"
# 1 "lz4hc_cs_adapter.h"
// externaly defined:
//   - GEN_SAFE: generate safe code
//   - GEN_X64: generate 64-bit version
# 59 "lz4hc_cs_adapter.h"
// LZ4HC


private const int MAXD = 1 << MAXD_LOG;
private const int MAXD_MASK = MAXD - 1;
private const int HASHHC_LOG = MAXD_LOG - 1;
private const int HASHHC_TABLESIZE = 1 << HASHHC_LOG;
private const int HASHHC_MASK = HASHHC_TABLESIZE - 1;
private const int MAX_NB_ATTEMPTS = 256;
private const int OPTIMAL_ML = (ML_MASK - 1) + MINMATCH;





// end of LZ4HC
# 205 "lz4hc_cs_adapter.h"
private class LZ4HC_Data_Structure
{
 public byte* src_base;
 public int hashTable[HASHHC_TABLESIZE];
 public ushort chainTable[MAXD];
 public byte* nextToUpdate;
};


// GOGOGO
# 1 "..\\..\\..\\original\\lz4hc.c" 1
/*

   LZ4 HC - High Compression Mode of LZ4

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
# 328 "..\\..\\..\\original\\lz4hc.c"
inline static int LZ4HC_Init (LZ4HC_Data_Structure* hc4, const byte* src_base)
{
 BlockFill((void*)hc4->hashTable, sizeof(hc4->hashTable), 0);
 BlockFill(hc4->chainTable, sizeof(hc4->chainTable), 0xFF);
 hc4->nextToUpdate = src_base + 0;
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
 /* gc */(*LZ4HC_Data);
 *LZ4HC_Data = NULL;
 return (1);
}


// Update chains up to ip (excluded)
forceinline static void LZ4HC_Insert (LZ4HC_Data_Structure* hc4, const byte* src_p)
{
 ushort* chainTable = hc4->chainTable;
 int* hash_table = hc4->hashTable;
 int src_base = 0;

 while(hc4->nextToUpdate < src_p)
 {
        const byte* p = hc4->nextToUpdate;
        int delta = (p) - (hash_table[(((Peek4(_, p)) * 2654435761u) >> HASH_ADJUST)] + src_base);
        if (delta>MAX_DISTANCE) delta = MAX_DISTANCE;
        chainTable[((int)p) & MAXD_MASK] = (ushort)delta;
        hash_table[(((Peek4(_, p)) * 2654435761u) >> HASH_ADJUST)] = (p) - src_base;
  hc4->nextToUpdate++;
 }
}


forceinline static int LZ4HC_CommonLength (const byte* p1, const byte* p2, const byte* const src_LASTLITERALS)
{
    const byte* p1t = p1;

    while (p1t<src_LASTLITERALS-(STEPSIZE_32-1))
    {
        uint diff = Peek4(_, p2) ^ Peek4(_, p1t);
        if (!diff) { p1t+=STEPSIZE_32; p2+=STEPSIZE_32; continue; }
        p1t += debruijn32[((uint)((uint)((diff) & -(diff)) * 0x077CB531u)) >> 27];
        return (p1t - p1);
    }
    if (0) if ((p1t<(src_LASTLITERALS-3)) && (Peek4(_, p2) == Peek4(_, p1t))) { p1t+=4; p2+=4; }
    if ((p1t<(src_LASTLITERALS-1)) && (Peek2(_, p2) == Peek2(_, p1t))) { p1t+=2; p2+=2; }
    if ((p1t<src_LASTLITERALS) && (*p2 == *p1t)) p1t++;
    return (p1t - p1);
}


forceinline static int LZ4HC_InsertAndFindBestMatch (LZ4HC_Data_Structure* hc4, const byte* src_p, const byte* const src_LASTLITERALS, const byte** matchpos)
{
 ushort* const chainTable = hc4->chainTable;
 int* const hash_table = hc4->hashTable;
 const byte* xxx_ref;
 int src_base = 0;
 int nbAttempts=MAX_NB_ATTEMPTS;
    int ml=0;

 // HC4 match finder
 LZ4HC_Insert(hc4, src_p);
 xxx_ref = (hash_table[(((Peek4(_, src_p)) * 2654435761u) >> HASH_ADJUST)] + src_base);


    if (xxx_ref >= src_p-4) // potential repetition
    {
        if (Peek4(_, xxx_ref) == Peek4(_, src_p)) // confirmed
        {
            const ushort delta = (ushort)(src_p-xxx_ref);
            const byte* ptr = src_p;
            const byte* end;
            ml = LZ4HC_CommonLength(src_p+MINMATCH, xxx_ref+MINMATCH, src_LASTLITERALS) + MINMATCH;
            end = src_p + ml - (MINMATCH-1);
            while(ptr < end-delta)
            {
                chainTable[((int)ptr) & MAXD_MASK] = delta; // Pre-Load
                ptr++;
            }
            do
            {
                chainTable[((int)ptr) & MAXD_MASK] = delta;
                hash_table[(((Peek4(_, ptr)) * 2654435761u) >> HASH_ADJUST)] = (ptr) - src_base; // Head of chain
                ptr++;
            } while(ptr < end);
            hc4->nextToUpdate = end;
            *matchpos = xxx_ref;
        }
        xxx_ref = ((xxx_ref) - (int)chainTable[((int)xxx_ref) & MAXD_MASK]);
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
  xxx_ref = ((xxx_ref) - (int)chainTable[((int)xxx_ref) & MAXD_MASK]);
 }

    return (int)ml;
}


forceinline static int LZ4HC_InsertAndGetWiderMatch (LZ4HC_Data_Structure* hc4, const byte* src_p, const byte* startLimit, const byte* src_LASTLITERALS, int longest, const byte** matchpos, const byte** startpos)
{
 ushort* const chainTable = hc4->chainTable;
 int* const hash_table = hc4->hashTable;
 int src_base = 0;
 const byte* xxx_ref;
 int nbAttempts = MAX_NB_ATTEMPTS;
 int delta = (int)(src_p-startLimit);

 // First Match
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

   while (ipt<src_LASTLITERALS-(STEPSIZE_32-1))
   {
    uint diff = Peek4(_, reft) ^ Peek4(_, ipt);
    if (!diff) { ipt+=STEPSIZE_32; reft+=STEPSIZE_32; continue; }
    ipt += debruijn32[((uint)((uint)((diff) & -(diff)) * 0x077CB531u)) >> 27];
    goto _endCount;
   }
   if (0) if ((ipt<(src_LASTLITERALS-3)) && (Peek4(_, reft) == Peek4(_, ipt))) { ipt+=4; reft+=4; }
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
  xxx_ref = ((xxx_ref) - (int)chainTable[((int)xxx_ref) & MAXD_MASK]);
 }

 return longest;
}


forceinline static int LZ4_encodeSequence(const byte** src_p, byte** dst_p, const byte** src_anchor, int ml, const byte* xxx_ref)
{
 int length, len;
 byte* xxx_token;

 // Encode Literal length
 length = (int)(*src_p - *src_anchor);
 xxx_token = (*dst_p)++;
 if (length>=(int)RUN_MASK) { *xxx_token=(RUN_MASK<<ML_BITS); len = length-RUN_MASK; for(; len > 254 ; len-=255) *(*dst_p)++ = 255; *(*dst_p)++ = (byte)len; }
 else *xxx_token = (length<<ML_BITS);

 // Copy Literals
 { _i = *dst_p + length; *src_anchor += BlindCopy32(_, *src_anchor, _, *dst_p, _i); *dst_p = _i; };

 // Encode Offset
 { Poke2(_, *dst_p, (ushort)(*src_p-xxx_ref)); *dst_p += 2; };

 // Encode MatchLength
 len = (int)(ml-MINMATCH);
 if (len>=(int)ML_MASK) { *xxx_token+=ML_MASK; len-=ML_MASK; for(; len > 509 ; len-=510) { *(*dst_p)++ = 255; *(*dst_p)++ = 255; } if (len > 254) { len-=255; *(*dst_p)++ = 255; } *(*dst_p)++ = (byte)len; }
 else *xxx_token += len;

 // Prepare next loop
 *src_p += ml;
 *src_anchor = *src_p;

 return 0;
}


//****************************
// Compression CODE
//****************************

static int LZ4_compressHCCtx(LZ4HC_Data_Structure* ctx,
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

 // Main Loop
 while (src_p < src_mflimit)
 {
  ml = LZ4HC_InsertAndFindBestMatch (ctx, src_p, src_LASTLITERALS, (&xxx_ref));
  if (!ml) { src_p++; continue; }

  // saved, in case we would skip too much
  start0 = src_p;
  ref0 = xxx_ref;
  ml0 = ml;

_Search2:
  if (src_p+ml < src_mflimit)
   ml2 = LZ4HC_InsertAndGetWiderMatch(ctx, src_p + ml - 2, src_p + 1, src_LASTLITERALS, ml, &ref2, &start2);
  else ml2=ml;

  if (ml2 == ml) // No better match
  {
   LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref);
   continue;
  }

  if (start0 < src_p)
  {
   if (start2 < src_p + ml0) // empirical
   {
    src_p = start0;
    xxx_ref = ref0;
    ml = ml0;
   }
  }

  // Here, start0==ip
  if ((start2 - src_p) < 3) // First Match too small : removed
  {
   ml = ml2;
   src_p = start2;
   xxx_ref =ref2;
   goto _Search2;
  }

_Search3:
  // Currently we have :
  // ml2 > ml1, and
  // ip1+3 <= ip2 (usually < ip1+ml1)
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
  // Now, we have start2 = ip+new_ml, with new_ml=min(ml, OPTIMAL_ML=18)

  if (start2 + ml2 < src_mflimit)
   ml3 = LZ4HC_InsertAndGetWiderMatch(ctx, start2 + ml2 - 3, start2, src_LASTLITERALS, ml2, &ref3, &start3);
  else ml3=ml2;

  if (ml3 == ml2) // No better match : 2 sequences to encode
  {
   // ip & ref are known; Now for ml
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
   // Now, encode 2 sequences
   LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref);
   src_p = start2;
   LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml2, ref2);
   continue;
  }

  if (start3 < src_p+ml+3) // Not enough space for match 2 : remove it
  {
   if (start3 >= (src_p+ml)) // can write Seq1 immediately ==> Seq2 is removed, so Seq3 becomes Seq1
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

  // OK, now we have 3 ascending matches; let's write at least the first one
  // ip & ref are known; Now for ml
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

 // Encode Last Literals
 {
  int lastRun = (int)(src_end - src_anchor);
  if (lastRun>=(int)RUN_MASK) { *dst_p++=(RUN_MASK<<ML_BITS); lastRun-=RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *dst_p++ = 255; *dst_p++ = (byte) lastRun; }
  else *dst_p++ = (lastRun<<ML_BITS);
  BlockCopy(_, src_anchor, _, dst_p, src_end - src_anchor);
  dst_p += src_end-src_anchor;
 }

 // End
 return (int) (((byte*)dst_p)-dst);
}


int LZ4_compressHC(const byte* src,
     byte* dst,
     int src_len)
{
 void* ctx = LZ4HC_Create((const byte*)src);
 int result = LZ4_compressHCCtx((LZ4HC_Data_Structure*)ctx, src, dst, src_len);
 LZ4HC_Free (&ctx);

 return result;
}
# 216 "lz4hc_cs_adapter.h" 2
