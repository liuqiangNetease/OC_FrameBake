using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using Core.Utils;
using OC.Profiler;
using OC.Raster;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArtPlugins;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace OC
{
    public class SingleScene : Tile
    {
        //private static LoggerAdapter _logger = new LoggerAdapter(typeof(SingleScene));
        private static readonly string OCDataFileSuffix = "_oc.txt";

        public BoundsOctree<OC.Cell> tree;
        public BoundsOctree<MeshRenderer> treeMesh;

        protected int _maxGameObjectIDCount = 4 * 8;

        private int[,] _neighborMaxObjIdTable;

        private Quaternion OldCameraRot;

        private Vector3 OldCameraPos;

        static string tempPath = "D:/OCTemp";

        public int GetNeighborSceneMaxObjectId(Index index)
        {
            return _neighborMaxObjIdTable == null ? 0 : _neighborMaxObjIdTable[index.x, index.y];
        }

        public int MaxGameObjectIDCount
        {
            get { return _maxGameObjectIDCount; }
        }

        private int curMultiTagID = 0;

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public SingleScene(string path, string name, World owner = null) :
            this(path, name, Index.InValidIndex, 1000, null, owner)
        {
            IsPrepared = false;
            IsFinish = false;

            curBakeVolume = 0;

            tree = null;
        }

        public SingleScene(string path, string name, Index index, int tileDimension, byte[] data, World owner) :
            base(index, tileDimension, data, owner)
        {
            IsPrepared = false;
            IsFinish = false;

            curBakeVolume = 0;

            Path = path;
            Name = name;

            renderableSet = new RenderableObjectSet(this);

            treeMesh = null;

            _neighborMaxObjIdTable = null;
        }

        private Dictionary<Vector3, HashSet<RenderableObj>> cellMap = new Dictionary<Vector3, HashSet<RenderableObj>>();

        public int CachVisiblePositionCount
        {
            get { return cellMap.Count; }
        }

        public void AddCacheCellPosition(Vector3 pos, HashSet<RenderableObj> objList)
        {
            cellMap.Add(pos, objList);
        }

        public bool GetRenderObjList(Vector3 pos, out HashSet<RenderableObj> ret)
        {
            return cellMap.TryGetValue(pos, out ret);
        }

        public List<VisVolume> volumelList = new List<VisVolume>();


        internal RenderableObjectSet renderableSet;


        #region disableobject
        public List<RenderableObj> disabledList = new List<RenderableObj>();

        public void AddDisabledRenderableObj(RenderableObj obj)
        {
            obj.SetVisible(false);
            disabledList.Add(obj);
        }

        public void UndoDisabledObjects()
        {
            foreach (var obj in disabledList)
            {
                obj.SetVisible(true);
            }

            disabledList.Clear();
        }
        #endregion


        public void DoCulling(Vector3 position)
        {
            UndoDisabledObjects();
            var cell = GetCurrentCell(position);
            if (cell != null)
                cell.Do();
        }


#if UNITY_EDITOR

        private bool IsPrepared;

        public bool IsFinish;

        private int curBakeVolume;

        internal bool Prepare()
        {
            EditorUtility.ClearProgressBar();
            bool ret = true;
            if (IsPrepared == false)
            {
                IsPrepared = true;
                StoreCamera();
                if (!GeneraterRenderableObjectID())
                {
                    return false;
                }
                Save();
                if (!ComputeVolumeCells())
                {
                    return false;
                }
                Cell.PrepareToGetRenderableModels();

            }
            return ret;
        }

        internal void Finish()
        {
            if (IsFinish)
            {
                Cell.FinishToGetRenderableModels();


                EditorApplication.update -= ComputePVS;
                RestoreCamera();

                SaveData();

                EditorUtility.ClearProgressBar();

                CopyOCDataTo(tempPath);
            }
        }

        private bool StreamComputePVS()
        {
            if (curBakeVolume >= volumelList.Count || IsFinish)
            {
                IsFinish = true;
                Finish();
                return true;
            }

            //OCProfiler.Start();
            for (int i = curBakeVolume; i < volumelList.Count; ++i)
            {
                var volume = volumelList[i];
                if (!volume.GetRenderableModels(String.Format("Volume {0}/{1} 正在生成PVS数据", i + 1, volumelList.Count),
                    Progress))
                {
                    //Finish();
                    break;
                }
                curBakeVolume++;
            }

            if (curBakeVolume >= volumelList.Count || IsFinish)
            {
                IsFinish = true;
                Finish();
                return true;
            }

            return IsFinish;
        }

        private void ComputePVS()
        {
            if (curBakeVolume >= volumelList.Count || IsFinish)
            {
                IsFinish = true;
                Finish();
                return;
            }

            //OCProfiler.Start();
            for (int i = curBakeVolume; i < volumelList.Count; ++i)
            {
                var volume = volumelList[i];
                if (!volume.GetRenderableModels(String.Format("Volume {0}/{1} 正在生成PVS数据", i + 1, volumelList.Count),
                    Progress))
                {
                    Finish();
                    break;
                }
                curBakeVolume++;
            }

            if (curBakeVolume >= volumelList.Count || IsFinish)
            {
                IsFinish = true;
                Finish();
                return;
            }
        }

        public override bool Bake(bool bFrame)
        {
            if(Prepare() == false)
            {
                Debug.Log("bake Prepare fail!");
                return false;
            }
            if(bFrame && Owner == null)
            {
                EditorApplication.update += ComputePVS;
            }
            else
            {
                ComputePVS();
                /*if (Owner == null)
                {
                    ComputePVS();
                    IsFinish = true;
                    Finish();
                }
                else
                {
                    StreamComputePVS();
                }*/

                
            }
            return IsFinish;
        }

        
       

        private void StoreCamera()
        {
            GameObject camera = null;
            if (Camera.main == null)
            {
                camera = new GameObject("OCCamera");
                camera.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            OldCameraPos = Camera.main.transform.position;
            OldCameraRot = Camera.main.transform.rotation;
        }

        private void RestoreCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.transform.position = OldCameraPos;
                Camera.main.transform.rotation = OldCameraRot;
            }
        }

        public void CopyOCDataTo(string temporaryContainer)
        {
            if (!Directory.Exists(temporaryContainer))
            {
                Directory.CreateDirectory(temporaryContainer);
            }

            var filePath = GetOCDataFilePath();
            var fileName = GetOCDataFileName(Name);
            var destFilePath = System.IO.Path.Combine(temporaryContainer, fileName);
            if (File.Exists(filePath))
            {
                if (File.Exists(destFilePath))
                {
                    File.Delete(destFilePath);
                }

                File.Copy(filePath, destFilePath);
            }
            else
            {
                Debug.LogErrorFormat("OC data file {0} does not exist!", filePath);
            }
            
        }

        private bool Progress(string strTitle, string strMessage, float fT)
        {
            return EditorUtility.DisplayCancelableProgressBar(strTitle, strMessage, fT);
        }


        private bool ComputeVolumeCells()
        {
            OCProfiler.Start();
            UnityEngine.SceneManagement.Scene curScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(Name);
            GameObject[] objs = curScene.GetRootGameObjects();

            var bounds = new Bounds();
            var colliderList = new List<Collider>();
            for (int i = 0; i < curScene.rootCount; i++)
            {
                GameObject obj = objs[i];

                var colliders = obj.GetComponentsInChildren<Collider>();
                foreach (var collider in colliders)
                {
                    if (IsStandableCollider(collider))
                    {
                        colliderList.Add(collider);

                        bounds.Encapsulate(collider.bounds);
                    }
                }
            }
            bounds.Expand(new Vector3(5.0f, 5.0f, 5.0f));

            if (Config.CustomVolume)
            {
                bounds.center = Config.CustomVolumeCenter;
                bounds.size = Config.CustomVolumeSize;
            }
            
            Debug.LogFormat("Total Raster Collider Count {0}", colliderList.Count);

            var success = true;
            if (Config.SimpleGenerateCell)
            {
                var visVolume = new VisVolume(this);
                visVolume.CellSize = Config.CellSize;
                visVolume.aabb = bounds;
                volumelList.Add(visVolume);

                GenerateVolumeCells();
               
            }
            else
            {
                var raster = new VolumeCellRaster(new RasterSettings(Config.CellSize, Config.MinPlayAreaHeight, Config.MaxPlayAreaHeight));
                raster.AddVolume(bounds.min, bounds.max);
                var cells = raster.ComputeVolumeCells(colliderList, Progress);
                
                if (cells != null)
                {
                    cells = ProprocessCells(cells);
                    var visVolume = new VisVolume(this);
                    visVolume.CellSize = Config.CellSize;
                    visVolume.aabb = bounds;
                    volumelList.Add(visVolume);

                    foreach (var cell in cells)
                    {
                        var c = new Cell(visVolume);
                        c.aabb = new Bounds(cell.Center, cell.Size);
                        visVolume.AddCell(c);
                    }
                }

                success = cells != null;
            }
            //BakeStat.RasterTime = OCProfiler.Stop();
            return success;
        }

        private List<VolumeCell> ProprocessCells(IList<VolumeCell> cells)
        {
            List<VolumeCell> ret = new List<VolumeCell>();
            foreach(var cell in cells)
            {
                var newCells = GenerateSquareCells(new Bounds(cell.Center, cell.Size));
                ret.AddRange(newCells);
            }
            return ret;
        }

        private List<VolumeCell> GenerateSquareCells(Bounds aabb)
        {
            float cellSize = Config.CellSize;

            List<VolumeCell> ret = new List<VolumeCell>();

            int countX = Mathf.CeilToInt(aabb.size.x / cellSize);
            int countY = Mathf.CeilToInt(aabb.size.y / cellSize);
            int countZ = Mathf.CeilToInt(aabb.size.z / cellSize);

            for (int k = 0; k < countY; k++)
                for (int j = 0; j < countZ; j++)
                    for (int i = 0; i < countX; i++)
                    {
                        Vector3 center = new Vector3(cellSize * (i + 0.5f), cellSize * (k + 0.5f), cellSize * (j + 0.5f));
                        center += aabb.min;                        
                        Vector3 size = new Vector3(cellSize, cellSize, cellSize);
                        
                        VolumeCell cell = new VolumeCell(center - size *0.5f, center + size * 0.5f);                        
                        ret.Add(cell);
                    }

            return ret;
            
        }

        private bool IsStandableCollider(Collider collider)
        {
            if (collider.isTrigger || !collider.enabled)
            {
                return false;
            }

            //var layerMask = 1 << collider.gameObject.layer;
            //return 0 != (layerMask & UnityLayers.SceneCollidableLayerMask);
            return true;
        }

        public void ProcessLastMultiTag(MultiTagBase tag)
        {
            List<MeshRenderer> renderers = new List<MeshRenderer>();

            GameObject go = tag.gameObject;

            var count = go.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = go.transform.GetChild(i);

                var renderer = child.GetComponent<MeshRenderer>();

                if (Util.IsValidOCRenderer(renderer))
                {  
                    List<MeshRenderer> lodRenderers = new List<MeshRenderer>();
                    if(IsLodMesh(renderer, lodRenderers))
                    {
                        foreach(var r in lodRenderers)
                        {
                            if(Util.IsValidOCRenderer(r))
                                Util.TryAdd(r, renderers);                           
                        }
                    }
                    else
                        Util.TryAdd(renderer, renderers);  
                }
            }

            if (renderers.Count > 0)
            {
                renderableSet.Add(renderers);
                tag.renderId = curMultiTagID;
                curMultiTagID++;
#if UNITY_EDITOR
                EditorUtility.SetDirty(tag);
#endif
            }

        }
        public void Traverse(GameObject go)
        {
            if (!go.activeSelf)
                return;

            var tag = go.GetComponent<MultiTagBase>();

            if (tag != null)
            {
                var childCount = go.transform.childCount;
                bool childHasEmbedPrefab = false;
                for (int i = 0; i < childCount; i++)
                {
                    var child = go.transform.GetChild(i);
                    if (child.GetComponent<MultiTagBase>() != null)
                    {
                        childHasEmbedPrefab = true;
                        break;
                    }
                }

                if (childHasEmbedPrefab == false)
                {
                    ProcessLastMultiTag(tag);
                    return;
                }
            }

            var count = go.transform.childCount;
            for (int i = 0; i < count; i++)
                Traverse(go.transform.GetChild(i).gameObject);
        }

        private List<MeshRenderer> GetSceneMeshes()
        {
            var meshList = new List<MeshRenderer>();

            //meshList.Clear();
            renderableSet.Clear();
            curMultiTagID = 0;
            UnityEngine.SceneManagement.Scene curScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(Name);
            Debug.LogFormat("Get OC RendererObjs for Scene {0}", name);
            GameObject[] objs = curScene.GetRootGameObjects();

            for (int i = 0; i < curScene.rootCount; i++)
            {
                GameObject obj = objs[i];

                if (!obj.activeInHierarchy || !obj.activeSelf)
                    continue;

                //重置ID
                MeshRenderer[] meshes = obj.GetComponentsInChildren<MeshRenderer>();
                for (int j = 0; j < meshes.Length; j++)
                {
                    var r = meshes[j];
                    if (r != null)
                    {
                        var com = r.gameObject.GetComponent<GameObjectID>();
                        if (com != null)
                            com.Reset();
                        else
                        {
                            if (Util.IsValidOCRenderer(r))
                            {
                                com = r.gameObject.AddComponent<GameObjectID>();
                                com.Reset();
                            }
                        }
                    }
                }

                //multiTag
                Traverse(obj);

                //lod meshes                
                var groups = obj.GetComponentsInChildren<LODGroup>();
                List<MeshRenderer> lodMeshes = new List<MeshRenderer>();
                for (int j = 0; j < groups.Length; j++)
                {
                    var group = groups[j];
                    lodMeshes.Clear();
                    var lods = group.GetLODs();
                    for (int k = 0; k < lods.Length; k++)
                    {
                        var lod = lods[k];
                        for (int l = 0; l < lod.renderers.Length; l++)
                        {
                            var mesh = lod.renderers[l] as MeshRenderer;
                            if (Util.IsValidOCRenderer(mesh))
                            {
                                //lodMeshes.Add(mesh);
                                Util.TryAdd(mesh, lodMeshes);
                            }
                        }
                    }
                    if (lodMeshes.Count > 0)
                        renderableSet.Add(lodMeshes);
                }

                //no lod group          
                for (int j = 0; j < meshes.Length; j++)
                {
                    MeshRenderer mr = meshes[j];

                    if (Util.IsValidOCRenderer(mr))
                    {
                        bool add = Util.TryAdd(mr, meshList);
                        if (add )
                            renderableSet.Add(mr);


                        if (Config.SoftRenderer && add)
                        {
                            if (Owner == null)
                                treeMesh.Add(mr, mr.bounds);
                            else
                                Owner.treeMesh.Add(mr, mr.bounds);
                        }

                    }
                }
            }

            return meshList;
        }

        private bool IsLodMesh(MeshRenderer mesh, List<MeshRenderer> lodRenderers)
        {
            bool ret = false;

            Transform parent = mesh.transform;
            while (parent != null)
            {
                LODGroup[] groups = parent.GetComponentsInChildren<LODGroup>();
                if (groups != null)
                {
                    for (int i = 0; i < groups.Length; i++)
                    {
                        var group = groups[i];
                        var lods = group.GetLODs();
                        for (int k = 0; k < lods.Length; k++)
                        {
                            var lod = lods[k];
                            for (int j = 0; j < lod.renderers.Length; j++)
                            {
                                MeshRenderer renderer = lod.renderers[j] as MeshRenderer;
                                if (mesh == renderer)
                                {
                                    ret = true;                                  
                                    //return ret;
                                }                               
                            }
                            if(ret)
                            {
                                foreach(var render in lod.renderers)
                                {
                                    var meshRenderer = render as MeshRenderer;
                                    if(meshRenderer!= null)
                                        lodRenderers.Add(meshRenderer);
                                }
                                return ret;
                            }
                        }
                    }
                }
                parent = parent.parent;
            }
            return ret;
        }

        private void ProcessAndMergeMeshRenders()
        {
            if (Config.IsBatchMode)
            {
                Debug.LogFormat("Generate Game Object Id");
            }
            else
            {
                Progress("生成ObjectId", "获取所有Mesh Render ...", 0.0f);
            }            

            var meshList = GetSceneMeshes();            
            
            if(Config.mergeObjectID == false)
                return;
      
            for (int i = 0; i < meshList.Count; i++)
            {
                var meshtobeMerged = meshList[i];
                RenderableObj obj = GetRenderableObjectByMeshRenderer(meshtobeMerged);

                if(!Config.IsBatchMode)
                    Progress("生成ObjectId", String.Format("合并Mesh Render {0}/{1} ...", i + 1, meshList.Count), ((float) i + 1) / meshList.Count);

                for (int j = i+1; j < meshList.Count; j++)
                {
                    var mesh = meshList[j];

                    RenderableObj obj2 = GetRenderableObjectByMeshRenderer(mesh);

                    if (MergeRenderableObjs(obj, obj2))
                        break;
                }
            }

            if (Config.IsBatchMode)
            {
                Debug.LogFormat("Delete Invalid Renderable Object.");
            }
            else
            {
                Progress("生成ObjectId", "删除无效对象", 0.0f);
            }

            renderableSet.RemoveEmptyRenerableObject();;
        }

        private bool MergeCondition(MeshRenderer from, MeshRenderer to)
        {
            bool ret = false;
            var fromPos = from.gameObject.transform.position;
            var toPos = to.gameObject.transform.position;
            var dis = (toPos - fromPos).sqrMagnitude;
            if (dis < Config.mergeObjectDistance * Config.mergeObjectDistance)
            {
                Vector3 boundFromSize = from.bounds.size;

                Vector3 boundToSize = to.bounds.size;

                if (boundFromSize.magnitude < Config.mergeObjectMaxSize &&
                    boundToSize.magnitude < Config.mergeObjectMaxSize)
                {
                    ret = true;
                }
            }
            return ret;
        }

        private bool MergeRenderableObjs(RenderableObj from, RenderableObj to)
        {
            bool ret = false;
            if (Config.mergeObjectID)
            {
                for (int i = 0; i < from.Count; i++)
                {
                    if (ret)
                        break;
                    for (int j = 0; j < to.Count; j++)
                    {
                        ret = MergeCondition(from[i], to[j]);
                        if (ret)
                            break;
                    }
                }

                if (ret)
                {
                    to.Add(from);
                    from.Clear();
                }
            }

            return ret;
        }

        public bool GeneraterRenderableObjectID()
        {
            //OCProfiler.Start();

            //meshList.Clear();
            renderableSet.Clear();

            ProcessAndMergeMeshRenders();

            ushort maxId = 0;
            var success = renderableSet.RecalcRenderableObjectGuid(out maxId, Progress);
            if (success)
            {
                _maxGameObjectIDCount = maxId;

                _maxGameObjectIDCount -= _maxGameObjectIDCount % 8;
                _maxGameObjectIDCount += 8;

                Debug.LogFormat("Scene {0} Max Id {1} ", Name, maxId);
            }

            //BakeStat.GenerateIdTime = OCProfiler.Stop();
            //Debug.LogFormat("Generate Id Time {0}", BakeStat.GenerateIdTime);

            return success;
        }

        public void AddVolume(VisVolume v)
        {
            volumelList.Add(v);
        }

