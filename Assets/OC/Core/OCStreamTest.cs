using OC.Editor;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using OC.Editor;
using System;
using System.IO;
using UnityEditor;
using System.Collections;
using OC.Profiler;

namespace OC
{
    public class OCStreamTest : MonoBehaviour
    {
        MultiScene streamScene;
        public string sceneName = "002";
        public bool OC = false;
        public int _tileX = 4;
        public int _tileY = 3;

        void Start()
        {
            OCSceneConfig config = OCGenerator.GetSceneConfig(sceneName);
            if (config.IsStreamScene)
            {
                var ocDataFilePath = MultiScene.GetOCDataFilePath(config.GetSceneAssetPath(), config.SceneNamePattern);
                if (!File.Exists(ocDataFilePath))
                {
                    EditorUtility.DisplayDialog("文件不存在", string.Format("OC 数据文件 {0} 不存在!", ocDataFilePath), "确定");
                    return;
                }
                int TileDimension = config.TileDimension;
                byte[] data = null;
                using (var fileStream = File.Open(ocDataFilePath, FileMode.Open))
                {
                    data = new byte[fileStream.Length];
                    if (fileStream.Read(data, 0, data.Length) != data.Length)
                    {
                        EditorUtility.DisplayDialog("文件读取失败", string.Format("读取 OC 数据文件 {0} 失败!", ocDataFilePath), "确定");
                        return;
                    }
                }

                streamScene = new MultiScene(config.GetSceneAssetPath(), config.SceneNamePattern, TileDimension, config.TileSize, data);
              
                foreach (var index in config.indices)
                {
                    if(index.x == _tileX && index.y == _tileY)
                        streamScene.Load(index.x, index.y);
                }
            }          
        }

        // Update is called once per frame
        void Update()
        {
            if (OC)
            {
                OCProfiler.Start();
                for (int i = 0; i < 1; i++)
                    streamScene.DoCulling(Camera.main.transform.position);
                var time = OCProfiler.Stop();
                //Debug.Log(time);

            }
            else
            {
                streamScene.UndoCulling();
            }
        }
    }
}
#endif
