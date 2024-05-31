using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UWS_MeshingRequest
{
    public bool Create;
    public UWS_WaterChunk Chunk;

    public bool Left;
    public bool Up;
    public bool Right;
    public bool Down;

    public UWS_MeshingRequest(bool create, UWS_WaterChunk chunk)
    {
        Create = create;
        Chunk = chunk;
    }

    public UWS_MeshingRequest(bool create, UWS_WaterChunk chunk, bool left, bool up, bool right, bool down)
    {
        Create = create;
        Chunk = chunk;

        Left = left;
        Up = up;
        Right = right;
        Down = down;
    }
}
