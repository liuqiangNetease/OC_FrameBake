using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OC.Stream
{
    internal class NoZipAlgorithm : IStreamAlgorithm
    {
        public StreamData Compress(StreamData data)
        {
            return data;
        }

        public StreamData Decompress(StreamData data)
        {
            return data;
        }

        public void Dispose()
        {
            
        }
    }
}
