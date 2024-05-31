using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class UWS_WaterDecal : MonoBehaviour
{
    [Header("Visual Settings")]

    public Texture DecalTexture;

    public float Weigth = 0.5f;

    public bool OverrideHeigth = false;

    private void OnEnable()
    {
        StartCoroutine(Registration());
    }

    public void UpdateDecal()
    {
        UWS_WaterDomain.s_Instance._renderer.RemoveDecal(this);
        UWS_WaterDomain.s_Instance._renderer.RegisterDecal(this);
    }

    private IEnumerator Registration()
    {
        while (UWS_WaterDomain.s_Instance == null)
        {
            yield return null;
        }

        UWS_WaterDomain.s_Instance._renderer.RegisterDecal(this);
    }

    private void OnDisable()
    {
        if (UWS_WaterDomain.s_Instance != null)
        {
            UWS_WaterDomain.s_Instance._renderer.RemoveDecal(this);
        }

    }
}
