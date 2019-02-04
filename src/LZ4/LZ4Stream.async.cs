#region license

/*
Copyright (c) 2013, Milosz Krajewski
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided 
that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions 
  and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
  and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED 
WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR 
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE 
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN 
IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

#endregion

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LZ4
{
    // ReSharper disable once PartialTypeWithSinglePart
    /// <summary>Block compression stream. Allows to use LZ4 for stream compression.</summary>
    public partial class LZ4Stream : Stream
    {
        #region utilities

        private async Task<ulong?> TryReadVarIntAsync()
        {
            var buffer = new byte[1];
            var count = 0;
            ulong result = 0;

            while (true)
            {
                if (await _innerStream.ReadAsync(buffer, 0, 1).ConfigureAwait(false) == 0)
                {
                    if (count == 0) return null;
                    throw EndOfStream();
                }
                var b = buffer[0];
                result = result + ((ulong)(b & 0x7F) << count);
                count += 7;
                if ((b & 0x80) == 0 || count >= 64) break;
            }

            return result;
        }

        private async Task<ulong> ReadVarIntAync()
        {
            var result = await TryReadVarIntAsync().ConfigureAwait(false);

            if (result == null)
            {
                throw EndOfStream();
            }

            return result.Value;
        }

        private async Task<int> ReadBlockAsync(byte[] buffer, int offset, int length)
        {
            var total = 0;

            while (length > 0)
            {
                var read = await _innerStream.ReadAsync(buffer, offset, length).ConfigureAwait(false);
                if (read == 0) break;
                offset += read;
                length -= read;
                total += read;
            }

            return total;
        }

        private async Task WriteVarIntAsync(ulong value)
        {
            var buffer = new byte[1];
            while (true)
            {
                var b = (byte)(value & 0x7F);
                value >>= 7;
                buffer[0] = (byte)(b | (value == 0 ? 0 : 0x80));
                await _innerStream.WriteAsync(buffer, 0, 1).ConfigureAwait(false);
                if (value == 0) break;
            }
        }

        private async Task FlushCurrentChunkAsync()
        {
            if (_bufferOffset <= 0) return;

            var compressed = new byte[_bufferOffset];
            var compressedLength = _highCompression
                ? LZ4Codec.EncodeHC(_buffer, 0, _bufferOffset, compressed, 0, _bufferOffset)
                : LZ4Codec.Encode(_buffer, 0, _bufferOffset, compressed, 0, _bufferOffset);

            if (compressedLength <= 0 || compressedLength >= _bufferOffset)
            {
                // incompressible block
                compressed = _buffer;
                compressedLength = _bufferOffset;
            }

            var isCompressed = compressedLength < _bufferOffset;

            var flags = ChunkFlags.None;

            if (isCompressed) flags |= ChunkFlags.Compressed;
            if (_highCompression) flags |= ChunkFlags.HighCompression;

            await WriteVarIntAsync((ulong)flags).ConfigureAwait(false);
            await WriteVarIntAsync((ulong)_bufferOffset).ConfigureAwait(false);
            if (isCompressed) await WriteVarIntAsync((ulong)compressedLength).ConfigureAwait(false);

            await _innerStream.WriteAsync(compressed, 0, compressedLength).ConfigureAwait(false);

            _bufferOffset = 0;
        }

        private async Task<bool> AcquireNextChunkAsync()
        {
            do
            {
                ulong? rawVarint = await TryReadVarIntAsync().ConfigureAwait(false);

                if (rawVarint == null)
                {
                    return false;
                }

                ulong varint = rawVarint.Value;

                var flags = (ChunkFlags)varint;
                var isCompressed = (flags & ChunkFlags.Compressed) != 0;

                var originalLength = (int)await ReadVarIntAync().ConfigureAwait(false);
                var compressedLength = isCompressed ? (int)await ReadVarIntAync().ConfigureAwait(false) : originalLength;
                if (compressedLength > originalLength) throw EndOfStream(); // corrupted

                var compressed = new byte[compressedLength];
                var chunk = await ReadBlockAsync(compressed, 0, compressedLength).ConfigureAwait(false);

                if (chunk != compressedLength) throw EndOfStream(); // corrupted

                if (!isCompressed)
                {
                    _buffer = compressed; // no compression on this chunk
                    _bufferLength = compressedLength;
                }
                else
                {
                    if (_buffer == null || _buffer.Length < originalLength)
                        _buffer = new byte[originalLength];
                    var passes = (int)flags >> 2;
                    if (passes != 0)
                        throw new NotSupportedException("Chunks with multiple passes are not supported.");
                    LZ4Codec.Decode(compressed, 0, compressedLength, _buffer, 0, originalLength, true);
                    _bufferLength = originalLength;
                }

                _bufferOffset = 0;
            } while (_bufferLength == 0); // skip empty block (shouldn't happen but...)

            return true;
        }

        #endregion

        #region overrides

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _bufferOffset > 0 && CanWrite ? FlushCurrentChunkAsync() : Task.CompletedTask;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanRead) throw NotSupported("Read");

            var total = 0;

            while (count > 0)
            {
                var chunk = Math.Min(count, _bufferLength - _bufferOffset);
                if (chunk > 0)
                {
                    Buffer.BlockCopy(_buffer, _bufferOffset, buffer, offset, chunk);
                    _bufferOffset += chunk;
                    total += chunk;
                    if (_interactiveRead) break;
                    offset += chunk;
                    count -= chunk;
                }
                else
                {
                    if (!await AcquireNextChunkAsync()) break;
                }
            }

            return total;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanWrite) throw NotSupported("Write");

            if (_buffer == null)
            {
                _buffer = new byte[_blockSize];
                _bufferLength = _blockSize;
                _bufferOffset = 0;
            }

            while (count > 0)
            {
                var chunk = Math.Min(count, _bufferLength - _bufferOffset);
                if (chunk > 0)
                {
                    Buffer.BlockCopy(buffer, offset, _buffer, _bufferOffset, chunk);
                    offset += chunk;
                    count -= chunk;
                    _bufferOffset += chunk;
                }
                else
                {
                    await FlushCurrentChunkAsync();
                }
            }
        }

        #endregion
    }
}
