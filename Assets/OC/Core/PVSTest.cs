﻿#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OC
{
    public class PVSTest
    {
        public static float errorRange = 0.35f;
        public static int _width;// = 256;
        public static int _height;// = 256;

        SingleScene singleScene;

        MultiScene streamScene;

        private Dictionary<Component, bool> _cameraComps = new Dictionary<Component, bool>();

        private List<ReflectionProbe> _reflectionProbeList = new List<ReflectionProbe>();
        private List<Light> _lightList = new List<Light>();
        private List<LightProbeGroup> _lightProbeGroupList = new List<LightProbeGroup>();

        public PVSTest(Camera camera, OCSceneConfig config)
        {
            cam = camera;
            _width = (int)cam.pixelRect.width / 4;
            _height = (int)cam.pixelRect.height / 4;
            this.config = config;
        }

        public void Test()
        {
            Prepare();
            Do();
            Finish();
        }
        public void Prepare()
        {
            _oldCameraPos = cam.transform.position;
            _oldCameraDir = cam.transform.forward;
            _oldTarget = cam.targetTexture;
            _oldActive = RenderTexture.active;

            _oldRenderPath = cam.renderingPath;
            cam.renderingPath = RenderingPath.Forward;

            _oldShadowQuality = QualitySettings.shadows;
            QualitySettings.shadows = ShadowQuality.Disable;

            _oldGiWorkflowMode = Lightmapping.giWorkflowMode;
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.Legacy;


            var probes = GameObject.FindObjectsOfType<ReflectionProbe>();
            foreach (var probe in probes)
            {
                if (probe.enabled)
                {
                    probe.enabled = false;
                    _reflectionProbeList.Add(probe);
                }
            }

            var lights = GameObject.FindObjectsOfType<Light>();
            foreach (var light in lights)
            {
                if (light.enabled)
                {
                    light.enabled = false;
                    _lightList.Add(light);
                }
            }

            var lightProbeGroup = GameObject.FindObjectsOfType<LightProbeGroup>();
            foreach (var lightProbe in lightProbeGroup)
            {
                if (lightProbe.enabled)
                {
                    lightProbe.enabled = false;
                    _lightProbeGroupList.Add(lightProbe);
                }
            }

            // 关闭相机的可能的后效组件影响
            _cameraComps.Clear();
            var cs = cam.GetComponents<Component>();
            foreach (Component c in cs)
            {
                if (c != null)
                {
                    Transform tr = c as Transform;
                    Camera ca = c as Camera;
                    Behaviour beh = c as Behaviour;
                    if (tr == null && ca == null && beh != null)
                    {
                        _cameraComps.Add(c, beh.enabled);
                        beh.enabled = false;
                    }
                }
            }

        }

        public void Finish()
        {
#if UNITY_EDITOR
            Lightmapping.giWorkflowMode = _oldGiWorkflowMode;
#endif
            cam.transform.position = _oldCameraPos;
            cam.transform.forward = _oldCameraDir;
            cam.targetTexture = _oldTarget;
            RenderTexture.active = _oldActive;
            cam.renderingPath = _oldRenderPath;

            QualitySettings.shadows = _oldShadowQuality;

            foreach (var light in _lightList)
            {
                light.enabled = true;
            }
            foreach (var lightProbe in _lightProbeGroupList)
            {
                lightProbe.enabled = true;
            }
            foreach (var reflectProbe in _reflectionProbeList)
            {
                reflectProbe.enabled = true;
            }

            // 恢复相机上可能的后效组件
            foreach (var pair in _cameraComps)
            {
                Behaviour beh = pair.Key as Behaviour;
                if (beh != null) beh.enabled = pair.Value;
            }
        }

        public void Do()
        {


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
                for (int i = 0; i < config.TileDimension; i++)
                    for (int j = 0; j < config.TileDimension; j++)
                        streamScene.Load(i, j);
            }
            else
            {
                singleScene = new OC.SingleScene(config.GetSceneAssetPath(), config.SceneNamePattern, Index.InValidIndex);
                singleScene.TestLoad();
            }

            //var name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            //SingleScene scene = new OC.SingleScene("", name, null);
            //scene.Load();

            int width = _width;
            int height = _height;

            RenderTexture renderTex = RenderTexture.GetTemporary(width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture.active = renderTex;
            cam.targetTexture = renderTex;

            Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);

            if (config.IsStreamScene)
            {
                foreach (var tile in streamScene.tileMap)
                {
                    var scene = tile.Value as SingleScene;

                    for (int v = 0; v < scene.volumelList.Count; v++)
                    {
                        string title = "test volume " + v + "/" + scene.volumelList.Count;
                        string info = "";
                        var volume = scene.volumelList[v];

                        int finalCellCount = volume.cellList.Count;

                        for (int i = 0; i < finalCellCount; i++)
                        {
                            info = "cell " + i + "/" + finalCellCount;
                            bool bCancel = EditorUtility.DisplayCancelableProgressBar(title, info, (float)i / finalCellCount);
                            if (bCancel)
                                break;
                            var cell = volume.cellList[i];
                            MoveCamera(scene, tex, cell.aabb.center);
                        }
                    }
                }
            }
            else
            {
                for (int v = 0; v < singleScene.volumelList.Count; v++)
                {
                    string title = "test volume " + v + "/" + singleScene.volumelList.Count;
                    string info = "";
                    var volume = singleScene.volumelList[v];

                    int finalCellCount = volume.cellList.Count;

                    for (int i = 0; i < finalCellCount; i++)
                    {
                        info = "cell " + i + "/" + finalCellCount;
                        bool bCancel = EditorUtility.DisplayCancelableProgressBar(title, info, (float)i / finalCellCount);
                        if (bCancel)
                            break;
                        var cell = volume.cellList[i];
                        MoveCamera(singleScene, tex, cell.aabb.center);
                    }
                }
            }


            EditorUtility.ClearProgressBar();
        }


        bool RotateCameraAndRender(SingleScene scene, Texture2D tex, Vector3 dir)
        {
            bool ret = true;

            dir.Normalize();
            cam.transform.forward = dir;

            int width = tex.width;
            int height = tex.height;

            scene.UndoCulling();
            cam.Render();
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Color[] p1s = tex.GetPixels(0);

            scene.DoCulling(cam.transform.position);

            cam.Render();
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Color[] p2s = tex.GetPixels(0);

            //compare pixels
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Color c1 = p1s[i * width + j];
                    Color c2 = p2s[i * width + j];

                    Color delta = c2 - c1;
                    if (delta.r > errorRange || delta.g > errorRange || delta.b > errorRange || delta.a > errorRange)
                    {
                        ret = false;
                        //Debug.LogFormat("pixel i{0},j{1}; c1 r{2},g{3},b{4} c2 r{5},g{6},b{7}", i, j, c1.r, c1.g, c1.b, c2.r, c2.g, c2.b);                        
                    }
                }
            }

            scene.UndoCulling();
            return ret;
        }


        void MoveCamera(SingleScene scene, Texture2D tex, Vector3 pos)
        {
            //bool bSame = true;

            cam.transform.position = pos;
            bool bSameF = RotateCameraAndRender(scene, tex, Vector3.forward);
            bool bSameB = RotateCameraAndRender(scene, tex, Vector3.back);
            bool bSameL = RotateCameraAndRender(scene, tex, Vector3.left);
            bool bSameR = RotateCameraAndRender(scene, tex, Vector3.right);
            //bool bSameU = RotateCameraAndRender(scene, tex, Vector3.up);
            //bool bSameD = RotateCameraAndRender(scene, tex, Vector3.down);
            if (bSameF == false)
                Debug.LogError("Forward is not the same color!" + pos);
            if (bSameB == false)
                Debug.LogError("Back is not the same color!" + pos);
            if (bSameL == false)
                Debug.LogError("left is not the same color!" + pos);
            if (bSameR == false)
                Debug.LogError("right is not the same color!" + pos);
            //if (bSameU == false)
            //Debug.Log("up is not the same color!");
            //if (bSameD == false)
            //Debug.Log("down is not the same color!");

            //bSame = bSameF && bSameB && bSameL && bSameR && bSameU && bSameD;
            //bSame = bSameF && bSameB && bSameL && bSameR;

            //if (bSame == false)
            // Debug.LogFormat("Camera pos: x{0},y{1},z{2} is not the same color!", cam.transform.position.x, cam.transform.position.y, cam.transform.position.z);
        }

        private Camera cam;

        OCSceneConfig config;

        private Vector3 _oldCameraPos;
        private Vector3 _oldCameraDir;

        private RenderingPath _oldRenderPath;


        private ShadowQuality _oldShadowQuality;
#if UNITY_EDITOR
        private Lightmapping.GIWorkflowMode _oldGiWorkflowMode;
#endif
        private RenderTexture _oldTarget;
        private RenderTexture _oldActive;
    }
}

#endif
