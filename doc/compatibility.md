## Compatibility

As many people is asking for this let me explain.
LZ4 comes in two modes: BLOCK and STREAM. At the time I was porting it only BLOCK mode was available, thus that was the only mode I've ported. On top of it I've implemented my own streaming format based on independent blocks. 
At the time STREAM mode was added the changes to original library were pretty massive. What made it hard to port was a lot of inlined functions which could not be inlined in .NET as there was no AggressiveInlining stratedy in .NET 4. Porting newer version would cause massive performance hit.

Long story short: block data is compatible with original (Yann's) LZ4 implementation, the block header is not. It would be possible to implement compatible stream but only in independent blocks mode. It was actually done by @UlyssesWu here: https://github.com/UlyssesWu/LZ4.Frame. Stream mode is out of reach for lz4net. 

## Good news!

As mentioned on front page:

> I'm in the process of porting 1.8.1 (latest stable @ 2018-02-01) to .NET Standard. It wont support .NET < 4.6 and will be "Unsafe" only. There is a lot of tricks and obscure pointer manipulations so I'm moving slowly and trying not to break things. Due to breaking changes it will be released as different nuget package. You can monitor development here: https://github.com/MiloszKrajewski/K4os.Compression.LZ4. Some things work already but it's a rocky road.

Actually, I have one bug to fix (unfortunatelly obscure one) before I can release alpha, which would fully compatible with LZ4 streams.

So, se you here: https://github.com/MiloszKrajewski/K4os.Compression.LZ4
