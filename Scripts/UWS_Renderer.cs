using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UWS_Renderer
{
    protected List<Property> _properties;

    public abstract List<Property> GetProperties();

    public abstract Material GetWaterMaterial();
    public abstract List<Property> GetWaterProperties();
    public abstract Material GetOceanMaterial();
    public abstract List<Property> GetOceanProperties();
    public abstract Material GetRiverMaterial();
    public abstract List<Property> GetRiverProperties();

    public virtual void FixMaterial(Material m) { }

    public virtual void RegisterDecal(UWS_WaterDecal decal) { }
    public virtual void RemoveDecal(UWS_WaterDecal decal) { }

    public virtual bool IsRequireMeshing() {return false;}

    public abstract void Update();

    public virtual void Initialize(List<Property> properties)
    {
        _properties = properties;
    }

    public abstract void SetOceanMaterialProperties(Material mat, List<Property> properties);
    public abstract void SetWaterMaterialProperties(Material mat, List<Property> properties);
    public abstract void SetRiverMaterialProperties(Material mat, List<Property> properties);


    protected Property GetProperty(string name)
    {
        return GetPropertyFromList(_properties, name);
    }

    protected Property GetPropertyFromList(List<Property> properties, string name)
    {
        for (int i = 0; i < properties.Count; i++)
        {
            if (properties[i].Name == name)
            {
                return properties[i];
            }
        }

        return new Property();
    }

    [Serializable]
    public struct Property
    {
        public String Name;

        public string Type;

        public int _intValue;

        public float _floatValue;

        public Vector4 _vectorValue;

        public Color _colorValue;

        public UnityEngine.Object _objectValue;        


        public Property(string name, object value)
        {
            Name = name;

            Type = "";
            _intValue = 0;
            _floatValue = 0.0f;
            _objectValue = null;
            _vectorValue = Vector4.zero;
            _colorValue = Color.white;

            if (value is int)
            {
                _intValue = (int)value;
                Type = "int";
            } else if (value is float)
            {
                _floatValue = (float)value;
                Type = "float";
            } else if (value is UnityEngine.Object)
            {
                _objectValue = (UnityEngine.Object)value;
                Type = value.GetType().Name;
            } else if (value is Vector2)
            {
                _vectorValue = new Vector4(((Vector2)value).x, ((Vector2)value).y, 0.0f, 0.0f);
                Type = "Vector2";
            } else if (value is Vector4)
            {
                _vectorValue = (Vector4)value;
                Type = "Vector4";
            } else if (value is Color)
            {
                _colorValue = (Color)value;
                Type = "Color";
            }
        }
    }

    
}