#if UNITY_EDITOR
        private void Open( UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            if (!SceneManager.GetSceneByName(Name).isLoaded)
            {
                Debug.LogFormat("Open Scene {0}", Name);
                EditorSceneManager.OpenScene(String.Format("{0}/{1}.unity", Path, Name), mode);
            }
        }

        public override void Open()
        {
            Open(OpenSceneMode.Additive);
        }

        public override bool InitOnOpen()
        {
            return GeneraterRenderableObjectID();
        }

        public override void Close()
        {
            var scene = SceneManager.GetSceneByName(Name);
            if (scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

#endif
        public void GenerateVolumeCells()
        {
            for (int i = 0; i < volumelList.Count; i++)
            {
                volumelList[i].GenerateCells();
            }
        }

        private void WriteNeighborMaxGameObjId(OCDataWriter writer)
        {
            //write max game object id count of neighbor scenes
            if (Owner == null)
            {
                writer.Write(0);
            }
            else
            {
                int tileDimension = Owner.TileDimension;
                writer.Write(tileDimension);
                for (int x = 0; x < tileDimension; ++x)
                {
                    for (int y = 0; y < tileDimension; ++y)
                    {
                        var tile = Owner.ExistTile(new Index(x, y));
                        if (tile == null)
                        {
                            writer.Write(0);
                        }
                        else
                        {
                            writer.Write(((SingleScene)tile).MaxGameObjectIDCount);
                        }
                    }
                }
            }
        }

        public void Save()
        {
            Scene scene = SceneManager.GetSceneByName(Name);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public bool SaveData()
        {
            bool cancelled = false;
            using (var writer = new OCDataWriter(GetOCDataFilePath()))
            {
                writer.BeginBlock();

                WriteNeighborMaxGameObjId(writer);

                writer.Write(_maxGameObjectIDCount);

                writer.Write(volumelList.Count);

                for (int i = 0; i < volumelList.Count; ++i)
                {
                    if (Progress("保存PVS数据", String.Format("Volume {0}/{1} ...", i + 1, volumelList.Count),
                        ((float)i + 1) / volumelList.Count))
                    {
                        cancelled = true;
                        break;
                    }

                    var volume = volumelList[i];
                    volume.Save(writer);
                }

                writer.EndBlock();

                //BakeStat.CompressRatio = writer.CompressRatio;
                return !cancelled;
            }
        }

#endif

        public static string GetOCDataFileName(string sceneName)
        {
            return sceneName + OCDataFileSuffix;
        }

#if UNITY_EDITOR
        public override string GetOCDataFilePath()
        {
            string scenePath = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByName(Name).path;
            if (scenePath == null)
            {
                Debug.LogError("GetOCDataFilePath: scenePath is null! " + Name);
                return "";
            }
            return scenePath.Replace(".unity", OCDataFileSuffix);
        }
#else
        public override string GetOCDataFilePath()
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name.Equals(Name))
                {
                    return scene.path.Replace(".unity", OCDataFileSuffix);
                }
            }
            
            return String.Empty;
        }

