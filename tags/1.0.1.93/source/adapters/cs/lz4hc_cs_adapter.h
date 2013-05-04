// externaly defined:
//   - GEN_SAFE: generate safe code
//   - GEN_X64: generate 64-bit version

#define LZ4_CS_ADAPTER
#define LZ4_MK_OPT
#define _MSC_VER 1600 // simulate Visual Studio 2010

#ifdef GEN_X64
    #define LZ4_ARCH64 1
#else
    #define LZ4_ARCH64 0
#endif

#define LZ4_FUNC(name) name

#ifdef LZ4HC_STANDALONE
private const int MINMATCH = 4;
private const int SKIPSTRENGTH = NOTCOMPRESSIBLE_DETECTIONLEVEL > 2 ? NOTCOMPRESSIBLE_DETECTIONLEVEL : 2;
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
private const int MAX_DISTANCE = (1 << MAXD_LOG) - 1;
#endif

// LZ4HC
#define DICTIONARY_LOGSIZE MAXD_LOG

private const int MAXD = 1 << MAXD_LOG;
private const int MAXD_MASK = MAXD - 1;
private const int HASHHC_LOG = MAXD_LOG - 1;
private const int HASHHC_TABLESIZE = 1 << HASHHC_LOG;
private const int HASHHC_MASK = HASHHC_TABLESIZE - 1;
private const int MAX_NB_ATTEMPTS = 256;
private const int OPTIMAL_ML = (ML_MASK - 1) + MINMATCH;

#define ALLOCATOR(s) (new byte[s])
#define FREEMEM /* gc */
#define MEM_INIT(b,v,l) BlockFill(b, l, v)

// end of LZ4HC

#define LZ4_FORCE_SW_BITCOUNT
#define HEAPMODE 1

#define expect(expr, value) (expr)
#define likely(expr)        (expr)
#define unlikely(expr)      (expr)

#define BYTE    byte
#define U16     ushort
#define U32     uint
#define S32     int
#define U64     ulong
#define S64     long

#ifdef GEN_SAFE
    #define A64(x) Peek8(_, x)
    #define A32(x) Peek4(_, x)
    #define A16(x) Peek2(_, x)
#else
    #define A64(x) (*(ulong*)(x))
    #define A32(x) (*(uint*)(x))
    #define A16(x) (*(ushort*)(x))
#endif


#define HASH_LOG HASHHC_LOG
#define HASH_MASK HASHHC_MASK
#define HASHTABLESIZE HASHHC_TABLESIZE

#if LZ4_ARCH64	// 64-bit
    #define STEPSIZE STEPSIZE_64
    #define UARCH U64
    #define AARCH A64
#else // 32-bit
    #define STEPSIZE STEPSIZE_32
    #define UARCH U32
    #define AARCH A32
#endif

#ifdef GEN_SAFE
    // #define COPY4(x,s,d) { byte[] xxx; xxx[d] = xxx[s]; xxx[d + 1] = xxx[s + 1]; xxx[d + 2] = xxx[s + 2]; xxx[d + 3] = xxx[s + 3]; }
    // #define COPY8(x,s,d) { byte[] xxx; xxx[d] = xxx[s]; xxx[d + 1] = xxx[s + 1]; xxx[d + 2] = xxx[s + 2]; xxx[d + 3] = xxx[s + 3]; xxx[d + 4] = xxx[s + 4]; xxx[d + 5] = xxx[s + 5]; xxx[d + 6] = xxx[s + 6]; xxx[d + 7] = xxx[s + 7]; }
	
    #define COPY4(x,s,d) Copy4(_, s, d)
    #define COPY8(x,s,d) Copy8(_, s, d)

    #if LZ4_ARCH64 // 64-bit
        #define LZ4_COPYSTEP(s,d)      	{ COPY8(_, s, d); d += 8; s += 8; }
        #define LZ4_COPYPACKET(s,d)    	{ COPY8(_, s, d); d += 8; s += 8; }
        #define LZ4_SECURECOPY(s,d,e) 	if (d < e) { _i = WildCopy(_, s, _, d, e); s += _i; d += _i; }
        #define HTYPE                   int
		#define INITBASE(b,s)           int b = src_0
        #define LZ4_WILDCOPY(s,d,e)     if (d < e) /*?*/{ _i = WildCopy(_, s, _, d, e); s += _i; d += _i; }
        #define LZ4_BLINDCOPY(s,d,l)    if (l > 0) /*?*/{ _i = d + l; s += WildCopy(_, s, _, d, _i); d = _i; }
        #define LZ4_NbCommonBytes(val)  debruijn64[((U64)((val) & -(val)) * 0x0218A392CDABBD3FL) >> 58]
    #else // 32-bit
        #define LZ4_COPYSTEP(s,d)       { COPY4(_, s, d); d += 4; s += 4; }
        #define LZ4_COPYPACKET(s,d)     { COPY8(_, s, d); d += 8; s += 8; }
        #define LZ4_SECURECOPY(s,d,e)   if (d < e) { _i = WildCopy(_, s, _, d, e); s += _i; d += _i; }
        #define HTYPE                   int
		#define INITBASE(b,s)           int b = src_0
        #define LZ4_WILDCOPY(s,d,e)     if (d < e) /*?*/{ _i = WildCopy(_, s, _, d, e); s += _i; d += _i; }
        #define LZ4_BLINDCOPY(s,d,l)    if (l > 0) /*?*/{ _i = d + l; s += WildCopy(_, s, _, d, _i); d = _i; }
        #define LZ4_NbCommonBytes(val)  debruijn32[((U32)((val) & -(val)) * 0x077CB531u) >> 27]
    #endif
    #define LZ4_WRITE_LITTLEENDIAN_16(p,v)  { Poke2(_, p, v); p += 2; }
    #define memcpy(d,s,l)                   BlockCopy(_, s, _, d, l)
