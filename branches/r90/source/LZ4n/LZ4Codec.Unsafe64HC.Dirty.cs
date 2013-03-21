#region Header
// --------------------------------------------------------------------------------------
// LZ4n.LZ4Codec.cs
// --------------------------------------------------------------------------------------
// 
// 
//
// Copyright (c) 2013 Sepura Plc 
//
// Sepura Confidential
//
// Created: 3/21/2013 4:27:45 PM : SEPURA/krajewskim on SEPURA1051 
// 
// --------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LZ4n
{
	public static partial class LZ4Codec
	{
		// Update chains up to ip (excluded)
		private unsafe static void LZ4HC_Insert_64(LZ4HC_Data_Structure hc4, byte* src_p)
		{
			fixed (ushort* chainTable = &hc4.chainTable[0])
			fixed (uint* hash_table = &hc4.hashTable[0])
			{
				byte* src_base = hc4.src;
				while (hc4.nextToUpdate < src_p)
				{
					byte* p = hc4.nextToUpdate;
					int delta = (int)((p) - (hash_table[((((*(uint*)(p))) * 2654435761u) >> HASH_ADJUST)] + src_base));
					if (delta>MAX_DISTANCE) delta = MAX_DISTANCE;
					chainTable[((int)p) & MAXD_MASK] = (ushort)delta;
					hash_table[((((*(uint*)(p))) * 2654435761u) >> HASH_ADJUST)] = (uint)((p) - src_base);
					hc4.nextToUpdate++;
				}

			}
		}
	}
}
