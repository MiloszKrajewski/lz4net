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
# 212 "lz4hc_cs_adapter.h"
private class LZ4HC_Data_Structure
{
 public byte* src_base;
 public byte* hashTable[HASHHC_TABLESIZE];
 public ushort chainTable[MAXD];
 public byte* nextToUpdate;
};

// GOGOGO
# 1 "..\\..\\..\\original\\lz4hc.c" 1
/*

   LZ4 HC - High Compression Mode of LZ4

   Copyright (C) 2011-2013, Yann Collet.

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
# 330 "..\\..\\..\\original\\lz4hc.c"
inline static int LZ4HC_Init (LZ4HC_Data_Structure* hc4, byte* src_base)
{
 BlockFill((void*)hc4.hashTable, sizeof(hc4.hashTable), 0);
 BlockFill(hc4.chainTable, sizeof(hc4.chainTable), 0xFF);
 hc4.nextToUpdate = src_base + 0;
 hc4.src_base = src_base;
 return 1;
}

inline static void* LZ4HC_Create (byte* src_base)
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
forceinline static void LZ4HC_Insert (LZ4HC_Data_Structure* hc4, byte* src_p)
{
 ushort* chainTable = hc4.chainTable;
 byte** hashTable = hc4.hashTable;
 const int src_base = 0;

 while(hc4.nextToUpdate < src_p)
 {
  byte* p = hc4.nextToUpdate;
  int delta = (p) - (hashTable[((((*(uint*)(p))) * 2654435761u) >> HASH_ADJUST)] + src_base);
  if (delta>MAX_DISTANCE) delta = MAX_DISTANCE;
  chainTable[((int)p) & MAXD_MASK] = (ushort)delta;
        hashTable[((((*(uint*)(p))) * 2654435761u) >> HASH_ADJUST)] = (byte*)((p) - src_base);
  hc4.nextToUpdate++;
 }
}

forceinline static int LZ4HC_CommonLength (byte* p1, byte* p2, byte* src_LASTLITERALS)
{
 byte* p1t = p1;

 while (p1t<src_LASTLITERALS-(STEPSIZE_32-1))
 {
  uint diff = (*(uint*)(p2)) ^ (*(uint*)(p1t));
  if (!diff) { p1t+=STEPSIZE_32; p2+=STEPSIZE_32; continue; }
  p1t += debruijn32[((uint)((uint)((diff) & -(diff)) * 0x077CB531u)) >> 27];
  return (p1t - p1);
 }
 if (0) if ((p1t<(src_LASTLITERALS-3)) && ((*(uint*)(p2)) == (*(uint*)(p1t)))) { p1t+=4; p2+=4; }
 if ((p1t<(src_LASTLITERALS-1)) && ((*(ushort*)(p2)) == (*(ushort*)(p1t)))) { p1t+=2; p2+=2; }
 if ((p1t<src_LASTLITERALS) && (*p2 == *p1t)) p1t++;
 return (p1t - p1);
}

forceinline static int LZ4HC_InsertAndFindBestMatch (LZ4HC_Data_Structure* hc4, byte* src_p, byte* src_LASTLITERALS, byte** matchpos)
{
 ushort* const chainTable = hc4.chainTable;
 byte** const hashTable = hc4.hashTable;
 byte* xxx_ref;
 const int src_base = 0;
 int nbAttempts=MAX_NB_ATTEMPTS;
    int repl=0, ml=0;
    ushort delta;

 // HC4 match finder
 LZ4HC_Insert(hc4, src_p);
 xxx_ref = (hashTable[((((*(uint*)(src_p))) * 2654435761u) >> HASH_ADJUST)] + src_base);

    // Detect repetitive sequences of length <= 4
 if (xxx_ref >= src_p-4) // potential repetition
 {
  if ((*(uint*)(xxx_ref)) == (*(uint*)(src_p))) // confirmed
  {
            delta = (ushort)(src_p-xxx_ref);
            repl = ml = LZ4HC_CommonLength(src_p+MINMATCH, xxx_ref+MINMATCH, src_LASTLITERALS) + MINMATCH;
   *matchpos = xxx_ref;
  }
  xxx_ref = ((xxx_ref) - (int)chainTable[((int)xxx_ref) & MAXD_MASK]);
 }

    while ((xxx_ref >= src_p-MAX_DISTANCE) && (nbAttempts))
 {
  nbAttempts--;
  if (*(xxx_ref+ml) == *(src_p+ml))
  if ((*(uint*)(xxx_ref)) == (*(uint*)(src_p)))
  {
   int mlt = LZ4HC_CommonLength(src_p+MINMATCH, xxx_ref+MINMATCH, src_LASTLITERALS) + MINMATCH;
   if (mlt > ml) { ml = mlt; *matchpos = xxx_ref; }
  }
  xxx_ref = ((xxx_ref) - (int)chainTable[((int)xxx_ref) & MAXD_MASK]);
 }

    // Complete table
    if (repl)
    {
        byte* ptr = src_p;
        byte* end;

        end = src_p + repl - (MINMATCH-1);
        while(ptr < end-delta)
        {
            chainTable[((int)ptr) & MAXD_MASK] = delta; // Pre-Load
            ptr++;
        }
        do
        {
            chainTable[((int)ptr) & MAXD_MASK] = delta;
            hashTable[((((*(uint*)(ptr))) * 2654435761u) >> HASH_ADJUST)] = (byte*)((ptr) - src_base); // Head of chain
            ptr++;
        } while(ptr < end);
        hc4.nextToUpdate = end;
    }

 return (int)ml;
}

forceinline static int LZ4HC_InsertAndGetWiderMatch (LZ4HC_Data_Structure* hc4, byte* src_p, byte* startLimit, byte* src_LASTLITERALS, int longest, byte** matchpos, byte** startpos)
{
 ushort* const chainTable = hc4.chainTable;
 byte** const hashTable = hc4.hashTable;
 const int src_base = 0;
 byte* xxx_ref;
 int nbAttempts = MAX_NB_ATTEMPTS;
 int delta = (int)(src_p-startLimit);

 // First Match
 LZ4HC_Insert(hc4, src_p);
 xxx_ref = (hashTable[((((*(uint*)(src_p))) * 2654435761u) >> HASH_ADJUST)] + src_base);

    while ((xxx_ref >= src_p-MAX_DISTANCE) && (nbAttempts))
 {
  nbAttempts--;
  if (*(startLimit + longest) == *(xxx_ref - delta + longest))
  if ((*(uint*)(xxx_ref)) == (*(uint*)(src_p)))
  {

   byte* reft = xxx_ref+MINMATCH;
   byte* ipt = src_p+MINMATCH;
   byte* startt = src_p;

   while (ipt<src_LASTLITERALS-(STEPSIZE_32-1))
   {
    uint diff = (*(uint*)(reft)) ^ (*(uint*)(ipt));
    if (!diff) { ipt+=STEPSIZE_32; reft+=STEPSIZE_32; continue; }
    ipt += debruijn32[((uint)((uint)((diff) & -(diff)) * 0x077CB531u)) >> 27];
    goto _endCount;
   }
   if (0) if ((ipt<(src_LASTLITERALS-3)) && ((*(uint*)(reft)) == (*(uint*)(ipt)))) { ipt+=4; reft+=4; }
   if ((ipt<(src_LASTLITERALS-1)) && ((*(ushort*)(reft)) == (*(ushort*)(ipt)))) { ipt+=2; reft+=2; }
   if ((ipt<src_LASTLITERALS) && (*reft == *ipt)) ipt++;
_endCount:
   reft = xxx_ref;

   while ((startt>startLimit) && (reft > hc4.src_base) && (startt[-1] == reft[-1])) {startt--; reft--;}

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

forceinline static int LZ4_encodeSequence(byte** src_p, byte** dst_p, byte** src_anchor, int matchLength, byte* xxx_ref, byte* dst_end)
{
 int length, len;
 byte* xxx_token;

 // Encode Literal length
 length = (int)(*src_p - *src_anchor);
 xxx_token = (*dst_p)++;
    if ((*dst_p + length + (2 + 1 + LASTLITERALS) + (length>>8)) > dst_end) return 1; // Check output limit
 if (length>=(int)RUN_MASK) { *xxx_token=(RUN_MASK<<ML_BITS); len = length-RUN_MASK; for(; len > 254 ; len-=255) *(*dst_p)++ = 255; *(*dst_p)++ = (byte)len; }
    else *xxx_token = (byte)(length<<ML_BITS);

 // Copy Literals
 { _p = *dst_p + (length); { do { *(uint*)*dst_p = *(uint*)*src_anchor; *dst_p += 4; *src_anchor += 4; *(uint*)*dst_p = *(uint*)*src_anchor; *dst_p += 4; *src_anchor += 4; } while (*dst_p < _p); }; *dst_p = _p; };

 // Encode Offset
 { *(ushort*)*dst_p = (ushort)(*src_p-xxx_ref); *dst_p += 2; };

 // Encode MatchLength
    len = (int)(matchLength-MINMATCH);
    if (*dst_p + (1 + LASTLITERALS) + (length>>8) > dst_end) return 1; // Check output limit
 if (len>=(int)ML_MASK) { *xxx_token+=ML_MASK; len-=ML_MASK; for(; len > 509 ; len-=510) { *(*dst_p)++ = 255; *(*dst_p)++ = 255; } if (len > 254) { len-=255; *(*dst_p)++ = 255; } *(*dst_p)++ = (byte)len; }
    else *xxx_token += (byte)len;

 // Prepare next loop
    *src_p += matchLength;
 *src_anchor = *src_p;

 return 0;
}

//****************************
// Compression CODE
//****************************

int LZ4_compressHCCtx(LZ4HC_Data_Structure* ctx,
     byte* src,
     byte* dst,
                 int src_len,
                 int dst_maxlen)
{
 byte* src_p = (byte*) src;
 byte* src_anchor = src_p;
    byte* src_end = src_p + src_len;
 byte* src_mflimit = src_end - MFLIMIT;
 byte* src_LASTLITERALS = (src_end - LASTLITERALS);

 byte* dst_p = (byte*) dst;
    byte* dst_end = dst_p + dst_maxlen;

 int ml, ml2, ml3, ml0;
 byte* xxx_ref=NULL;
 byte* start2=NULL;
 byte* ref2=NULL;
 byte* start3=NULL;
 byte* ref3=NULL;
 byte* start0;
 byte* ref0;

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
            if (LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref, dst_end)) return 0;
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
            if (start2 < src_p+ml) ml = (int)(start2 - src_p);
   // Now, encode 2 sequences
            if (LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref, dst_end)) return 0;
   src_p = start2;
            if (LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml2, ref2, dst_end)) return 0;
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

                if (LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref, dst_end)) return 0;
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
        if (LZ4_encodeSequence(&src_p, &dst_p, &src_anchor, ml, xxx_ref, dst_end)) return 0;

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
        if (((byte*)dst_p - dst) + lastRun + 1 + ((lastRun+255-RUN_MASK)/255) > (uint)dst_maxlen) return 0; // Check output limit
  if (lastRun>=(int)RUN_MASK) { *dst_p++=(RUN_MASK<<ML_BITS); lastRun-=RUN_MASK; for(; lastRun > 254 ; lastRun-=255) *dst_p++ = 255; *dst_p++ = (byte) lastRun; }
        else *dst_p++ = (byte)(lastRun<<ML_BITS);
  BlockCopy(src_anchor, dst_p, (int)(src_end - src_anchor));
  dst_p += src_end-src_anchor;
 }

 // End
 return (int) (((byte*)dst_p)-dst);
}

int LZ4_compressHC_limitedOutput(byte* src,
     byte* dst,
                 int src_len,
                 int dst_maxlen)
{
 void* ctx = LZ4HC_Create((byte*)src);
    int result = LZ4_compressHCCtx(ctx, src, dst, src_len, dst_maxlen);
 LZ4HC_Free (&ctx);

 return result;
}

int LZ4_compressHC(byte* src,
                 byte* dst,
                 int src_len)
{
    return LZ4_compressHC_limitedOutput(src, dst, src_len, LZ4_compressBound(src_len)+1);
}
# 223 "lz4hc_cs_adapter.h" 2
