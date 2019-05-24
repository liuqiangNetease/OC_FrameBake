#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OC.Profiler;
using UnityEngine;

namespace OC.Raster
{
    public struct RasterSettings
    {
        public float CellSize;
        public float MinPlayAreaHeight;
        public float MaxPlayAreaHeight;

        public RasterSettings(float cellSize, float minPlayAreaHeight, float maxPlayAreaHeight)
        {
            CellSize = cellSize;
            MinPlayAreaHeight = minPlayAreaHeight;
            MaxPlayAreaHeight = maxPlayAreaHeight;
        }
    }

   
    public struct RasterStat
    {
        public int CellCount;
        public long TotalTime;
        public long RasterizeTime;
        public long Pass1Time;
        public long Pass2Time;

        public long TotalMemory;

        public void Reset()
        {
            CellCount = 0;
            TotalTime = 0;
            RasterizeTime = 0;
            Pass1Time = 0;
            Pass2Time = 0;
            TotalMemory = 0;
        }
    }

    public struct VolumeCell
    {
        public Vector3 BoundsMin;
        public Vector3 BoundsMax;

        public VolumeCell(Vector3 boundsMin, Vector3 boundsMax)
        {
            BoundsMin = boundsMin;
            BoundsMax = boundsMax;
        }

        public Vector3 Center
        {
            get { return RasterVectorUtils.Scale(RasterVectorUtils.Add(BoundsMin, BoundsMax), 0.5f); }
        }

        public Vector3 Extent
        {
            get { return RasterVectorUtils.Scale(RasterVectorUtils.Substract(BoundsMax, BoundsMin), 0.5f); }
        }

        public Vector3 Size
        {
            get { return RasterVectorUtils.Substract(BoundsMax, BoundsMin); }
        }
    }

    public class VolumeCellRaster
    {
        private RasterSettings _settings;
        private RasterVolumes _volumes;

        private RasterStat _stat;

        // Magic numbers for numerical precision.
        private static readonly float DELTA = 0.00001f;

        public VolumeCellRaster(RasterSettings settings)
        {
            _settings = settings;
            _volumes = new RasterVolumes();
        }

        public RasterStat Stat
        {
            get { return _stat; }
        }


        public void AddVolume(Vector3 boundsMin, Vector3 boundsMax)
        {
            _volumes.AddVolume(boundsMin, boundsMax);
        }

        public void AddVolume(Vector3 size, Vector3 center, Transform transform)
        {
            var x = Mathf.Abs(size.x) * 0.5f;
            var y = Mathf.Abs(size.y) * 0.5f;
            var z = Mathf.Abs(size.z) * 0.5f;

            var vertices = new Vector3[]
            {
                new Vector3(-x, -y, z),
                new Vector3(x, -y, z),
                new Vector3(-x, y, z),
                new Vector3(x, y, z),

                new Vector3(-x, -y, -z),
                new Vector3(x, -y, -z),
                new Vector3(-x, y, -z),
                new Vector3(x, y, -z)
            };

            var bmins = RasterVectorUtils.MaxVector3;
            var bmaxs = RasterVectorUtils.MinVector3;

            foreach (var v in vertices)
            {
                var worldVert = RasterVectorUtils.Add(center, transform.localToWorldMatrix.MultiplyPoint3x4(v));
                bmins = RasterVectorUtils.Min(bmins, worldVert);
                bmaxs = RasterVectorUtils.Max(bmaxs, worldVert);
            }

            AddVolume(bmins, bmaxs);
        }

        private static readonly Vector2[] _subsamplePositions = new Vector2[]
        {
            new Vector2(.5f, .5f),
            new Vector2(0, .5f),
            new Vector2(.5f, 0),
            new Vector2(1, .5f),
            new Vector2(.5f, 1),
            new Vector2(1, 1),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(0, 0)
        };

