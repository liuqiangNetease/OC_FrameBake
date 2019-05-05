using System;
using System.Collections;
using System.Collections.Generic;
using OC.Profiler;
using UnityEngine;

namespace OC
{

    public class Cell
    {
        public VisVolume owner { get; private set; }

        public Cell(VisVolume owner)
        {
            this.owner = owner;
        }

        ~Cell()
        {
            //Debug.Log("clear cell suc!");
        }
        public List<Cell> children = new List<Cell>();

        public Cell parent = null;

        private UnityEngine.Bounds _aabb;

        public UnityEngine.Bounds aabb
        {
            get { return _aabb; }
            set { _aabb = value; }
        }

        public HashSet<RenderableObj> visibleModelList = new HashSet<RenderableObj>();

        public Dictionary<Index, BitArray> visFlagDic = new Dictionary<Index, BitArray>();

        public void AddChild(Cell child)
        {
            child.visibleModelList.Clear();
            child.parent = this;
            children.Add(child);
            for (int i = 0; i < child.children.Count; i++)
                children.Add(child.children[i]);
            child.children.Clear();

        }

        public static void PrepareToGetRenderableModels()
        {
            if (Config.SoftRenderer)
                SoftRenderer.Instance.Prepare();
            else
                MinRenderer.Instance.Prepare();
        }

        public static void FinishToGetRenderableModels()
        {
            if (Config.SoftRenderer)
                SoftRenderer.Instance.Finish();
            else
                MinRenderer.Instance.Finish();
        }

        public void GetRenderableModels()
        {
            //OCProfiler.Start();
            MoveCameraPosition(aabb.min);//1(0,0,0)
            MoveCameraPosition(aabb.max);//8(1,1,1)
            MoveCameraPosition(aabb.center);//9
            MoveCameraPosition(new Vector3(aabb.max.x, aabb.min.y, aabb.min.z));//2 (1,0,0)
            MoveCameraPosition(new Vector3(aabb.min.x, aabb.max.y, aabb.min.z));//5 (0,1,0)
            MoveCameraPosition(new Vector3(aabb.min.x, aabb.min.y, aabb.max.z));//4  (0,0,1)
            MoveCameraPosition(new Vector3(aabb.max.x, aabb.min.y, aabb.max.z));//3  (1,0,1)
            MoveCameraPosition(new Vector3(aabb.min.x, aabb.max.y, aabb.max.z));//6  (0,1,1)
            MoveCameraPosition(new Vector3(aabb.max.x, aabb.max.y, aabb.min.z));//7  (1,1,0)
            //var renderTime = OCProfiler.Stop();

            //var stat = stat0.Add(stat1).Add(stat2).Add(stat3).Add(stat4).Add(stat5).Add(stat6).Add(stat7).Add(stat8);
            //stat.RenderTime = renderTime;
            //return stat;
        }

        private void MoveCameraPosition(Vector3 pos)
        {
            var bCache = Config.UseVisibleCache;
            HashSet<RenderableObj> cacheList = null;
            if (bCache)
            {

                HashSet<RenderableObj> ret = null;
                if (owner.owner.GetRenderObjList(pos, out ret))
                {
                    OCProfiler.Start();
                    foreach (var obj in ret)
                    {
                        bool bContains = visibleModelList.Contains(obj);
                        if (bContains == false)
                            visibleModelList.Add(obj);
                    }
                    var time = OCProfiler.Stop();
                    //return new CellVisRenderStat(0, time);
                    return;
                }

                cacheList = new HashSet<RenderableObj>();
            }

            Camera.main.transform.position = pos;
            //6 dir look and render 
            RotateCameraDir(Vector3.forward, cacheList);
            RotateCameraDir(Vector3.back, cacheList);
            RotateCameraDir(Vector3.up, cacheList);
            RotateCameraDir(Vector3.down, cacheList);
            RotateCameraDir(Vector3.left, cacheList);
            RotateCameraDir(Vector3.right, cacheList);

            if (bCache)
                owner.owner.AddCacheCellPosition(pos, cacheList);

            //return stat0.Add(stat1).Add(stat2).Add(stat3).Add(stat4).Add(stat5);
        }

