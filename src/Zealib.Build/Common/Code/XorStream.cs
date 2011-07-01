namespace Zealib.IO
{
    using System;
    using System.IO;

    public class XorStream : Stream
    {
        private int m_FactorIndex;

        public XorStream(Stream baseStream, byte[] factors)
        {
            BaseStream = baseStream;
            Factors = new byte[factors.Length];
            Array.Copy(factors, Factors, factors.Length);
        }

        public byte[] Factors { get; private set; }

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
