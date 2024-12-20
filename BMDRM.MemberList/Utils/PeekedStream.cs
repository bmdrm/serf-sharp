using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMDRM.MemberList.Utils
{
    /// <summary>
    /// Helper class to handle peeked data in a stream.
    /// </summary>
    internal class PeekedStream : Stream
    {
        private readonly byte[] _peekedData;
        private int _peekedPosition;
        private readonly Stream _baseStream;

        public PeekedStream(Stream baseStream, byte[] peekedData)
        {
            _peekedData = peekedData;
            _peekedPosition = 0;
            _baseStream = baseStream;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_peekedPosition < _peekedData.Length)
            {
                var bytesToCopy = Math.Min(count, _peekedData.Length - _peekedPosition);
                Array.Copy(_peekedData, _peekedPosition, buffer, offset, bytesToCopy);
                _peekedPosition += bytesToCopy;
                return bytesToCopy;
            }

            return _baseStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_peekedPosition < _peekedData.Length)
            {
                var bytesToCopy = Math.Min(count, _peekedData.Length - _peekedPosition);
                Array.Copy(_peekedData, _peekedPosition, buffer, offset, bytesToCopy);
                _peekedPosition += bytesToCopy;
                return bytesToCopy;
            }

            return await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_peekedPosition < _peekedData.Length)
            {
                var bytesToCopy = Math.Min(buffer.Length, _peekedData.Length - _peekedPosition);
                _peekedData.AsMemory(_peekedPosition, bytesToCopy).CopyTo(buffer);
                _peekedPosition += bytesToCopy;
                return bytesToCopy;
            }

            return await _baseStream.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
