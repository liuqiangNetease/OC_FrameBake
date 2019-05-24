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

namespace OC
{
    public partial class OCGenerator : MonoBehaviour
    {
        public int ScreenWidth = 600;
        public int ScreenHeight = 600;
        public float CellSize = 2;
       
        public bool SoftRasterization = false;

        public bool MergeCell = false;
        public float CellWeight = 0.8f;

        public bool MergeObjectID = false;
        public float MergeObjectDistance = 1;
        public float MergeObjectMaxSize = 1;

        public bool DrawCells = true;

        public bool UseComputeShader = true;
        public bool UseVisibleCache = true;    

        public bool ComputePerframe = false;
        public int PerframeExecCount = 1000;

        private SingleScene _scene;

        public string StreamOCTemporaryContainer = "D:/buildtemp";
       

        public void TestPVS()
        {
            //var config = GetSceneConfig(gameObject.scene.name);
            //if (string.IsNullOrEmpty(config.MapName))
            //{
            // config.IsStreamScene = false;
            // config.SceneAssetPath = GetScenePath();
            //config.SceneNamePattern = gameObject.scene.name;
            // }

            InitConfig();
            OCSceneConfig config = new OCSceneConfig();
            config.IsStreamScene = false;
            config.SceneAssetPath = GetScenePath();
            config.SceneNamePattern = gameObject.scene.name;
            var testPVS = new PVSTest(Camera.main, config);
            testPVS.Test();
        }

        public void BakeSingleScene()
        {            
            //var config = GetSceneConfig(gameObject.scene.name);

            //if (string.IsNullOrEmpty(config.MapName))
            //{
                InitConfig();
                _scene = new SingleScene(GetScenePath(), gameObject.scene.name, Index.InValidIndex);
                _scene.Bake(Config.ComputePerframe, "D;/OCTemp");
            //}
            //else
            //{
                //ConfigGenerator(config);
                //_scene = new SingleScene(config.GetSceneAssetPath(), config.SceneNamePattern, Index.InValidIndex);
                //_scene.Bake(config.ComputePerframe, config.TemporaryContainer);
            //}
        }

        private string GetScenePath()
        {
            var sceneName = gameObject.scene.name;
            var scenePath = gameObject.scene.path;
            var sceneFullName = sceneName + ".unity";
            var path = scenePath.Substring(0, scenePath.Length - sceneFullName.Length - 1);

            return path;
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
     

            Config.ComputePerframe = ComputePerframe;
            Config.PerframeExecCount = PerframeExecCount;

        
            Config.SoftRenderer = SoftRasterization;   
            
        }

     
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (DrawCells)
            {
                //if (IsFixedScene)
                {
                    if (_scene != null)
                    {
                        DrawCellsOfSingleScene(_scene);
                    }
                }
              
                
            }          
        }

        private void DrawCellsOfSingleScene(SingleScene scene)
        {
            Gizmos.color = Color.blue;
            var volumeList = scene.volumelList;
            if (volumeList == null)
                return;
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
