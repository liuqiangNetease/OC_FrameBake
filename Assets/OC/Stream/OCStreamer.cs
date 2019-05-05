using System;

namespace OC.Stream
{
   
    public class OCStreamer : IDisposable
    {
        private IStreamProcessor _streamProcessor;

        private StreamAlgorithm _algorithm;
        public StreamAlgorithm Algorithm
        {
            get { return _algorithm; }
        }
        
        public OCStreamer(bool multiThread = false, StreamAlgorithm algorithm = StreamAlgorithm.GZip)
        {
            _algorithm = algorithm;

            if (multiThread)
            {
                _streamProcessor = new StreamAsyncProcessor(algorithm);
            }
            else
            {
                _streamProcessor = new StreamSyncProcessor(algorithm);
            }
        }

        public StreamStats Stats
        {
            get { return _streamProcessor.Stats; }
        }

        public void Compress(byte[] data, int offset, int count, 
            Action<byte[], int> onComplete)
        {
            _streamProcessor.Process(StreamMode.Compress, new StreamData(data, offset, count), onComplete);
        }

        public void Decompress(byte[] data, int offset, int count,
            Action<byte[], int> onComplete)
        {
            _streamProcessor.Process(StreamMode.Decompress, new StreamData(data, offset, count), onComplete);
        }

        public void Close()
        {
            _streamProcessor.Dispose();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
