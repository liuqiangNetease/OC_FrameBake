using OC.Core;
using System;
using System.Collections.Generic;
using System.IO;
//using ArtPlugins;
//using OC.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;


namespace OC
{

    public class MultiScene: World
    {
#if UNITY_EDITOR
        int curBakeScene;
        List<Index> bakeTiles;
        Window _window;
        string tempPath;
#endif

        private string _namePattern;
        public string NamePattern
        {
            get { return _namePattern; }
            set { _namePattern = value; }
        }

        private string _path;

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        private Dictionary<string, Index> _sceneName2Index = new Dictionary<string, Index>();

        private int _tileDimension;
        public override  int TileDimension
        {
            get { return _tileDimension; }
        }

        private int _tileSize;
        private byte[] _data;

        private int[,] _maxIDs;

        public void AddMaxGUID(int tileX, int tileY, int maxID)
        {
            if (IsValidIndex(new Index(tileX, tileY)))
                _maxIDs[tileX, tileY] = maxID;
        }

        public int GetMaxGUID(int tileX, int tileY)
        {
            if (IsValidIndex(new Index(tileX, tileY)))
                return _maxIDs[tileX, tileY];

            return SingleScene.InvalidID;
        }

        public void Save()
        {
            using (BinaryWriter w = new BinaryWriter(File.Open(Path + NamePattern, FileMode.Create)))
            {
                Writer writer = new Writer(w);
                //w.Write(NamePattern);
                //w.Write(TileDimension);
                //w.Write(_tileSize);
                
                for(int i=0; i< TileDimension; i++)
                    for(int j =0; j< TileDimension; j++)
                    {
                        var guid = _maxIDs[i, j];
                        w.Write(guid);
                    }
            }
        }

        public void Load()
        {
            using (BinaryReader reader = new BinaryReader(File.Open(Path + NamePattern, FileMode.Open)))
            {
                Reader r = new Reader(reader);

                for (int i = 0; i < TileDimension; i++)
                    for (int j = 0; j < TileDimension; j++)
                    {              
                        _maxIDs[i, j] = r.ReadInt(); 
                    }
            }
        }

        public MultiScene(string path, string namePattern, int tileDimension, int tileSize, byte[] data = null)
        {
#if UNITY_EDITOR
            curBakeScene = 0;
            _window = null;

            treeMesh = null;
            if(Config.SoftRenderer)
                treeMesh = new BoundsOctree<MeshRenderer>();
#endif
            _maxIDs = new int[tileDimension, tileDimension];
            for (int i = 0; i < TileDimension; i++)
                for (int j = 0; j < TileDimension; j++)
                {
                    _maxIDs[i, j] = SingleScene.InvalidID;                   
                }

            Path = path;
            NamePattern = namePattern;

            _tileDimension = tileDimension;
            _tileSize = tileSize;
            _data = data;
            

            SetWorldLimits(0, _tileDimension * _tileSize, 0, _tileDimension * tileSize, _tileDimension, _tileDimension);

            for (int x = 0; x < tileDimension; ++x)
            {
                for (int y = 0; y < tileDimension; ++y)
                {
                    var sceneName = GetSceneName(x, y);
                    _sceneName2Index.Add(sceneName, new Index(x, y));
                }
            }
        }

        public Cell GetCurrentCell(Vector3 pos)
        {
            Cell cell = null;

            if (cell == null)
            {
                foreach (var tile in tileMap)
                {
                    SingleScene scene = tile.Value as SingleScene;
                    cell = scene.GetCurrentCell(pos);
                    if (cell != null)
                        break;
                }
            }

            return cell;
        }
        public void DoCulling(Vector3 position)
        {
            UpdateLoadCallbacks();
       
            var cell = GetCurrentCell(position);
            if (cell != null)
            {
                cell.Do();
            }
        }

        private void UpdateLoadCallbacks()
        {

        }

        public bool Load(int x, int y, Action<int, int> OnPVSLoaded = null)
        {

            Tile tile;
            string sceneName = null;
            if (!tileMap.TryGetValue(new Index(x, y), out tile))
            {
                sceneName = GetSceneName(x, y);
                var index = new Index(x, y);
                tile = new SingleScene(Path, sceneName, index, _data, this);
                tileMap.Add(index, tile);
            }

            if (sceneName == null)
                sceneName = ((SingleScene)tile).Name;
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                return false;
            }

