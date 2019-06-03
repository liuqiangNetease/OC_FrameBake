using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core.Utils;
using OC.Core;
using OC.Stream;
using UnityEngine;

namespace OC
{

    public class OCDataReader : IDisposable
    {
        //private LoggerAdapter _logger = new LoggerAdapter(typeof(OCDataReader));

        private OCDataHeader _dataHeader;

        private byte[] _data;

        private MemoryStream _stream;
        private BinaryReader _reader;
        private OCStreamer _ocStreamer;

        private OCDataReader()
        {
            _stream = new MemoryStream(10 * 1024 * 1024);
            _reader = new BinaryReader(_stream);
            _ocStreamer = new OCStreamer();
        }

        public OCDataReader(byte[] data) : this()
        {
            _data = data;

            ReadOCDataHeader();
        }


        public OCDataReader(String fileName) : this()
        {
            FileStream fileStream = null;
            try
            {
                fileStream = File.Open(fileName, FileMode.Open);

                var length = fileStream.Length;
                _data = new byte[length];

                if (fileStream.Read(_data, 0, (int) length) != length)
                {
                    //_logger.ErrorFormat("Read oc data from {0} Error", fileName);
                    _data = new byte[0];
                }

                ReadOCDataHeader();
            }
            catch (Exception e)
            {
                //_logger.Error(String.Format("Can not read oc data from {0}", fileName), e);
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
            }
        }

        private void ReadOCDataHeader()
        {
            bool success = false;
            if (_data.Length >= 8)
            {
                var magic = BitConverter.ToInt32(_data, 0);
                if (magic == OCDataHeader.Magic)
                {
                    var dimension = BitConverter.ToInt32(_data, 4);

                    if (dimension > 1)
                    {
                        for (int i = 0; i < dimension * dimension; i++)
                            BitConverter.ToInt32(_data, 4 * i + 8);
                    }
                    
                    _dataHeader = new OCDataHeader(dimension);

                    for (int blockIndex = 0; blockIndex < dimension * dimension; ++blockIndex)
                    {
                        var block = new OCDataBlock();
                        
                        block.Offset = BitConverter.ToInt32(_data, 8 * blockIndex + 8);
                        block.Length = BitConverter.ToInt32(_data, 8 * blockIndex + 12);
                    
                      
                        _dataHeader[blockIndex] = block;
                    }

                    success = true;
                }
            }

            if (!success)
            {
                //_logger.ErrorFormat("OC Data Format Error!");
                _dataHeader = new OCDataHeader(0);
            }

        }

        public bool TrySetBlock(int blockIndex)
        {
            var block = _dataHeader[blockIndex];
            if (block.Length > 0)
            {
                _ocStreamer.Decompress(_data, block.Offset, block.Length, OnDecompressComplete);
                return true;
            }
           
            return false;
        }

        public OCDataBlock GetDataBlock(int blockIndex)
        {
            return _dataHeader[blockIndex];
        }

        private void OnDecompressComplete(byte[] output, int length)
        {
            _stream.Position = 0;
            _stream.Write(output, 0,  length);
            _stream.Position = 0;
        }

        public float ReadFloat()
        {
            return _reader.ReadSingle();
        }

        public int ReadInt()
        {
            return _reader.ReadInt32();
        }

        public Vector3 ReadVector3()
        {
            Vector3 res;
            res.x = _reader.ReadSingle();
            res.y = _reader.ReadSingle();
            res.z = _reader.ReadSingle();
            return res;
        }

        public Bounds ReadBounds()
        {
            Bounds res = new Bounds();
            res.center = ReadVector3();
            res.size = ReadVector3();
            return res;
        }

        public byte ReadByte()
        {
            return _reader.ReadByte();
        }

        public byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
        }

        public void Close()
        {
            _reader.Close();
            _stream.Close();
            _ocStreamer.Close();
        }

        public void Dispose()
        {
            
        }
    }
}

