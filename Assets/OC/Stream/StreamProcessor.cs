using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;


namespace OC.Stream
{
    internal enum StreamMode
    {
        Compress,
        Decompress
    }

    internal interface IStreamProcessor : IDisposable
    {
        StreamStats Stats { get; }
        void Process(StreamMode mode, StreamData data, Action<byte[], int> callback);
    }

    internal class AsyncStreamRequest
    {
        public StreamMode Mode;
        public StreamData Data;
        public Action<byte[], int> Callback;

        public AsyncStreamRequest(StreamMode mode, StreamData data, Action<byte[], int> callback)
        {
            Mode = mode;
            Data = data;
            Callback = callback;
        }
    }

    internal class StreamTask
    {
        private StreamUnit _streamUnit;
        private AsyncStreamRequest _request;

        public StreamTask(StreamUnit streamUnit, AsyncStreamRequest request)
        {
            _streamUnit = streamUnit;
            _request = request;
            _streamUnit.Busy = true;
        }

        public void Run()
        {
            try
            {
                var output = _streamUnit.Process(_request.Mode, _request.Data);
                _request.Callback(output.Array, output.Count);

            }
            finally
            {
                _streamUnit.Busy = false;
            }
        }
    }

    internal class StreamAsyncProcessor : IStreamProcessor
    {
        private List<StreamUnit> _unitPool = new List<StreamUnit>();

        private StreamAlgorithm _algorithm;
        private bool _disposed;
        public StreamAsyncProcessor(StreamAlgorithm algorithm)
        {
            _algorithm = algorithm;
            for (int i = 0; i < 9; ++i)
            {
                _unitPool.Add(new StreamUnit(algorithm));
            }
        }

        public StreamStats Stats
        {
            get
            {
                var stats = new StreamStats();
                foreach (var unit in _unitPool)
                {
                    var thStats = unit.Stats;
                    stats.CompressTime += thStats.CompressTime;
                    stats.DecompressTime += thStats.DecompressTime;
                }

                return stats;
            }
        }

        public void Process(StreamMode mode, StreamData data, Action<byte[], int> callback)
        {
            if (!_disposed)
            {
                var request = new AsyncStreamRequest(mode, data, callback);
                var task = new StreamTask(GetIdleUnit(), request);
                ThreadPool.QueueUserWorkItem(_ => { task.Run(); });
            }
        }

        private StreamUnit GetIdleUnit()
        {
            int count = _unitPool.Count;
            for (int i = 0; i < count; ++i)
            {
                var unit = _unitPool[i];
                if (!unit.Busy)
                {
                    return unit;
                }
            }

            var newUnit = new StreamUnit(_algorithm);
            _unitPool.Add(newUnit);

            return newUnit;
        }

        public void Dispose()
        {
            _disposed = true;

            //wait until all units is idle
            foreach (var unit in _unitPool)
            {
                while(unit.Busy)
                    Thread.Sleep(100);
            }

            foreach (var unit in _unitPool)
            {
                unit.Dispose();
            }
        }
    }

    internal class StreamSyncProcessor : IStreamProcessor
    {
        private StreamUnit _streamUnit;

        public StreamSyncProcessor(StreamAlgorithm algorithm)
        {
            _streamUnit = new StreamUnit(algorithm);
        }

        public StreamStats Stats
        {
            get { return _streamUnit.Stats; }
        }

        public void Process(StreamMode mode, StreamData data, Action<byte[], int> callback)
        {
            var output = _streamUnit.Process(mode, data);
            callback(output.Array, output.Count);
        }

        public void Dispose()
        {
            _streamUnit.Dispose();
        }
    }
}
