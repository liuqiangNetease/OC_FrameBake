using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OC.Raster
{
    internal class CellPlacementRasterPolicy : IRasterPolicyType
    {
        private readonly float _cellSize;
        private CellToHeightsMap _heightsMap;
        private readonly RasterVolumes _volumes;

        private readonly Vector3 _volumeCenter;
        private readonly Vector3 _volumeHalfExtent;
        private long _triangleIndex;

        public long TriangleIndex
        {
            set { _triangleIndex = value; }
        }

        public CellPlacementRasterPolicy(float cellSize, CellToHeightsMap heightMap, RasterVolumes volumes)
        {
            _cellSize = cellSize;
            _heightsMap = heightMap;
            _volumes = volumes;

            var volBounds = volumes.GetVolumeBounds(cellSize);
            _volumeCenter = volBounds.Center;
            _volumeHalfExtent = volBounds.HalfExtent;
        }

        public int MaxX
        {
            get { return _heightsMap.SizeX; }
        }

        public int MaxY
        {
            get { return _heightsMap.SizeY; }
        }

        public int MinX
        {
            get { return 0; }
        }

        public int MinY
        {
            get { return 0; }
        }

        public void ProcessPixel(int x, int y, Vector3 worldPosition)
        {
            if (_volumes.IsInVolume(worldPosition))
            {
                var cell = _heightsMap[x, y];
                if (cell.TriangleIndex != _triangleIndex)
                {
                    // If this is the first hit on this cell from the current triangle, add a new sample
                    HeightSpan span = new HeightSpan();
                    var gridPosition = RasterVectorUtils.Add(RasterVectorUtils.Substract(_volumeCenter, _volumeHalfExtent), RasterVectorUtils.Scale(new Vector3(x, 0, y), _cellSize));
                    span.Range = new Vector2(worldPosition.y, worldPosition.y);
                    cell.HitTriangles.AddLast(span);
                    cell.Position = new Vector2(gridPosition.x + _cellSize * 0.5f, gridPosition.z + _cellSize * 0.5f);
                    cell.TriangleIndex = _triangleIndex;
                }
                else
                {
                    // If this is not the first hit on this cell from the current triangle, expand the sample min and max
                    var span = cell.HitTriangles.Last.Value;
                    span.Range.x = Mathf.Min(span.Range.x, worldPosition.y);
                    span.Range.y = Mathf.Max(span.Range.y, worldPosition.y);
                }
            }
        }
    }
}
