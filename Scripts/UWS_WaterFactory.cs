using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

using ClipperLib;
using Poly2Tri;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using System.Threading;
using System;
//using System.Diagnostics;

public class UWS_WaterFactory
{
    public List<UWS_WaterObject> Objects = new List<UWS_WaterObject>();

    public ConcurrentQueue<UWS_WaterFactoryResult> ReturnQueue = new ConcurrentQueue<UWS_WaterFactoryResult>();

    private ConcurrentQueue<Food> FoodQueue = new ConcurrentQueue<Food>();

    private AutoResetEvent FoodEvent = new AutoResetEvent(false);

    private List<Thread> _threads = new List<Thread>();

    public bool IsDynamic = false;

    public int CompletedMeshes = 0;
    private int _foodCount = 0;

    public UWS_WaterFactory(int threadCount)
    {
        for (int i = 0; i < threadCount; i++)
        {
            var _thread = new Thread(ProcessMeshing);
            //_thread.Priority = System.Threading.ThreadPriority.Lowest;
            _threads.Add(_thread);
            _thread.Start();
        }
    }

    public bool IsCompleted()
    {
        if (_foodCount == CompletedMeshes)
        {
            return true;
        } else
        {
            return false;
        }
    }

    public void Reset()
    {
        CompletedMeshes = 0;
        _foodCount = 0;
    }

    public void Feed(UWS_MeshingRequest request)
    {
        foreach (UWS_WaterObject waterObject in Objects)
        {
            _foodCount++;

            var f = new Food();

            f.waterChunk = request.Chunk;
            f.waterObject = waterObject;
            f.Request = request;
            
            FoodQueue.Enqueue(f);
            FoodEvent.Set();
        }
    }

    public void Stop()
    {
        for (int i = 0; i < _threads.Count; i++)
        {
            _threads[i].Abort();
        }
    }

    private Path GetClip(UWS_WaterChunk chunk)
    {
        Path Clip = new Path(4);

        double sd2 = chunk.Size / 2.0;

        Clip.Add(new IntPoint((chunk.Position.x - sd2) * 1000, (chunk.Position.z + sd2) * 1000));
        Clip.Add(new IntPoint((chunk.Position.x + sd2) * 1000, (chunk.Position.z + sd2) * 1000));
        Clip.Add(new IntPoint((chunk.Position.x + sd2) * 1000, (chunk.Position.z - sd2) * 1000));
        Clip.Add(new IntPoint((chunk.Position.x - sd2) * 1000, (chunk.Position.z - sd2) * 1000));
        return Clip;
    }
    
    private void ProcessMeshing()
    {
        while (true)
        {
            bool result;
            Food food;

            result = FoodQueue.TryDequeue(out food);

            if (result)
            {
                Path clip = GetClip(food.waterChunk);

                UWS_Meshing meshing = null;

                if (food.waterObject is UWS_Ocean)
                {
                    meshing = new UWS_OceanMeshing();
                }
                else if (food.waterObject is UWS_Lake)
                {
                    meshing = new UWS_LakeMeshing();
                }
                else if (food.waterObject is UWS_Pool)
                {
                    meshing = new UWS_PoolMeshing();
                }
                else if (food.waterObject is UWS_River)
                {
                    meshing = new UWS_RiverMeshing();
                } else if (food.waterObject is UWS_Island)
                {
                    Interlocked.Increment(ref CompletedMeshes);
                }

                if (meshing != null)
                {
                    meshing.GenerateMesh(this, ReturnQueue, food.waterObject, clip, food.waterChunk, Objects, food.Request);
                }
            } else
            {
                FoodEvent.WaitOne();
            }
        }

        
        

    }

    private struct Food
    {
        public UWS_WaterChunk waterChunk;
        public UWS_WaterObject waterObject;
        public UWS_MeshingRequest Request;
    }

    
}
