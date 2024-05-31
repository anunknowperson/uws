using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UWS_RendererMobile : UWS_Renderer
{
    public override Material GetOceanMaterial()
    {
        return GetWaterMaterial();
    }

    public override List<Property> GetOceanProperties()
    {

        return GetWaterProperties();
    }
    public override List<Property> GetWaterProperties()
    {
        return new List<Property>(new Property[]
        {
            new Property("(T) Fallback Texture", null),
            new Property("(T) Normals", Resources.Load<Texture2D>("Textures/water1")),
            new Property("(V) Distortions (Bump waves, Reflection, Fresnel power, Fresnel bias)", new Vector4(1.02f, 0.2f, 2.7f, -0.4f)),
            new Property("(V) Auto blend parameter (Edge, Shore, Distance scale)", new Vector4(0.27f, 0.08f, 0.09f, 0.48f)),
            new Property("(V) Animation Tiling (Displacement)", new Vector4(0.4f, 0.391f, 0.56f, 0.7f)),
            new Property("(V) Animation Direction (displacement)", new Vector4(2f, 1f, -1f, 1f)),
            new Property("(V) Bump Tiling", new Vector4(0.04f, 0.04f, 0.04f, 0.08f)),
            new Property("(V) Bump Direction & Speed", new Vector4(1.0f, 30.0f, 20.0f, -20.0f)),
            new Property("(F) Fresnel Scale", 0.38f),
            new Property("(C) Base color", new Color(0.17f, 0.22f, 0.24f)),
            new Property("(C) Reflection color", new Color(0.47f, 0.6f, 0.66f)),
            new Property("(C) Specular color", new Color(0.81f, 0.8f, 0.77f)),
            new Property("(V) Specular light direction", new Vector4(0.01f, -0.17f, -0.98f, 0f)),
            new Property("(F) Shininess", 200.0f),
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

        UnityEngine.Object val = GetPropertyFromList(properties, "(T) Fallback Texture")._objectValue;

        if (val != null)
        {
            mat.SetTexture("_MainTex", (Texture)val);
        }

        val = GetPropertyFromList(properties, "(T) Normals")._objectValue;
        if (val != null)
        {
            mat.SetTexture("_BumpMap", (Texture)val);
        }
        mat.SetVector("_DistortParams", GetPropertyFromList(properties, "(V) Distortions (Bump waves, Reflection, Fresnel power, Fresnel bias)")._vectorValue);
        mat.SetVector("_InvFadeParemeter", GetPropertyFromList(properties, "(V) Auto blend parameter (Edge, Shore, Distance scale)")._vectorValue);
        mat.SetVector("_AnimationTiling", GetPropertyFromList(properties, "(V) Animation Tiling (Displacement)")._vectorValue);
        mat.SetVector("_AnimationDirection", GetPropertyFromList(properties, "(V) Animation Direction (displacement)")._vectorValue);

        mat.SetVector("_BumpTiling", GetPropertyFromList(properties, "(V) Bump Tiling")._vectorValue);
        mat.SetVector("_BumpDirection", GetPropertyFromList(properties, "(V) Bump Direction & Speed")._vectorValue);

        mat.SetFloat("_FresnelScale", GetPropertyFromList(properties, "(F) Fresnel Scale")._floatValue);

        mat.SetColor("_BaseColor", GetPropertyFromList(properties, "(C) Base color")._colorValue);
        mat.SetColor("_ReflectionColor", GetPropertyFromList(properties, "(C) Reflection color")._colorValue);
        mat.SetColor("_SpecularColor", GetPropertyFromList(properties, "(C) Specular color")._colorValue);

        mat.SetVector("_WorldLightDir", GetPropertyFromList(properties, "(V) Specular light direction")._vectorValue);
        mat.SetFloat("_Shininess", GetPropertyFromList(properties, "(F) Shininess")._floatValue);

        mat.SetFloat("_GerstnerIntensity", GetPropertyFromList(properties, "(F) Per vertex displacement")._floatValue);

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
        Material _waterMaterial = new Material(Shader.Find("Hidden/MobileWater"));

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
