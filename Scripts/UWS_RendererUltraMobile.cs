using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UWS_RendererUltraMobile : UWS_Renderer
{
    public override Material GetOceanMaterial()
    {
        return GetWaterMaterial();
    }

    public override List<Property> GetOceanProperties()
    {
        return GetWaterProperties();
    }

    public override bool IsRequireMeshing()
    {
        return false;
    }
    public override List<Property> GetWaterProperties()
    {
        return new List<Property>(new Property[]
        {
            new Property("(C) Horizon Color", new Color(0f, 0.12f, 0.19f)),
            new Property("(F) Wave Scale", 0.7f),

            new Property("(T) Reflective color (RGB) fresnel (A)", Resources.Load<Texture2D>("Textures/WaterBasicDaytimeGradient")),
            new Property("(T) Waves Normalmap", Resources.Load<Texture2D>("Textures/water5")),

            new Property("(V) Wave speed (map1 x,y; map2 x,y)", new Vector4(9f, 4.5f, -8f, -3.5f)),
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

        UnityEngine.Object val = GetPropertyFromList(properties, "(T) Reflective color (RGB) fresnel (A)")._objectValue;

        if (val != null)
        {
            mat.SetTexture("_ColorControl", (Texture)val);
        }

        val = GetPropertyFromList(properties, "(T) Waves Normalmap")._objectValue;
        if (val != null)
        {
            mat.SetTexture("_BumpMap", (Texture)val);
        }

        mat.SetColor("_horizonColor", GetPropertyFromList(properties, "(C) Horizon Color")._colorValue);
        mat.SetVector("_DistortParams", GetPropertyFromList(properties, "(V) Distortions (Bump waves, Reflection, Fresnel power, Fresnel bias)")._vectorValue);
        mat.SetFloat("_WaveScale", GetPropertyFromList(properties, "(F) Wave Scale")._floatValue);

    }
    public override void SetRiverMaterialProperties(Material mat, List<Property> properties)
    {
        SetWaterMaterialProperties(mat, properties);
    }

    public override List<Property> GetProperties()
    {

        return new List<Property>(new Property[]
        {
            
        });

    }

    public override Material GetRiverMaterial()
    {
        return GetWaterMaterial();

    }

    public override Material GetWaterMaterial()
    {
        Material _waterMaterial = new Material(Shader.Find("Hidden/UltraMobileWater"));

        return _waterMaterial;
    }

    public override void Initialize(List<Property> properties)
    {
        base.Initialize(properties);
    }

    
    public override void Update()
    {
        
    }
}
