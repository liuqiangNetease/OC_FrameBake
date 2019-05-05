using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ArtPlugins;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.SceneManagement;

namespace OC.Editor
{
    public partial class OCGenerator : MonoBehaviour
    {
        public int ScreenWidth = 800;
        public int ScreenHeight = 600;
        public float CellSize = 2;
        public bool SimpleGenerateCell = false;
        public bool SoftRasterization = false;

        public bool MergeCell = false;
        public float CellWeight = 0.8f;

        public bool MergeObjectID = false;
        public float MergeObjectDistance = 1;
        public float MergeObjectMaxSize = 1;

        public bool DrawCells = true;

        public bool ClearLightmapping = false;
        public bool ClearLightProbes = false;
        public bool UseComputeShader = false;
        public bool UseVisibleCache = true;
    
        public bool SavePerCell = false;
        public bool ClearOnSave = false;
        public bool ComputePerframe = false;
        public int PerframeExecCount = 3000;

        public int TestCellCount = 100;

        public bool CustomVolume;
        public Vector3 CustomVolumeCenter;
        public Vector3 CustomVolumeSize;

        public bool IsFixedScene = true;
        private SingleScene _scene;
        private MultiScene _multiScene;

        public string StreamOCTemporaryContainer = @"D:\voyager_related";
        public string StreamSceneNamePattern;   

        public void TestPVS()
        {
            var testPVS = new PVSTest(Camera.main);
            testPVS.Test(TestCellCount);
        }

        public void GenerateFixedSceneOCData()
        {
            InitConfig();

            _scene = new SingleScene(GetScenePath(), gameObject.scene.name);

            _scene.Bake(Config.ComputePerframe);

            //var contextManager = new OCBakeContextManager(_scene, () => { _scene.CopyOCDataTo(StreamOCTemporaryContainer); });
            //contextManager.Bake();
            //_scene.BakeOC();
        }

        public void GenerateStreamSceneOCData()
        {
            InitConfig();

            const int TileDimension = 8;
            int tileSize = 10;
            _multiScene = new MultiScene(GetScenePath(), StreamSceneNamePattern, TileDimension, tileSize);
            int x = 0, y = 0;
            if (GetStreamSceneIndex(TileDimension, out x, out y))
            {
                _multiScene.BakeOne(x, y);
                // var contetIter = _multiScene.GetBakeContexts(x, y, StreamOCTemporaryContainer, (tileX, tileY, tile) =>
                // {
                //     if (tileX == x && tileY == y)
                //     {
                //         (tile as SingleScene).Save();
                //     }
                // });

                // var contextManger = new OCBakeContextManager(contetIter);
                // contextManger.Bake();
            }
            else
            {
                EditorUtility.DisplayDialog("场景索引错误", String.Format("场景名称 {0} 和场景名称模式 {1} 不符合!", GetSceneName(), StreamSceneNamePattern), "确定");
                Debug.LogErrorFormat("Can not get stream scene index for scene name pattern {0} of scene name", StreamSceneNamePattern, GetSceneName());
            }
        }

        public void LoadStreamSceneOCData()
        {
            InitConfig();

            var ocDataFilePath = MultiScene.GetOCDataFilePath(GetScenePath(), StreamSceneNamePattern);
            if (!File.Exists(ocDataFilePath))
            {
                EditorUtility.DisplayDialog("文件不存在", String.Format("OC 数据文件 {0} 不存在!", ocDataFilePath), "确定");
                return;
            }
            const int TileDimension = 8;
            byte[] data = null;
            using (var fileStream = File.Open(ocDataFilePath, FileMode.Open))
            {
                data = new byte[fileStream.Length];
                if (fileStream.Read(data, 0, data.Length) != data.Length)
                {
                    EditorUtility.DisplayDialog("文件读取失败", String.Format("读取 OC 数据文件 {0} 失败!", ocDataFilePath), "确定");
                    return;
                }
            }

            _multiScene = new MultiScene(GetScenePath(), StreamSceneNamePattern, TileDimension, 1000, data);
            int x = 0, y = 0;
            if (GetStreamSceneIndex(TileDimension, out x, out y))
            {
                _multiScene.Load(x, y, null, true);
            }
            else
            {
                EditorUtility.DisplayDialog("场景索引错误", String.Format("场景名称 {0} 和场景名称模式 {1} 不符合!", GetSceneName(), StreamSceneNamePattern), "确定");
                Debug.LogErrorFormat("Can not get stream scene index for scene name pattern {0} of scene name", StreamSceneNamePattern, GetSceneName());
            }
        }