        private void RotateCameraDir(Vector3 forward, HashSet<RenderableObj> ret)
        {
            Camera.main.transform.forward = forward;

            OCProfiler.Start();

            HashSet<MeshRenderer> visList = null;

            if (Config.SoftRenderer)
            {
                if (owner.owner.Owner == null)
                {
                    var renders = owner.owner.treeMesh.GetWithinFrustum(Camera.main);
                    Debug.Log("renders:" + renders.Count);
                    visList = SoftRenderer.Instance.GetVisibleModels(renders);
                }
                else
                {
                    var renders = owner.owner.Owner.treeMesh.GetWithinFrustum(Camera.main);
                    visList = SoftRenderer.Instance.GetVisibleModels(renders);
                }
            }
            else
            {
                visList = MinRenderer.Instance.GetVisibleModels();
            }
            var calcVisTime = OCProfiler.Stop();

            OCProfiler.Start();
            foreach (var mr in visList)
            {
                RenderableObj renderObj = null;
                if (owner.owner.Owner == null)
                    renderObj = owner.owner.GetRenderableObjectByMeshRenderer(mr);
                else
                {
                    MultiScene world = owner.owner.Owner as MultiScene;
                    renderObj = world.GetRenderableObjectByMeshRenderer(mr);
                }

                if (renderObj == null)
                {
                    Debug.LogError("renderObj is null!");
                    continue;
                }

                if (!visibleModelList.Contains(renderObj))
                    visibleModelList.Add(renderObj);

                if (ret != null && !ret.Contains(renderObj))
                    ret.Add(renderObj);
            }

            var updateVisTime = OCProfiler.Stop();
        }

        public void Load(OCDataReader reader)
        {
            _aabb.center = reader.ReadVector3();

            _aabb.size = new Vector3(owner.CellSize, owner.CellSize, owner.CellSize);

            if (owner.owner.Owner == null)
            {
                Byte[] datas = reader.ReadBytes(owner.owner.MaxGameObjectIDCount / 8);
                BitArray flag = new BitArray(datas);
                visFlagDic.Add(new Index(0, 0), flag);
            }
            else
            {               
                //Byte[] mainDatas = reader.ReadBytes(0);
                //BitArray mainFlag = new BitArray(mainDatas);
                //visFlagDic.Add(Index.InValidIndex, mainFlag);

                Index currentIndex = owner.owner.TileIndex;
                for (int i = -1; i < 2; i++)
                    for (int j = -1; j < 2; j++)
                    {
                        Index newIndex = currentIndex + new Index(i, j);
                        if (owner.owner.Owner.IsValidIndex(newIndex))
                        {
                            var multiScene = owner.owner.Owner as MultiScene;
                            var scene = multiScene.GetTile(newIndex) as SingleScene;

                            if (scene != null)
                            {
                                var maxGameObjectIdCount = owner.owner.GetNeighborSceneMaxObjectId(newIndex);
                                if (maxGameObjectIdCount > 0)
                                {
                                    Byte[] datas = reader.ReadBytes(maxGameObjectIdCount / 8);
                                    BitArray flag = new BitArray(datas);
                                    visFlagDic.Add(new Index(i, j), flag);
                                }
                            }
                        }
                    }
            }

            int childCount = reader.ReadInt();

            for (int i = 0; i < childCount; i++)
            {
                Cell cell = new Cell(owner);
                cell._aabb.center = reader.ReadVector3();
                cell._aabb.size = Vector3.one;
                AddChild(cell);
            }
        }

