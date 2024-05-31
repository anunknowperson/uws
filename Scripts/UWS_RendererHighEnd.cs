using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class UWS_RendererHighEnd : UWS_Renderer
{
    public  UWS_FFT _fft;

    private List<UWS_WaterDecal> _decals = new List<UWS_WaterDecal>();

    public override void Initialize(List<Property> properties)
    {
        base.Initialize(properties);

        InitializeFFT();
    }


    public override void RegisterDecal(UWS_WaterDecal decal) {
        _decals.Add(decal);
        UpdateDecals();
    }
    public override void RemoveDecal(UWS_WaterDecal decal) {
        _decals.Remove(decal);
        UpdateDecals();
    }

    private void UpdateDecals()
    {
        Shader.SetGlobalInt("_DecalCount", _decals.Count);

        var matrices = new Matrix4x4[16];
        var positions = new Vector4[16];
        var weigths = new float[16];
        var transitions = new float[16];

        for  (int i = 0; i < Math.Min(_decals.Count, 16); i++)
        {
            var decal = _decals[i];

            Shader.SetGlobalTexture("_Decal" + (i + 1), decal.DecalTexture);
            matrices[i] = decal.transform.worldToLocalMatrix;
            positions[i] = decal.transform.position;
            weigths[i] = decal.Weigth;
            transitions[i] = decal.OverrideHeigth ? 1.0f : 0.0f;
        }

        Shader.SetGlobalMatrixArray("_Transform", matrices);
        Shader.SetGlobalVectorArray("_Positions", positions);
        Shader.SetGlobalFloatArray("_Weigths", weigths);
        Shader.SetGlobalFloatArray("_Transitions", transitions);
    }

    public override bool IsRequireMeshing() 
    { 
        return true;
    }

    private void UpdateDecalMatrices()
    {
        var matrices = new Matrix4x4[16];
        var positions = new Vector4[16];
        var weigths = new float[16];
        var transitions = new float[16];

        for (int i = 0; i < Math.Min(_decals.Count, 16); i++)
        {
            var decal = _decals[i];

            matrices[i] = decal.transform.worldToLocalMatrix;
            positions[i] = decal.transform.position;
            weigths[i] = decal.Weigth;
            transitions[i] = decal.OverrideHeigth ? 1.0f : 0.0f;
        }

        Shader.SetGlobalMatrixArray("_Transform", matrices);
        Shader.SetGlobalVectorArray("_Positions", positions);
        Shader.SetGlobalFloatArray("_Weigths", weigths);
        Shader.SetGlobalFloatArray("_Transitions", transitions);
    }

    private void InitializeFFT()
    {
        int resolution = GetProperty("FFT Resolution")._intValue;
        Vector4 wind = GetProperty("FFT Wind")._vectorValue;
        float size = GetProperty("FFT Size")._floatValue;
        float choppiness = GetProperty("FFT Wave Choppiness")._floatValue;

        _fft = new UWS_FFT(resolution, new Vector2(wind.x, wind.y), size, choppiness);

        
    }

    public override Material GetOceanMaterial()
    {
        return GetWaterMaterial();
    }

    public override void FixMaterial(Material m)
    {
        if (m == null)
        {
            return;
        }
        m.SetTexture("_DisplacementTexture", _fft.GetDisplacementTexture());
        m.SetTexture("_NormalTexture", _fft.GetNormalTexture());
    }

    public override Material GetWaterMaterial()
    {
        Material _oceanMaterial = new Material(Shader.Find("Hidden/HighEndWater"));

        _oceanMaterial.SetTexture("_DisplacementTexture", _fft.GetDisplacementTexture());
        _oceanMaterial.SetTexture("_NormalTexture", _fft.GetNormalTexture());

        return _oceanMaterial;
    }

    public override Material GetRiverMaterial()
    {
        return GetWaterMaterial();
    }

    public override void Update()
    {
        _fft.Update();

        UpdateDecalMatrices();
    }


    public override List<Property> GetProperties()
    {
        return new List<Property>(new Property[]
        {
            new Property("FFT Resolution", 512),
            new Property("FFT Wind", new Vector2(10.0f, 10.0f)),
            new Property("FFT Size", 200.0f),
            new Property("FFT Wave Choppiness", 3.6f),

        });
    }

    /*
    float3 _WaterColor;// = float3(0.6f, 0.87f, 0.9f);
    float3 _TurbidityColor;// = float3(0.3f, 0.4f, 0.5f);
    float _Turbidity;// = 0.9f;
    float _Transparency;// = 50.0f;
    float _RefractionStrength;// = 0.05f;
			
    float _DisplacementScale;
    */

    public override List<Property> GetOceanProperties()
    {
        return GetWaterProperties();
    }
    public override List<Property> GetWaterProperties()
    {
        return new List<Property>(new Property[]
        {
            new Property("(C) Main Color", new Color(0.6f, 0.87f, 0.9f)),
            new Property("(C) Turbidity Color", new Color(0.3f, 0.4f, 0.5f)),
            new Property("(F) Turbidity", 0.9f),
            new Property("(F) Transparency", 50.0f),
            new Property("(F) Refraction Strength", 0.05f),
            new Property("(F) Displacement Scale", 1.0f),

        });
    }
    public override List<Property> GetRiverProperties()
    {
        return GetWaterProperties();
    }
    public override void SetOceanMaterialProperties(Material mat, List<Property> properties)
    {
        SetWaterMaterialProperties(mat, properties);

    }
    public override void SetWaterMaterialProperties(Material mat, List<Property> properties)
    {
        if (mat == null)
        {
            return;

        }

        mat.SetColor("_WaterColor", GetPropertyFromList(properties, "(C) Main Color")._colorValue);
        mat.SetColor("_TurbidityColor", GetPropertyFromList(properties, "(C) Turbidity Color")._colorValue);
        mat.SetFloat("_Turbidity", GetPropertyFromList(properties, "(F) Turbidity")._floatValue);
        mat.SetFloat("_Transparency", GetPropertyFromList(properties, "(F) Transparency")._floatValue);
        mat.SetFloat("_RefractionStrength", GetPropertyFromList(properties, "(F) Refraction Strength")._floatValue);
        mat.SetFloat("_DisplacementScale", GetPropertyFromList(properties, "(F) Displacement Scale")._floatValue);
    }
    public override void SetRiverMaterialProperties(Material mat, List<Property> properties)
    {
        SetWaterMaterialProperties(mat, properties);
    }
}
