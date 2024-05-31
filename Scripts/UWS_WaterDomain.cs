using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class UWS_WaterDomain : MonoBehaviour
{
    public static UWS_WaterDomain s_Instance;

    public GameObject ChunkPrefab;

    private Dictionary<UWS_WaterChunk, List<GameObject>> _chunks = new Dictionary<UWS_WaterChunk, List<GameObject>>();

    public string RendererName = "UWS_RendererMobile";
    
    public List<UWS_Renderer.Property> RendererSettings;

    public UWS_Renderer _renderer;

    public UnityEvent AsynchronousLoadingFinishedEvent;

    public float Scale = 100.0f;
    public float LodCoefficient = 1.0f;
    public int LodDepth = 5;

    public int MeshingResolutionLowDetail = 2;

    public int MeshingResolutionMidDetailLayerNumber = 5;
    public int MeshingResolutionMidDetail = 2;

    public int MeshingResolutionHighDetailLayerNumber = 3;
    public int MeshingResolutionHighDetail = 2;

    public UWS_ReflectionType ReflectionType = UWS_ReflectionType.Planar;

    public float TextureScale = 1.0f;
    public int CullingMask = ~(1 << 4);
    public bool Hdr = false;
    public float ClipPlaneOffset = 0.07f;

    public bool EnableUnderwaterEffects = false;
    public float UnderwaterTransparency = 50.0f;
    public Color UnderwaterColor = new Color(0.6f, 0.87f, 0.9f);
    public float UnderwaterTurbidity = 0.9f;
    public Color UnderwaterTurbidityColor = new Color(0.3f, 0.4f, 0.5f);


    public int FactoryThreadCount = 3;

    private UWS_PlanarReflection _planarReflection;

    private UWS_WaterChunk _tree;

    private UWS_TreeThread _treeThread;

    private List<UWS_WaterObject> _objects = new List<UWS_WaterObject>();

    private List<GameObject> _toDestroy = new List<GameObject>();

    private UWS_WaterFactory _factory;

    private int _state = 0;

    private bool _loaded = false;

    public void RegisterObject(UWS_WaterObject obj)
    {
        if (!_objects.Contains(obj))
        {
            _objects.Add(obj);
        }
        
    }

    public void OnGUI()
    {

       // GUI.DrawTexture(new Rect(10, 10, 100, 100), ((UWS_RendererHighEnd)_renderer)._fft.GetDisplacementTexture(), ScaleMode.ScaleToFit, true, 1.0F);

    }

    public void RemoveObject(UWS_WaterObject obj)
    {
        if (_objects.Contains(obj))
        {
            _objects.Remove(obj);
        }
    }


    private void OnValidate()
    {
        if (_renderer != null && _renderer.GetType().Name != RendererName)
        {
            if (_renderer.IsRequireMeshing())
            {
                _treeThread.Stop();
                _factory.Stop();
            }


            RemoveMeshesUnsafe();

            OnEnable();

            for (int i = 0; i < _objects.Count; i++)
            {
                _objects[i].ResetMaterials();
            }
        }
    }

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

    private void Awake()
    {

        s_Instance = this;
    }
#if UNITY_EDITOR
    private float _scheduleTime = 0.0f;
    

    public void ScheduleRebuild()
    {
        _scheduleTime = Time.realtimeSinceStartup;
    }
#endif

    private void OnEnable()
    {
        _loaded = false;

        //print(~(1 << 4));

        //LodDepth = 11;

        s_Instance = this;

        if (ReflectionType == UWS_ReflectionType.Planar)
        {
            _planarReflection = new UWS_PlanarReflection(TextureScale, CullingMask, Hdr, ClipPlaneOffset);
            Shader.EnableKeyword("REFLECTION_PLANAR");
            Shader.DisableKeyword("REFLECTION_NO");
            Shader.DisableKeyword("REFLECTION_CUBEMAP");

        } else if (ReflectionType == UWS_ReflectionType.Ligth)
        {
            Shader.DisableKeyword("REFLECTION_PLANAR");
            Shader.EnableKeyword("REFLECTION_NO");
            Shader.DisableKeyword("REFLECTION_CUBEMAP");

        }
        else if (ReflectionType == UWS_ReflectionType.Cubemap)
        {
            Shader.DisableKeyword("REFLECTION_PLANAR");
            Shader.DisableKeyword("REFLECTION_NO");
            Shader.EnableKeyword("REFLECTION_CUBEMAP");

        }

        _renderer = (UWS_Renderer)Activator.CreateInstance(GetRendererType(RendererName));

        if (RendererSettings == null || RendererSettings.Count == 0 || !CheckProperties())
        {
            RendererSettings = _renderer.GetProperties();
        }

        _renderer.Initialize(RendererSettings);

        _factory = new UWS_WaterFactory(FactoryThreadCount);

        _tree = new UWS_WaterChunk();

        _tree.Position = new Vector3(0.0f, 0.0f, 0.0f);
        _tree.Size = Scale;
        _tree.HasChildrens = false;
        _tree.Depth = 1;

        if (_renderer.IsRequireMeshing())
        {
            _factory.IsDynamic = true;

            _treeThread = new UWS_TreeThread(_tree);

            _treeThread.LodCoefficient = LodCoefficient;
            _treeThread.MaxDepth = LodDepth;

            _treeThread.Start();
            _treeThread.Request.Set();

        } else
        {
            _factory.IsDynamic = false;

            StartCoroutine(CreateMeshes());
        }
    }

    private IEnumerator CreateMeshes()
    {

        yield return null;

        
        UWS_MeshingRequest meshingRequest = new UWS_MeshingRequest(true, _tree, false, false, false, false);

        _factory.Objects = _objects;

        _factory.Feed(meshingRequest);

        _chunks.Add(meshingRequest.Chunk, new List<GameObject>());

        while (!_factory.IsCompleted())
        {
            yield return null;
        }

        bool result;
        UWS_WaterFactoryResult resultRequest;

        result = _factory.ReturnQueue.TryDequeue(out resultRequest);

        while (result)
        {
            InstanceMesh(resultRequest);

            result = _factory.ReturnQueue.TryDequeue(out resultRequest);

        }

        _factory.Stop();
        
        AsynchronousLoadingFinishedEvent.Invoke();
        
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (_scheduleTime != 0.0f)
        {
            if (Time.realtimeSinceStartup >= _scheduleTime + 5.0f)
            {
                _scheduleTime = 0.0f;

                if (_renderer.IsRequireMeshing())
                {
                    _treeThread.Stop();
                    _factory.Stop();
                }


                RemoveMeshesUnsafe();

                OnEnable();

                for (int i = 0; i < _objects.Count; i++)
                {
                    _objects[i].ResetMaterials();
                }
            }
        }
#endif

        if (_renderer.IsRequireMeshing())
        {
            UpdateCameras();

            if (_state == 0)
            {
                DequeueMeshing();

            }
            else if (_state == 1)
            {
                CheckFactoryResults();
            }
        }

        UpdateProjection();
        UpdateAmbientLight();

        

        _renderer.Update();
    }

    public void OnWillRenderObject()
    {
        Camera camera = Camera.current;

        if (camera != null)
        {
            if (ReflectionType == UWS_ReflectionType.Planar)
            {
                _planarReflection.UpdateCamera(camera);
            }
        }

    }

    private void UpdateProjection()
    {
        Camera camera = Camera.current;

        if (camera != null)
        {
            Shader.SetGlobalMatrix("_CameraProjection", camera.projectionMatrix);
        }
    }

    private void UpdateAmbientLight()
    {
        UnityEngine.Rendering.SphericalHarmonicsL2 sh;
        LightProbes.GetInterpolatedProbe(Vector3.zero, null, out sh);
        var ambient = new Vector3(sh[0, 0] - sh[0, 6], sh[1, 0] - sh[1, 6], sh[2, 0] - sh[2, 6]);
        ambient = Vector3.Max(ambient, Vector3.zero);
        Shader.SetGlobalVector("_AmbientLight", ambient);
    }

    private void UpdateCameras()
    {
        if (_treeThread.CamerasFeed.Count == 0)
        {
            var positions = new List<Vector3>();

            for (int i = 0; i < Camera.allCamerasCount; i++)
            {
                positions.Add(Camera.allCameras[i].transform.position);
            }

            _treeThread.CamerasFeed.Enqueue(positions);
        }
    }

    private void CheckFactoryResults()
    {
        if (_treeThread.IsCompleted && _factory.IsCompleted())
        {
            bool result;
            UWS_WaterFactoryResult request;

            result = _factory.ReturnQueue.TryDequeue(out request);

            while (result)
            {
                InstanceMesh(request);
                
                result = _factory.ReturnQueue.TryDequeue(out request);

            }

            foreach (GameObject g in _toDestroy)
            {
                UWS_Utility.SafeDestroy(g);
            }

            _treeThread.IsCompleted = false;
            
            _treeThread.Request.Set();

            _state = 0;

            if (!_loaded)
            {
                _loaded = true;
                AsynchronousLoadingFinishedEvent.Invoke();
            }
        }
    }

    private void InstanceMesh(UWS_WaterFactoryResult request)
    {
        if (!_chunks.ContainsKey(request.Chunk))
        {
            return;
        }

        GameObject g = Instantiate(ChunkPrefab);

        g.hideFlags = HideFlags.HideAndDontSave;

        for (int i = 0; i < g.transform.childCount; i++)
        {
            g.transform.GetChild(i).gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        g.transform.parent = transform;
        //g.transform.position = request.Chunk.Position;
        //g.transform.localScale = new Vector3(request.Chunk.Size, request.Chunk.Size, request.Chunk.Size);

        Mesh m = new Mesh();

        m.vertices = request.vertices.ToArray();
        m.triangles = request.tris.ToArray();

        if (request.uvs != null)
        {
            m.uv = request.uvs.ToArray();
        }

        m.RecalculateNormals();
        m.RecalculateTangents();

        m.Optimize();

        g.GetComponentInChildren<MeshFilter>().mesh = m;

        g.GetComponentInChildren<MeshRenderer>().material = request.WaterObject.GetMaterial();

        Bounds b = m.bounds;

        b.size *= 1.5f;

        m.bounds = b;

        g.GetComponentInChildren<MeshCollider>().sharedMesh = m;

        

        _chunks[request.Chunk].Add(g);
    }

    private void DequeueMeshing()
    {
        if (_treeThread.IsCompleted)
        {
            
            if (_treeThread.MeshingQueue.Count == 0)
            {
                _treeThread.IsCompleted = false;
                _treeThread.Request.Set();
            }

            
            _factory.Reset();
            
            for (int i =  0; i < _treeThread.MeshingQueue.Count; i++)
            {
                ProcessMeshing(_treeThread.MeshingQueue[i]);

            }

            _treeThread.MeshingQueue.Clear();

            _state = 1;
        }

        
        

        
    }

    private void ProcessMeshing(UWS_MeshingRequest request)
    {
        if (request.Create)
        {
            if (_chunks.ContainsKey(request.Chunk))
            {
                foreach (GameObject g in _chunks[request.Chunk])
                {
                    _toDestroy.Add(g);
                }

                _chunks.Remove(request.Chunk);
            }

            _factory.Objects = _objects;

            _factory.Feed(request);

            _chunks.Add(request.Chunk, new List<GameObject>());

            //_chunks.Add(request.Chunk, new List<GameObject>());

        } else
        {
            if (_chunks.ContainsKey(request.Chunk))
            {
                foreach (GameObject g in _chunks[request.Chunk])
                {
                    _toDestroy.Add(g);
                }

                _chunks.Remove(request.Chunk);
            }
        }
    }

    public bool Raycast(Vector3 position, out float heigth)
    {
        int layerMask = 1 << 4;

        RaycastHit hit;

        position.y = 1000.0f;

        if (Physics.Raycast(position, Vector3.down, out hit, Mathf.Infinity, layerMask))
        {
            heigth = hit.point.y;
            return true;
        }
        else
        {
            heigth = 0.0f;
            return false;
        }

        
    }

    private void RemoveMeshes()
    {
        foreach (List<GameObject> l in _chunks.Values)
        {
            foreach (GameObject g in l)
            {
                UWS_Utility.SafeDestroy(g);
            }
        }

        _chunks.Clear();
    }

    private void RemoveMeshesUnsafe()
    {
        foreach (List<GameObject> l in _chunks.Values)
        {
            foreach (GameObject g in l)
            {

#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    UWS_Utility.SafeDestroy(g);
                };
#else
                Destroy(g);
#endif

            }
        }

        _chunks.Clear();
    }

    private void Clean()
    {
        if (_renderer.IsRequireMeshing())
        {
            _treeThread.Stop();
            _factory.Stop();
        }




        RemoveMeshes();
    }

    private void OnDisable()
    {
        Clean();

        s_Instance = null;
    }

    private static string[] _avaiableRenderers = null;
    private static Type[] _avaiableRenderersTypes = null;

    public static string[] GetAvaiableRenderers()
    {
        if (_avaiableRenderers != null)
        {
            return _avaiableRenderers;
        }

        Type[] types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(domainAssembly => domainAssembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(UWS_Renderer))
            ).ToArray();

        _avaiableRenderersTypes = types;

        string[] typeNames = new string[types.Length];

        for (int i = 0; i < types.Length; i++)
        {
            typeNames[i] = types[i].Name;
        }

        _avaiableRenderers = typeNames;

        return _avaiableRenderers;
    }

    private Type GetRendererType(string rendererName)
    {
        for (int i = 0; i < GetAvaiableRenderers().Length; i++)
        {
            if (GetAvaiableRenderers()[i] == rendererName)
            {
                return _avaiableRenderersTypes[i];
            }
        }

        return null;
    }
}
