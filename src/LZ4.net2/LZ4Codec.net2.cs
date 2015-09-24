using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using LZ4.Services;
using Microsoft.Win32;

namespace LZ4
{
	public static partial class LZ4Codec
	{
		public delegate void Action();
		public delegate T Func<T>();

		/// <summary>Determines whether VS2010 runtime is installed. 
		/// Note, on Mono the Registry class is not available at all, 
		/// so access to it have to be isolated.</summary>
		/// <returns><c>true</c> it VS2010 runtime is installed, <c>false</c> otherwise.</returns>
		private static bool Has2010Runtime()
		{
			return false;
		}

		// ReSharper disable InconsistentNaming

		/// <summary>Initializes codecs from LZ4mm.</summary>
		private static void InitializeLZ4mm()
		{
			_service_MM32 = _service_MM64 = null;
		}

		/// <summary>Initializes codecs from LZ4cc.</summary>
		private static void InitializeLZ4cc()
		{
			_service_CC32 = _service_CC64 = null;
		}

		/// <summary>Initializes codecs from LZ4n.</summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void InitializeLZ4n()
		{
			_service_N32 = TryService<Unsafe32LZ4Service>();
			_service_N64 = TryService<Unsafe64LZ4Service>();
		}

		/// <summary>Initializes codecs from LZ4s.</summary>
		private static void InitializeLZ4s()
		{
			_service_S32 = _service_S64 = null;
		}

		// ReSharper restore InconsistentNaming
	}
}