        private Vector3[] _triVertices = new Vector3[3];
        private Vector2[] _XYPositions = new Vector2[3];
        // Fractions of PrecomputedVisibilitySettings.PlayAreaHeight to guarantee have cell coverage
        //private static readonly float[] _testHeights = new float[]{ .4f, .6f, .8f };
        public IList<VolumeCell> ComputeVolumeCells(IEnumerable<Collider> allColliders, Func<string, string, float, bool> progress = null)
        {
            if (_volumes.GetRasterBoundCount() <= 0)
                return null;
          
            GC.Collect();

            _stat.Reset();
            OCProfiler.Start();
            var startMemory = GC.GetTotalMemory(false);

            var allMeshes = new LinkedList<IRasterMesh>();
            foreach (var collider in allColliders)
            {
                allMeshes.AddLast(RasterMeshFactory.CreateRasterMesh(collider));
            }
            
            var bounds = _volumes.GetVolumeBounds(_settings.CellSize);
            var boundsCenter = bounds.Center;
            var boundHalfExtent = bounds.HalfExtent;
            var volumeSizes = bounds.HalfExtent * 2.0f / _settings.CellSize;
            int sizeX = (int) (volumeSizes.x + DELTA) + 1;
            int sizeY = (int) (volumeSizes.z + DELTA) + 1;


            var heightsMap =  new CellToHeightsMap(sizeX, sizeY);
            var rasterPolicy = new CellPlacementRasterPolicy(_settings.CellSize, heightsMap, _volumes);
            TriangleRasterizer rasterizer = new TriangleRasterizer(rasterPolicy);

            bool cancelled = false;
            OCProfiler.Start();
            long nextTriangleIndex = 1;
            // Rasterize the scene to determine potential cell heights
            int meshCount = 0;

            Debug.LogFormat("Rasterize Mesh Total Mesh {0}", meshCount);
            foreach (var mesh in allMeshes)
            {
                meshCount++;
                if (!Config.IsBatchMode && progress != null)
                {
                    if (progress("体素化", String.Format("当前正在处理Mesh {0}/{1} ...", meshCount, allMeshes.Count),
                        ((float) meshCount) / allMeshes.Count))
                    {
                        cancelled = true;
                        break;
                    }
                        
                }

                var meshBounds = mesh.WorldBounds;
                // Only process meshes whose bounding box intersects a PVS volume
                if (_volumes.Intersect(meshBounds))
                {
                    var triCount = mesh.TriangleCount;

                    for (int triIndex = 0; triIndex < triCount; ++triIndex)
                    {
                        mesh.GetTriangle(triIndex, _triVertices);

                        var normal = TriangleNormal(_triVertices[0], _triVertices[1], _triVertices[2]);

                        const float EdgePullback = .1f;
                        // Only rasterize upward facing triangles
                        if (normal.y > 0.0f)
                        {
                            for (int vertIndex = 0; vertIndex < 3; vertIndex++)
                            {
                                // Transform world space positions from [PrecomputedVisibilityBounds.Origin - PrecomputedVisibilityBounds.BoxExtent, PrecomputedVisibilityBounds.Origin + PrecomputedVisibilityBounds.BoxExtent] into [0,1]
                                Vector3 transformedPosition;
                                var v = _triVertices[vertIndex];
                                transformedPosition.x =
                                    (v.x - boundsCenter.x + boundHalfExtent.x) / (2.0f * boundHalfExtent.x);
                                //transformedPosition.y =
                                 //   (v.y - boundsCenter.y + boundHalfExtent.y) / (2.0f * boundHalfExtent.y);
                                transformedPosition.z =
                                    (v.z - boundsCenter.z + boundHalfExtent.z) / (2.0f * boundHalfExtent.z);

                                // Project positions onto the XY plane
                                _XYPositions[vertIndex] = new Vector2(transformedPosition.x * (sizeX - 1), transformedPosition.z * (sizeY - 1));
                            }

                            rasterizer.TriangleIndex = nextTriangleIndex;

                            for (int sampleIndex = 0; sampleIndex < 9; sampleIndex++)
                            {
                                var samplePosition = RasterVectorUtils.Add(RasterVectorUtils.Scale(_subsamplePositions[sampleIndex], (1 - 2 * EdgePullback)), new Vector2(EdgePullback, EdgePullback));

                                rasterizer.DrawTriangle(
                                    _triVertices[0],
                                    _triVertices[1],
                                    _triVertices[2],
                                    RasterVectorUtils.Substract(_XYPositions[0], samplePosition),
                                    RasterVectorUtils.Substract(_XYPositions[1], samplePosition),
                                    RasterVectorUtils.Substract(_XYPositions[2], samplePosition)
                                );
                            }

                            nextTriangleIndex++;
                        }

                    }
                }
            }
            _stat.RasterizeTime = OCProfiler.Stop();

            if (cancelled)
                return null;

            var cells = new List<VolumeCell>(sizeX * sizeY * 2);
            var placedHeightRanges = new List<Vector2>();

            Debug.LogFormat("Calculate Cell Total Cells {0}", sizeX * sizeY);
            for (int y = 0; y < sizeY; ++y)
            {
                if (cancelled)
                {
                    break;
                }

                for (int x = 0; x < sizeX; ++x)
                {
                    if (!Config.IsBatchMode && progress != null)
                    {
                        if (progress("计算Cell区域",
                            String.Format("当前正在处理Cell {0}/{1} ...", x + y * sizeX + 1, sizeX * sizeY),
                            ((float) (x + y * sizeX + 1)) / (sizeX * sizeY)))
                        {
                            cancelled = true;
                            break;
                        }
                    }

                    var cell = heightsMap[x, y];
                    var currentPosition = cell.Position;

                    var sortedHitTriangles = cell.HitTriangles.OrderByDescending(span => -span.Range.y).ToList();
                    float lastSampleHeight = float.NegativeInfinity;
                    placedHeightRanges.Clear();

                    OCProfiler.Start();
                    int count = sortedHitTriangles.Count();
                    // Pass 1 - only place cells in the largest holes which are most likely to be where the play area is
                    // Place the bottom slightly above the surface, since cells that clip through the floor often have poor occlusion culling
                    for(int heightIndex = 0; heightIndex < count; heightIndex++)
                    {
                        float currentMaxHeight = sortedHitTriangles[heightIndex].Range.y;

                        // Place a new cell if this is the highest height
                        if (heightIndex + 1 == count
                            // Or if there's a gap above this height of size MinPlayAreaHeight
                            || ((sortedHitTriangles[heightIndex + 1].Range.y - currentMaxHeight) > _settings.MinPlayAreaHeight
                                // And this height is not within a cell that was just placed
                                && currentMaxHeight - lastSampleHeight > _settings.MinPlayAreaHeight))
                        {

                            var boundsMin = new Vector3(
                                currentPosition.x - _settings.CellSize * 0.5f,
                                currentMaxHeight,
                                currentPosition.y - _settings.CellSize * 0.5f);

                            var boundsMax = new Vector3(currentPosition.x + _settings.CellSize * 0.5f,
                                currentMaxHeight + _settings.MaxPlayAreaHeight,
                                currentPosition.y + _settings.CellSize * 0.5f);

                            cells.Add(new VolumeCell(boundsMin, boundsMax));
                            lastSampleHeight = currentMaxHeight;
                            placedHeightRanges.Add(new Vector2(boundsMin.y, boundsMax.y));
                        }
                    }

                    _stat.Pass1Time += OCProfiler.Stop();

                    OCProfiler.Start();
                    // Pass 2 - make sure the space above every triangle is covered by precomputed visibility cells, even if the cells are placed poorly (intersecting the floor)
                    /*for (int heightIndex = 0; heightIndex < count - 1; heightIndex++)
                    {
                        for (int extremaIndex = 0; extremaIndex < 2; extremaIndex++)
                        {
                            var currentMaxHeight = extremaIndex == 0 ? sortedHitTriangles[heightIndex].Range.x : sortedHitTriangles[heightIndex].Range.y;
                            var compareHeight = currentMaxHeight + .5f * _settings.MaxPlayAreaHeight;

                            for (int testIndex = 0; testIndex < 3; testIndex++)
                            {
                                var testHeight = currentMaxHeight + _testHeights[testIndex] * _settings.MaxPlayAreaHeight;

                                int closestCellInZIndex = -1;
                                float closestCellInZDistance = float.MaxValue;
                                bool bInsideCell = false;

                                for (int placedHeightIndex = 0; placedHeightIndex < placedHeightRanges.Count; placedHeightIndex++)
                                {
                                    var cellHeightRange = placedHeightRanges[placedHeightIndex];

                                    if (testHeight > cellHeightRange.x && testHeight < cellHeightRange.y)
                                    {
                                        bInsideCell = true;
                                        break;
                                    }

                                    float absDistance = Mathf.Min(Mathf.Abs(compareHeight - cellHeightRange.x), Mathf.Abs(compareHeight - cellHeightRange.y));

                                    if (absDistance < closestCellInZDistance)
                                    {
                                        closestCellInZDistance = absDistance;
                                        closestCellInZIndex = placedHeightIndex;
                                    }
                                }

                                // Place a cell if TestHeight was not inside any existing cells
                                if (!bInsideCell)
                                {
                                    float desiredCellBottom = currentMaxHeight;

                                    if (closestCellInZIndex >= 0)
                                    {
                                        var nearestCellHeightRange = placedHeightRanges[closestCellInZIndex];
                                        var nearestCellCompareHeight = (nearestCellHeightRange.x + nearestCellHeightRange.y) / 2;

                                        // Move the bottom of the cell to be placed such that it doesn't overlap the nearest cell
                                        // This makes use of the cell's full height to cover space
                                        if (compareHeight < nearestCellCompareHeight)
                                        {
                                            desiredCellBottom = Mathf.Min(desiredCellBottom, nearestCellHeightRange.x - _settings.MaxPlayAreaHeight);
                                        }
                                        else if (compareHeight > nearestCellCompareHeight)
                                        {
                                            desiredCellBottom = Mathf.Max(desiredCellBottom, nearestCellHeightRange.y);
                                        }
                                    }

                                    var boundsMin = new Vector3(
                                        currentPosition.x - _settings.CellSize * 0.5f,
                                        desiredCellBottom,
                                        currentPosition.y - _settings.CellSize * 0.5f);
                                    var boundsMax = new Vector3(
                                        currentPosition.x + _settings.CellSize * 0.5f,
                                        desiredCellBottom + _settings.MaxPlayAreaHeight,
                                        currentPosition.y + _settings.CellSize * 0.5f);

                                    cells.Add(new VolumeCell(boundsMin, boundsMax));
                                    placedHeightRanges.Add(new Vector2(boundsMin.y, boundsMax.y));
                                }
                            }
                        }
                    }*/

                    _stat.Pass2Time += OCProfiler.Stop();
                }
            }
            _stat.TotalTime = OCProfiler.Stop();

            if (cancelled)
                return null;

            GC.Collect();
            _stat.TotalMemory = GC.GetTotalMemory(false) - startMemory;
            return cells;
        }

        private Vector3 TriangleNormal(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var a = RasterVectorUtils.Substract(v1, v0);
            var b = RasterVectorUtils.Substract(v2, v0);

            return RasterVectorUtils.Cross(a, b);
        }


        public static void Draw(IList<VolumeCell> cells)
        {
            if (cells != null && cells.Count > 0)
            {
                Gizmos.color = Color.blue;
                foreach (var cell in cells)
                {
                    var center = cell.Center;
                    Gizmos.DrawWireCube(center, cell.Size);
                }
            }

            //Gizmos.color = Color.red;
            //Gizmos.DrawCube(new Vector3(255, 13, 250), new Vector3(10, 10, 10));
        }

        public static void Draw(OC.Cell cell)
        {
            Gizmos.color = Color.green;
            var center = cell.aabb.center;
            Gizmos.DrawWireCube(center, cell.aabb.size);
        }
    }
}
#endif
