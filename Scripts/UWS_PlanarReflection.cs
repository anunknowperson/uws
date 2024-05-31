using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UWS_PlanarReflection
{
    private GameObject _reflectionCameraObject;
    private Camera _reflectionCamera;

    private RenderTexture _reflectionTexture;
    private float _textureScale;
    private int _cullingMask;
    private bool _hdr;
    private float _clipPlaneOffset;

    private Vector3 _oldPosition;
    private Transform _transform; // TODO: heigth

    public UWS_PlanarReflection(float textureScale, int cullingMask, bool hdr, float clipPlaneOffset)
    {
        _textureScale = textureScale;
        _cullingMask = cullingMask;
        _hdr = hdr;
        _clipPlaneOffset = clipPlaneOffset;
    }

    public void UpdateCamera(Camera cam)
    {
        CheckCamera(cam);

        if (cam == null) return;

        GL.invertCulling = true;

        Transform reflectiveSurface = _transform;

        Vector3 eulerA = cam.transform.eulerAngles;

        _reflectionCamera.transform.eulerAngles = new Vector3(-eulerA.x, eulerA.y, eulerA.z);
        _reflectionCamera.transform.position = cam.transform.position;

        Vector3 pos = reflectiveSurface.transform.position;
        pos.y = reflectiveSurface.position.y;
        Vector3 normal = reflectiveSurface.transform.up;
        float d = -Vector3.Dot(normal, pos) - _clipPlaneOffset;
        Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflection = Matrix4x4.zero;
        reflection = CalculateReflectionMatrix(reflection, reflectionPlane);
        _oldPosition = cam.transform.position;
        Vector3 newpos = reflection.MultiplyPoint(_oldPosition);

        _reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

        Vector4 clipPlane = CameraSpacePlane(_reflectionCamera, pos, normal, 1.0f);

        Matrix4x4 projection = cam.projectionMatrix;
        projection = CalculateObliqueMatrix(projection, clipPlane);
        _reflectionCamera.projectionMatrix = projection;

        _reflectionCamera.transform.position = newpos;
        Vector3 euler = cam.transform.eulerAngles;
        _reflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);


        _reflectionCamera.Render();


        GL.invertCulling = false;
    }

    public void CheckCamera(Camera cam)
    {
        _transform = UWS_WaterDomain.s_Instance.transform;

        if (_reflectionCameraObject == null)
        {
            _reflectionTexture = new RenderTexture((int)(Screen.width * _textureScale), (int)(Screen.height * _textureScale), 16, RenderTextureFormat.Default);
            _reflectionTexture.DiscardContents();
            _reflectionCameraObject = new GameObject("UWS Water Reflection Camera");
            _reflectionCameraObject.hideFlags = HideFlags.DontSave;
            _reflectionCameraObject.transform.position = _transform.position;
            _reflectionCameraObject.transform.rotation = _transform.rotation;

            _reflectionCamera = _reflectionCameraObject.AddComponent<Camera>();
            _reflectionCamera.depth = cam.depth - 10;
            _reflectionCamera.renderingPath = cam.renderingPath;
            _reflectionCamera.depthTextureMode = DepthTextureMode.None; //todo check
            _reflectionCamera.cullingMask = _cullingMask;
            _reflectionCamera.allowHDR = _hdr;
            _reflectionCamera.useOcclusionCulling = false;
            _reflectionCamera.enabled = false;
            _reflectionCamera.targetTexture = _reflectionTexture;

            Shader.SetGlobalTexture("_ReflectionTexture", _reflectionTexture);
        }
    }

    public void Clear()
    {
        if (_reflectionCameraObject)
        {
            UWS_Utility.SafeDestroy(_reflectionCameraObject);
            _reflectionCameraObject = null;
        }
        if (_reflectionTexture)
        {
            UWS_Utility.SafeDestroy(_reflectionTexture);
            _reflectionTexture = null;
        }
    }

    public static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 q = projection.inverse * new Vector4(
            Sgn(clipPlane.x),
            Sgn(clipPlane.y),
            1.0F,
            1.0F
            );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
        // third row = clip plane - fourth row
        projection[2] = c.x - projection[3];
        projection[6] = c.y - projection[7];
        projection[10] = c.z - projection[11];
        projection[14] = c.w - projection[15];

        return projection;
    }

    public static Matrix4x4 CalculateReflectionMatrix(Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1.0F - 2.0F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2.0F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2.0F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2.0F * plane[3] * plane[0]);

        reflectionMat.m10 = (-2.0F * plane[1] * plane[0]);
        reflectionMat.m11 = (1.0F - 2.0F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2.0F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2.0F * plane[3] * plane[1]);

        reflectionMat.m20 = (-2.0F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2.0F * plane[2] * plane[1]);
        reflectionMat.m22 = (1.0F - 2.0F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2.0F * plane[3] * plane[2]);

        reflectionMat.m30 = 0.0F;
        reflectionMat.m31 = 0.0F;
        reflectionMat.m32 = 0.0F;
        reflectionMat.m33 = 1.0F;

        return reflectionMat;
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * _clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    private static float Sgn(float a)
    {
        if (a > 0.0F)
        {
            return 1.0F;
        }
        if (a < 0.0F)
        {
            return -1.0F;
        }
        return 0.0F;
    }
}