#else
    #if LZ4_ARCH64 // 64-bit
        #define LZ4_COPYSTEP(s, d)      *(ulong*)d = *(ulong*)s; d += 8; s += 8;
        #define LZ4_COPYPACKET    		LZ4_COPYSTEP
        #define LZ4_SECURECOPY(s, d, e) if (d < e) LZ4_WILDCOPY(s, d, e)
        #define HTYPE                   uint
		#define INITBASE(b,s)           byte* b = s
        #define LZ4_NbCommonBytes(val)  debruijn64[((U64)((U64)((val) & -(val)) * 0x0218A392CDABBD3FL)) >> 58]
    #else // 32-bit
        #define LZ4_COPYSTEP(s, d)      *(uint*)d = *(uint*)s; d += 4; s += 4;
        #define LZ4_COPYPACKET(s, d)    *(uint*)d = *(uint*)s; d += 4; s += 4; *(uint*)d = *(uint*)s; d += 4; s += 4;
        #define LZ4_SECURECOPY          LZ4_WILDCOPY
        #define HTYPE                   uint
		#define INITBASE(b,s)		    byte* b = s
        #define LZ4_NbCommonBytes(val)  debruijn32[((U32)((U32)((val) & -(val)) * 0x077CB531u)) >> 27]
    #endif
    #define LZ4_WILDCOPY(s,d,e)     		{ do { LZ4_COPYPACKET(s, d) } while (d < e); }
    #define LZ4_BLINDCOPY(s,d,l)    		{ _p = d + (l); LZ4_WILDCOPY(s, d, _p); d = _p; }
    #define LZ4_WRITE_LITTLEENDIAN_16(p,v)  { *(ushort*)p = v; p += 2; }
    #define memcpy(d,s,l)                   BlockCopy(s, d, (int)(l))
#endif

#define LZ4_READ_LITTLEENDIAN_16(d,s,p) { d = (s) - A16(p); }

#define LZ4_HASH_FUNCTION(i)    (((i) * 2654435761u) >> HASHHC_ADJUST)
#define LZ4_HASH_VALUE(p)       LZ4_HASH_FUNCTION(A32(p))

#define HASH_VALUE              LZ4_HASH_VALUE
#define HASH_POINTER(p)         (HashTable[LZ4_HASH_VALUE(p)] + base)
#define DELTANEXT(p)            chainTable[((size_t)p) & MAXD_MASK] 
#define GETNEXT(p)              ((p) - (size_t)DELTANEXT(p))

#define __inline
#define char byte
#define size_t int
#define unsigned uint
#define restrict

#define ref xxx_ref
#define token xxx_token

#define base src_base
#define source src
#define dest dst
#define osize dst_len
#define cpy dst_cpy
#define isize src_len
#define inputSize src_len
#define maxOutputSize dst_maxlen
#define oend dst_end
#define iend src_end
#define ip src_p
#define op dst_p
#define forwardH h_fwd
#define forwardIp src_p_fwd
#define skipStrength SKIPSTRENGTH
#define anchor src_anchor
#define mflimit src_mflimit
#define HashTable hashTable

#define matchlimit src_LASTLITERALS
#define iend_LASTLITERALS_1 src_LASTLITERALS_1
#define iend_LASTLITERALS_3 src_LASTLITERALS_3
#define iend_LASTLITERALS_STEPSIZE_1 src_LASTLITERALS_STEPSIZE_1
#define oend_LASTLITERALS_1 dst_LASTLITERALS_1
#define oend_LASTLITERALS_3 dst_LASTLITERALS_3

private class LZ4HC_Data_Structure
{
	public byte* src_base;
	public HTYPE hashTable[HASHTABLESIZE];
	public U16 chainTable[MAXD];
	public byte* nextToUpdate;
};


// GOGOGO
#include "..\..\..\original\lz4hc.c"
