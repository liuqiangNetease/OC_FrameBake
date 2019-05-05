// using System;
// using System.Collections.Generic;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif
// using UnityEngine;

// namespace OC
// {
//     public class OCBakeContextManager
//     {
//         private IEnumerator<OCBakeContext> _contextIter;

//         public OCBakeContextManager(Tile scene, Action onSuccessCallback = null)
//         {
//             _contextIter = ToContextIterator(scene, onSuccessCallback);
//         }

//         public OCBakeContextManager(IEnumerator<OCBakeContext> contextIterator)
//         {
//             _contextIter = contextIterator;
//         }


//         private IEnumerator<OCBakeContext> ToContextIterator(Tile scene, Action onSuccessCallback)
//         {
//             var contexts =  new OCBakeContext(scene, onSuccessCallback);
//             yield return contexts;
//         }

//         public void BakeAll()
//         {
//             while (_contextIter.MoveNext())
//             {
//                 var context = _contextIter.Current;
//                 //context.OnFinalCallback = Bake;
//                 context.Bake();
//             }
//         }
   

//         public void Bake()
//         {
//             if (_contextIter.MoveNext())
//             {
//                 var context = _contextIter.Current;
//                 context.OnFinalCallback = Bake;
//                 context.Bake();
//             }
//             else
//             {
// #if UNITY_EDITOR
//                 if (Config.IsBatchMode)
//                 {
//                     Debug.LogFormat("Exit On Bake Finish.");
//                     EditorApplication.Exit(0);
//                 }
// #endif
//                 GC.Collect();
//             }

//         }

//     }

//     public class OCBakeContext : IDisposable
//     {
//         public Tile Scene;
//         public Index TileIndex;
//         public Action OnSuccessCallback;
//         public Action OnFinishCallback;
//         public Action OnFinalCallback { private get; set; }

//         public bool IsBakeSuccess;
//         public bool Finished;
//         public IEnumerator<VisVolume> VolumeCalculator;
//         public GameObject CustomCameraObject;
//         public Vector3 CameraPosition;
//         public Quaternion CameraRotation;

//         private int ExecTarget;
//         private bool _inEditorUpdate;

//         public OCBakeContext(Tile scene, Action onSuccessCallback = null)
//         {
//             Scene = scene;
//             TileIndex = Scene.TileIndex;
//             OnSuccessCallback = onSuccessCallback;      

//             ExecTarget = Config.PerframeExecCount;
//             VolumeCalculator = null;
//             IsBakeSuccess = true;
//             Finished = false;

//             _inEditorUpdate = false;
//         }

//         public void Bake()
//         {
//             Debug.LogFormat("BakeContext: Start Bake ComptePerFrame {0}", Config.ComputePerframe);
//             if (Config.ComputePerframe)
//             {
// #if UNITY_EDITOR
//                 if (!_inEditorUpdate)
//                 {
//                     EditorApplication.update += ExecBake;
//                     _inEditorUpdate = true;
//                 }
// #endif
//             }
//             else
//             {
//                 ExecBake();
//             }
//         }

//         private void ExecBake()
//         {
//             try
//             {
//                 if (Scene != null && !Finished)
//                 {
//                     Scene.Bake(this);
//                     if (Finished)
//                     {
//                         if (IsBakeSuccess)
//                         {
//                             Debug.LogFormat("Bake Successfully for scene tile {0}", TileIndex);
//                             if (OnSuccessCallback != null)
//                             {
//                                 OnSuccessCallback();
//                             }
//                         }
//                         else
//                         {
//                             Debug.LogErrorFormat("Bake Fail for scene tile {0}", TileIndex);
//                         }
                        

//                         if(OnFinishCallback != null)
//                             OnFinishCallback();
//                     }
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogException(e);
//                 throw;
//             }
//             finally
//             {
//                 if (Finished)
//                 {
// #if UNITY_EDITOR
//                     if (_inEditorUpdate)
//                     {
//                         EditorApplication.update -= ExecBake;
//                         _inEditorUpdate = false;
//                     }
// #endif
//                     Debug.LogFormat("Bake Tile {0} Finish Result {1}", TileIndex, IsBakeSuccess);

//                     Dispose();

//                     GC.Collect();

//                     if (OnFinalCallback != null)
//                     {
//                         OnFinalCallback();
//                         OnFinalCallback = null;
//                     }
//                 }
//             }
//         }

//         public bool IsPrepared
//         {
//             get { return VolumeCalculator != null; }
//         }

//         public bool IsReachExecTarget(int execCount)
//         {
//             return execCount >= ExecTarget;
//         }

//         public void StepExecTarget()
//         {
//             ExecTarget += Config.PerframeExecCount;
//         }

//         public void Dispose()
//         {
//             Scene = null;
//             OnSuccessCallback = null;
//             OnFinishCallback = null;

//             VolumeCalculator = null;
//             CustomCameraObject = null;

//             GC.Collect();
//         }
//     }
// }
