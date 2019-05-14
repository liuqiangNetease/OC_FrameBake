using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OC.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace OC.Editor
{
    [CustomEditor(typeof(OCGenerator))]
    public class OCGeneratorEditor : UnityEditor.Editor
    {
        private SerializedProperty PropSoftRasterization;
        private SerializedProperty PropSimpleGenerateCell;   

        private SerializedProperty PropDrawCells;

        private SerializedProperty PropClearLightmapping;
        private SerializedProperty PropClearLightProbes;
        private SerializedProperty PropUseComputeShader;        
     
        private SerializedProperty PropUseVisibleCache;
        private SerializedProperty PropSavePerCell;
        private SerializedProperty PropClearOnSave;
        private SerializedProperty PropComputePerframe;
        private SerializedProperty PropPerframeExecCount;

        private SerializedProperty PropIsFixedScene;
        private SerializedProperty PropStreamOCTemporaryContainer;
        private SerializedProperty PropStreamSceneNamePattern;



        private SerializedProperty PropCustomVolume;
        private SerializedProperty PropCustomVolumeCenter;
        private SerializedProperty PropCustomVolumeSize;



        private SerializedProperty PropCellSize;

        private SerializedProperty PropScreenWidth;
        private SerializedProperty PropScreenHeight;

        private SerializedProperty PropIsMergeObjectID;
        private SerializedProperty PropMergeObjectDistance;
        private SerializedProperty PropMergeObjectMaxSize;

        private SerializedProperty PropIsMergeCell;
        private SerializedProperty PropMergeWeight;


        void OnEnable()
        {
            PropSoftRasterization = serializedObject.FindProperty("SoftRasterization");
            PropSimpleGenerateCell = serializedObject.FindProperty("SimpleGenerateCell");

            PropDrawCells = serializedObject.FindProperty("DrawCells");

            PropClearLightmapping = serializedObject.FindProperty("ClearLightmapping");
            PropClearLightProbes = serializedObject.FindProperty("ClearLightProbes");

            PropUseComputeShader = serializedObject.FindProperty("UseComputeShader");         
            PropUseVisibleCache = serializedObject.FindProperty("UseVisibleCache");
            PropSavePerCell = serializedObject.FindProperty("SavePerCell");
            PropClearOnSave = serializedObject.FindProperty("ClearOnSave");
            PropComputePerframe = serializedObject.FindProperty("ComputePerframe");
            PropPerframeExecCount = serializedObject.FindProperty("PerframeExecCount");
          
            PropScreenWidth = serializedObject.FindProperty("ScreenWidth");
            PropScreenHeight = serializedObject.FindProperty("ScreenHeight");

            PropCellSize = serializedObject.FindProperty("CellSize");

            PropIsMergeCell = serializedObject.FindProperty("MergeCell");
            PropMergeWeight = serializedObject.FindProperty("CellWeight");

            PropIsMergeObjectID = serializedObject.FindProperty("MergeObjectID");
            PropMergeObjectDistance = serializedObject.FindProperty("MergeObjectDistance");
            PropMergeObjectMaxSize = serializedObject.FindProperty("MergeObjectMaxSize");

            PropIsFixedScene = serializedObject.FindProperty("IsFixedScene");
            PropStreamOCTemporaryContainer = serializedObject.FindProperty("StreamOCTemporaryContainer");
            PropStreamSceneNamePattern = serializedObject.FindProperty("StreamSceneNamePattern");         

            PropCustomVolume = serializedObject.FindProperty("CustomVolume");
            PropCustomVolumeCenter = serializedObject.FindProperty("CustomVolumeCenter");
            PropCustomVolumeSize = serializedObject.FindProperty("CustomVolumeSize");          
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var generator = (OCGenerator) target;

            PropSimpleGenerateCell.boolValue = EditorGUILayout.Toggle(
                new GUIContent("SimpleGenerateCell", 
                "Generate cell simple"), 
                PropSimpleGenerateCell.boolValue);

            PropDrawCells.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Show Cells",
                    "Show OC Cells"),
                PropDrawCells.boolValue);

            PropClearLightmapping.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Clear Lightmapping Data",
                    "Clear Lightmapping Data Before Generation(Irreversible Operation)"),
                PropClearLightmapping.boolValue);
            PropClearLightProbes.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Clear Light Probes",
                    "Clear Light Probes Before Generation(Irreversible Operation)"),
                PropClearLightProbes.boolValue);

            PropUseComputeShader.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Use Compute Shader",
                    "Use Compute Shader To Get Visible Set"),
                PropUseComputeShader.boolValue);

            PropSoftRasterization.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Use Soft rasterization",
                    "Use Soft rasterization To Get Visible Set"),
                PropSoftRasterization.boolValue);




            PropUseVisibleCache.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Use Visible Cache",
                    "Cache Temporary Visbile Result"),
                PropUseVisibleCache.boolValue);

            PropSavePerCell.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Save Per Cell",
                    "Save cell immediately after cell is generated."),
                PropSavePerCell.boolValue);

            PropClearOnSave.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Clear On Save",
                    "Clear pvs data when data is saved."),
                PropClearOnSave.boolValue);

         
            PropScreenWidth.intValue = EditorGUILayout.IntField(
                    new GUIContent("Screen Width",
                        "bake screen width"),
                    PropScreenWidth.intValue);

            PropScreenHeight.intValue = EditorGUILayout.IntField(
                    new GUIContent("Screen Height",
                        "bake screen height"),
                    PropScreenHeight.intValue);

            PropCellSize.floatValue = EditorGUILayout.FloatField(
                    new GUIContent("Cell Size",
                        "bake cell size"),
                    PropCellSize.floatValue);


            PropComputePerframe.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Compute Per Frame",
                    "Compute PVS  data perframe in Editor Update callback"),
                PropComputePerframe.boolValue);

            if (PropComputePerframe.boolValue)
            {
                PropPerframeExecCount.intValue = EditorGUILayout.IntField(
                    new GUIContent("Per frame Exec Count",
                        "Execution count of PVS computation per frame"),
                    PropPerframeExecCount.intValue);
            }

           




            /*if (GUILayout.Button("Test OC Data File"))
            {
                OCGenerator.TestOCDataFile();
            }

            if (GUILayout.Button("Test Camera"))
            {
                OCGenerator.TestCameraRender();
            }

            if (GUILayout.Button("Test OC Data Apply"))
            {
                OCGenerator.TestApplyOCData();
            }

            if (GUILayout.Button("Test OC Gen Map Config"))
            {
                OCGenerator.TestGenerateOCGenMapConfigFile();
            }

            if (GUILayout.Button("Test Stream OC Generation"))
            {
                OCGenerator.TestGenerateStreamSceneOCData("Assets/Assets/Maps/maps/0001/Scenes", "002 {0}x{1}", @"D:\voyager_related", 6, 2);
            }*/

            PropIsFixedScene.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Is Fixed Scene",
                    "Use Fixed Scene Generator"),
                PropIsFixedScene.boolValue);


            PropIsMergeCell.boolValue = EditorGUILayout.Toggle(new GUIContent("Is Merge Cell", "是否合并cell"), PropIsMergeCell.boolValue);
            PropMergeWeight.floatValue = EditorGUILayout.FloatField(new GUIContent("Merge Weight","cell合并的相似度"), PropMergeWeight.floatValue);

            PropIsMergeObjectID.boolValue = EditorGUILayout.Toggle(new GUIContent("Is Merge ObjectID", "是否合并物件ID"), PropIsMergeObjectID.boolValue);
            PropMergeObjectDistance.floatValue = EditorGUILayout.FloatField(new GUIContent("Merge Object distance", "合并物体id的距离"), PropMergeObjectDistance.floatValue);
            PropMergeObjectMaxSize.floatValue = EditorGUILayout.FloatField(new GUIContent("Merge Object Max Size", "合并物体id的最大大小"), PropMergeObjectMaxSize.floatValue);

            if (GUILayout.Button("Test PVS"))
            {
                generator.TestPVS();
            }          

            if (PropIsFixedScene.boolValue)
            {
                if (GUILayout.Button("Generate OC Data (Fixed) "))
                {
                    generator.BakeSingleScene();
                }     
            }
            else
            {
                PropStreamOCTemporaryContainer.stringValue = EditorGUILayout.TextField(
                    new GUIContent("Stream Temporary Directory",
                        "Temporary directory to contain temporary OC Data"),
                    PropStreamOCTemporaryContainer.stringValue);

                PropStreamSceneNamePattern.stringValue = EditorGUILayout.TextField(
                    new GUIContent("Stream Scene Name Pattern",
                        "Scene Name Pattern For Stream Scene"),
                    "002 {0}x{1}");
              

                if (GUILayout.Button("Generate OC Data (Stream)"))
                {
                    generator.BakeStreamScene();
                }              

                if (GUILayout.Button("Load OC Data (Stream)"))
                {
                    generator.BakeStreamScene();
                }
            }

            PropCustomVolume.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Custom Volume",
                    "Specified A Custom Volume"),
                PropCustomVolume.boolValue);
            if (PropCustomVolume.boolValue)
            {
                PropCustomVolumeCenter.vector3Value = EditorGUILayout.Vector3Field("Volume Center", PropCustomVolumeCenter.vector3Value);
                PropCustomVolumeSize.vector3Value = EditorGUILayout.Vector3Field("Volume Size", PropCustomVolumeSize.vector3Value);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
