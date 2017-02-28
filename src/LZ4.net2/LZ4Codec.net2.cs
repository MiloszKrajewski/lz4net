using LZ4.Services;
using System.Runtime.CompilerServices;

namespace LZ4
{
	public static partial class LZ4Codec
	{
		/// <summary>Polyfill for .NET4's Action</summary>
		public delegate void Action();

		// ReSharper disable once TypeParameterCanBeVariant
		/// <summary>Polyfill for .NET4's Func</summary>
		public delegate T Func<T>();

		/// <summary>Determines whether VS2015 runtime is installed. 
		/// Note, on Mono the Registry class is not available at all, 
		/// so access to it have to be isolated.</summary>
		/// <returns><c>true</c> it VS2015 runtime is installed, <c>false</c> otherwise.</returns>
		private static bool Has2015Runtime()
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
		private static void InitializeLZ4n()
		{
			_service_N32 = _service_N64 = null;
		}

		/// <summary>Initializes codecs from LZ4s.</summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void InitializeLZ4s()
		{
			_service_S32 = TryService<Safe32LZ4Service>();
			_service_S64 = TryService<Safe64LZ4Service>();
		}

		// ReSharper restore InconsistentNaming
	}
}
