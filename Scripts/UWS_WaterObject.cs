using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;

[RequireComponent(typeof(UWS_Spline)), ExecuteAlways]
public abstract class UWS_WaterObject : MonoBehaviour
{
    protected UWS_Spline _spline;

    public int Resolution = 30;
    public bool UseFlowmap = false;
    public float FlowSpeedScale = 1.0f;

    private ComputeBuffer _buffer = null;

    [SerializeField]
    protected Texture2D _flowmap;

    [SerializeField]
    protected Vector2 _flowmapPosition;

    [SerializeField]
    protected Vector2 _flowmapSize;

    public void GenerateFlowmap(int resolution)
    {

        Texture2D flowmap = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
        RenderTexture flowmapTemp = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);

        Path points = GetPolygon();

        float pointLeft, pointUp, pointRight, pointDown;

        FindBorderPoints(points, out pointLeft, out pointUp, out pointRight, out pointDown);

        Vector2 center = new Vector2((pointLeft + pointRight) / 2.0f, (pointDown + pointUp) / 2.0f);
        Vector2 size = new Vector2(Mathf.Abs(pointRight - pointLeft), Mathf.Abs(pointUp - pointDown));

        _flowmapPosition = center;
        _flowmapSize = size;

        if (this is UWS_River)
        {
            Material mat = new Material(Shader.Find("Hidden/FlowmapGenerator"));
            SetBuffer(mat, resolution);

            mat.SetVector("_Position", _flowmapPosition);
            mat.SetVector("_Scale", _flowmapSize);

            Graphics.Blit(flowmap, flowmapTemp, mat);
        } else
        {
            Material mat = new Material(Shader.Find("Hidden/EmptyFlowmap"));
            Graphics.Blit(flowmap, flowmapTemp, mat);
        }

        

        RenderTexture tmp = RenderTexture.active;
        RenderTexture.active = flowmapTemp;
        flowmap.ReadPixels(new Rect(0, 0, flowmapTemp.width, flowmapTemp.height), 0, 0);
        flowmap.Apply();
        RenderTexture.active = tmp;

        _flowmap = flowmap;


    }

    private void SetBuffer(Material mat, int resolution)
    {
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector2));

        Vector2[] arr = new Vector2[resolution];

        for (int i = 0; i < resolution; i++)
        {
            var vector = _spline.GetPoint(i / (float)(resolution - 1));

            arr[i] = new Vector2(vector.x, vector.z);
        }

        _buffer = new ComputeBuffer(arr.Length, stride, ComputeBufferType.Default);
        _buffer.SetData(arr);

        mat.SetInt("_RiverPointsCount", arr.Length);
        mat.SetBuffer("_RiverPoints", _buffer);

    }

    protected void SetupFlowmap()
    {
        var mat = GetMaterial();

        if (mat == null)
        {
            return;
        }

        mat.SetFloat("_FlowSpeedScale", FlowSpeedScale);

        mat.SetTexture("_Flowmap", _flowmap);
        mat.SetVector("_FlowmapPosition", _flowmapPosition);
        mat.SetVector("_FlowmapScale", _flowmapSize);

        if (UseFlowmap)
        {
            mat.EnableKeyword("FLOW_FLOWMAP");
            mat.DisableKeyword("FLOW_DIRECTION");

        }
        else
        {
            mat.EnableKeyword("FLOW_DIRECTION");
            mat.DisableKeyword("FLOW_FLOWMAP");
        }
    }

    private void FindBorderPoints(Path spline, out float pointLeft, out float pointUp, out float pointRight, out float pointDown)
    {
        float pL = float.PositiveInfinity, pU = float.PositiveInfinity, pR = float.NegativeInfinity, pD = float.NegativeInfinity;

        for (int i = 0; i < spline.Count; i++)
        {
            if (spline[i].X / 1000.0f > pR)
            {
                pR = spline[i].X / 1000.0f;
            }

            if (spline[i].X / 1000.0f < pL)
            {
                pL = spline[i].X / 1000.0f;
            }

            if (spline[i].Y / 1000.0f > pD)
            {
                pD = spline[i].Y / 1000.0f;
            }

            if (spline[i].Y / 1000.0f < pU)
            {
                pU = spline[i].Y / 1000.0f;
            }
        }

        pointLeft = pL;
        pointRight = pR;
        pointUp = pU;
        pointDown = pD;
    }

    public Texture2D GetFlowmap() { return _flowmap; }
    public void RemoveFlowmap() { _flowmap = null; }

    public Vector2 GetFlowmapSize()
    {
        return _flowmapSize;
    }

    public abstract void ResetMaterials();
    public abstract Material GetMaterial();
    public abstract Path GetPolygon();
    public abstract float GetHeight(Vector2 position);

#if UNITY_EDITOR
    private Shader _tempShader;

    public void EnterFlowmapEdit()
    {
        _tempShader = GetMaterial().shader;

        GetMaterial().shader = Shader.Find("Hidden/FlowmapVisual");


        GetMaterial().SetTexture("_Flowmap", _flowmap);
        GetMaterial().SetVector("_FlowmapPosition", _flowmapPosition);
        GetMaterial().SetVector("_FlowmapScale", _flowmapSize);
    }

    public void ExitFlowmapEdit()
    {
        GetMaterial().shader = _tempShader;

        GetMaterial().SetTexture("_Flowmap", _flowmap);
        GetMaterial().SetVector("_FlowmapPosition", _flowmapPosition);
        GetMaterial().SetVector("_FlowmapScale", _flowmapSize);

    }

    public void Paint(Vector2 position, Vector2 direction, int size)
    {
        Vector2 flowMapUV = (position - _flowmapPosition) / _flowmapSize - new Vector2(0.5f, 0.5f);

        Vector2Int flowMapResolution = new Vector2Int(GetFlowmap().width, GetFlowmap().height);

        DrawCircle(GetFlowmap(), new Color((direction.x + 1.0f) / 2.0f, (direction.y + 1.0f) / 2.0f, 0.0f, 1.0f), (int)(flowMapResolution.x * flowMapUV.x), (int)(flowMapResolution.y * flowMapUV.y), size);

        
    }

    public Texture2D DrawCircle(Texture2D tex, Color color, int x, int y, int radius = 20)
    {
        float q =  _flowmapSize.x / _flowmapSize.y;

        float rSquared = radius * (radius/q);

        for (int u = x - radius; u < x + radius + 1; u++)
            for (int v = y - (int)(radius*q); v < y + (radius*q) + 1; v++)
                if ((x - u) * (x - u) + (y/q - v/q) * (y/q - v/q) < rSquared)
                    tex.SetPixel(u, v, color);

        tex.Apply();

        return tex;
    }
#endif

}
