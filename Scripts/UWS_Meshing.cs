using ClipperLib;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

using Poly2Tri;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using System.Threading;
using System;

public abstract class UWS_Meshing 
{
    public abstract void GenerateMesh(UWS_WaterFactory factory, ConcurrentQueue<UWS_WaterFactoryResult> returnQueue, UWS_WaterObject obj, Path clip, UWS_WaterChunk chunk, List<UWS_WaterObject> objects, UWS_MeshingRequest request);

    protected void ProcessMeshing(UWS_WaterFactory factory, ConcurrentQueue<UWS_WaterFactoryResult> returnQueue, UWS_WaterChunk chunk, UWS_WaterObject obj, Paths meshes, Dictionary<Path, Path> holes, UWS_MeshingRequest request)
    {
        for (int i = 0; i < meshes.Count; i++)
        {
            if (holes.ContainsKey(meshes[i]))
            {
                ProcessOneShape(factory, returnQueue, chunk, obj, meshes[i], request, holes[meshes[i]]);

            } else
            {
                ProcessOneShape(factory, returnQueue, chunk, obj, meshes[i], request);
            }
        }

        Interlocked.Increment(ref factory.CompletedMeshes);
    }

    int GetResolution(int depth)
    {
        int res = UWS_WaterDomain.s_Instance.MeshingResolutionLowDetail;

        if (UWS_WaterDomain.s_Instance.LodDepth - UWS_WaterDomain.s_Instance.MeshingResolutionMidDetailLayerNumber + 2 < depth)
        {
            res = UWS_WaterDomain.s_Instance.MeshingResolutionMidDetail;
        }

        if (UWS_WaterDomain.s_Instance.LodDepth - UWS_WaterDomain.s_Instance.MeshingResolutionHighDetailLayerNumber + 2 < depth)
        {
            res = UWS_WaterDomain.s_Instance.MeshingResolutionHighDetail;
        }

        return res;
    }

