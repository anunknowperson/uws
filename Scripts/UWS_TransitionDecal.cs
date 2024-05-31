using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UWS_WaterDecal))]
[ExecuteAlways]
public class UWS_TransitionDecal : MonoBehaviour
{
    public static UWS_TransitionEffect _transitionEffect;

    private UWS_WaterDecal _decal;

    private void OnEnable()
    {
        StartCoroutine(Registration());
    }


    private IEnumerator Registration()
    {
        while (UWS_WaterDomain.s_Instance == null)
        {
            yield return null;
        }

        if (_transitionEffect == null)
        {
            _transitionEffect = new UWS_TransitionEffect();
        }

        _decal = GetComponent<UWS_WaterDecal>();
        _decal.DecalTexture = _transitionEffect.TransitionTexture;

        _decal.UpdateDecal();
    }

        // Update is called once per frame
        void Update()
    {
        if (_transitionEffect == null)
        {
            return;
        }

        _transitionEffect.Update(Time.frameCount);

        _decal.DecalTexture = _transitionEffect.TransitionTexture;
    }
}
