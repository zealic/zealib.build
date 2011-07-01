namespace Zealib.IO
{
    using System;
    using System.IO;
    using System.Security.Cryptography;

    public class Base64Stream : Stream
    {
        public enum Mode { Encode, Decode }

        private bool _isClosed, _disposed;
        private Mode _mode;
        private int _totalWritten, _totalFlushed, _outputLineLength;
        private Stream _captiveStream;
        private MemoryStream _buffer;
        private CryptoStream _innerStream;
        private static readonly int _BUFFER_THRESHOLD = 1024;

        public Base64Stream(Stream stream, Mode mode)
        {
            _captiveStream = stream;
            _mode = mode;
        }

        public int OutputLineLength
        {
            get { return _outputLineLength; }
            set { _outputLineLength = value; }
        }

        public bool Rfc2045Compliant
        {
            get { return (_outputLineLength == 76); }
            set { _outputLineLength = (value) ? 76 : 0; }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    if (disposing && (this._innerStream != null))
                    {
                        _innerStream.Flush();
                        _innerStream.Close(); // final transform
                        _isClosed = true;
                        HandleBufferedOutput(true);
                    }
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            if (_disposed) throw new ObjectDisposedException("Base64Stream");
            _innerStream.Flush();
            HandleBufferedOutput(true);
        }

        private void HandleBufferedOutput(bool flush)
        {
            if (!CanWrite) return;
            // are we buffering?
            if (_outputLineLength > 0)
            {
                // is the buffer is full? or are we closed?
                if (_isClosed || (_buffer.Length > _BUFFER_THRESHOLD) || (flush && _buffer.Length > 0))
                {
                    byte[] b = _buffer.ToArray();
                    int i;
                    for (i = 0; i + _outputLineLength <= b.Length; i += _outputLineLength)
                    {
                        _captiveStream.Write(b, i, _outputLineLength);
                        _captiveStream.WriteByte(13); //13==CR
                        _captiveStream.WriteByte(10); //10==LF
                        _totalFlushed += _outputLineLength;
                    }

                    if (!_isClosed)
                        _buffer.SetLength(0);
                    if (flush)
                    {
                        _captiveStream.Write(b, i, b.Length - i);
                        _totalFlushed += b.Length - i;
                    }
                    else
                        _buffer.Write(b, i, b.Length - i);
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed) throw new ObjectDisposedException("Base64Stream");

            if (_innerStream == null)
            {
                ICryptoTransform transform = (_mode == Mode.Decode)
                  ? (ICryptoTransform)new FromBase64Transform()
                  : (ICryptoTransform)new ToBase64Transform();

                _innerStream = new CryptoStream(_captiveStream, transform, CryptoStreamMode.Read);
            }

            return _innerStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed) throw new ObjectDisposedException("Base64Stream");

            if (_innerStream == null)
            {
                ICryptoTransform transform = (_mode == Mode.Decode)
                  ? (ICryptoTransform)new FromBase64Transform()
                  : (ICryptoTransform)new ToBase64Transform();

                Stream s = null;
                if (_outputLineLength > 0)
                {
                    _buffer = new MemoryStream();
                    s = _buffer;
                }
                else s = _captiveStream;

                _innerStream = new CryptoStream(s, transform, CryptoStreamMode.Write);
            }

            if (count == 0) return;

            _innerStream.Write(buffer, offset, count);
            _totalWritten += count;

            HandleBufferedOutput(false);
        }

        public override bool CanRead { get { return (_mode == Mode.Decode); } }

        public override bool CanWrite { get { return (_mode == Mode.Encode); } }

        public override bool CanSeek { get { return false; } }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override long Length { get { throw new NotSupportedException(); } }

        public override long Position
        {
            get { return _totalWritten; }
            set { throw new NotSupportedException(); }
        }
    }
}
