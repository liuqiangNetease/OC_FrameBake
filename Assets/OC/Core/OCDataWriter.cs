#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OC.Core;
using OC.Stream;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OC
{
    public class OCDataWriter : IDisposable
    {
        private MemoryStream _stream;
        private BinaryWriter _writer;
        private string _filePath;
        private FileStream _fileStream;

        private OCStreamer _ocStreamer;

		private int[,] _maxIDs;
        private int _dimension;
        private int _maxBlockCount;
        private int _blockCount;

        private int _originLength;
        private int _compressLength;
        public float CompressRatio
        {
            get { return ((float) _compressLength) / _originLength; }
        }

		public OCDataWriter(String filePath, int dimension = 1, int[,] maxIDs = null)
        {
            _stream = new MemoryStream(10 * 1024 * 1024);
            _writer = new BinaryWriter(_stream);
            _ocStreamer = new OCStreamer();

            _filePath = filePath;
            _fileStream = File.Open(filePath, FileMode.Create);

            _dimension = dimension;
            _maxBlockCount = dimension * dimension;
            _blockCount = 0;

            _maxIDs = maxIDs;

            WriteOCDataHeader();
        }

		

        private void WriteOCDataHeader()
        {
            _fileStream.Write(BitConverter.GetBytes(OCDataHeader.Magic), 0, 4);

            _fileStream.Write(BitConverter.GetBytes(_dimension), 0, 4);


            if (_maxIDs != null)
            {
                for (int i = 0; i < _dimension; i++)
                    for (int j = 0; j < _dimension; j++)
                        _fileStream.Write(BitConverter.GetBytes(_maxIDs[i, j]), 0, 4);
            }
			

            var count = _maxBlockCount * 8; //sizeof(OCDataBlock) == 8 

            for (int i = 0; i < count; ++i)
            {
                _fileStream.WriteByte(0);
            }

            _originLength = _compressLength = 8 + count;
        }

        private void FillOCDataBlock(int blockIndex, int length)
        {
            var curPos = _fileStream.Position;

			int blockPosition = 8 + blockIndex * 8;
	
			blockPosition = 8 + sizeof(int) * _dimension * _dimension + blockIndex * 8; //sizeof(OCDataBlock) == 8 

            _fileStream.Position = blockPosition;
            _fileStream.Write(BitConverter.GetBytes(curPos), 0, sizeof(int));
            _fileStream.Write(BitConverter.GetBytes(length), 0, sizeof(int));

            _fileStream.Position = curPos;
        }

        public void FillOCDataBlock(int blockIndex, byte[] data, int offset, int length)
        {
            FillOCDataBlock(blockIndex, length);
            if(data != null && length > 0)
                _fileStream.Write(data, offset, length);
        }

        public void Write(float v)
        {
            _writer.Write(v);
        }

        public void Write(int v)
        {
            _writer.Write(v);
        }

        public void Write(Vector3 v)
        {
            _writer.Write(v.x);
            _writer.Write(v.y);
            _writer.Write(v.z);
        }

        public void Write(Bounds v)
        {
            Write(v.center);
            Write(v.size);
        }

        public void Write(byte v)
        {
            _writer.Write(v);
        }

        public void Write(byte[] v)
        {
            _writer.Write(v);
        }

        public int Position
        {
            get { return (int) _stream.Position; }
            set { _stream.Position = value; }
        }

        public void BeginBlock()
        {
            _stream.Position = 0;
        }

        public void EndBlock()
        {
            _writer.Flush();

            var length = (int) _stream.Position;
            if (length > 0 && _blockCount < _maxBlockCount)
            {
                var buffer = _stream.GetBuffer();
                _ocStreamer.Compress(buffer, 0, length, OnCompressComplete);
                _originLength += length;
            }
        }

        private void OnCompressComplete(byte[] output, int length)
        {
            FillOCDataBlock(_blockCount, length);

            _fileStream.Write(output, 0, length);

            _stream.Position = 0;
            _blockCount++;

            _compressLength += length;

            Debug.LogFormat("Compress Data block {0} length {1}", _blockCount, length);
        }

        public void Close()
        {
            _writer.Close();
            _stream.Close();
            _fileStream.Close();
            _ocStreamer.Close();

#if UNITY_EDITOR
            AssetDatabase.ImportAsset(_filePath);
            var importer = AssetImporter.GetAtPath(_filePath);
            importer.SetAssetBundleNameAndVariant("OC", null);
#endif
        }

        public void Dispose()
        {
            Close();
        }
    }
}
#endif

