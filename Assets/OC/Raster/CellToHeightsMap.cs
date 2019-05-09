#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OC.Raster
{
    internal class HeightSpan
    {
      /** 
	 * Min and max world space heights of a single sample on a triangle. 
	 * This is necessary because supersampling is used during rasterization.
	 */
        public Vector2 Range;
    }

    internal class CellHeights
    {
        /** Last triangle index that rasterized to this cell. */
        public long TriangleIndex;
        /** World space X and Y position of this cell. */
        public Vector2 Position;
        /** Array of triangle hits on this cell. */
        public LinkedList<HeightSpan> HitTriangles = new LinkedList<HeightSpan>();
    }

    internal class CellToHeightsMap
    {
    
        /** The mapping data. */
        private List<CellHeights> _data;

        /** The width of the mapping data. */
        private int _sizeX;

        /** The height of the mapping data. */
        private int _sizeY;

        public int SizeX { get { return _sizeX; } }

        public int SizeY { get { return _sizeY; } }

        public CellToHeightsMap(int sizeX, int sizeY)
        {
            _sizeX = sizeX;
            _sizeY = sizeY;

            _data = new List<CellHeights>(sizeX * sizeY);
            for (int i = 0; i < sizeX * sizeY; ++i)
            {
                _data.Add(new CellHeights());
            }
        }

        public CellHeights this[int x, int y]
        {
            get
            {
                int texelIndex = y * _sizeX + x;

                return _data[texelIndex];
            }

            private set
            {
                int texelIndex = y * _sizeX + x;
                _data[texelIndex] = value;
            }
        }
    }
}
#endif
