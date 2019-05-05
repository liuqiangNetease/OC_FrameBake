using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace OC.Stream
{
    internal class GZipAlgorithm : IStreamAlgorithm
    {
        private const int INIT_CAPACITY = 1024*1024;

        private byte[] _outBuffer;
        private MemoryStream _compStream;
        private MemoryStream _decompStream;

        public GZipAlgorithm()
        {
            _outBuffer = new byte[INIT_CAPACITY];
            _compStream = new MemoryStream(INIT_CAPACITY);
            _decompStream = new MemoryStream(INIT_CAPACITY);
        }

        public StreamData Compress(StreamData data)
        {
            _compStream.Position = 0;
            var zipStream = new GZipStream(_compStream, CompressionMode.Compress, true);
            zipStream.Write(data.Array, data.Offset, data.Count);
            zipStream.Close();

            return ReadStreamData(_compStream);
        }

        public StreamData Decompress(StreamData data)
        {
            _compStream.Position = 0;
            _compStream.Write(data.Array, data.Offset, data.Count);

            _compStream.Position = 0;
            var unzipStream = new GZipStream(_compStream, CompressionMode.Decompress, true);
            const int Size = INIT_CAPACITY;
            _decompStream.Position = 0;
            while (true)
            {
                int count = unzipStream.Read(_outBuffer, 0, Size);
                if (count == 0)
                    break;

                _decompStream.Write(_outBuffer, 0, count);
            }

            unzipStream.Close();

            return ReadStreamData(_decompStream);
        }

        public void Dispose()
        {
            _compStream.Close();
            _decompStream.Close();

            _outBuffer = null;
        }

        private StreamData ReadStreamData(MemoryStream memStream)
        {
            var position = (int)memStream.Position;
            if (_outBuffer.Length < position)
            {
                _outBuffer = new byte[position];
            }

            memStream.Position = 0;
            memStream.Read(_outBuffer, 0, position);
            return new StreamData(_outBuffer, 0, position);
        }
    }
}
