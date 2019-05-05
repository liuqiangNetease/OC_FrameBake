using System;

namespace OC.Stream
{
    public enum StreamAlgorithm
    {
        NoZip,
        GZip,
    }

    internal interface IStreamAlgorithm : IDisposable
    {
        StreamData Compress(StreamData data);
        StreamData Decompress(StreamData data);
    }

    internal class StreamAlgorithmFactory
    {
        public static IStreamAlgorithm CreateAlgorithm(StreamAlgorithm algorithm)
        {
            if (algorithm == StreamAlgorithm.GZip)
            {
                return new GZipAlgorithm();
            }

            return new NoZipAlgorithm();
        }
    }
}