#endif

        public override void Load(byte[] ocData, int blockIndex = 0)
        {
            using (var ocReader = new OCDataReader(ocData))
            {
                tree = new BoundsOctree<Cell>(10000, Vector3.zero, 8 * 2/*cellSize*/, 1.25f);
                LoadBlock(ocReader, blockIndex);
                GerneraterRenderableObjs();
            }

        }

#if UNITY_EDITOR
        public void TestLoad()
        {
            Open(OpenSceneMode.Single);
            using (var ocReader = new OCDataReader(GetOCDataFilePath()))
            {
                tree = new BoundsOctree<Cell>(10000, Vector3.zero, 8 * 2/*cellSize*/, 1.25f);
                LoadBlock(ocReader, 0);
                GerneraterRenderableObjs();
            }
        }
#endif

        public void Load()
        {
            using (var ocReader = new OCDataReader(GetOCDataFilePath()))
            {
                tree = new BoundsOctree<Cell>(10000, Vector3.zero, 8 * 2/*cellSize*/, 1.25f);
                LoadBlock(ocReader, 0);
                GerneraterRenderableObjs();
            }
        }

        private void LoadBlock(OCDataReader ocReader, int blockIndex)
        {
            if (ocReader.TrySetBlock(blockIndex))
            {
                LoadNeighborMaxGameObjId(ocReader);
                _maxGameObjectIDCount = ocReader.ReadInt();

                int len = ocReader.ReadInt();

                for (int i = 0; i < len; i++)
                {

                    VisVolume volume = new VisVolume(this);
                    volume.Load(ocReader);
                    volumelList.Add(volume);
                }
            }
        }

        private void LoadNeighborMaxGameObjId(OCDataReader ocReader)
        {
            var tileDimension = ocReader.ReadInt();
            if (tileDimension > 0)
            {
                _neighborMaxObjIdTable = new int[tileDimension, tileDimension];
                for (int x = 0; x < tileDimension; ++x)
                {
                    for (int y = 0; y < tileDimension; ++y)
                    {
                        _neighborMaxObjIdTable[x, y] = ocReader.ReadInt();
                    }
                }
            }
        }

        public override void Unload()
        {
            volumelList.Clear();
            renderableSet.Clear();
            cellMap.Clear();
            disabledList.Clear();
            tree = new BoundsOctree<Cell>(10000, Vector3.zero, 8 * 2/*cellSize*/, 1.25f);
        }

        public override void OnGameObjectLoad(GameObject go, int renderId)
        {
            if (renderId >= 0)
            {
                var meshRenders = go.GetComponentsInChildren<MeshRenderer>();

                foreach (var renderer in meshRenders)
                {
                    //if(IsOpaqueRenderer(renderer))
                        renderableSet.Add((ushort)renderId, renderer);
                }
            }
            else
            {
                Debug.LogWarningFormat("The render id for loaded gameobject {0} is invalid!", go.name);
            }
        }
        public override void OnGameObjectUnload(GameObject go, int renderId)
        {
            if (renderId >= 0)
            {
                var meshRenders = go.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in meshRenders)
                {
                    renderableSet.Remove(renderer);
                }
            }
            else
            {
                Debug.LogWarningFormat("The render id for unloading gameobject {0} is invalid!", go.name);
            }
        }

        public void GerneraterRenderableObjs()
        {
            renderableSet.Clear();
            //var coms = GameObject.FindObjectsOfType<GameObjectID>();
            Scene scene = SceneManager.GetSceneByName(Name);
            for (int k = 0; k < scene.rootCount; k++)
            {
                GameObject[] objs = scene.GetRootGameObjects();
                GameObject go = objs[k];
                var coms = go.GetComponentsInChildren<GameObjectID>();

                for (int i = 0; i < coms.Length; i++)
                {
                    var guid = coms[i].GUID;

                    renderableSet.Add(guid, coms[i].gameObject.GetComponent<MeshRenderer>());
                }
            }
           
        }
        public RenderableObj GetRenderableObject(ushort id)
        {
            return renderableSet.GetByGuid(id);
        }

        public RenderableObj GetRenderableObjectByMeshRenderer(MeshRenderer mesh)
        {
            return renderableSet.GetByMeshRenderer(mesh);
        }

        public Cell GetCurrentCell(Vector3 pos)
        {
            Cell cell = null;
            foreach (var visVolume in volumelList)
            {
                cell = visVolume.GetCell(pos);
                if(cell!= null)
                     break;
            }

            return cell;
        }
    }

}
