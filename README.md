# FYI

I've just finished porting 1.8.1 (latest stable @ 2018-02-01) to .NET Standard. It does not support .NET < 4.6 and is Unsafe/64bit  only, but handles both BLOCK and STREAM modes. Due to breaking changes it is released as different nuget package. You can find it here: [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4).

**If you are on .NET Core, .NET 4.6+ or Xamarin I strongly recommend using K4os.Compression.LZ4**

# lz4net
**LZ4** - ultra fast compression algorithm - for all .NET platforms

LZ4 is lossless compression algorithm, sacrificing compression ratio for compression/decompression speed. Its compression speed is ~400 MB/s per core while decompression speed reaches ~2 GB/s, not far from RAM speed limits.

LZ4net brings LZ4 to all (most?) .NET platforms: .NET 2.0+, .NET 4.0+, .NET Core, Mono, Windows Phone, Xamarin.iOS, Xamarin.Android and Silverlight

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
    * Does not have AnyCPU configuration (this can be solved though, see: [Automatic loading of x86 or x64])
* **C++/CLI** - original C sources recompiled for CLR
  * Pros:
    * Almost as fast as Mixed Mode
    * Only managed code
  * Cons:
    * Contains unsafe code, may not be allowed in some environments
    * Does not have AnyCPU configuration (this can be solved though, see: [Automatic loading of x86 or x64])
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
| Portable | Unsafe, Safe | Windows Phone, Xamarin, Windows Store (1) |
| Silverlight | Safe | anyone? |
| .NET Standard 1.0 | Unsafe, Safe | be first person to try it (2)(3) |

* (1) It looks like .NET Standard is picked anyway on Xamarin, so the "portable" version may be obsolete.
* (2) Still experimental but seems to be working
* (3) I've tested it on Android 6.0 (Nexus 7) and Android 2.3.5 (ancient HTC Desire HD)

## Use with streams

This LZ4 library can be used in two distinctive ways: to compress streams and packets. Compressing streams follow decorator pattern: `LZ4Stream` is-a `Stream` and takes-a `Stream`. Let's start with some imports as text we are going to compress:

```csharp
using System;
using System.IO;
using LZ4;

const string LoremIpsum = 
    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sit amet mauris diam. " +
    "Mauris mollis tristique sollicitudin. Nunc nec lectus nec ipsum pharetra venenatis. " +
    "Fusce et consequat massa, eu vestibulum erat. Proin in lectus a lacus fermentum viverra. " +
    "Aliquam vel tellus aliquam, eleifend justo ultrices, elementum elit. " +
    "Donec sed ullamcorper ex, ac sagittis ligula. Pellentesque vel risus lacus. " +
    "Proin aliquet lectus et tellus tristique, eget tristique magna placerat. " +
    "Maecenas ut ipsum efficitur, lobortis mauris at, bibendum libero. " +
    "Curabitur ultricies rutrum velit, eget blandit lorem facilisis sit amet. " +
    "Nunc dignissim nunc iaculis diam congue tincidunt. Suspendisse et massa urna. " +
    "Aliquam sagittis ornare nisl, quis feugiat justo eleifend iaculis. " +
    "Ut pulvinar id purus non convallis.";
```

Now, we can write this text to compressed stream:

```csharp
static void WriteToStream() 
{
    using (var fileStream = new FileStream("lorem.lz4", FileMode.Create))
    using (var lz4Stream = new LZ4Stream(fileStream, LZ4StreamMode.Compress))
    using (var writer = new StreamWriter(lz4Stream))
    {
        for (var i = 0; i < 100; i++)
            writer.WriteLine(LoremIpsum);
    }
}
```

and read it back:

```csharp
static void ReadFromStream() 
{
    using (var fileStream = new FileStream("lorem.lz4", FileMode.Open))
    using (var lz4Stream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
    using (var reader = new StreamReader(lz4Stream))
    {
        string line;
        while ((line = reader.ReadLine()) != null) 
            Console.WriteLine(line);
    }
}
```

`LZ4Stream` constructor requires inner stream and compression mode, plus takes some optional arguments, but their defaults are relatively sane:

```csharp
LZ4Stream(
    Stream innerStream,
    LZ4StreamMode compressionMode,
    LZ4StreamFlags compressionFlags = LZ4StreamFlags.Default,
    int blockSize = 1024*1024);
```

where:

