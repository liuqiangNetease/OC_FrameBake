#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OC.Raster
{
    internal struct RasterBounds
    {
        public static readonly RasterBounds ZeroBounds = new RasterBounds(Vector3.zero, Vector3.zero);
        public static readonly RasterBounds NegativeBounds = new RasterBounds(RasterVectorUtils.MaxVector3, RasterVectorUtils.MinVector3);

        private readonly Vector3 _boundsMin;
        private readonly Vector3 _boundsMax;

        public Vector3 BoundsMin
        {
            get { return _boundsMin; }
        }

        public Vector3 BoundsMax
        {
            get { return _boundsMax; }
        }

        public RasterBounds(Vector3 boundsMin, Vector3 boundsMax)
        {
            _boundsMin = boundsMin;
            _boundsMax = boundsMax;
        }

        internal RasterBounds Scale(float s)
        {
            var boundsMin = RasterVectorUtils.Scale(_boundsMin, s);
            var boundsMax = RasterVectorUtils.Scale(_boundsMax, s);

            return new RasterBounds(boundsMin, boundsMax);
        }

        public Vector3 Center
        {
            get
            {
                float x = (_boundsMin.x + _boundsMax.x) * 0.5f;
                float y = (_boundsMin.y + _boundsMax.y) * 0.5f;
                float z = (_boundsMin.z + _boundsMax.z) * 0.5f;

                return  new Vector3(x, y, z);
            }
        }

        public Vector3 HalfExtent
        {
            get
            {
                float x = (_boundsMax.x - _boundsMin.x) * 0.5f;
                float y = (_boundsMax.y - _boundsMin.y) * 0.5f;
                float z = (_boundsMax.z - _boundsMin.z) * 0.5f;

                return new Vector3(x, y, z);
            }
        }

        public bool Inbounds(Vector3 p)
        {
            return p.x >= _boundsMin.x && p.x <= _boundsMax.x &&
                   p.y >= _boundsMin.y && p.y <= _boundsMax.y &&
                   p.z >= _boundsMin.z && p.z <= _boundsMax.z;
        }

        public bool Intersect(RasterBounds bounds)
        {
            return Intersect(bounds._boundsMin, bounds._boundsMax);
        }

        private bool Intersect(Vector3 boundsMin, Vector3 boundsMax)
        {
            if ((_boundsMin.x > boundsMax.x) || (boundsMin.x > _boundsMax.x))
            {
                return false;
            }

            if ((_boundsMin.y > boundsMax.y) || (boundsMin.y > _boundsMax.y))
            {
                return false;
            }

            if ((_boundsMin.z > boundsMax.z) || (boundsMin.z > _boundsMax.z))
            {
                return false;
            }

            return true;
        }

    public static bool operator ==(RasterBounds one, RasterBounds other)
        {
            return one._boundsMin.Equals(other._boundsMin) && one._boundsMax.Equals(other._boundsMax);
        }

        public static bool operator !=(RasterBounds one, RasterBounds other)
        {
            return !(one == other);
        }

        public override int GetHashCode()
        {
            return _boundsMin.GetHashCode() * 397 + _boundsMax.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (o is RasterBounds)
            {
                var b = (RasterBounds) o;
                return this == b;
            }

            return false;
        }

        public static RasterBounds operator+(RasterBounds one, RasterBounds other)
        {
            Vector3 boundsMin, boundsMax;
            boundsMin.x = Mathf.Min(one._boundsMin.x, other._boundsMin.x);
            boundsMin.y = Mathf.Min(one._boundsMin.y, other._boundsMin.y);
            boundsMin.z = Mathf.Min(one._boundsMin.z, other._boundsMin.z);

            boundsMax.x = Mathf.Max(one._boundsMax.x, other._boundsMax.x);
            boundsMax.y = Mathf.Max(one._boundsMax.y, other._boundsMax.y);
            boundsMax.z = Mathf.Max(one._boundsMax.z, other._boundsMax.z);
            return new RasterBounds(boundsMin, boundsMax);
        }
    }

    internal class RasterVolumes
    {
        private LinkedList<RasterBounds> _volumes;

        public RasterVolumes()
        {
            _volumes = new LinkedList<RasterBounds>();
        }

        public void AddVolume(Vector3 boundsMin, Vector3 boundsMax)
        {
            _volumes.AddLast(new RasterBounds(boundsMin, boundsMax));
        }

        public RasterBounds GetVolumeBounds(float cellSize)
        {
            RasterBounds b = RasterBounds.NegativeBounds;
            foreach (var vol in _volumes)
            {
                b += vol;
            }

            // Round the max up to the next cell boundary
            if (_volumes.Count > 0)
            {
                var extents = b.HalfExtent * 2.0f;
                extents.x = extents.x - (extents.x % cellSize) + cellSize;
                extents.z = extents.z - (extents.z % cellSize) + cellSize;

                var bmaxs = RasterVectorUtils.Add(b.BoundsMin, extents);

                return new RasterBounds(b.BoundsMin, bmaxs);
            }

            return RasterBounds.ZeroBounds;
        }

        
        public bool IsInVolume(Vector3 p)
        {
            foreach (var vol in _volumes)
            {
                if (vol.Inbounds(p))
                    return true;
            }

            return false;
        }

        public bool Intersect(RasterBounds bounds)
        {
            foreach (var vol in _volumes)
            {
                if (vol.Intersect(bounds))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
