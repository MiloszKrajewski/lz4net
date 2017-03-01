## 1.0.11.93
- ADDED: support for .NET Core / .NET Standard 1.6
- CHANGED: converted C++ projects to VC++ 2015
- NOTE: support for Xamarin (Android and iOS) is still questionable

## 1.0.10.93
- BUGFIX: support for Mono (with full .NET 4 assembly, not only Portable)
- ISSUE: added InteractiveRead mode (useful with network streams)
- DEPRECATED: some LZ4Stream constructors are now depreacated, please use new ones instead

## 1.0.9.93
- ADDED: support for .NET 2
- ADDED: support for Windows Phone
- ADDED: support for Xamarin (Android and iOS)
- ADDED: support for Silverlight (yes, really)
- NOTE: use "Portable" for Mono (still some issues with "NET4" compatibility)

## 1.0.6.93
- ADDED: support for Mono
- BUGFIX: fixed a problem with Wrap for small buffers

## 1.0.5.93
- ADDED: VS 2010 runtime detection
- ADDED: wrapping/unwrapping functions
- CHANGED: all-in-one package generated with LibZ 1.1.0.2

## 1.0.3.93
- BUGFIX: LZ4Stream transferring data over TCP/IP stream was not handling the fact that data may not be there yet

## 1.0.2.93
- BUGFIX: Last chunk in LZ4Stream was always 1MB (or whatever was chosen as block size)

## 1.0.1.93
- PORT: Adapted from r93 release of LZ4
- ADDED: LZ4HC codec
- ADDED: LZ4Stream
- IMPROVEMENT: Safe compression improved by ~4%
- IMPROVEMENT: Safe decompression improved by ~20-30% (!)
- IMPROVEMENT: Minimal improvement of compression/decompression of other algorithms (0%-5%)

## 1.0.0.88
- PORT: Adapted from r88 release of LZ4
- ADDED: All assemblies has been merged into one (google:LibZ)
- ADDED: MixedMode x86/x64, C++/CLI x86/x64, Unsafe and Safe
