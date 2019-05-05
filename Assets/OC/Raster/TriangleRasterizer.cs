using UnityEngine;

namespace OC.Raster
{
    internal interface IRasterPolicyType
    {
        int MinX { get; }
        int MaxX { get; }
        int MinY { get; }
        int MaxY { get; }
        long TriangleIndex { set; }

        void ProcessPixel(int x, int y, Vector3 worldPosition);
    }

    //A generic 2d triangle rasterizer. It inherits a templated policy class (RasterPolicyType) and interpolates vertices according to its parameters.
    internal class TriangleRasterizer
    {

        private IRasterPolicyType _policyType;
        public TriangleRasterizer(IRasterPolicyType policyType)
        {
            _policyType = policyType;
        }

        public long TriangleIndex
        {
            set { _policyType.TriangleIndex = value; }
        }

        private Vector3[] _interpolants = new Vector3[3];
        private Vector2[] _points = new Vector2[3];
        public void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            _interpolants[0] = v0;
            _interpolants[1] = v1;
            _interpolants[2] = v2;

            _points[0] = p0;
            _points[1] = p1;
            _points[2] = p2;

            // Find the top point.

            if (_points[1].y < _points[0].y && _points[1].y <= _points[2].y)
            {
                Exchange(ref _points[0], ref _points[1]);
                Exchange(ref _interpolants[0], ref _interpolants[1]);
            }
            else if (_points[2].y < _points[0].y && _points[2].y <= _points[1].y)
            {
                Exchange(ref _points[0], ref _points[2]);
                Exchange(ref _interpolants[0], ref _interpolants[2]);
            }

            // Find the bottom point.

            if (_points[1].y > _points[2].y)
            {
                Exchange(ref _points[2], ref _points[1]);
                Exchange(ref _interpolants[2], ref _interpolants[1]);
            }

            // Calculate the edge gradients.

            float topMinDiffX = (_points[1].x - _points[0].x) / (_points[1].y - _points[0].y),
                topMaxDiffX = (_points[2].x - _points[0].x) / (_points[2].y - _points[0].y);
            Vector3 topMinDiffInterpolant = RasterVectorUtils.ScaleInv(RasterVectorUtils.Substract(_interpolants[1], _interpolants[0]), (_points[1].y - _points[0].y)),
                topMaxDiffInterpolant = RasterVectorUtils.ScaleInv(RasterVectorUtils.Substract(_interpolants[2], _interpolants[0]), (_points[2].y - _points[0].y));

            float bottomMinDiffX = (_points[2].x - _points[1].x) / (_points[2].y - _points[1].y),
                bottomMaxDiffX = (_points[2].x - _points[0].x) / (_points[2].y - _points[0].y);
            Vector3 bottomMinDiffInterpolant = RasterVectorUtils.ScaleInv(RasterVectorUtils.Substract(_interpolants[2], _interpolants[1]), (_points[2].y - _points[1].y)),
                bottomMaxDiffInterpolant = RasterVectorUtils.ScaleInv(RasterVectorUtils.Substract(_interpolants[2], _interpolants[0]), (_points[2].y - _points[0].y));

            DrawTriangleTrapezoid(
                _interpolants[0],
                topMinDiffInterpolant,
                _interpolants[0],
                topMaxDiffInterpolant,
                _points[0].x,
                topMinDiffX,
                _points[0].x,
                topMaxDiffX,
                _points[0].y,
                _points[1].y
            );

            DrawTriangleTrapezoid(
                _interpolants[1],
                bottomMinDiffInterpolant,
                RasterVectorUtils.Add(_interpolants[0], RasterVectorUtils.Scale(topMaxDiffInterpolant, (_points[1].y - _points[0].y))),
                bottomMaxDiffInterpolant,
                _points[1].x,
                bottomMinDiffX,
                _points[0].x + topMaxDiffX * (_points[1].y - _points[0].y),
                bottomMaxDiffX,
                _points[1].y,
                _points[2].y
            );
        }

        private void DrawTriangleTrapezoid(
            Vector3 topMinInterpolant,
            Vector3 deltaMinInterpolant,
            Vector3 topMaxInterpolant,
            Vector3 deltaMaxInterpolant,
            float topMinX,
            float deltaMinX,
            float topMaxX,
            float deltaMaxX,
            float minY,
            float maxY)
        {
           
            int intMinY = Mathf.Clamp(Mathf.CeilToInt(minY), _policyType.MinY, _policyType.MaxY + 1),
                intMaxY = Mathf.Clamp(Mathf.CeilToInt(maxY), _policyType.MinY, _policyType.MaxY + 1);

            for(int intY = intMinY;intY<intMaxY;intY++)
            {
                float y = intY - minY,
                    minX = topMinX + deltaMinX * y,
                    maxX = topMaxX + deltaMaxX * y;
                    Vector3 minInterpolant = RasterVectorUtils.Add(topMinInterpolant, RasterVectorUtils.Scale(deltaMinInterpolant, y)),
                    maxInterpolant = RasterVectorUtils.Add(topMaxInterpolant, RasterVectorUtils.Scale(deltaMaxInterpolant, y));

                if(minX > maxX)
                {
                    Exchange(ref minX, ref maxX);
                    Exchange(ref minInterpolant, ref maxInterpolant);
                }

                if(maxX > minX)
                {
                    int intMinX = Mathf.Clamp(Mathf.CeilToInt(minX), _policyType.MinX, _policyType.MaxX + 1),
                        intMaxX = Mathf.Clamp(Mathf.CeilToInt(maxX), _policyType.MinX, _policyType.MaxX + 1);
                    Vector3 deltaInterpolant = RasterVectorUtils.ScaleInv(RasterVectorUtils.Substract(maxInterpolant, minInterpolant), (maxX - minX));

                    for(int x = intMinX;x<intMaxX;x++)
                    {
                        _policyType.ProcessPixel(x, intY, minInterpolant + deltaInterpolant* (x - minX));
                    }
                }
            }
        }

        private void Exchange<T>(ref T t0, ref T t1) where T: struct
        {
            var t = t0;
            t0 = t1;
            t1 = t;
        }
    }
}
