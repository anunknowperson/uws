using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
public class UWS_TreeThread
{
	public int MaxDepth;

	public float LodCoefficient;

    public ConcurrentQueue<List<Vector3>> CamerasFeed;
	public List<UWS_MeshingRequest> MeshingQueue;

	private List<Vector3> Cameras;

	private UWS_WaterChunk _tree;
	private Dictionary<Vector2, UWS_WaterChunk> _grid;
	private List<UWS_MeshingRequest> _updateList;

	private Thread _thread;

	public AutoResetEvent Request;
	public bool IsCompleted = false;
	
	public UWS_TreeThread(UWS_WaterChunk tree)
    {
        _tree = tree;

		_grid = new Dictionary<Vector2, UWS_WaterChunk>();
		_updateList = new List<UWS_MeshingRequest>();

		_grid.Add(new Vector2(0.0f, 0.0f), _tree);

		CamerasFeed = new ConcurrentQueue<List<Vector3>>();
		MeshingQueue = new List<UWS_MeshingRequest>();

		MaxDepth = 5;
		LodCoefficient = 1.0f;

		Request = new AutoResetEvent(false);
	}

	public void Start()
	{
		_thread = new Thread(new ThreadStart(Work));
		_thread.Start();
	}

    private void Work()
    {
		while (true)
		{
			Request.WaitOne();

			var result = false;

			while (!result) {
				result = CamerasFeed.TryDequeue(out Cameras);

				if (Cameras != null && Cameras.Count == 0)
				{
					result = false;
				}
			}

			//_updateList.Clear();

			int count;
			do
			{
				

				count = ProcessChunk(_tree, 0);

				

			} while (count != 0);

			ProcessQueue();

			IsCompleted = true;
		}
    }

	public void Stop()
	{
		_thread.Abort();
	}

	private void AddPendingUpdate(UWS_MeshingRequest request)
	{
		_updateList.Add(request);
	}

    private int ProcessChunk(UWS_WaterChunk chunk, int depth)
    {
		int count = 0;

		if (!chunk.HasChildrens)
		{
			if (depth > MaxDepth)
			{
				return 0;
			}

			var result = false;

			for (int i = 0; i < Cameras.Count; i++)
			{
				var viewer = Cameras[i];

				if (Vector2.Distance(new Vector2(viewer.x, viewer.z), new Vector2(chunk.Position.x, chunk.Position.z)) < Mathf.Sqrt(2) * chunk.Size * LodCoefficient)
				{
					result = true;
					break;
				}
			}

			if (result)
			{
				chunk.Children0 = new UWS_WaterChunk();

				chunk.Children0.Position = new Vector3(chunk.Position.x + chunk.Size * -0.25f, chunk.Position.y, chunk.Position.z + chunk.Size * -0.25f);
				chunk.Children0.Size = chunk.Size / 2.0f;
				chunk.Children0.HasChildrens = false;
				chunk.Children0.Depth = chunk.Depth + 1;
				chunk.Children0.Enabled = true;

				GridAdd(chunk.Children0);
				

				chunk.Children1 = new UWS_WaterChunk();
				chunk.Children1.Position = new Vector3(chunk.Position.x + chunk.Size * -0.25f, chunk.Position.y, chunk.Position.z + chunk.Size * 0.25f);
				chunk.Children1.Size = chunk.Size / 2.0f;
				chunk.Children1.HasChildrens = false;
				chunk.Children1.Depth = chunk.Depth + 1;
				chunk.Children1.Enabled = true;

				GridAdd(chunk.Children1);

				chunk.Children2 = new UWS_WaterChunk();
				chunk.Children2.Position = new Vector3(chunk.Position.x + chunk.Size * 0.25f, chunk.Position.y, chunk.Position.z + chunk.Size * -0.25f);
				chunk.Children2.Size = chunk.Size / 2.0f;
				chunk.Children2.HasChildrens = false;
				chunk.Children2.Depth = chunk.Depth + 1;
				chunk.Children2.Enabled = true;

				GridAdd(chunk.Children2);

				chunk.Children3 = new UWS_WaterChunk();
				chunk.Children3.Position = new Vector3(chunk.Position.x + chunk.Size * 0.25f, chunk.Position.y, chunk.Position.z + chunk.Size * 0.25f);
				chunk.Children3.Size = chunk.Size / 2.0f;
				chunk.Children3.HasChildrens = false;
				chunk.Children3.Depth = chunk.Depth + 1;
				chunk.Children3.Enabled = true;

				GridAdd(chunk.Children3);

				AddPendingUpdate(new UWS_MeshingRequest(true, chunk.Children0));
				AddPendingUpdate(new UWS_MeshingRequest(true, chunk.Children1));
				AddPendingUpdate(new UWS_MeshingRequest(true, chunk.Children2));
				AddPendingUpdate(new UWS_MeshingRequest(true, chunk.Children3));

				

				/*MeshingQueue.Enqueue(new UWS_MeshingRequest(true, chunk.Children0));
				MeshingQueue.Enqueue(new UWS_MeshingRequest(true, chunk.Children1));
				MeshingQueue.Enqueue(new UWS_MeshingRequest(true, chunk.Children2));
				MeshingQueue.Enqueue(new UWS_MeshingRequest(true, chunk.Children3));*/

				chunk.HasChildrens = true;
				chunk.Enabled = false;

				//MeshingQueue.Enqueue(new UWS_MeshingRequest(false, chunk));

				AddPendingUpdate(new UWS_MeshingRequest(false, chunk));

				//GridRemove(chunk);

				count++;
			}
		}
		else
		{
			var noChildSplits = true;

			var newDepth = depth + 1;

			count += ProcessChunk(chunk.Children0, newDepth);
			count += ProcessChunk(chunk.Children1, newDepth);
			count += ProcessChunk(chunk.Children2, newDepth);
			count += ProcessChunk(chunk.Children3, newDepth);

			if (chunk.Children0.HasChildrens || chunk.Children1.HasChildrens || chunk.Children2.HasChildrens || chunk.Children3.HasChildrens)
			{
				noChildSplits = false;
			}
			

			if (noChildSplits)
			{
				var result = false;

				for (int i = 0; i < Cameras.Count; i++)
				{
					var viewer = Cameras[i];

					if (Vector2.Distance(new Vector2(viewer.x, viewer.z), new Vector2(chunk.Position.x, chunk.Position.z)) < Mathf.Sqrt(2) * chunk.Size * LodCoefficient)
					{
						result = true;
						break;
					}
				}


				if (!result)
				{

					/*MeshingQueue.Enqueue(new UWS_MeshingRequest(false, chunk.Children0));
					MeshingQueue.Enqueue(new UWS_MeshingRequest(false, chunk.Children1));
					MeshingQueue.Enqueue(new UWS_MeshingRequest(false, chunk.Children2));
					MeshingQueue.Enqueue(new UWS_MeshingRequest(false, chunk.Children3));*/

					AddPendingUpdate(new UWS_MeshingRequest(false, chunk.Children0));
					AddPendingUpdate(new UWS_MeshingRequest(false, chunk.Children1));
					AddPendingUpdate(new UWS_MeshingRequest(false, chunk.Children2));
					AddPendingUpdate(new UWS_MeshingRequest(false, chunk.Children3));

					chunk.Children0.Enabled = false;
					chunk.Children1.Enabled = false;
					chunk.Children2.Enabled = false;
					chunk.Children3.Enabled = false;

					GridRemove(chunk.Children0);
					GridRemove(chunk.Children1);
					GridRemove(chunk.Children2);
					GridRemove(chunk.Children3);

					chunk.HasChildrens = false;

					/*MeshingQueue.Enqueue(new UWS_MeshingRequest(true, chunk));*/

					AddPendingUpdate(new UWS_MeshingRequest(true, chunk));
					
					//GridAdd(chunk);
					chunk.Enabled = true;

					count++;
				}
			}
		}

		return count;
	}

