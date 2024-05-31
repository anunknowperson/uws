using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UWS_TransitionEffect
{
    public RenderTexture TransitionTexture;

    private Material _material;

    private Texture2D _temp;

    private int _lastFrame = 0;

    public UWS_TransitionEffect()
    {

        Init();

    }

    private void Init()
    {
        _material = new Material(Shader.Find("Hidden/TransitionEffect"));

        //_temp = new Texture2D(512, 512, TextureFormat.ARGB32, false);
        _temp = Resources.Load<Texture2D>("Textures/Foam3");

        TransitionTexture = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
    }

    public void Update(int frame)
    {
        if (frame != _lastFrame) {
            _lastFrame = frame;
            Render();
        }
    }

    private void Render()
    {
        if (_material == null)
        {
            Init();
        }

        Graphics.Blit(_temp, TransitionTexture, _material);
    }
}
