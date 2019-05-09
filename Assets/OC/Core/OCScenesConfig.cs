using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OC
{
    [Serializable]
    public struct OCScenesConfig
    {
        public List<OCSceneConfig> scenesConfig;// = new List<OCSceneConfig>();
    }
    [Serializable]
    public struct OCSceneConfig
    {
        public string MapName;

        public float CellSize;

        public int ScreenWidth;
        public int ScreenHeight;

        public float MaxPlayerHeight;
        public float MinPlayerHeight;

        public string SceneAssetPath;
        public string GetSceneAssetPath()
        {
            string ret = SceneAssetPath;
            if(UnityEditorInternal.InternalEditorUtility.inBatchMode == false)
            {
                ret = "Assets/" + ret;
            }
            return ret;
        }
        public string SceneNamePattern;

        public string TemporaryContainer;

        public bool MergeCell;
        public float MergeCellWeight;

        public bool MergeObjectID;
        public float MergeObjectSize;
        public float MergeObjectDistance;

        public bool IsStreamScene;

        public bool UseComputeShader;

        public bool UseVisbileCache;

        public bool ComputePerframe;
        public int PerframeExecCount;

        public int TileDimension;
        public int TileSize;

        public bool CustomVolume;
        public Vector3 VolumeCenter;
        public Vector3 VolumeSize;

        public List<Index> indices;

        public override string ToString()
        {
            var str = String.Format(
                "MapName {0}, Stream {1}, AssetPath {2}, SceneNamePattern {3} TempContainer {4} TileDim {5} TileSize {6} ComputeShader {7} CellSize {8} CustomVolume {9}",
                MapName, IsStreamScene, SceneAssetPath, SceneNamePattern, TemporaryContainer, TileDimension, TileSize, UseComputeShader, CellSize, CustomVolume);


            if (indices == null)
            {
                str += " No index";
            }
            else
            {
                str += " Tiles: ";
                foreach (var tile in indices)
                {
                    str += String.Format("[{0}, {1}]", tile.x, tile.y);
                }
            }

            return str;
        }

        public List<Index> GetBakeIndices()
        {
            var tiles = indices;
            if (tiles == null)
            {
                //bake all tiles if there is no any tile specified to bake
                tiles = new List<Index>();
                var dimension = TileDimension;
                for (int x = 0; x < dimension; ++x)
                {
                    for (int y = 0; y < dimension; ++y)
                    {
                        tiles.Add(new Index(x, y));
                    }
                }
            }

            return tiles;
        }

        public string GetSceneNameOf(int x, int y)
        {
            if (IsStreamScene)
            {
                return string.Format(SceneNamePattern, x, y);
            }

            return SceneNamePattern;
        }

        public string GetOCDataFilePath()
        {
            return Path.Combine(TemporaryContainer, GetOCDataFileName());
        }

        public string GetOCDataFileName()
        {
            string fileName;
            if (IsStreamScene)
            {
                fileName = MultiScene.GetOCDataFileName(SceneNamePattern);
            }
            else
            {
                fileName = SingleScene.GetOCDataFileName(SceneNamePattern);
            }

            return fileName;
        }
    }

}