            tile.DoLoad();

            if (OnPVSLoaded != null)
            {
                OnPVSLoaded(x, y);
            }

            return true;
        }

        public string GetSceneName(int x, int y)
        {
            //return GetSceneNameOfPattern(NamePattern, x, y);
            return string.Format(NamePattern, x, y);
        }

        public void Unload(int x, int y)
        {
            Tile tile;
            if (tileMap.TryGetValue(new Index(x, y), out tile))
            {
                tile.DoUnload();
            }
        }


        public void OnGameObjectLoad(GameObject go)
        {
            var multitag = go.GetComponent<MultiTagBase>();
            if (multitag != null)
            {
                int renderId = multitag.renderId;

                var sceneName = multitag.sceneName;// go.scene.name;
                Index index;
                if (_sceneName2Index.TryGetValue(sceneName, out index))
                {
                    Tile tile;
                    if (tileMap.TryGetValue(index, out tile))
                    {
                        tile.OnGameObjectLoad(go, renderId);
                    }
                }
            }
        }

        public void OnGameObjectUnload(GameObject go)
        {
            var multitag = go.GetComponent<MultiTagBase>();
            if (multitag != null)
            {
                int renderId = multitag.renderId;

                var sceneName = multitag.sceneName;// go.scene.name;
                Index index;
                if (_sceneName2Index.TryGetValue(sceneName, out index))
                {
                    Tile tile;
                    if (tileMap.TryGetValue(index, out tile))
                    {
                        tile.OnGameObjectUnload(go, renderId);
                    }
                }
            }
        }
        public void UndoCulling()
        {
            foreach (var pair in tileMap)
            {
                var scene = pair.Value as SingleScene;
                scene.UndoCulling();
            }
        }

        //----------------------------
#if UNITY_EDITOR
        public RenderableObj GetRenderableObjectByMeshRenderer(MeshRenderer mesh)
        {
            RenderableObj ret = null;
            foreach (var pair in tileMap)
            {
                SingleScene singleScene = pair.Value as SingleScene;
                var temp = singleScene.GetRenderableObjectByMeshRenderer(mesh);
                if (temp != null)
                {
                    ret = temp;
                    break;
                }
            }
            return ret;
        }
        public RenderableObj GetRenderableObject(int id)
        {
            RenderableObj ret = null;

            if (ret == null)
            {
                foreach (var pair in tileMap)
                {
                    SingleScene tile = pair.Value as SingleScene;
                    ret = tile.GetRuntimeOCObject(id);
                    if (ret != null)
                        break;
                }
            }
            return ret;
        }

         public override Tile BuildTile(Index index)
        {
            string sceneName = GetSceneName(index.x, index.y);
            var ret = new SingleScene(Path, sceneName, index, _data, this);
            return ret;
        }

        public void MergeOCData(string temporaryContainer)
        {
            var allSceneNames = new List<string>();
            for (int x = 0; x < _tileDimension; ++x)
            {
                for (int y = 0; y < _tileDimension; ++y)
                {
                    allSceneNames.Add(String.Format(_namePattern, x, y));
                }
            }

            var files = Directory.GetFiles(temporaryContainer);

            var filePathList = new List<string>();
            foreach (var f in files)
            {
                foreach (var sceneName in allSceneNames)
                {
                    if (f.Contains(sceneName) && !f.EndsWith(Config.OCPatchFileSuffix))
                    {
                        filePathList.Add(f);
                    }
                }
            }

            MergeOCData(filePathList);
        }

