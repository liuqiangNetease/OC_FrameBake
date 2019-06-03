using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Utils;

namespace OC.Core
{
    public struct OCDataBlock
    {
        public int Offset;
        public int Length;

        public static OCDataBlock Empty = new OCDataBlock()
        {
            Offset =  0,
            Length =  0,
        };
    }

    public class OCDataHeader
    {
        public static int Magic = 0x7070;

        private OCDataBlock[] _blocks;

        public int _dimension;
        public OCDataHeader(int dimension)
        {
            _dimension = dimension;
            _blocks = new OCDataBlock[dimension * dimension];
        }

        public int Count
        {
            get { return _blocks.Length; }
        }

        public OCDataBlock this[int blockIndex]
        {
           get
           {
               if (blockIndex >= 0 && blockIndex < _blocks.Length)
               {
                   return _blocks[blockIndex];
               }

               return OCDataBlock.Empty;
           }
            set
            {
               // AssertUtility.Assert(blockIndex >= 0 && blockIndex < _dimension * _dimension);
                _blocks[blockIndex] = value;
            }
        }
        
    }
}
