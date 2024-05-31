using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using ClipperLib;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;


public class UWS_Pool : UWS_WaterObject
{
    public List<UWS_Renderer.Property> RendererSettings;

    private Material _material;
    private void OnEnable()
    {
        _spline = GetComponent<UWS_Spline>();

        StartCoroutine(Registration());
    }

#if UNITY_EDITOR
    private void Update()
    {

        UWS_WaterDomain.s_Instance._renderer.FixMaterial(_material);

        UWS_WaterDomain.s_Instance._renderer.SetOceanMaterialProperties(_material, RendererSettings);


        SetupFlowmap();
    }
#endif

    private bool CheckProperties()
    {
        List<UWS_Renderer.Property> real = UWS_WaterDomain.s_Instance._renderer.GetWaterProperties();

        if (real.Count != RendererSettings.Count)
        {
            return false;
        }

        for (int i = 0; i < real.Count; i++)
        {
            if (real[i].Name != RendererSettings[i].Name)
            {
                return false;
            }
        }

        return true;
    }
    public override void ResetMaterials()
    {
        OnEnable();
    }
    private IEnumerator Registration()
    {
        while (UWS_WaterDomain.s_Instance == null)
        {
            yield return null;
        }

        if (RendererSettings == null || RendererSettings.Count == 0 || !CheckProperties())
        {
            RendererSettings = UWS_WaterDomain.s_Instance._renderer.GetWaterProperties();
        }

        _material = UWS_WaterDomain.s_Instance._renderer.GetWaterMaterial();

        UWS_WaterDomain.s_Instance._renderer.SetWaterMaterialProperties(_material, RendererSettings);
        SetupFlowmap();
        UWS_WaterDomain.s_Instance.RegisterObject(this);
    }

    private void OnDisable()
    {
        if (UWS_WaterDomain.s_Instance != null)
        {
            UWS_WaterDomain.s_Instance.RemoveObject(this);
        }

    }

    public void OnValidate()
    {
        if (_material != null)
        {
            if (UWS_WaterDomain.s_Instance == null)
            {
                return;
            }


            UWS_WaterDomain.s_Instance._renderer.SetWaterMaterialProperties(_material, RendererSettings);
        }

    }

    public override Material GetMaterial()
    {
        return _material;
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
        return _spline.GetControlPoint(0).y;
    }
}
