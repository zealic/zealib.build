namespace Zealib.IO
{
    using System;
    using System.IO;

    public class XorStream : Stream
    {
        private int m_FactorIndex;
        private readonly byte[] _Factors;

        public XorStream(Stream baseStream, byte[] factors)
        {
            if (baseStream == null) throw new ArgumentNullException("baseStream");
            if (factors == null) throw new ArgumentNullException("factors");
            BaseStream = baseStream;
            _Factors = (byte[])factors.Clone();
        }

        public byte[] Factors { get { return (byte[])_Factors.Clone(); } }

        public Stream BaseStream { get; private set; }

        private void ProcessData(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < offset + count; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ Factors[m_FactorIndex % Factors.Length]);
                m_FactorIndex++;
            }
        }

        public override bool CanRead
        {
            get { return BaseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return BaseStream.CanWrite; }
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override long Length
        {
            get { return BaseStream.Length; }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var reads = BaseStream.Read(buffer, offset, count);
            if (reads > 0) ProcessData(buffer, offset, reads);
            return reads;
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
            ProcessData(buffer, offset, count);
            BaseStream.Write(buffer, offset, count);
        }
    }
}
