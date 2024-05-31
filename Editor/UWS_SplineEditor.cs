using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UWS_Spline))]
public class UWS_SplineEditor : Editor
{

    private const int stepsPerCurve = 10;
    private const float directionScale = 0.5f;
    private const float handleSize = 0.04f;
    private const float pickSize = 0.06f;

    private static Color[] modeColors = {
        Color.white,
        Color.yellow,
        Color.cyan
    };

    private UWS_Spline spline;
    private Transform handleTransform;
    private Quaternion handleRotation;
    private int selectedIndex = -1;

    Tool lastTool = Tool.None;

    

    void OnEnable()
    {
        lastTool = Tools.current;
        Tools.current = Tool.None;
    }

    void OnDisable()
    {
        Tools.current = lastTool;
    }

    public override void OnInspectorGUI()
    {
        spline = target as UWS_Spline;

        EditorGUILayout.LabelField("Spline Settings", EditorStyles.boldLabel);

        bool loop = spline.Loop;

        if (spline.GetComponent<UWS_River>() != null)
        {

            if (spline.Loop != false)
            {
                spline.Loop = false;

                EditorUtility.SetDirty(spline);
            }
        } else
        {
            if (spline.Loop != true)
            {
                spline.Loop = true;

                EditorUtility.SetDirty(spline);
            }
        }

        if (selectedIndex >= 0 && selectedIndex < spline.ControlPointCount)
        {
            DrawSelectedPointInspector();
        }
        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(spline, "Add Point");
            spline.AddCurve();
            EditorUtility.SetDirty(spline);

            UWS_WaterDomain.s_Instance.ScheduleRebuild();
        }
    }

    private void DrawSelectedPointInspector()
    {
        GUILayout.Label("Selected Point", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        Vector3 point = EditorGUILayout.Vector3Field("Position", spline.GetControlPoint(selectedIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spline, "Move Point");
            EditorUtility.SetDirty(spline);
            spline.SetControlPoint(selectedIndex, point);

            UWS_WaterDomain.s_Instance.ScheduleRebuild();
        }
        EditorGUI.BeginChangeCheck();
        UWS_BezierControlPointMode mode = (UWS_BezierControlPointMode)EditorGUILayout.EnumPopup("Mode", spline.GetControlPointMode(selectedIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(spline, "Change Point Mode");
            spline.SetControlPointMode(selectedIndex, mode);
            EditorUtility.SetDirty(spline);

            UWS_WaterDomain.s_Instance.ScheduleRebuild();
        }
    }

    private void OnSceneGUI()
    {
        spline = target as UWS_Spline;
        handleTransform = spline.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        Vector3 p0 = ShowPoint(0);
        
        for (int i = 1; i < spline.ControlPointCount; i += 3)
        {
            Vector3 p1 = ShowPoint(i);
            Vector3 p2 = ShowPoint(i + 1);
            Vector3 p3 = ShowPoint(i + 2);

            Handles.color = Color.green;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            Handles.DrawBezier(p0, p3, p1, p2, Color.red, null, 2f);

            if ((target as UWS_Spline).GetComponent<UWS_River>() != null)
            {
                if (i == 1)
                {
                    DoWidthHandle(p0, (p0 - p1).normalized, spline.ControlPointCount + i, 0);
                }

                DoWidthHandle(p3, (p2 - p3).normalized, spline.ControlPointCount + i, 1);
            }
            

            p0 = p3;
        }


        //ShowDirections();
    }

    private void DoWidthHandle(Vector3 start, Vector3 direction, int index, int offset)
    {
        Vector3 rotated = new Vector3(-direction.z, 0f, direction.x);

        Vector3 pointA = start + rotated * spline.GetControlPointWidth((index - spline.ControlPointCount - 1) / 3 + offset);
        Vector3 pointB = start - rotated * spline.GetControlPointWidth((index - spline.ControlPointCount - 1) / 3 + offset);

        Handles.color = Color.yellow;
        Handles.DrawLine(start, pointA);
        Handles.DrawLine(start, pointB);

        Handles.color = Color.red;
        float size = HandleUtility.GetHandleSize(pointA);

        if (Handles.Button(pointA, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedIndex = index + offset * 2;
            Repaint();
        }

        if (Handles.Button(pointB, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedIndex = index + 1 + offset * 2;
            Repaint();
        }

        Quaternion localHandleRotation = new Quaternion();
        localHandleRotation.SetLookRotation(rotated);

        if (selectedIndex == index + offset * 2)
        {
            EditorGUI.BeginChangeCheck();

            pointA = Handles.DoPositionHandle(pointA, localHandleRotation);
            
            if (EditorGUI.EndChangeCheck())
            {
                spline.SetControlPointWidth((index - spline.ControlPointCount - 1) / 3 + offset, Vector3.Distance(start, pointA));

                Undo.RecordObject(spline, "Move Point");
                EditorUtility.SetDirty(spline);
                //spline.SetControlPoint(index, point);

                UWS_WaterDomain.s_Instance.ScheduleRebuild();
            }
        }

        if (selectedIndex == index + 1 + offset * 2)
        {
            EditorGUI.BeginChangeCheck();

            pointB = Handles.DoPositionHandle(pointB, localHandleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                spline.SetControlPointWidth((index - spline.ControlPointCount - 1) / 3 + offset, Vector3.Distance(start, pointB));

                Undo.RecordObject(spline, "Move Point");
                EditorUtility.SetDirty(spline);
                //spline.SetControlPoint(index, point);

                UWS_WaterDomain.s_Instance.ScheduleRebuild();
            }
        }

        
    }

    private Vector3 ShowPoint(int index)
    {
        Vector3 point = (spline.GetControlPoint(index));
        float size = HandleUtility.GetHandleSize(point);
        if (index == 0)
        {
            size *= 2f;
        }
        Handles.color = modeColors[(int)spline.GetControlPointMode(index)];
        if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
        {
            selectedIndex = index;
            Repaint();
        }
        if (selectedIndex == index)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 temp = point;

            point = Handles.DoPositionHandle(point, handleRotation);

            bool isRiver = spline.GetComponent<UWS_River>() != null;


            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "Move Point");
                EditorUtility.SetDirty(spline);
                spline.SetControlPoint(index, point);

                if (!isRiver)
                {
                    for (int i = 0; i < spline.ControlPointCount; i++)
                    {
                        var p = spline.GetControlPoint(i);
                        p.y = point.y;
                        spline.SetControlPoint(i, p);

                    }
                }

                UWS_WaterDomain.s_Instance.ScheduleRebuild();
                
            }
        }
        return point;
    }
}