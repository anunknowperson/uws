using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UWS_FFT
{
    int Resolution
    {
        get
        {
            return _resolution;
        }
        set
        {
            _resolution = value;
            _changed = true;
        }
    }

    Vector2 Wind
    {
        get
        {
            return _wind;
        }
        set
        {
            _wind = value;
            _changed = true;
        }
    }

    float Size
    {
        get
        {
            return _size;
        }
        set
        {
            _size = value;
            _changed = true;
        }
    }

    float Choppiness
    {
        get
        {
            return _choppiness;
        }
        set
        {
            _choppiness = value;
            _changed = true;
        }
    }

    private int _resolution = 512;
    private Vector2 _wind = new Vector2(10.0f, 10.0f);
    private float _size = 200.0f;
    private float _choppiness = 3.6f;

    private bool _changed = true;
    private bool _initial = true;
    private bool _pingPhase = true;

    private RenderTexture _initialSpectrumFramebuffer;
    private RenderTexture _spectrumFramebuffer;
    private RenderTexture _pingPhaseFramebuffer;
    private RenderTexture _pongPhaseFramebuffer;
    private RenderTexture _pingTransformFramebuffer;
    private RenderTexture _pongTransformFramebuffer;
    private RenderTexture _displacementMapFramebuffer;
    private RenderTexture _normalMapFramebuffer;

    private Texture2D _pingPhaseTexture;
    private Texture2D _temp;

    private Material _oceanHorizontalMaterial;
    private Material _oceanVerticalMaterial;
    private Material _initialSpectrumMaterial;
    private Material _phaseMaterial;
    private Material _spectrumMaterial;
    private Material _normalMaterial;

    public UWS_FFT(int resolution, Vector2 wind, float size, float choppiness)
    {

        Initialize(resolution, wind, size, choppiness);
    }

    private void Initialize(int resolution, Vector2 wind, float size, float choppiness)
    {
        _resolution = resolution;
        _wind = wind;
        _size = size;
        _choppiness = choppiness;

        _initialSpectrumFramebuffer = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _spectrumFramebuffer = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _pingPhaseFramebuffer = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _pongPhaseFramebuffer = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _pingTransformFramebuffer = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _pongTransformFramebuffer = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _displacementMapFramebuffer = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);
        _normalMapFramebuffer = new RenderTexture(_resolution, _resolution, 0, RenderTextureFormat.ARGBFloat);

        _initialSpectrumFramebuffer.wrapMode = TextureWrapMode.Repeat;
        _spectrumFramebuffer.wrapMode = TextureWrapMode.Repeat;
        _pingPhaseFramebuffer.wrapMode = TextureWrapMode.Repeat;
        _pongPhaseFramebuffer.wrapMode = TextureWrapMode.Repeat;
        _pingTransformFramebuffer.wrapMode = TextureWrapMode.Repeat;
        _pongTransformFramebuffer.wrapMode = TextureWrapMode.Repeat;
        _displacementMapFramebuffer.wrapMode = TextureWrapMode.Repeat;
        _normalMapFramebuffer.wrapMode = TextureWrapMode.Repeat;

        Shader oceanHorizontalShader = Shader.Find("Hidden/FFT_Subtransform_H");
        _oceanHorizontalMaterial = new Material(oceanHorizontalShader);
        _oceanHorizontalMaterial.SetFloat("transformSize", _resolution);

        Shader oceanVerticalShader = Shader.Find("Hidden/FFT_Subtransform_V");
        _oceanVerticalMaterial = new Material(oceanVerticalShader);
        _oceanVerticalMaterial.SetFloat("transformSize", _resolution);

        Shader initialSpectrumShader = Shader.Find("Hidden/FFT_Initial_Spectrum");
        _initialSpectrumMaterial = new Material(initialSpectrumShader);
        _initialSpectrumMaterial.SetVector("wind", _wind);
        _initialSpectrumMaterial.SetFloat("resolution", _resolution);

        Shader phaseShader = Shader.Find("Hidden/FFT_Ocean_Phase");
        _phaseMaterial = new Material(phaseShader);
        _phaseMaterial.SetFloat("resolution", _resolution);

        Shader spectrumShader = Shader.Find("Hidden/FFT_Ocean_Spectrum");
        _spectrumMaterial = new Material(spectrumShader);
        _spectrumMaterial.SetFloat("resolution", _resolution);
        _spectrumMaterial.SetFloat("choppiness", _choppiness);

        Shader normalShader = Shader.Find("Hidden/FFT_Ocean_Normals");
        _normalMaterial = new Material(normalShader);
        _normalMaterial.SetFloat("resolution", _resolution);

        _temp = new Texture2D(_resolution, _resolution);

        GenerateSeedPhaseTexture();
    }

    public RenderTexture GetDisplacementTexture()
    {
        return _displacementMapFramebuffer;
    }

    public RenderTexture GetNormalTexture()
    {
        return _normalMapFramebuffer;
    }

    

    public void Update()
    {
#if UNITY_EDITOR
        if (!_changed && !_phaseMaterial.HasProperty("size")) // Detect material destroy
        {
            _changed = true;
            _initial = true;
             _pingPhase = true;

             Initialize(_resolution, _wind, _size, _choppiness);
        }
#endif

        Render();
    }

    void Render()
    {
        if (_changed)
        {
            RenderInitialSpectrum();
        }

        RenderWavePhase();
        RenderSpectrum();
        RenderSpectrumFFT();
        RenderNormalMap();
    }

    void GenerateSeedPhaseTexture()
    {
        _pingPhase = true;

        Color[] colors = new Color[_resolution * _resolution];

        for (var i = 0; i < _resolution; i++)
        {
            for (var j = 0; j < _resolution; j++)
            {
                colors[i * _resolution + j] = new Color(UnityEngine.Random.Range(0.0f, 1.0f) * 2.0f * Mathf.PI, 0.0f, 0.0f, 0.0f);
            }
        }

        _pingPhaseTexture = new Texture2D(_resolution, _resolution, TextureFormat.RGBAFloat, false);
        _pingPhaseTexture.SetPixels(colors);
        _pingPhaseTexture.Apply();

    }

    void RenderInitialSpectrum()
    {
        _initialSpectrumMaterial.SetVector("wind", _wind);
        _initialSpectrumMaterial.SetFloat("size", _size);

        Graphics.Blit(_temp, _initialSpectrumFramebuffer, _initialSpectrumMaterial);
    }

    void RenderWavePhase()
    {
        if (_initial)
        {
            _phaseMaterial.SetTexture("phases", _pingPhaseTexture);
            _initial = false;
        }
        else
        {
            _phaseMaterial.SetTexture("phases", _pingPhase ? _pingPhaseFramebuffer : _pongPhaseFramebuffer);
        }

        _phaseMaterial.SetFloat("deltaTime", Time.deltaTime);
        _phaseMaterial.SetFloat("size", _size);

        Graphics.Blit(_temp, _pingPhase ? _pongPhaseFramebuffer : _pingPhaseFramebuffer, _phaseMaterial);
        _pingPhase = !_pingPhase;
    }

    void RenderSpectrum()
    {
        _spectrumMaterial.SetTexture("initialSpectrum", _initialSpectrumFramebuffer);
        _spectrumMaterial.SetTexture("phases", _pingPhase ? _pingPhaseFramebuffer : _pongPhaseFramebuffer);
        _spectrumMaterial.SetFloat("size", _size);

        Graphics.Blit(_temp, _spectrumFramebuffer, _spectrumMaterial);
    }

    void RenderSpectrumFFT()
    {
        int iterations = (int)(Mathf.Log(_resolution, 2) * 2);

        Material program = _oceanHorizontalMaterial;

        RenderTexture frameBuffer;
        RenderTexture inputBuffer;

        for (var i = 0; i < iterations; i++)
        {
            if (i == 0)
            {
                inputBuffer = _spectrumFramebuffer;
                frameBuffer = _pingTransformFramebuffer;
            }
            else if (i == iterations - 1)
            {
                inputBuffer = ((iterations % 2 == 0) ? _pingTransformFramebuffer : _pongTransformFramebuffer);
                frameBuffer = _displacementMapFramebuffer;
            }
            else if (i % 2 == 1)
            {
                inputBuffer = _pingTransformFramebuffer;
                frameBuffer = _pongTransformFramebuffer;
            }
            else
            {
                inputBuffer = _pongTransformFramebuffer;
                frameBuffer = _pingTransformFramebuffer;
            }

            if (i == iterations / 2)
            {
                program = _oceanVerticalMaterial;
            }

            program.SetTexture("input", inputBuffer);

            program.SetFloat("subtransformSize", Mathf.Pow(2, (i % (iterations / 2) + 1)));

            Graphics.Blit(_temp, frameBuffer, program);
        }
    }

    void RenderNormalMap()
    {
        if (_changed)
        {
            _normalMaterial.SetFloat("size", _size);
            _changed = false;
        }

        _normalMaterial.SetTexture("displacementMap", _displacementMapFramebuffer);
        Graphics.Blit(_temp, _normalMapFramebuffer, _normalMaterial);
    }
}
