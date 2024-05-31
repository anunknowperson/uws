using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

[CustomEditor(typeof(UWS_WaterObject), true)]
public class UWS_WaterObjectEditor : Editor
{
    /*
    // Start is called before the first frame update
    private void OnEnable()
    {
        domain = (UWS_WaterDomain)target;
    }*/
    
    public UWS_WaterObject WaterObj;

    private int _flowmapGenerationResolution = 150;

    private int _brushSize = 30;
    private Vector2 _brushValue = new Vector2(0.0f, 0.0f);

    private bool _flowmapEdit = false;

    public override void OnInspectorGUI()
    {
        WaterObj = (UWS_WaterObject)target;



        if (!(WaterObj is UWS_Island))
        {
            EditorGUILayout.LabelField("Renderer Settings", EditorStyles.boldLabel);

            SerializedProperty list = serializedObject.FindProperty("RendererSettings");

            for (int i = 0; i < list.arraySize; i++)
            {
                DrawRendererProperty(list.GetArrayElementAtIndex(i));
            }

            EditorGUILayout.Separator();
        }

        EditorGUILayout.LabelField("Meshing Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Resolution"));

        if (WaterObj is UWS_Island)
        {
            serializedObject.ApplyModifiedProperties();

            return;
        }

        EditorGUILayout.Separator();

        

        if (WaterObj is UWS_River)
        {
            EditorGUILayout.LabelField("Flow Intersection Effects", EditorStyles.boldLabel);

            if (GUILayout.Button("Place"))
            {
                ((UWS_River)WaterObj).PlaceDecals();
            }
        }
        


        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Flow Settings", EditorStyles.boldLabel);

        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("UseFlowmap"));

        if (WaterObj.UseFlowmap)
        {
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FlowSpeedScale"));
            _flowmapGenerationResolution = EditorGUILayout.IntField("Flowmap Generation Resolution", _flowmapGenerationResolution);

            if (GUILayout.Button("Generate flowmap"))
            {
                WaterObj.GenerateFlowmap(_flowmapGenerationResolution);
            }

            if (WaterObj.GetFlowmap() != null)
            {
                if (!_flowmapEdit)
                {
                    if (GUILayout.Button("Edit Flowmap"))
                    {
                        _flowmapEdit = true;
                        WaterObj.EnterFlowmapEdit();

                    }
                } else
                {
                    if (GUILayout.Button("Save Flowmap"))
                    {
                        _flowmapEdit = false;
                        WaterObj.ExitFlowmapEdit();

                    }

                    EditorGUILayout.LabelField("Draw with Alt + Right Click", EditorStyles.boldLabel);

                    _brushSize = EditorGUILayout.IntField("Brush Size", _brushSize);
                    _brushValue = EditorGUILayout.Vector2Field("Brush Vector", _brushValue);
                }
                

                var style = new GUIStyle();

                style.stretchHeight = true;
                style.stretchWidth = true;
                style.alignment = TextAnchor.MiddleCenter;
                

                GUILayout.Box(WaterObj.GetFlowmap(), style, GUILayout.MaxHeight(300));

                if(GUILayout.Button("Remove Flowmap"))
                {
                    WaterObj.RemoveFlowmap();
                    
                }
            }
            

        }
        

        serializedObject.ApplyModifiedProperties();

    }

    bool _drawing = false;

    Vector3 LastPoint;

    void OnSceneGUI()
    {


        if (_flowmapEdit)
        {
            if (Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hitInfo;

                    if (Physics.Raycast(worldRay, out hitInfo, 10000))
                    {
                        _drawing = true;
                        LastPoint = hitInfo.point;
                    }

                    Event.current.Use();



                }

                if (_drawing)
                {
                    if (Event.current.type == EventType.MouseDrag)
                    {

                        Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        RaycastHit hitInfo;

                        if (Physics.Raycast(worldRay, out hitInfo, 10000))
                        {
                            Vector2 A = new Vector2(LastPoint.x, LastPoint.z);
                            Vector2 B = new Vector2(hitInfo.point.x, hitInfo.point.z);

                            Vector2 dir = (B - A).normalized;

                            WaterObj.Paint(B, _brushValue, _brushSize);

                            LastPoint = hitInfo.point;

                            /*float radius = 7.0f;

                            float rSquared = (WaterObj.GetFlowmapSize().x / 1024.0f * radius) * (WaterObj.GetFlowmapSize().y / 1024.0f * radius);

                            if ((A.x - B.x) * (A.x - B.x) + (A.y - B.y) * (A.y - B.y) > rSquared){
                                Vector2 dir = (B - A).normalized;
                                Debug.Log(dir);

                                for (int i = 0; i < 10; i++)
                                {
                                    WaterObj.Paint(Vector2.Lerp(A, B, i / 9.0f), dir);
                                }



                            }*/



                        }

                        

                        Event.current.Use();
                    }
                }
                

                if (Event.current.type == EventType.MouseUp)
                {
                    
                    _drawing = false;
                    EditorUtility.SetDirty(WaterObj);
                    Event.current.Use();
                }


                
            }
            
            
        }

    }

private void OnDisable()
    {
        if (_flowmapEdit)
        {
            _flowmapEdit = false;
            WaterObj.ExitFlowmapEdit();
        }
    }

    private void DrawRendererProperty(SerializedProperty prop)
    {
        string name = prop.FindPropertyRelative("Name").stringValue;
        string type = prop.FindPropertyRelative("Type").stringValue;

        if (type == "int")
        {
            var editProp = prop.FindPropertyRelative("_intValue");
            editProp.intValue = EditorGUILayout.IntField(new GUIContent(name), editProp.intValue);
        }
        else if (type == "float")
        {
            var editProp = prop.FindPropertyRelative("_floatValue");
            editProp.floatValue = EditorGUILayout.FloatField(new GUIContent(name), editProp.floatValue);
        }
        else if (type == "Texture2D")
        {
            var editProp = prop.FindPropertyRelative("_objectValue");
            EditorGUILayout.ObjectField(editProp, new GUIContent(name));
        }
        else if (type == "Vector2")
        {
            var editProp = prop.FindPropertyRelative("_vectorValue");
            editProp.vector4Value = EditorGUILayout.Vector2Field(new GUIContent(name), new Vector2(editProp.vector4Value.x, editProp.vector4Value.y));
        }
        else if (type == "Vector4")
        {
            var editProp = prop.FindPropertyRelative("_vectorValue");
            editProp.vector4Value = EditorGUILayout.Vector4Field(new GUIContent(name), editProp.vector4Value);
        }
        else if (type == "Color")
        {
            var editProp = prop.FindPropertyRelative("_colorValue");
            editProp.colorValue = EditorGUILayout.ColorField(new GUIContent(name), editProp.colorValue);
        }
    }
}
