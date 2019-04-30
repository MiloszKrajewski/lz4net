## Compatibility

As many people are asking for this let me explain.
LZ4 comes in two modes: BLOCK and STREAM. At the time I was porting it only BLOCK mode was available, thus that was the only mode I've ported. On top of it I've implemented my own streaming format based on independent blocks. 
At the time STREAM mode was added the changes to original library were pretty massive. What made it hard to port was a lot of inlined functions which could not be inlined in .NET as there was no AggressiveInlining strategy in .NET 4. Porting newer version at this point in time would cause massive performance hit.

Long story short: block data is compatible with original (Yann's) LZ4 implementation, the block header is not. It would be possible to implement compatible stream but only in independent blocks mode. It was actually done by @UlyssesWu here: https://github.com/UlyssesWu/LZ4.Frame. Stream mode is out of reach for lz4net. 

## Good news!

Due to breaking changes new port of LZ4 is developed in separate repo: https://github.com/MiloszKrajewski/K4os.Compression.LZ4.

Everything is working now and I can release alpha, which is fully compatible with LZ4 streams: BLOCK and STREAM modes including dependent blocks. What I need is to write some documentation and do the formal release to nuget.

So, [**Happy birthday K4os.Compression.LZ4 1.0.0-alpha!**](https://github.com/MiloszKrajewski/K4os.Compression.LZ4)