        public void Save(OCDataWriter writer)
        {
            writer.Write(aabb.center);

            //OCProfiler.Start();
            if (owner.owner.Owner == null)
            {
                var flag = new BitArray(owner.owner.MaxGameObjectIDCount);
                flag.SetAll(false);
                visFlagDic.Add(new Index(0, 0), flag);
            }
            else
            {
                //var mainFlag = new BitArray(0);
                //mainFlag.SetAll(false);
                //visFlagDic.Add(Index.InValidIndex, mainFlag);

                //
                Index currentIndex = owner.owner.TileIndex;

                for (int i = -1; i < 2; i++)
                    for (int j = -1; j < 2; j++)
                    {
                        Index newIndex = new Index(i, j) + currentIndex;

                        if (owner.owner.Owner.IsValidIndex(newIndex))
                        {
                            var multiScene = owner.owner.Owner as MultiScene;
                            var scene = multiScene.GetTile(newIndex) as SingleScene;
                            if (scene != null)
                            {
                                var flag = new BitArray(scene.MaxGameObjectIDCount);
                                flag.SetAll(false);
                                visFlagDic.Add(new Index(i, j), flag);
                            }
                        }
                    }
            }

            foreach (var obj in visibleModelList)
            {
                SingleScene scene = obj.Owner;
                if (scene.Owner != null)
                {
                    if (scene.Owner.IsValidIndex(scene.TileIndex))
                    {
                        int deltaX = scene.TileIndex.x - owner.owner.TileIndex.x;
                        int deltaY = scene.TileIndex.y - owner.owner.TileIndex.y;

                        visFlagDic[new Index(deltaX, deltaY)].Set(obj.GUID, true);
                    }
                    else
                    {
                        //main scene
                        //visFlagDic[Index.InValidIndex].Set(obj.GUID, true);
                    }
                }
                else
                {
                    visFlagDic[new Index(0, 0)].Set(obj.GUID, true);
                }
            }

            //var cellSaveInitTime = OCProfiler.Stop();

            //OCProfiler.Start();
            foreach (var bitArray in visFlagDic)
            {
                writer.Write(OC.Util.ConvertBitArray(bitArray.Value));
            }

            writer.Write(children.Count);

            foreach (var child in children)
            {
                writer.Write(child.aabb.center);
            }

            if (Config.ClearOnSave)
            {
                Clear();
            }
            //var cellSaveTime = OCProfiler.Stop();

           // Debug.LogFormat("Cell Init Time {0}, Save Time {1}", cellSaveInitTime, cellSaveTime);
        }

        public void Do()
        {
            foreach (var pair in visFlagDic)
            {
                var key = pair.Key;
                var visFlags = pair.Value;
                for (ushort i = 0; i < visFlags.Count; i++)
                {
                    RenderableObj go = null;
                    if (owner.owner.Owner == null)
                    {
                        go = owner.owner.GetRenderableObject(i);
                    }
                    else
                    {
                        if (key.Equals(Index.InValidIndex) == false)
                        {
                            Index selfIndex = owner.owner.TileIndex;

                            if (owner.owner.Owner.IsValidIndex(selfIndex))
                            {
                                Index targetIndex = Index.InValidIndex;
                                targetIndex.x = selfIndex.x + key.x;
                                targetIndex.y = selfIndex.y + key.y;

                                Tile tile = null;
                                owner.owner.Owner.tileMap.TryGetValue(targetIndex, out tile);

                                if (tile != null)
                                {
                                    SingleScene curScene = tile as SingleScene;
                                    go = curScene.GetRenderableObject(i);
                                }
                            }

                        }

                    }
                    if (go != null)
                    {
                        bool bVis = visFlags[i];
                        if (bVis == false)
                        {
                            //go.SetVisible(visFlags[i]);
                            owner.owner.AddDisabledRenderableObj(go);
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            visFlagDic = null;
            visibleModelList = null;
            children = null;
            parent = null;
            
        }
    }
}

