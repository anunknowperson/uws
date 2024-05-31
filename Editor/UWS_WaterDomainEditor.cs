using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

[CustomEditor(typeof(UWS_WaterDomain))]
public class UWS_WaterDomainEditor : Editor
{
    /*
    // Start is called before the first frame update
    private void OnEnable()
    {
        domain = (UWS_WaterDomain)target;
    }*/
    public UWS_WaterDomain Domain;

    private Color DisabledColor = Color.gray;
    private Color EnabledColor = Color.white;

    private bool ButtonSetup = true;
    private bool ButtonRenderer = false;
    private bool ButtonMeshing = false;
    private bool ButtonReflection = false;
    private bool ButtonTools = false;
    private bool ButtonInfo = false;

    string[] tabs = {"Renderer", "Meshing", "Reflection", "Underwater", "Other", "Info"};
    int selectedTab = 5;

    public override void OnInspectorGUI()
    {
        Domain = (UWS_WaterDomain)target;

        selectedTab = GUILayout.SelectionGrid(selectedTab, tabs, 3);

        EditorGUILayout.Separator();
        
        if (selectedTab == 0)
        {
            DrawRendererTab();
        } else if (selectedTab == 1)
        {
            DrawMeshingTab();
        } else if (selectedTab == 2)
        {
            DrawReflectionTab();
        }
        else if (selectedTab == 3)
        {
            DrawUnderwaterTab();
        }
        else if (selectedTab == 4)
        {
            DrawOtherTab();
        }
        else if (selectedTab == 5)
        {
            DrawInfoTab();
        }


        serializedObject.ApplyModifiedProperties();

    }

    private void DrawInfoTab()
    {
        EditorGUILayout.HelpBox("Changes to some properties may only take effect after the scene is reloaded.", MessageType.Info);
    }

    private void DrawUnderwaterTab()
    {
        EditorGUILayout.LabelField("Underwater Effects Settings", EditorStyles.boldLabel);

        EditorGUILayout.Separator();

        EditorGUILayout.HelpBox("The camera must have a UWS_CameraEffects component.\nUnderwater effects do not work with the editor's camera.", MessageType.Info);

        EditorGUILayout.Separator();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableUnderwaterEffects"));

        if (Domain.EnableUnderwaterEffects)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UnderwaterTransparency"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UnderwaterColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UnderwaterTurbidity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UnderwaterTurbidityColor"));
        }
    }

    private void DrawOtherTab()
    {
        //

        //EditorGUILayout.Separator();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("AsynchronousLoadingFinishedEvent"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("FactoryThreadCount"));

        /* public bool AsynchronousLoading = true;
    public Event AsynchronousLoadingFinishedEvent;
    */
    }

    private void DrawRendererTab()
    {
        serializedObject.FindProperty("RendererName").stringValue = GetRendererName(EditorGUILayout.Popup("Renderer", GetRendererIndex(Domain.RendererName), UWS_WaterDomain.GetAvaiableRenderers()));

        EditorGUILayout.LabelField("Renderer Settings", EditorStyles.boldLabel);

        SerializedProperty list = serializedObject.FindProperty("RendererSettings");

        for (int i = 0; i < list.arraySize; i++)
        {
            DrawRendererProperty(list.GetArrayElementAtIndex(i));
        }

        //EditorGUILayout.PropertyField(serializedObject.FindProperty("RendererSettings"), new GUIContent("Renderer Settings"));

    }

    private void DrawRendererProperty(SerializedProperty prop)
    {
        string name = prop.FindPropertyRelative("Name").stringValue;
        string type = prop.FindPropertyRelative("Type").stringValue;

        if (type == "int")
        {
            var editProp = prop.FindPropertyRelative("_intValue");
            editProp.intValue = EditorGUILayout.IntField(new GUIContent(name), editProp.intValue);
        } else if (type == "float")
        {
            var editProp = prop.FindPropertyRelative("_floatValue");
            editProp.floatValue = EditorGUILayout.FloatField(new GUIContent(name), editProp.floatValue);
        }else if (type == "Texture2D")
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
        } else if (type == "Color")
        {
            var editProp = prop.FindPropertyRelative("_colorValue");
            editProp.colorValue = EditorGUILayout.ColorField(new GUIContent(name), editProp.colorValue);
        }
    }

    private void DrawMeshingTab()
    {
        
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Current meshing mode: ");

        string meshingMode = Domain._renderer.IsRequireMeshing() ? "Dynamic" : "Static";

        var s = new GUIStyle();
        s.normal.textColor = Color.yellow;
        s.fontStyle = FontStyle.Bold;
        EditorGUILayout.LabelField(meshingMode, s);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Domain Tree Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("Scale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LodCoefficient"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LodDepth"));

        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Meshing Tree Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("MeshingResolutionLowDetail"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("MeshingResolutionMidDetailLayerNumber"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MeshingResolutionMidDetail"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("MeshingResolutionHighDetailLayerNumber"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MeshingResolutionHighDetail"));

    }

    private void DrawReflectionTab()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ReflectionType"));

        if (Domain.ReflectionType == UWS_ReflectionType.Planar)
        {
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("TextureScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CullingMask"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Hdr"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ClipPlaneOffset"));
        }
    }

   

    private int GetRendererIndex(string rendererName)
    {
        for (int i = 0; i < UWS_WaterDomain.GetAvaiableRenderers().Length; i++)
        {
            if (UWS_WaterDomain.GetAvaiableRenderers()[i] == rendererName)
            {
                return i;
            }
        }

        Debug.LogError("No renderer with name " + rendererName + " has been found.");
        return -1;
    }

    private string GetRendererName(int index)
    {
        return UWS_WaterDomain.GetAvaiableRenderers()[index];
    }
}
