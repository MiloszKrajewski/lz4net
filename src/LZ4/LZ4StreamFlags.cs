using System;

namespace LZ4
{
	/// <summary>
	/// Additional flags for LZ4Stream.
	/// </summary>
	[Flags]
	public enum LZ4StreamFlags
	{
		/// <summary>Default settings.</summary>
		Default = 0x00,

		/// <summary>Enforces full block reads.</summary>
		FullBlockRead = 0x01,

		/// <summary>Uses high compression version of algorithm.</summary>
		HighCompression = 0x02,

		/// <summary>Isolates inner stream so it does not get 
		/// closed when LZ4 stream is closed.</summary>
		IsolateInnerStream = 0x04,
	}
}
