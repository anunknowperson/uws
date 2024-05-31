using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ClipperLib;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using System;

public class UWS_River : UWS_WaterObject
{
    public float Width = 0.5f;

    

    public List<UWS_Renderer.Property> RendererSettings;

    private Material _material;

    

    //List<Vector3> pts = new List<Vector3>();

    private void OnEnable()
    {
        _spline = GetComponent<UWS_Spline>();

        StartCoroutine(Registration());
    }

#if UNITY_EDITOR
    private void Update()
    {

        UWS_WaterDomain.s_Instance._renderer.FixMaterial(_material);

        UWS_WaterDomain.s_Instance._renderer.SetRiverMaterialProperties(_material, RendererSettings);

       // _material.SetFloat("_RiverLength", GetLength());
        SetupFlowmap();

    }

    public void PlaceDecals()
    {
        Vector3 A = _spline.GetPoint(0.0f);
        Vector3 ADir = _spline.GetDirection(0.0f);

        Vector3 B = _spline.GetPoint(1.0f);
        Vector3 BDir = _spline.GetDirection(1.0f);

        SpawnDecal(A, ADir);
        SpawnDecal(B, BDir);
    }


    private void SpawnDecal(Vector3 position, Vector3 direction)
    {
        GameObject d = Instantiate(Resources.Load<GameObject>("Prefabs/AutoPlacedDecal"));

        d.transform.parent = transform;
        d.transform.position = position;
        d.transform.rotation = Quaternion.LookRotation(direction);

        
    }
#endif
    public override void ResetMaterials()
    {
        OnEnable();
    }
    private bool CheckProperties()
    {
        List<UWS_Renderer.Property> real = UWS_WaterDomain.s_Instance._renderer.GetRiverProperties();

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

    private IEnumerator Registration()
    {
        while (UWS_WaterDomain.s_Instance == null)
        {
            yield return null;
        }

        if (RendererSettings == null || RendererSettings.Count == 0 || !CheckProperties())
        {
            RendererSettings = UWS_WaterDomain.s_Instance._renderer.GetRiverProperties();
        }

        _material = UWS_WaterDomain.s_Instance._renderer.GetRiverMaterial();

        UWS_WaterDomain.s_Instance._renderer.SetRiverMaterialProperties(_material, RendererSettings);

        _material.SetFloat("_RiverLength", GetLength());
        SetupFlowmap();

        UWS_WaterDomain.s_Instance.RegisterObject(this);
    }


    


    

    private void OnDisable()
    {
        if (UWS_WaterDomain.s_Instance != null)
        {
            UWS_WaterDomain.s_Instance.RemoveObject(this);
        }

    }
    public void OnValidate()
    {
        if (_material != null)
        {
            if (UWS_WaterDomain.s_Instance == null)
            {
                return;
            }

            UWS_WaterDomain.s_Instance._renderer.SetRiverMaterialProperties(_material, RendererSettings);

            _material.SetFloat("_RiverLength", GetLength());
            _material.SetFloat("_FlowSpeedScale", FlowSpeedScale);

            _material.SetTexture("_Flowmap", _flowmap);
            _material.SetVector("_FlowmapPosition", _flowmapPosition);
            _material.SetVector("_FlowmapScale", _flowmapSize);

            if (UseFlowmap)
            {
                _material.EnableKeyword("FLOW_FLOWMAP");
                _material.DisableKeyword("FLOW_DIRECTION");

            }
            else
            {
                _material.EnableKeyword("FLOW_DIRECTION");
                _material.DisableKeyword("FLOW_FLOWMAP");
            }
        }

    }

    

    private float GetLength()
    {
        float v = 0.0f;

        for (int i = 0; i < Resolution - 1; i+=1)
        {
            Vector3 A = _spline.GetPoint((float)i / 10.0f);
            Vector3 B = _spline.GetPoint((float)(i + 1) / 10.0f);

            v += Vector2.Distance(new Vector2(A.x, A.z), new Vector2(B.x, B.z));
        }

        return v;
    }

    public override Material GetMaterial()
    {
        return _material;
    }

    public override Path GetPolygon()
    {

        //pts.Clear();

        Path points = new Path();

        for (int i = 0; i < Resolution; i++)
        {
            var vector = _spline.GetPoint(i / (float)(Resolution - 1));

            Vector3 direction3 = _spline.GetDirection(i / (float)(Resolution - 1));
            Vector2 direction = new Vector2(direction3.x, direction3.z);

            vector += new Vector3(-direction.y, 0, direction.x).normalized * _spline.GetWidth(i / (float)(Resolution - 1));



            points.Add(new IntPoint(vector.x * 1000, vector.z * 1000));
        }

        for (int i = Resolution - 1; i >= 0; i--)
        {
            var vector = _spline.GetPoint(i / (float)(Resolution - 1));

            Vector3 direction3 = _spline.GetDirection(i / (float)(Resolution - 1));
            Vector2 direction = new Vector2(direction3.x, direction3.z);

            vector -= new Vector3(-direction.y, 0, direction.x).normalized * _spline.GetWidth(i / (float)(Resolution - 1));

            points.Add(new IntPoint(vector.x * 1000, vector.z * 1000));
        }

        int intersectionStart = 0;
        int intersectionEnd = 0;

        Vector2 intersectionPoint = Vector2.zero;

        bool intersection = false;

        do
        {
            intersection = false;

            for (int i = 1; i < points.Count; i++)
            {
                Vector2 A = new Vector3(points[i - 1].X * 0.001f, points[i - 1].Y * 0.001f);
                Vector2 B = new Vector3(points[i].X * 0.001f, points[i].Y * 0.001f);

                for (int j = i + 2; j < points.Count; j++)
                {
                    Vector2 C = new Vector3(points[j - 1].X * 0.001f, points[j - 1].Y * 0.001f);
                    Vector2 D = new Vector3(points[j].X * 0.001f, points[j].Y * 0.001f);


                    

                    if (LineSegementsIntersect(A, B, C, D, out intersectionPoint))
                    {
                        intersection = true;
                        intersectionStart = i - 1;
                        intersectionEnd = j;

                        break;
                    }


                }

                if (intersection)
                {
                    break;
                }
            }

            if (intersection)
            {
                //int a = 0;
                points.RemoveRange(intersectionStart, intersectionEnd - intersectionStart);
                points.Insert(intersectionStart, new IntPoint(intersectionPoint.x * 1000, intersectionPoint.y * 1000));
            }

        } while (intersection);

        /*for (int i = 0; i < points.Count; i++)
        {
            pts.Add(new Vector3(points[i].X * 0.0001f, 0.0f, points[i].Y * 0.0001f));
        }*/

        return points;
    }

    static bool IsOnSegment(double xi, double yi, double xj, double yj,
                        double xk, double yk)
    {
        return (xi <= xk || xj <= xk) && (xk <= xi || xk <= xj) &&
               (yi <= yk || yj <= yk) && (yk <= yi || yk <= yj);
    }

    static int ComputeDirection(double xi, double yi, double xj, double yj,
                                 double xk, double yk)
    {
        double a = (xk - xi) * (yj - yi);
        double b = (xj - xi) * (yk - yi);
        return a < b ? -1 : a > b ? 1 : 0;
    }

    /** Do line segments (x1, y1)--(x2, y2) and (x3, y3)--(x4, y4) intersect? */
    bool DoLineSegmentsIntersect(double x1, double y1, double x2, double y2,
                                 double x3, double y3, double x4, double y4)
    {
        int d1 = ComputeDirection(x3, y3, x4, y4, x1, y1);
        int d2 = ComputeDirection(x3, y3, x4, y4, x2, y2);
        int d3 = ComputeDirection(x1, y1, x2, y2, x3, y3);
        int d4 = ComputeDirection(x1, y1, x2, y2, x4, y4);
        return (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) ||
               (d1 == 0 && IsOnSegment(x3, y3, x4, y4, x1, y1)) ||
               (d2 == 0 && IsOnSegment(x3, y3, x4, y4, x2, y2)) ||
               (d3 == 0 && IsOnSegment(x1, y1, x2, y2, x3, y3)) ||
               (d4 == 0 && IsOnSegment(x1, y1, x2, y2, x4, y4));
    }

    public float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    public bool LineSegementsIntersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2,
    out Vector2 intersection, bool considerCollinearOverlapAsIntersect = false)
    {
        intersection = new Vector2();

        var r = p2 - p;
        var s = q2 - q;
        
        var rxs = Cross(r, s);
        var qpxr = Cross(q - p, r);

        // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
        if (Mathf.Abs(rxs) < 1e-8 && Mathf.Abs(qpxr) < 1e-8)
        {
            // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
            // then the two lines are overlapping,
            if (considerCollinearOverlapAsIntersect)
                if ((0 <= Vector2.Dot((q - p), r) && Vector2.Dot((q - p), r) <= Vector2.Dot(r, r)) || (0 <= Vector2.Dot((p - q), s) && Vector2.Dot((p - q), s) <= Vector2.Dot(s ,s)))
                    return true;

            // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
        if (Mathf.Abs(rxs) < 1e-8 && !(Mathf.Abs(qpxr) < 1e-8))
            return false;

        // t = (q - p) x s / (r x s)
        var t = Cross((q - p), s) / rxs;

        // u = (q - p) x r / (r x s)

        var u = Cross((q - p), r) / rxs;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point p + t r = q + u s.
        if (!(Mathf.Abs(rxs) < 1e-8) && (0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = p + t * r;

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }

    Vector2 nearestPoint(Vector2 start, Vector2 end, Vector2 pnt)
    {
        Vector2 ln = (end - start);
        float len = ln.magnitude;
        ln = ln.normalized;

        Vector2 v = pnt - start;
        float d = Vector2.Dot(v, ln);
        
        d = Mathf.Clamp(d, 0.0f, len);
        return start + ln * d;
    }

    public Vector2 GetDirection(Vector2 position)
    {
        float minv = 0.0f;
        Vector2 direction = Vector2.zero;


        Vector3[] points = new Vector3[Resolution];

        for (int i = 0; i < Resolution; i++)
        {
            points[i] = _spline.GetPoint((float)i / (float)(Resolution - 1));
        }

        for (int i = 0; i < Resolution - 1; i++)
        {
            /*Vector2 P = new Vector2(points[i].x, points[i].z);
            
            Vector2 E = position;
            
            float reqAns = Vector2.Distance(E, P);*/

            Vector2 A = new Vector2(points[i].x, points[i].z);
            Vector2 B = new Vector2(points[i + 1].x, points[i + 1].z);
            Vector2 E = position;

            Vector2 nearest = nearestPoint(A, B, E);


            float reqAns = Vector2.Distance(E, nearest);
            float distance_A = Vector2.Distance(A, nearest);
            float distance_B = Vector2.Distance(B, nearest);

            float t = distance_A / (distance_A + distance_B);
            

            if (i == 0)
            {
                minv = reqAns;

                Vector2 uv;

                Vector3 dir = _spline.GetDirection(t);
                uv = new Vector2(dir.x, dir.z);

                /*float D = (E.x - A.x) * (B.y - A.y) - (E.y - A.y) * (B.x - A.x);

                if (D >= 0)
                {
                    uv.x = 0.5f - reqAns / (Width) / 2.0f;
                } else
                {
                    uv.x = 0.5f + reqAns / (Width) / 2.0f;
                }

                

                float a = (float)i / (float)(Resolution - 1);
                float b = (float)(i + 1) / (float)(Resolution - 1);

                float da = Vector2.Distance(A, nearest);
                float db = Vector2.Distance(nearest, B);



                uv.y = Mathf.Lerp(a, b, da / (da + db));*/

                //Vector3 dir = _spline.GetDirection((float)i / (float)(Resolution - 1));

                //uv = new Vector2(dir.x, dir.z);

                //uv = (A - B).normalized;

                direction = uv;

                /*if (ReverseDirection)
                {
                    direction = (A - B).normalized;
                }
                else
                {
                    direction = (B - A).normalized;
                }*/

            }
            else
            {
                if (reqAns <= minv)
                {
                    minv = reqAns;

                    Vector2 uv;

                    Vector3 dir = _spline.GetDirection(t);
                    uv = new Vector2(dir.x, dir.z);

                    /*float D = (E.x - A.x) * (B.y - A.y) - (E.y - A.y) * (B.x - A.x);

                    if (D >= 0)
                    {
                        uv.x = 0.5f - reqAns / (Width) / 2.0f;
                    }
                    else
                    {
                        uv.x = 0.5f + reqAns / (Width) / 2.0f;
                    }

                    float a = (float)i / (float)(Resolution - 1);
                    float b = (float)(i + 1) / (float)(Resolution - 1);

                    float da = Vector2.Distance(A, nearest);
                    float db = Vector2.Distance(nearest, B);



                    uv.y = Mathf.Lerp(a, b, da / (da + db));*/

                    //Vector3 dir = _spline.GetDirection((float)i / (float)(Resolution - 1));

                    //uv = new Vector2(dir.x, dir.z);


                    direction = uv;

                    /*if (ReverseDirection)
                    {
                        direction = (A - B).normalized;
                    }
                    else
                    {
                        direction = (B - A).normalized;
                    }*/
                }
            }
        }


        return direction ;
    }

    public override float GetHeight(Vector2 position)
    {
        float minv = 0.0f, heigth = 0.0f;

        Vector3[] points = new Vector3[Resolution];

        for (int i = 0; i < Resolution; i++)
        {
            points[i] = _spline.GetPoint((float)i / (float)(Resolution - 1));
        }

        for (int i = 0; i < Resolution - 1; i++)
        {
            Vector2 A = new Vector2(points[i].x, points[i].z);
            Vector2 B = new Vector2(points[i + 1].x, points[i + 1].z);
            Vector2 E = position;

            Vector2 nearest = nearestPoint(A, B, E);


            float reqAns = Vector2.Distance(E, nearest);
            float distance_A = Vector2.Distance(A, nearest);
            float distance_B = Vector2.Distance(B, nearest);

            float t = distance_A / (distance_A + distance_B);
            float h = Mathf.Lerp(points[i].y, points[i + 1].y, t);

            if (i == 0)
            {
                minv = reqAns;
                heigth = h;
            } else
            {
                if (reqAns < minv)
                {
                    minv = reqAns;
                    heigth = h;
                }
             }
        }

        if (float.IsNaN(heigth) || float.IsInfinity(heigth))
        {
            return 100;
        }

        return heigth;
    }
}