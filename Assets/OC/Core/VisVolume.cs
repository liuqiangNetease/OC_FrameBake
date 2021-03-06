﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OC.Profiler;
using UnityEngine;

namespace OC
{
    [Serializable]
    public class VisVolume
    {
        public SingleScene owner { get; private set; }

        public VisVolume(SingleScene owner)
        {
            this.owner = owner;
#if UNITY_EDITOR
            _curBakeCell = 0;
#endif
        }

        internal void Clear()
        {
            cellList = null;
        }

        private UnityEngine.Bounds _aabb;
        public UnityEngine.Bounds aabb
        {
            get { return _aabb; }
            set { _aabb = value; }
        }
        public List<Cell> cellList = new List<Cell>();

        private float cellSize;
        public float CellSize
        {
            get { return cellSize; }
            set { cellSize = value; }
        }

        public void AddCell(Cell cell)
        {
            cellList.Add(cell);
            if(owner.tree != null)
                owner.tree.Add(cell, cell.aabb);
            for (int i = 0; i < cell.children.Count; i++)
            {
                Cell child = cell.children[i];
                if (owner.tree != null)
                    owner.tree.Add(child, child.aabb);
            }
        }

      
        

        public void Load(OCDataReader reader)
        {
            cellSize = reader.ReadFloat();

            aabb = reader.ReadBounds();

            int len = reader.ReadInt();

            for (int i = 0; i < len; i++)
            {
                Cell cell = new Cell(this);
                cell.Load(reader);
                AddCell(cell);
            }
        }

#if UNITY_EDITOR
        private int _curBakeCell;

        public void GenerateCells()
        {
            int countX = Mathf.CeilToInt(aabb.size.x / cellSize);
            int countY = Mathf.CeilToInt(aabb.size.y / cellSize);
            int countZ = Mathf.CeilToInt(aabb.size.z / cellSize);

            for (int k = 0; k < countY; k++)
                for (int j = 0; j < countZ; j++)
                    for (int i = 0; i < countX; i++)
                    {
                        //int index = k * countZ * countX + j * countX + i;
                        Cell cell = new Cell(this);
                        //Vector3 center = new Vector3(i, k, j);
                        Vector3 center = new Vector3(cellSize * (i + 0.5f), cellSize * (k + 0.5f), cellSize * (j + 0.5f));
                        center += aabb.min;
                        Vector3 size = new Vector3(cellSize, cellSize, cellSize);
                        Bounds cellAABB = new Bounds(center, size);
                        cell.aabb = cellAABB;
                        //cellList.Add(cell);
                        AddCell(cell);
                    }
        }

        public bool GetRenderableModels(string progressTitle, Func<string, string, float, bool> progress = null)
        {
            bool cancelled = false;

            if (_curBakeCell >= cellList.Count)
                return false;

            int count = 0;

            //for (int i = cellList.Count-1; i>= 0; i--)
            for (int i = _curBakeCell; i < cellList.Count; i++)
            {
                if (Config.ComputePerframe)
                    if (count > Config.PerframeExecCount)
                    {
                        Debug.Log("batch mode Scene name:" + owner.Name + " current bake Cell count:" + _curBakeCell);
                        cancelled = true;
                        break;
                    }

                

                Cell cell = cellList[i];

                OCProfiler.Start();
                cell.GetRenderableModels();
                var oneCellTime = OCProfiler.Stop();

                if (progress != null)
                {
                    if (progress(progressTitle, string.Format("Cell {0}/{1} 正在生成PVS数据, OneCellTime:{2}", i, cellList.Count, oneCellTime), ((float)i) / cellList.Count))
                    {
                        cancelled = true;
                        owner.IsFinish = true;
                        break;
                    }
                }

                _curBakeCell++;
                count++;
            }

            return !cancelled;
        }

        public void MergeCells()
        {
            int beforeMerge = cellList.Count;
            for (int i = cellList.Count - 1; i >= 0; i--)
            {
                var from = cellList[i];

                if (!Config.IsBatchMode)
                    if (Util.Progress("生成ObjectId", String.Format("合并cell {0}/{1} ...", i + 1, cellList.Count), ((float)i + 1) / cellList.Count))
                        break;

                for (int j = i - 1; j >= 0; j--)
                {
                    var to = cellList[j];

                    if (MergeCell(from, to))
                        break;
                }
            }
            int afterMerge = cellList.Count;

            int mergeCount = beforeMerge - afterMerge;

            Debug.Log("batch mode Merge Cells count:" + mergeCount);
        }

        public bool MergeCell(Cell from, Cell to)
        {
            bool suc = false;
            if (Config.mergeCell)
            {
                var intersect = from.visibleModelList.Intersect(to.visibleModelList, new RenderableObjectListEquality()).ToList();
                //var except = from.visibleModelList.Except(to.visibleModelList, new RenderableObjectListEquality()).ToList();
                //int exCount = except.Count;
                var union = from.visibleModelList.Union(to.visibleModelList, new RenderableObjectListEquality()).ToList();

                int intersectCount = intersect.Count;
                int unionCount = union.Count;

                float scale = 1.0f;

                if (from.visibleModelList.Count > 0)
                    scale = (float)intersectCount / unionCount;

                if (scale > Config.CellWeight)
                {
                    var temp = new HashSet<RenderableObj>(union);

                    to.visibleModelList = temp;
                    to.AddChild(from);

                    cellList.Remove(from);

                    suc = true;
                }
            }
            return suc;
        }

        public void Save(OCDataWriter writer)
        {
            writer.Write(cellSize);

            writer.Write(aabb);

            writer.Write(cellList.Count);

            //foreach (var cell in cellList)
            //{
               // cell.Save(writer);
           // }
            for(int i= cellList.Count-1; i>=0; i--)
            {
                var cell = cellList[i];
                cell.Save(writer);
                //cellList.Remove(cell);
            }
        }
#endif
    }

}
