using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ClipperLib;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using System;

public class UWS_Island : UWS_WaterObject
{
    private void OnEnable()
    {
        _spline = GetComponent<UWS_Spline>();

        StartCoroutine(Registration());
    }

    private IEnumerator Registration()
    {
        while (UWS_WaterDomain.s_Instance == null)
        {
            yield return null;
        }

        UWS_WaterDomain.s_Instance.RegisterObject(this);
    }

    public override void ResetMaterials()
    {
        OnEnable();
    }
    private void OnDisable()
    {
        if (UWS_WaterDomain.s_Instance != null)
        {
            UWS_WaterDomain.s_Instance.RemoveObject(this);
        }

    }

    public override Material GetMaterial()
    {
        return null;
    }

    public override Path GetPolygon()
    {
        Path points = new Path();

        for (int i = 0; i < Resolution; i++)
        {
            var vector = _spline.GetPoint(i / (float)(Resolution - 1));
            points.Add(new IntPoint(vector.x * 1000, vector.z * 1000));
        }

        return points;
    }

    public override float GetHeight(Vector2 position)
    {
        throw new NotImplementedException();
    }
}
