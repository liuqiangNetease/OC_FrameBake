
#if UNITY_EDITOR
//using Core.Utils;
//using OC.Profiler;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace OC
{
    public class MinRenderer: IRenderer
    {
        public MinRenderer()
        {
            _shader = null;

            _computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Assets/ArtPlugins/OC/OCVisCompute.compute");
            if (_computeShader == null)
            {
                _computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ArtPluginOut/OC/OCVisCompute.compute");
            }
        }

        /// <summary>
        /// 记录渲染场景的相机
        /// </summary>
        private Camera _cam;
        private Camera cam
        {
            get
            {
                if (_cam == null)
                {
                    _cam = Camera.main;
                }
                return _cam;
            }
        }

        private Shader _shader;
        private Shader shader
        {
            get
            {
                if (_shader == null)
                {
                    if (Config.UseComputeShader)
                        _shader = Shader.Find("OC/Color");
                    else
                        _shader = Shader.Find("OC/ColorNormal");

                    if (_shader == null)
                    {
                        Debug.LogErrorFormat("MinRenderSet error, can't get shader");
                    }
                }
                return _shader;
            }
        }

        private ComputeShader _computeShader;

        private ColorN _colorN = new ColorN();
     
        private int _oldQualityLevel;
        private ShadowQuality _oldShadowQuality;

        private float _oldLodBias;

        private Lightmapping.GIWorkflowMode _oldGiWorkflowMode;

        private struct TerrainMaterial
        {
            public Terrain.MaterialType MatType;
            public Material Mat;
            public bool DetailDraw;

            public TerrainMaterial(Terrain.MaterialType matType, Material mat, bool detailDraw)
            {
                MatType = matType;
                Mat = mat;
                DetailDraw = detailDraw;
            }
        }
        private Dictionary<Renderer, Material[]> _oldTransparentMats = new Dictionary<Renderer, Material[]>();
        private Dictionary<Terrain, TerrainMaterial> _oldTerrainMats = new Dictionary<Terrain, TerrainMaterial>();
        private Dictionary<MeshRenderer, Material[]> _oldRenderMats = new Dictionary<MeshRenderer, Material[]>();
        private Dictionary<int, MeshRenderer> _renderColors = new Dictionary<int, MeshRenderer>();
        private List<MeshRenderer> _visibleSet = new List<MeshRenderer>();
        private Dictionary<Component, bool> _cameraComps = new Dictionary<Component, bool>();
        private RenderTexture _oldTarget;
        private RenderTexture _oldActive;
        private RenderTexture _newTarget;
        private RenderTexture _dummyTarget;
        private Texture2D _texture;

        private int _textureWidth;
        private int _textureHeight;
        private float _oldCamFov;
        private float _oldAspect;
        private float _oldNear;
        private float _oldFar;
        private int _oldCullingMask;
        private CameraClearFlags _oldClearFlags;
        private Color _oldBackgroundColor;
        private RenderingPath _oldRenderingPath;

        private ComputeBuffer _computeBuffer;
        private ComputeBuffer _computeIndexBuffer;
        private ComputeBuffer _computeIndexLengthBuffer;
        private int[] _computeLengthData;
        private int[] _computeIndexData;
     
        public void RestoreTransparentAlpha()
        {
            foreach (var tranMat in _oldTransparentMats)
            {
                var mr = tranMat.Key;
                var mats = tranMat.Value;
                mr.sharedMaterials = mats;
            }
        }

        public bool SetTransparentAlpha(Renderer mr)
        {
            bool ret = false;

            //
            Material[] oldMats = mr.sharedMaterials;

            Material[] finalMats = new Material[oldMats.Length];

            for (int i = 0; i < oldMats.Length; i++)
            {
                var mat = oldMats[i];

                if (mat == null)
                   continue;

                float mode = 0;
                if (mat.HasProperty("_Mode"))
                {
                    mode = mat.GetFloat("_Mode");

                }
                //mode == 1    cutOff donot replace material 
                if (mat.shader.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent || mode > 1)
                //if (mode > 1)
                {
                    Material newMat = new Material(Shader.Find("OC/Alpha Blend"));
                    newMat.color = new Color(1, 1, 1, 0);
                    finalMats[i] = newMat;
                    ret = true;
                }
                else
                {
                    finalMats[i] = mat;
                }
            }


            if (ret)
            {
                _oldTransparentMats.Add(mr, oldMats);
                mr.sharedMaterials = finalMats;
            }
            return ret;
        }


        private static readonly string LowLevelName = "Very Low";
        public void Prepare()
        {
            _oldLodBias = QualitySettings.lodBias;

            if(Config.ChangeLODBias)
                QualitySettings.lodBias = 20000;

            _oldQualityLevel = QualitySettings.GetQualityLevel();
            var qualityNames = QualitySettings.names;
            for (int i = 0; i < qualityNames.Length; ++i)
            {
                if (qualityNames[i].Contains(LowLevelName))
                {
                    QualitySettings.SetQualityLevel(i);
                }
            }

            _oldShadowQuality = QualitySettings.shadows;
            QualitySettings.shadows = ShadowQuality.Disable;


            _oldGiWorkflowMode = Lightmapping.giWorkflowMode;
            //Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.Legacy;

            //set terrain materials
            _oldTerrainMats.Clear();
            Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
            var terrainMaterial = new Material(shader);
            terrainMaterial.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f));
            foreach (var terrain in terrains)
            {
                _oldTerrainMats.Add(terrain, new TerrainMaterial(terrain.materialType, terrain.materialTemplate, terrain.drawTreesAndFoliage));
                //terrain.materialType = Terrain.MaterialType.Custom;
                //terrain.materialTemplate = terrainMaterial;
                terrain.drawTreesAndFoliage = false;
            }

            Renderer[] mrs = Object.FindObjectsOfType<Renderer>();
            //            Debug.LogFormat("DisableInvisibleRenderers call, find all meshrenderer count:{0} time:{1}", mrs.Length, System.DateTime.Now);

            // 查找所有潜在可视集合
            _visibleSet.Clear();
            _oldTransparentMats.Clear();
            for (int i = 0; i < mrs.Length; i++)
            {
                Renderer mr = mrs[i];
                if (mr == null || !mr.gameObject.activeSelf || !mr.enabled || mr.sharedMaterials.Length <= 0)
                    continue;


                if (SetTransparentAlpha(mr))
                    continue;

                //cutoff 
                if (Util.ContainAnyTransparentOrCutOff(mr))
                    continue;

                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    continue;

                var meshRenderer = mr as MeshRenderer;
                _visibleSet.Add(meshRenderer);
            }

            //            Debug.LogFormat("DisableInvisibleRenderers call, find potential meshrenderer count:{0} time:{1}", visibleSet.Count, System.DateTime.Now);

            // 替换为纯色shader
            _oldRenderMats.Clear();
            _renderColors.Clear();
            int maxKey = -int.MaxValue;
            //System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < _visibleSet.Count; i++)
            {
                MeshRenderer mr = _visibleSet[i];
                if (mr == null || mr.sharedMaterials.Length <= 0) continue;

                _oldRenderMats.Add(mr, mr.sharedMaterials);

                Material mat = new Material(shader);
                Color32 color = _colorN.IntToColorN(i);
                mat.SetColor("_Color", color);

                Material[] mats = new Material[mr.sharedMaterials.Length];
                for (int k = 0; k < mats.Length; k++) mats[k] = mat;
                mr.sharedMaterials = mats;

                //var key = (color.r << 16) | (color.g << 8) | color.b;
                var key = i;
                _renderColors.Add(key, mr);

                if (maxKey < key)
                {
                    maxKey = key;
                }

                // Debug.LogFormat("{3} key {4} Set Color is {0} {1} {2}", color.r, color.g, color.b, mr.name, key);
                //sb.AppendLine(color.ToString());
            }


            //            Debug.LogFormat("DisableInvisibleRenderers call, replace color shader, count:{0} time:{1}", oldRenderMats.Count, System.DateTime.Now);
            //System.IO.File.WriteAllText(@"C:\Users\Myself\Desktop\writePixels.txt", sb.ToString());

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

            _oldTarget = cam.targetTexture;
            _oldActive = RenderTexture.active;
            _oldCamFov = cam.fieldOfView;
            _oldAspect = cam.aspect;
            _oldNear = cam.nearClipPlane;
            _oldFar = cam.farClipPlane;
            _oldCullingMask = cam.cullingMask;
            _oldClearFlags = cam.clearFlags;
            _oldBackgroundColor = cam.backgroundColor;
            _oldRenderingPath = cam.renderingPath;

            //_textureWidth = (int)Camera.main.pixelRect.width;
            //_textureHeight = (int)Camera.main.pixelRect.height;
            _textureWidth = Config.ScreenWidth;
            _textureHeight = Config.ScreenHeight;

            if (_newTarget != null)           
                RenderTexture.ReleaseTemporary(_newTarget);
           

            if (Config.UseComputeShader)
                _newTarget = RenderTexture.GetTemporary(_textureWidth, _textureHeight, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            else
                _newTarget = RenderTexture.GetTemporary(_textureWidth, _textureHeight, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            if (_dummyTarget != null)           
                RenderTexture.ReleaseTemporary(_dummyTarget);           
            _dummyTarget = RenderTexture.GetTemporary(_textureWidth, _textureHeight, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);


            _newTarget.autoGenerateMips = false;
            _newTarget.DiscardContents();

            cam.targetTexture = _newTarget;

            if(Config.ChangeCameraFOV)
            {
                cam.fieldOfView = 90.0f;
                cam.aspect = 1.0f;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 3000;
            }

            //cam.cullingMask &= ~UnityLayerManager.GetLayerMask(EUnityLayerName.UI);
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = Color.white;

            cam.renderingPath = RenderingPath.Forward;

            //  Debug.LogFormat("MiniRenderer Prepare Sreen Width {0} Height {1} Aspect {2}", Screen.width, Screen.height, _oldAspect);

            if (Config.UseComputeShader)
            {
                if (_computeBuffer != null)
                {
                    _computeBuffer.Dispose();
                }

                if (_computeIndexBuffer != null)
                {
                    _computeIndexBuffer.Dispose();
                }

                if (_computeIndexLengthBuffer != null)
                    _computeIndexLengthBuffer.Dispose();


                if (maxKey < 0) maxKey = 0;
                maxKey += 1;
                Debug.LogFormat("MinRenderer Max Key {0}", maxKey);
                _computeBuffer = new ComputeBuffer(maxKey, sizeof(int));
                _computeIndexBuffer = new ComputeBuffer(maxKey, sizeof(int));              
                _computeIndexLengthBuffer = new ComputeBuffer(1, sizeof(int));
                _computeLengthData = new int[1];
                _computeIndexData = new int[maxKey];

                var computeShader = _computeShader;
                var prepareKernal = computeShader.FindKernel("OCVisPrepare");
                computeShader.SetBuffer(prepareKernal, "OCResultBuffer", _computeBuffer);
                computeShader.SetBuffer(prepareKernal, "OCIndexBuffer", _computeIndexBuffer);
                computeShader.SetBuffer(prepareKernal, "OCIndexLengthBuffer", _computeIndexLengthBuffer);
                var computeKernal = computeShader.FindKernel("OCVisCompute");
                computeShader.SetBuffer(computeKernal, "OCResultBuffer", _computeBuffer);
                computeShader.SetBuffer(computeKernal, "OCIndexBuffer", _computeIndexBuffer);
                computeShader.SetBuffer(computeKernal, "OCIndexLengthBuffer", _computeIndexLengthBuffer);
                computeShader.SetInt("BufferLength", _computeBuffer.count);
                computeShader.SetInt("Offset", _colorN.Offset);
                computeShader.SetInt("Alpha", _colorN.Alpha);
                computeShader.SetTexture(computeKernal, "OCTexture", _newTarget);
            }
            else
            {
                _texture = new Texture2D(_textureWidth, _textureHeight, TextureFormat.ARGB32, false);
            }
        }

        private HashSet<MeshRenderer> _visibleRenders = new HashSet<MeshRenderer>();
        public HashSet<MeshRenderer> Do(List<MeshRenderer> renderers = null)
        {
            if (Config.UseComputeShader)
            {
                //OCProfiler.Start();
                var prepareKernal = _computeShader.FindKernel("OCVisPrepare");
                _computeShader.Dispatch(prepareKernal, _computeBuffer.count, 1, 1);
                //var visPrepareTime = OCProfiler.Stop();
                //Debug.LogFormat("Vis Prepare Time is {0}", visPrepareTime);
            }

            //OCProfiler.Start();
            _newTarget.DiscardContents();
            //var discardTime = OCProfiler.Stop();
            //Debug.LogFormat("Target Discard Time is {0}", discardTime);
            // 屏幕纹理捕捉
            //OCProfiler.Start();
            cam.Render();
            //var renderTime = OCProfiler.Stop();
            //Debug.LogFormat("Camera Render Time is {0}", renderTime);

            _visibleRenders.Clear();
            int notFoundKeysCount = 0;
            if (Config.UseComputeShader)
            {              
                RenderTexture.active = _dummyTarget;
                
                var computeKernal = _computeShader.FindKernel("OCVisCompute");
                _computeShader.Dispatch(computeKernal, _textureWidth, _textureHeight, 1);               
                
                _computeIndexLengthBuffer.GetData(_computeLengthData);
                var length = _computeLengthData[0];     

                if (length > 0)
                {
                    //OCProfiler.Start();
                    if(Config.NewGetData == false)
                    {
                        _computeIndexData = new int[length];
                        _computeIndexBuffer.GetData(_computeIndexData);
                    }
                    else
                        _computeIndexBuffer.GetData(_computeIndexData, length);
                    
                    //var visComputeGetData = OCProfiler.Stop();
                    //Debug.LogFormat("Vis Compute Get Data Time is {0}", visComputeGetData);
                    //OCProfiler.Start();
                    for (int key = 0; key < length; ++key)
                    {                 
                        MeshRenderer mr = null;
                        if (!_renderColors.TryGetValue(_computeIndexData[key], out mr))
                        {
                            notFoundKeysCount++;
                            continue;
                        }

                        if (mr != null)
                        {
                            _visibleRenders.Add(mr);
                        }
                    }

                   //var visResultTime = OCProfiler.Stop();
                   //Debug.LogFormat("Vis Result Time is {0}", visResultTime);
                }
            }
            else
            {
#if true
                Graphics.CopyTexture(_newTarget, _texture);
#else
                RenderTexture.active = _newTarget;
                _texture.ReadPixels(new Rect(0f, 0f, _textureWidth, _textureHeight), 0, 0);
                _texture.Apply();
#endif

                // 获取实际渲染的物体
                // Color32[] pixels = _texture.GetPixels32(0);
                //Debug.LogFormat("pixelsCount:{0} width:{1} height:{2} w*h:{3}", pixels.Length, texture.width, texture.height, texture.width * texture.height);
                //sb.Length = 0;
                for (int i = 0; i < _textureHeight; i++)
                {
                    for (int j = 0; j < _textureWidth; j++)
                    {
                        var color = _texture.GetPixel(i, j);
                       
                        int r = (int)(255.0f * color.r + 0.5f);
                        int g = (int)(255.0f * color.g + 0.5f);
                        int b = (int)(255.0f * color.b + 0.5f);
                        int Offset = _colorN.Offset;
                        int N = 256 - Offset;

                        int relR = r - Offset;
                        int relG = g - Offset;
                        int relB = b - Offset;
                        if (relR < 0 || relG < 0 || relB < 0)
                        {
                            Debug.LogFormat("Found A Invalid Pixel Pos {0} {1}, Value {2} {3} {4}",
                                i, j, r, g, b);
                            continue;
                        }

                        int key = relR * N * N + relG * N + relB;

                        //Color32 color = pixels[i * _textureWidth + j];
                        //var key = (color.r << 16) | (color.g << 8) | color.b;

                        //var colorFloat = _texture.GetPixel(i, j);
                        //var r = ((int)(colorFloat.r * 255.0f + 0.5f)) << 16;
                        //var g = ((int)(colorFloat.g * 255.0f + 0.5f)) << 8;
                        //var b = (int)(colorFloat.b * 255.0f + 0.5f);
                        //var key = r | g | b;

                        MeshRenderer mr = null;
                        if (!_renderColors.TryGetValue(key, out mr))
                        {
                            notFoundKeysCount++;
                            continue;
                        }

                        if (mr != null)
                        {
                            _visibleRenders.Add(mr);
                        }
                    }
                }
            }
            //            Debug.LogFormat("DisableInvisibleRenderers call, get real renderers, foundcount:{0} notfoundCount:{1} time:{2}", foundRenderers.Count, notFoundKeysCount, System.DateTime.Now);
            //System.IO.File.WriteAllText(@"C:\Users\Myself\Desktop\readPixels.txt", sb.ToString());


            //            Debug.LogFormat("DisableInvisibleRenderers call, recovery materials, time:{0}", System.DateTime.Now);



            //            Debug.LogFormat("DisableInvisibleRenderers call, disable all not real renderers, time:{0}", System.DateTime.Now);

            // if (texture != null) Object.DestroyImmediate(texture);

            return _visibleRenders;

        }


        public void Finish()
        {
            QualitySettings.lodBias = _oldLodBias;
            RestoreTransparentAlpha();
            QualitySettings.SetQualityLevel(_oldQualityLevel);
            QualitySettings.shadows = _oldShadowQuality;

            Lightmapping.giWorkflowMode = _oldGiWorkflowMode;

            // 恢复相机上可能的后效组件
            foreach (var pair in _cameraComps)
            {
                Behaviour beh = pair.Key as Behaviour;
                if (beh != null) beh.enabled = pair.Value;
            }

            // 材质复原
            for (int i = 0; i < _visibleSet.Count; i++)
            {
                MeshRenderer mr = _visibleSet[i];
                if (mr == null || mr.sharedMaterials.Length <= 0) continue;

                Material[] mats;
                if (_oldRenderMats.TryGetValue(mr, out mats))
                {
                    mr.sharedMaterials = mats;
                }
            }

            foreach (var terrainMatPair in _oldTerrainMats)
            {
                var terrain = terrainMatPair.Key;
                var terrainMat = terrainMatPair.Value;
                terrain.materialType = terrainMat.MatType;
                terrain.materialTemplate = terrainMat.Mat;
                terrain.drawTreesAndFoliage = terrainMat.DetailDraw;
            }

            cam.targetTexture = _oldTarget;
            cam.fieldOfView = _oldCamFov;
            cam.aspect = _oldAspect;
            cam.nearClipPlane = _oldNear;
            cam.farClipPlane = _oldFar;
            cam.cullingMask = _oldCullingMask;
            cam.clearFlags = _oldClearFlags;
            cam.backgroundColor = _oldBackgroundColor;
            cam.renderingPath = _oldRenderingPath;

            RenderTexture.active = _oldActive;
            //Object.DestroyImmediate(_newTarget);
            //_newTarget = null;
            _oldTarget = null;
            _oldActive = null;

            if (_computeBuffer != null)            
                _computeBuffer.Dispose();
          
            if (_computeIndexBuffer != null)
                _computeIndexBuffer.Dispose();

            if (_computeIndexLengthBuffer != null)
                _computeIndexLengthBuffer.Dispose();

            // Debug.LogFormat("MiniRenderer Finish Sreen Width {0} Height {1} Aspect {2}", Screen.width, Screen.height, _oldAspect);
        }      
    }

}
#endif