        public void CopyOCDataTo(string temporaryContainer)
        {
            if (!Directory.Exists(temporaryContainer))
            {
                Directory.CreateDirectory(temporaryContainer);
            }

            var filePath = GetOCDataFilePath();
            var fileName = GetOCDataFileName(_namePattern);
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

        private void MergeOCData(List<string> dataFilePathList)
        {
            var ocDatas = new byte[_tileDimension, _tileDimension][];
            for (int x = 0; x < _tileDimension; ++x)
            {
                for (int y = 0; y < _tileDimension; ++y)
                {
                    ocDatas[x, y] = null;

                    var fileName = GetSceneName(x, y);
                    foreach (var filePath in dataFilePathList)
                    {
                        if (filePath.Contains(fileName))
                        {
                            ocDatas[x, y] = GetDataFrom(filePath);
                        }
                    }
                }
            }

            MergeOCData(ocDatas);
        }

        private byte[] GetDataFrom(string filePath)
        {
            if (File.Exists(filePath))
            {
                using (var fs = File.Open(filePath, FileMode.Open))
                {
                    var length = fs.Length;
                    var data = new byte[length];
                    if (fs.Read(data, 0, (int) length) != length)
                    {
                        Debug.LogErrorFormat("batch mode Read OC Data File Failed {0}", filePath);
                        return null;
                    }

                    return data;
                }
            }
            return null;
        }

        private void MergeOCData(byte[ , ][] ocDatas)
        {
            var filePath = GetOCDataFilePath();
            try
            {
                Debug.LogFormat("Merge OC Data for MultiScene Name Pattern {0}", NamePattern);
                using (var writer = new OCDataWriter(filePath, _tileDimension))
                {
                    for (int x = 0; x < _tileDimension; ++x)
                    {
                        for (int y = 0; y < _tileDimension; ++y)
                        {

                            if(!Config.IsBatchMode)
                                Progress("合并OC数据",
                                    String.Format("处理OC数据{0}/{1}", x * _tileDimension + y, _tileDimension * _tileDimension),
                                    ((float) x * _tileDimension + y) / _tileDimension * _tileDimension);
                            var data = ocDatas[x, y];
                            var ocDataBlock = OCDataBlock.Empty;
                            if (data != null)
                            {
                                var reader = new OCDataReader(data);
                                ocDataBlock = reader.GetDataBlock(0);
                            }

                            writer.FillOCDataBlock(x * _tileDimension + y, data, ocDataBlock.Offset,
                                ocDataBlock.Length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                if(!Config.IsBatchMode)
                    EditorUtility.ClearProgressBar();
            }
        }
        private bool Progress(string strTitle, string strMessage, float fT)
        {
            return EditorUtility.DisplayCancelableProgressBar(strTitle, strMessage, fT);
        }

        public void AddData(byte[] inData)
        {
            
        }

        private string GetOCDataFilePath()
        {
            return GetOCDataFilePath(_path, _namePattern);
        }

        public static string GetOCDataFilePath(string path, string namePattern)
        {
            var fileName = GetOCDataFileName(namePattern);
            var filePath = System.IO.Path.Combine(path, fileName);
            return filePath;
        }

        public static string GetOCDataFileName(string namePattern)
        {
            var outputName = String.Format(namePattern, "oc", "oc");
            outputName = SingleScene.GetOCDataFileName(outputName);

            return outputName;
        }

        public void SaveData()
        {

        }

        private void FrameBake()
        {
            if (curBakeScene >= bakeTiles.Count)
            {
                //finish

                EditorApplication.update -= FrameBake;

                return;
            }
   
            for (int i = curBakeScene; i < bakeTiles.Count; i++)
            {
                var index = bakeTiles[i];           

                _window.Update(bakeTiles[i]);
                if (_window.Bake(true, tempPath) == false)
                {
                    break;
                }
                curBakeScene++;
            }          
        }

        public bool BakeTiles(List<Index> indices, bool bFrame, string ocTempPath)
        {
            tempPath = ocTempPath;
            if (indices.Count <= 0)
                return false;

            Util.ClearScenes();

            if(bFrame)
            {
                _window = new Window(this, 1);
                _window.Init(indices[0]);
                bakeTiles = indices;
                EditorApplication.update += FrameBake;   
            }
            else
            {
                Window window = new Window(this, 1);
                window.Init(indices[0]);
                for (int i=0; i< indices.Count; i++)
                {
                    window.Update(indices[i]);
                    window.Bake(false, tempPath);
                }
            }           
           
            return true;
        }
#endif
    }
}