        public void MergeStreamSceneOCData()
        {
            if (!Directory.Exists(StreamOCTemporaryContainer))
            {
                EditorUtility.DisplayDialog("临时文件夹不存在", String.Format("临时文件夹 {0} 不存在！", StreamOCTemporaryContainer), "确定");
                return;
            }

            const int TileDimension = 8;
            //OCGenerator.MergeStreamSceneOCData(GetScenePath(), StreamSceneNamePattern, StreamOCTemporaryContainer, TileDimension);
        }

        private bool GetStreamSceneIndex(int tileDimension, out int x, out int y)
        {
            var sceneName = GetSceneName();
            for (int xi = 0; xi < tileDimension; ++xi)
            {
                for (int yi = 0; yi < tileDimension; ++yi)
                {
                    if (sceneName.Contains(String.Format(StreamSceneNamePattern, xi, yi)))
                    {
                        x = xi;
                        y = yi;
                        return true;
                    }
                }
            }

            x = y = 0;
            return false;
        }

        private string GetSceneName()
        {
            return gameObject.scene.name;
        }

        private string GetScenePath()
        {
            var sceneName = gameObject.scene.name;
            var scenePath = gameObject.scene.path;
            var sceneFullName = sceneName + ".unity";
            var path = scenePath.Substring(0, scenePath.Length - sceneFullName.Length - 1);

            return path;
        }        

        public static ComputeShader GetOCVisComputeShader()
        {
            var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Assets/ArtPlugins/OC/OCVisCompute.compute");
            if (computeShader == null)
            {
                computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ArtPluginOut/OC/OCVisCompute.compute");
            }

            return computeShader;
        }

        private void InitConfig()
        {
           
            Config.UseComputeShader = UseComputeShader;
            

            Config.mergeCell = MergeCell;
            Config.CellWeight = CellWeight;

            Config.mergeObjectID = MergeObjectID;
            Config.mergeObjectDistance = MergeObjectDistance;
            Config.mergeObjectMaxSize = MergeObjectMaxSize;

            Config.ScreenWidth = ScreenWidth;
            Config.ScreenHeight = ScreenHeight;
            Config.CellSize = CellSize;
            Config.UseVisibleCache = UseVisibleCache;
            Config.IgnoreFailureOnTileInit = false;
            Config.SavePerCell = SavePerCell;
            Config.ClearOnSave = ClearOnSave;
            Config.ComputePerframe = ComputePerframe;
            Config.PerframeExecCount = PerframeExecCount;

            Config.SimpleGenerateCell = SimpleGenerateCell;
            Config.SoftRenderer = SoftRasterization;        
            Config.CustomVolume = CustomVolume;
            Config.CustomVolumeCenter = CustomVolumeCenter;
            Config.CustomVolumeSize = CustomVolumeSize;

            if (ClearLightmapping)
            {
                //OCGenerator.ClearLightmappingData();
            }
            Config.ClearLightProbes = ClearLightProbes;
        }

     
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (DrawCells)
            {
                if (IsFixedScene)
                {
                    if (_scene != null)
                    {
                        DrawCellsOfSingleScene(_scene);
                    }
                }
                else
                {
                    if (_multiScene != null)
                    {
                        Gizmos.color = Color.blue;
                        foreach (var tile in _multiScene.tileMap)
                        {
                            DrawCellsOfSingleScene(tile.Value as SingleScene);
                        }                        
                    }
                }
                
            }          
        }

        private void DrawCellsOfSingleScene(SingleScene scene)
        {
            Gizmos.color = Color.blue;
            var volumeList = scene.volumelList;
            foreach (var volume in volumeList)
            {
                foreach (var cell in volume.cellList)
                {
                    var bounds = cell.aabb;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }
#endif       

    }
}