    private void ProcessOneShape(UWS_WaterFactory factory, ConcurrentQueue<UWS_WaterFactoryResult> returnQueue, UWS_WaterChunk chunk, UWS_WaterObject obj, Path mesh, UWS_MeshingRequest request, Path hole = null)
    {


        List<PolygonPoint> ppts = new List<PolygonPoint>();

        foreach (IntPoint intPoint in mesh)
        {
            PolygonPoint triPoint = new PolygonPoint(intPoint.X * 0.001, intPoint.Y * 0.001);
            ppts.Add(triPoint);
        }

        double mStep = chunk.Size / UWS_WaterDomain.s_Instance.MeshingResolutionLowDetail;

        if (UWS_WaterDomain.s_Instance.LodDepth - UWS_WaterDomain.s_Instance.MeshingResolutionMidDetailLayerNumber + 2 < chunk.Depth)
        {
            mStep = chunk.Size / UWS_WaterDomain.s_Instance.MeshingResolutionMidDetail;
        }

        if (UWS_WaterDomain.s_Instance.LodDepth - UWS_WaterDomain.s_Instance.MeshingResolutionHighDetailLayerNumber + 2 < chunk.Depth)
        {
            mStep = chunk.Size / UWS_WaterDomain.s_Instance.MeshingResolutionHighDetail;
        }

        if (factory.IsDynamic)
        {
            

            for (int i = 0; i < ppts.Count; i++)
            {
                PolygonPoint another;

                if (i != ppts.Count - 1)
                {
                    another = ppts[i + 1];
                }
                else
                {
                    another = ppts[0];
                }


                if (ppts[i].X == another.X)
                {
                    double localStep = mStep;

                    if (request.Right && ppts[i].X > chunk.Position.x)
                    {
                        double neighborSize = chunk.Size * 2;

                        int neighborRes = GetResolution(chunk.Depth - 1);

                        localStep = neighborSize / neighborRes;
                    }

                    if (request.Left && ppts[i].X < chunk.Position.x)
                    {
                        double neighborSize = chunk.Size * 2;

                        int neighborRes = GetResolution(chunk.Depth - 1);

                        localStep = neighborSize / neighborRes;
                    }

                    double y;

                    if (another.Y > ppts[i].Y)
                    {
                        y = ppts[i].Y + localStep;
                    }
                    else
                    {
                        y = ppts[i].Y - localStep;
                    }


                    int j = 1;
                    while (true)
                    {

                        if ((another.Y > ppts[i].Y && y >= another.Y) || (another.Y <= ppts[i].Y && y <= another.Y))
                        {
                            break;
                        }


                        //if (ppts[i].Y != y && ((another.Y > ppts[i].Y && y > ppts[i].Y) || (another.Y <= ppts[i].Y && y < ppts[i].Y)))
                        //{
                        if (Math.Abs(ppts[i].Y - y) >= 0.01f && Math.Abs(another.Y - y) >= 0.01f)
                        {

                            ppts.Insert(i + j, new PolygonPoint(ppts[i].X, y));
                            j++;
                        }
                        //}

                        if (another.Y > ppts[i].Y)
                        {
                            y += localStep;
                        }
                        else
                        {
                            y -= localStep;
                        }



                    }

                    i += j - 1;



                }
                else if (ppts[i].Y == another.Y)
                {
                    double localStep = mStep;

                    if (request.Up && ppts[i].Y > chunk.Position.z)
                    {
                        double neighborSize = chunk.Size * 2;

                        int neighborRes = GetResolution(chunk.Depth - 1);

                        localStep = neighborSize / neighborRes;
                    }

                    if (request.Down && ppts[i].Y < chunk.Position.z)
                    {
                        double neighborSize = chunk.Size * 2;

                        int neighborRes = GetResolution(chunk.Depth - 1);

                        localStep = neighborSize / neighborRes;
                    }

                    double x;

                    if (another.X > ppts[i].X)
                    {
                        x = ppts[i].X + localStep;
                    }
                    else
                    {
                        x = ppts[i].X - localStep;
                    }


                    int j = 1;
                    while (true)
                    {
                        if ((another.X > ppts[i].X && x >= another.X) || (another.X <= ppts[i].X && x <= another.X))
                        {
                            break;
                        }

                        //if (ppts[i].X != x && ((another.X > ppts[i].X && x > ppts[i].X) || (another.X <= ppts[i].X && x < ppts[i].X)))
                        //{
                        if (Math.Abs(ppts[i].X - x) >= 0.01f && Math.Abs(another.X - x) >= 0.01f)
                        {
                            ppts.Insert(i + j, new PolygonPoint(x, ppts[i].Y));
                            j++;
                        }

                        //}

                        if (another.X > ppts[i].X)
                        {
                            x += localStep;
                        }
                        else
                        {
                            x -= localStep;
                        }


                    }

                    i += j - 1;
                }
            }
        }
        

        Polygon p = new Polygon(ppts);

        //meshShape.Points.Add(new TriPoint(0, 0));

        if (hole != null)
        {
            List<PolygonPoint> pts = new List<PolygonPoint>();

            foreach (IntPoint intPoint in hole)
            {
                PolygonPoint triPoint = new PolygonPoint(intPoint.X * 0.001, intPoint.Y * 0.001);
                pts.Add(triPoint);
            }

            p.AddHole(new Polygon(pts));
        }

        double sd2 = chunk.Size / 2.0;
        if (factory.IsDynamic)
        {
            for (double x = chunk.Position.x - sd2 + mStep; x <= chunk.Position.x + sd2 - mStep; x += mStep)
            {
                for (double y = chunk.Position.z - sd2 + mStep; y <= chunk.Position.z + sd2 - mStep; y += mStep)
                {
                    if (Clipper.PointInPolygon(new IntPoint(x * 1000, y * 1000), mesh) == 1)
                    {
                        p.AddSteinerPoint(new TriangulationPoint(x * 1, y * 1));

                    }
                }
            }
        }
        try
        {
            P2T.Triangulate(p);
        } catch (Exception)
        {
            int a = 0;
        }
        

        


        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();
        
        for (int i = 0; i < p.Triangles.Count; i++)
        {
            TriangulationPoint v = p.Triangles[i].Points[0];
            
            vertices.Add(new Vector3((float)v.X, 0.0f, (float)v.Y));

            v = p.Triangles[i].Points[1];

            vertices.Add(new Vector3((float)v.X, 0.0f, (float)v.Y));

            v = p.Triangles[i].Points[2];

            vertices.Add(new Vector3((float)v.X, 0.0f, (float)v.Y));

            tris.Add(i * 3 + 2);
            tris.Add(i * 3 + 1);
            tris.Add(i * 3 + 0);
        }

        if (obj is UWS_Lake)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                float h = ((UWS_Lake)obj).GetHeight(new Vector2(vertices[i].x, vertices[i].z));

                vertices[i] = new Vector3(vertices[i].x, h, vertices[i].z);
            }
        }
        else if (obj is UWS_Pool)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                float h = ((UWS_Pool)obj).GetHeight(new Vector2(vertices[i].x, vertices[i].z));

                vertices[i] = new Vector3(vertices[i].x, h, vertices[i].z);
            }
        } else if (obj is UWS_Ocean)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                float h = ((UWS_Ocean)obj).GetHeight(new Vector2(vertices[i].x, vertices[i].z));

                vertices[i] = new Vector3(vertices[i].x, h, vertices[i].z);
            }
        }
        else if (obj is UWS_River)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                float h = ((UWS_River)obj).GetHeight(new Vector2(vertices[i].x, vertices[i].z));

                vertices[i] = new Vector3(vertices[i].x, h, vertices[i].z);
            }
        }

        

        UWS_WaterFactoryResult factoryResult = new UWS_WaterFactoryResult();

        factoryResult.vertices = vertices;
        factoryResult.tris = tris;

        /*if (obj is UWS_River && !((UWS_River)obj).UseFlowmap)
        {
            List<Vector2> uvs = new List<Vector2>();


            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 d = ((UWS_River)obj).GetDirection(new Vector2(vertices[i].x, vertices[i].z));

                uvs.Add(d);
            }

            factoryResult.uvs = uvs;
        }*/

        factoryResult.Chunk = chunk;
        factoryResult.WaterObject = obj;

        
        returnQueue.Enqueue(factoryResult);
        
        
    }

    protected bool PolygonInPolygon(Path a, Path b)
    {
        bool result = true;

        foreach (IntPoint p in a)
        {
            if (Clipper.PointInPolygon(p, b) == 0)
            {
                result = false;
            }
        }

        return result;
    }
}
