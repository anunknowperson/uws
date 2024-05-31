using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UWS_CameraEffects : MonoBehaviour
{
    private Material _material;

    private MeshRenderer _r;



    void OnEnable()
    {
        StartCoroutine(Registration());
        
    }

    private IEnumerator Registration()
    {
        while (UWS_WaterDomain.s_Instance == null)
        {
            yield return null;
        }

        GameObject _obj = Instantiate(Resources.Load<GameObject>("Prefabs/UnderwaterEffect"));

        _obj.transform.parent = transform;


        _obj.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
        _obj.transform.localPosition = new Vector3(0.0f, 0.0f, GetComponent<Camera>().nearClipPlane + 0.01f);

        //_obj.transform.transform.rotation = GetComponent<Camera>().transform.rotation;
        //_obj.transform.Translate(_obj.transform.transform.TransformDirection(Vector3.forward) * (GetComponent<Camera>().nearClipPlane + 0.01f));
        

        _r = GetComponentInChildren<MeshRenderer>();

        _material = _r.material;

        _material.SetColor("_WaterColor", UWS_WaterDomain.s_Instance.UnderwaterColor);
        _material.SetColor("_TurbidityColor", UWS_WaterDomain.s_Instance.UnderwaterTurbidityColor);

        _material.SetFloat("_Transparency", UWS_WaterDomain.s_Instance.UnderwaterTransparency);
        _material.SetFloat("_Turbidity", UWS_WaterDomain.s_Instance.UnderwaterTurbidity);


    }

    public void Update()
    {
        if (_r == null)
        {
            return;
        }

        float h;

        if (UWS_WaterDomain.s_Instance.Raycast(transform.position, out h))
        {
            if (transform.position.y <= h)
            {
                _r.enabled = true;
            } else
            {
                _r.enabled = false;
            }
        } else
        {
            _r.enabled = false;
        }
    }
}
