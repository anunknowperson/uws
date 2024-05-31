using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UWS_Utility : MonoBehaviour
{
    public static void SafeDestroy<T>(T component) where T : UnityEngine.Object
    {
        if (component == null) return;

        if (!Application.isPlaying)
            UnityEngine.Object.DestroyImmediate(component);
        else
            UnityEngine.Object.Destroy(component);
    }
}