```csharp
enum LZ4StreamMode { 
    Compress, 
    Decompress 
};

[Flags] enum LZ4StreamFlags { 
    None, 
    InteractiveRead, 
    HighCompression, 
    IsolateInnerStream, 
    Default = None 
}
```

`compressionMode` configures `LZ4Stream` to either `Compress` or `Decompress`. `compressionFlags` is optional argument and allows to:

* use `HighCompression` mode, which provides better compression ratio for the price of performance. This is relevant on compression only.
* use `IsolateInnerStream` mode to leave inner stream open after disposing `LZ4Stream`.
* use `InteractiveRead` mode to read bytes as soon as they are available. This option may be useful when dealing with network stream, but not particularly useful with regular file streams.

`blockSize` is set 1MB by default but can be changed. Bigger `blockSize` allows better compression ratio, but uses more memory, stresses garbage collector and increases latency. It might be worth to experiment with it.

## Use with byte arrays

You can also compress byte arrays. It is useful when compressed chunks are relatively small and their size in known when compressing. `LZ4Codec.Wrap` compresses byte array and returns byte array:

```csharp
static string CompressBuffer()
{
    var text = Enumerable.Repeat(LoremIpsum, 5).Aggregate((a, b) => a + "\n" + b);

    var compressed = Convert.ToBase64String(
        LZ4Codec.Wrap(Encoding.UTF8.GetBytes(text)));

    return compressed;
}
```

In the example above we a little bit more, of course: first we concatenate multiple strings (`Enumerable.Repeat(...).Aggregate(...)`), then encode text as UTF8 (`Encoding.UTF8.GetBytes(...)`), then compress it (`LZ4Codec.Wrap(...)`) and then encode it with Base64 (`Convert.ToBase64String(...)`). On the end we have base64-encoded compressed string.

To decompress it we can use something like this:

```csharp
static string DecompressBuffer(string compressed)
{
    var lorems =
        Encoding.UTF8.GetString(
                LZ4Codec.Unwrap(Convert.FromBase64String(compressed)))
            .Split('\n');

    foreach (var lorem in lorems)
        Console.WriteLine(lorem);
}
```

Which is a reverse operation: decoding base64 string (`Convert.FromBase64String(...)`), decompression (`LZ4Codec.Unwrap(...)`), decoding UTF8 (`Encoding.UTF8.GetString(...)`) and splitting the string (`Split('\n')`).

## Compatibility

Both `LZ4Stream` and `LZ4Codec.Wrap` is not compatible with original LZ4. It is an outstanding task to implement compatible streaming protocol and, to be honest, it does not seem to be likely in nearest future, but...

If you want to do it yourself, you can. It requires a little bit more understanding though, so let's look at "low level" compression. Let's create some compressible data:

```charp
var inputBuffer = 
    Encoding.UTF8.GetBytes(
        Enumerable.Repeat(LoremIpsum, 5).Aggregate((a, b) => a + "\n" + b));
```

we also need to allocate buffer for compressed data. 
Please note, it might be actually more than input data (as not all data can be compressed):

```csharp
var inputLength = inputBuffer.Length;
var maximumLength = LZ4Codec.MaximumOutputLength(inputLength);
var outputBuffer = new byte[maximumLength];
```

Now, we can run actual compression:

```csharp
var outputLength = LZ4Codec.Encode(
    inputBuffer, 0, inputLength, 
    outputBuffer, 0, maximumLength);
```

`Encode` method returns number of bytes which were actually used. It might be less or equal to `maximumLength`. It me be also `0` (or less) to indicate that compression failed. This happens when provided buffer is too small.

Buffer compressed this way can be decompressed with: 

```csharp
LZ4Codec.Decode(
    inputBuffer, 0, inputLength, 
    outputBuffer, 0, outputLength, 
    true);
```

Last argument (`true`) indicates that we actually know output length. Alternatively we don't have to provide it, and use:

```csharp
var guessedOutputLength = inputLength * 10; // ???
var outputBuffer = new byte[guessedOutputLength];
var actualOutputLength = LZ4Codec.Decode(
    inputBuffer, 0, inputLength, 
    outputBuffer, 0, guessedOutputLength);
```

but this will require guessing outputBuffer size (`guessedOutputLength`) which might be quite inefficient.

**Buffers compressed this way are fully compatible with original implementation if LZ4.** 

Both `LZ4Stream` and `LZ4Codec.Wrap/Unwrap` use them internally.
