using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OC.Raster;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Rasterizer
{
    public class VolumeCellRasterExample : MonoBehaviour
    {
        public float CellSize = 10.0f;
        public float MinPlayAreaHeight = 1.0f;
        public float MaxPlayAreaHeight = 10.0f;

        public BoxCollider[] Volumes = new BoxCollider[0];

        private IList<VolumeCell> _cells = new List<VolumeCell>();
        List<OC.Cell> cellList = new List<OC.Cell>();
        void Start()
        {
            ComputeVolumeCells();
        }

        [ContextMenu("Compute Volume Cells")]
        void ComputeVolumeCells()
        {
            var scene = SceneManager.GetActiveScene();
            var colliderList = new LinkedList<Collider>();

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if(!root.gameObject.activeSelf)
                    continue;

                var colliders = root.GetComponentsInChildren<Collider>();
                foreach (var collider in colliders)
                {
                    if (!collider.isTrigger && collider.GetComponent<VolumeCellRasterExample>() == null && collider.enabled && collider.gameObject.activeSelf)
                    {
                        colliderList.AddLast(collider);
                    }
                }
            }

            Debug.LogFormat("Collider Count {0}", colliderList.Count);
            var raster = new VolumeCellRaster(new RasterSettings(CellSize, MinPlayAreaHeight, MaxPlayAreaHeight));

            foreach (var vol in Volumes)
            {
                raster.AddVolume(vol.size, vol.center, vol.transform);
            }

            _cells = raster.ComputeVolumeCells(colliderList);

            for (int i = 0; i < _cells.Count; ++i)
            {
                var cell = _cells[i];
                GenerateCells(new Bounds(cell.Center, cell.Size), CellSize);
                Debug.LogFormat("{0} Min {1} Max {2} Size{3}", i, cell.BoundsMin, cell.BoundsMax, cell.Size);
            }
        }

        public void GenerateCells(Bounds aabb, float cellSize)
        {
            int countX = Mathf.CeilToInt(aabb.size.x / cellSize);
            int countY = Mathf.CeilToInt(aabb.size.y / cellSize);
            int countZ = Mathf.CeilToInt(aabb.size.z / cellSize);

            for (int k = 0; k < countY; k++)
                for (int j = 0; j < countZ; j++)
                    for (int i = 0; i < countX; i++)
                    {
                        OC.Cell cell = new OC.Cell(null);
                        Vector3 center = new Vector3(cellSize * (i + 0.5f), cellSize * (k + 0.5f), cellSize * (j + 0.5f));
                        center += aabb.min;
                        Vector3 size = new Vector3(cellSize, cellSize, cellSize);
                        Bounds cellAABB = new Bounds(center, size);
                        cell.aabb = cellAABB;
                        cellList.Add(cell);
                    }
           
        }

        

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            VolumeCellRaster.Draw(_cells);
            foreach (var cell in cellList)
                VolumeCellRaster.Draw(cell);
        }
#endif

    }
}