	private void GridAdd(UWS_WaterChunk chunk)
	{
		_grid[new Vector2(chunk.Position.x, chunk.Position.z)] = chunk;
	}

	private void GridRemove(UWS_WaterChunk chunk)
	{
		if (_grid.ContainsKey(new Vector2(chunk.Position.x, chunk.Position.z)))
		{
			_grid.Remove(new Vector2(chunk.Position.x, chunk.Position.z));
		}
	}

	private void ProcessQueue()
	{
		int len = _updateList.Count;
		
		for (int i = 0; i < len; i++)
		{
			UWS_MeshingRequest request = _updateList[i];

			Vector2 planePosition = new Vector2(request.Chunk.Position.x, request.Chunk.Position.z);

			Vector2 leftPosition = planePosition + new Vector2(-request.Chunk.Size, 0);
			Vector2 upPosition = planePosition + new Vector2(0, request.Chunk.Size);
			Vector2 rightPosition = planePosition + new Vector2(request.Chunk.Size, 0);
			Vector2 downPosition = planePosition + new Vector2(0, -request.Chunk.Size);

			if (_grid.ContainsKey(leftPosition) && _grid[leftPosition].Enabled)
			{ 
				AddPendingUpdate(new UWS_MeshingRequest(true, _grid[leftPosition]));
			}

			if (_grid.ContainsKey(upPosition) && _grid[upPosition].Enabled)
			{
				AddPendingUpdate(new UWS_MeshingRequest(true, _grid[upPosition]));
			}

			if (_grid.ContainsKey(rightPosition) && _grid[rightPosition].Enabled)
			{
				AddPendingUpdate(new UWS_MeshingRequest(true, _grid[rightPosition]));
			}

			if (_grid.ContainsKey(downPosition) && _grid[downPosition].Enabled)
			{
				AddPendingUpdate(new UWS_MeshingRequest(true, _grid[downPosition]));
			}
		}

		for (int i = 0; i < _updateList.Count; i++)
		{
			UWS_MeshingRequest request = _updateList[i];

			bool left = false, up = false, right = false, down = false;

			Vector2 planePosition = new Vector2(request.Chunk.Position.x, request.Chunk.Position.z);

			Vector2 leftPosition = planePosition + new Vector2(-request.Chunk.Size, 0);
			Vector2 upPosition = planePosition + new Vector2(0, request.Chunk.Size);
			Vector2 rightPosition = planePosition + new Vector2(request.Chunk.Size, 0);
			Vector2 downPosition = planePosition + new Vector2(0, -request.Chunk.Size);

			left = !_grid.ContainsKey(leftPosition);
			up = !_grid.ContainsKey(upPosition);
			right = !_grid.ContainsKey(rightPosition);
			down = !_grid.ContainsKey(downPosition);

			for (int j = 0; j < MeshingQueue.Count; j++)
			{
				if (MeshingQueue[j].Chunk == request.Chunk)
				{

						MeshingQueue.RemoveAt(j);

					
					//break;
				}
			}


			MeshingQueue.Add(new UWS_MeshingRequest(request.Chunk.Enabled, request.Chunk, left, up, right, down));
				
			

		}

		_updateList.Clear();
	}
}
