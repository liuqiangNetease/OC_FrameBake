using OC.Profiler;
using System;

namespace OC.Stream
{
    public struct StreamStats
    {
        public StreamAlgorithm Algorithm;
        public int CompressTime;
        public int DecompressTime;

        public StreamStats(StreamAlgorithm algorithm)
        {
            Algorithm = algorithm;
            CompressTime = 0;
            DecompressTime = 0;
        }
    }

    internal struct StreamData
    {
        public byte[] Array;
        public int Offset;
        public int Count;

        public StreamData(byte[] array, int offset, int Count)
        {
            Array = array;
            Offset = offset;
            this.Count = Count;
        }

        public  static readonly StreamData Empty = new StreamData(new byte[0], 0, 0);
    }

    internal class StreamUnit : IDisposable
    {
        private IStreamAlgorithm _algorithm;
        private StreamStats _stats;
        public StreamStats Stats
        {
            get { return _stats; }
        }

        public volatile bool Busy;

        public StreamUnit(StreamAlgorithm algorithm)
        {
            _algorithm = StreamAlgorithmFactory.CreateAlgorithm(algorithm);
            _stats = new StreamStats(algorithm);
            Busy = false;
        }

        public StreamData Process(StreamMode mode, StreamData data)
        {
            try
            {
                //OCProfiler.Start();
                if (mode == StreamMode.Compress)
                    return Compress(data);
                else
                    return Decompress(data);
            }
            finally
            {
                //var elasped = (int) OCProfiler.Stop();

                if (mode == StreamMode.Compress)
                {
                    //_stats.CompressTime += elasped;
                }
                else
                {
                    //_stats.DecompressTime += elasped;
                }
            }
        }

        private StreamData Compress(StreamData data)
        {
            return _algorithm.Compress(data);
        }

        private StreamData Decompress(StreamData data)
        {
            return _algorithm.Decompress(data);
        }

        public void Dispose()
        {
            _algorithm.Dispose();
        }
    }
}
