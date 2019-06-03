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

        private SerializedProperty PropDrawCells;
      
        private SerializedProperty PropUseComputeShader;        
     
        private SerializedProperty PropUseVisibleCache;
   
        private SerializedProperty PropComputePerframe;
        private SerializedProperty PropPerframeExecCount;

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

            PropDrawCells = serializedObject.FindProperty("DrawCells");

            PropUseComputeShader = serializedObject.FindProperty("UseComputeShader");         
            PropUseVisibleCache = serializedObject.FindProperty("UseVisibleCache");
        
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
               
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var generator = (OCGenerator) target;


            PropDrawCells.boolValue = EditorGUILayout.Toggle(
                new GUIContent("Show Cells",
                    "Show OC Cells"),
                PropDrawCells.boolValue);
           

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

            PropIsMergeCell.boolValue = EditorGUILayout.Toggle(new GUIContent("Is Merge Cell", "是否合并cell"), PropIsMergeCell.boolValue);
            PropMergeWeight.floatValue = EditorGUILayout.FloatField(new GUIContent("Merge Weight","cell合并的相似度"), PropMergeWeight.floatValue);

            PropIsMergeObjectID.boolValue = EditorGUILayout.Toggle(new GUIContent("Is Merge ObjectID", "是否合并物件ID"), PropIsMergeObjectID.boolValue);
            PropMergeObjectDistance.floatValue = EditorGUILayout.FloatField(new GUIContent("Merge Object distance", "合并物体id的距离"), PropMergeObjectDistance.floatValue);
            PropMergeObjectMaxSize.floatValue = EditorGUILayout.FloatField(new GUIContent("Merge Object Max Size", "合并物体id的最大大小"), PropMergeObjectMaxSize.floatValue);

            if (GUILayout.Button("Test PVS"))
            {
                generator.TestPVS();
            }          

          
            if (GUILayout.Button("Bake Single Scene"))
            {
                generator.BakeSingleScene();
            }    
          
            serializedObject.ApplyModifiedProperties();
        }
    }
}
