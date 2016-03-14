# lz4net
**LZ4** - ultra fast compression algorithm - for all .NET platforms

LZ4 is lossless compression algorithm, sacrificing compression ratio for compression/decompression speed. Its compression speed is ~400 MB/s per core while decompression speed reaches ~2 GB/s, not far from RAM speed limits.

LZ4net brings LZ4 to all (most?) .NET platforms: .NET 2.0+, Mono, Windows Phone, Xamarin.iOS, Xamarin.Android and Silverlight

Original LZ4 has been written by Yann Collet and original C sources can be found [here](https://github.com/Cyan4973/lz4)

## Migration from codeplex
Sources has been moved to GitHub, while project documentation has not been properly migrated yet and is still hosted at [codeplex](https://lz4net.codeplex.com/)

## Change log
You can find it [here](CHANGES.md)

## NuGet
You can download lz4net from [NuGet](http://nuget.org/packages/lz4net/)

## Releases
Releases are also available on [github](https://github.com/MiloszKrajewski/lz4net/releases)

## What is 'Fast compression algorithm'?
While compression algorithms you use day-to-day to archive your data work around the speed of 10MB/s giving you quite decent compression ratios, 'fast algorithms' are designed to work 'faster than your hard drive' sacrificing compression ratio.
One of the most famous fast compression algorithms in Google's own [Snappy](http://code.google.com/p/snappy/) which is advertised as 250MB/s compression, 500MB/s decompression on i7 in 64-bit mode.
Fast compression algorithms help reduce network traffic / hard drive load compressing data on the fly with no noticeable latency.

I just tried to compress some sample data (Silesia Corpus) receiving:
* **zlib** (7zip) - 7.5M/s compression, 110MB/s decompression, 44% compression ratio
* **lzma** (7zip) - 1.5MB/s compression, 50MB/s decompression, 37% compression ratio
* **lz4** - 280MB/s compression, 520MB/s decompression, 57% compression ratio

**Note**: Values above are for illustration only. they are affected by HDD read/write speed (in fact LZ4 decompression in much faster). The 'real' tests are taking HDD speed out of equation. For detailed performance tests see [Performance Testing] and [Comparison to other algorithms].

## Why use it?
Here is the thing. At first I needed fast compression to pass huge amount of data to SQL Server over network of unknown speed (or maybe even via shared memory on the same machine) . If network speed was known to be large I wouldn't use compression at all because it would just slow the whole process down. If network speed was known to be small I would use Deflate or ZLib ([DotNetZip](http://dotnetzip.codeplex.com/) - it would reduce the amount of data sent but compression would be fast enough to feed the connection.
Anyway, I decided to go for 'near memcpy' compression algorithm. It reduces the amount of data pushed over the network and does not introduce much latency when using local server. 

## Other 'Fast compression algorithms'
There are multiple fast compression algorithms, to name a few: [LZO](http://lzohelper.codeplex.com/), [QuickLZ](http://www.quicklz.com/index.php), [LZF](http://csharplzfcompression.codeplex.com/), [Snappy](https://github.com/Kintaro/SnappySharp), FastLZ. 
You can find comparison of them on [LZ4 webpage](http://code.google.com/p/lz4/) or [here](http://www.technofumbles.com/weblog/2011/04/22/survey-of-fast-compression-algorithms-part-1-2/)

Personally I found LZ4 most interesting. Quite good compression ratio, with decent compression speed and excellent decompression speed. I actually trusted the author that is is faster than others and never thoroughly tested it. You are most welcome to do it (please note, make the comparison fair: do not compare native C++ implementation to .NET safe implementation).

## Why not just link to pre-compiled (native) .DLL?
If my life was depending on it I would, but otherwise I just don't like P/Invoke.

## There is already [LZ4Sharp](https://github.com/stangelandcl/LZ4Sharp), why not use it?
The other thing was that I needed 'safe' (in .NET terms - pure CLR, no pointers) implementation for SQL Server side. LZ4Sharp uses 'unsafe' code.
But then I also wanted it to be fast when application is 'trusted', so I also did 'unsafe' implementation. Hey why not 'Mixed Mode' then? Come on, I have sources so I can also do C++/CLI. Still pure CLR but on original sources with no risk of making a mistake during translation.

So I ended up with 4 implementations:

* **Mixed Mode** - C# interface + native C in one assembly
  * Pros:
    * Fastest (almost as fast as original)
  * Cons:
    * Requires VC++ 2010 Redistributable to be installed on target machine ([x86](http://www.microsoft.com/en-us/download/details.aspx?id=5555) and/or [x64](http://www.microsoft.com/en-us/download/details.aspx?id=14632)
    * Contains unmanaged code, may not be allowed in some environments
    * Does not have AnyCPU configuration (this can be solved though, see: [Automiatic loading of x86 or x64])
* **C++/CLI** - original C sources recompiled for CLR
  * Pros:
    * Almost as fast as Mixed Mode
    * Only managed code
  * Cons:
    * Contains unsafe code, may not be allowed in some environments
    * Does not have AnyCPU configuration (this can be solved though, see: [Automiatic loading of x86 or x64])
* **unsafe C#** - C# but still fast
  * Pros:
    * C# (more .NET-ish)
    * Still quite fast
  * Cons:
    * Contains unsafe code, may not be allowed in some environments
* **safe C#** - just in case (mobile phone maybe?)
  * Pros:
    * Runs everywhere
  * Cons:
    * Slow (for LZ4 standards; it still beats Deflate by a mile)

Plus class which chooses the best available implementation for the job: [One class to access them all] and [Performance Testing]

## Platform availability

| Platform | Implementations | Notes |
| --- | --- | --- |
| NET 2.0 | Safe | could be Unsafe as well, but I didn't bother |
| NET 4.0 | MixedMode, C++/CLI, Unsafe, Safe | does work on Mono as well |
| Portable | Unsafe, Safe | Windows Phone, Xamarin.*, Windows Store |
| Silverlight | Safe | anyone? |
