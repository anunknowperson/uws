using ClipperLib;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

using Poly2Tri;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using System.Threading;

public class UWS_PoolMeshing : UWS_Meshing
{
    public override void GenerateMesh(UWS_WaterFactory factory, ConcurrentQueue<UWS_WaterFactoryResult> returnQueue, UWS_WaterObject obj, List<IntPoint> clip, UWS_WaterChunk chunk, List<UWS_WaterObject> objects, UWS_MeshingRequest request)
    {
        UWS_MeshingShape shape = new UWS_MeshingShape();

        shape.SetClip(clip);

        shape.AddGeometry(obj.GetPolygon());

        Paths meshes;
        Dictionary<Path, Path> holes;

        shape.ReturnGeometry(out meshes, out holes);

        ProcessMeshing(factory, returnQueue, chunk, obj, meshes, holes, request);
    }
}
