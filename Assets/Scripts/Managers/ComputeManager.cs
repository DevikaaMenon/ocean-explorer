using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class ComputeManager : MonoBehaviour {
    #region SINGLETON
    public static ComputeManager Instance { get; private set; }

    public void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            if (Application.isPlaying) {
                Destroy(this);
            } else {
                DestroyImmediate(this);
            }
        }
    }
    private void OnDestroy() {
        if (Application.isPlaying) {
            DisposeAll();
        }
    }
    private void OnApplicationQuit() {
        DisposeAll();
    }
    #endregion

    private const int _threadGroupSize = 8;
    private const int _bufferNumberMultiplayer = 3;

    private List<DataBuffer> _allDataBuffers;
    private Queue<DataBuffer> _availableDataBuffers;
    private int _bufferNumber = 0;

    [Header("Shaders")]
    public ComputeShader MeshShader;
    public ComputeShader DensityShader;

    [Header("Settings")]
    public NoiseSetting NoiseSetting;
    [Range(1, 10)]
    public int MaxChunksPerFrame = 4;

    public void Initialize() {
        DisposeAll();

        _allDataBuffers = new List<DataBuffer>();
        _availableDataBuffers = new Queue<DataBuffer>();

        for (int i = 0; i < MaxChunksPerFrame * _bufferNumberMultiplayer; i++) {
            CreateNewBuffer(true);
        }
    }

    public void GetTriangles(Chunk chunk, bool force, Action<List<Triangle>> callback) {
        // if the mesh data is present and there were no changes
        if (chunk.IsDataGenerated && !force) {
            callback(null);
            return;
        }
        int numCubesXZ = WorldManager.Instance.WorldSetting.NumCubesXZ;
        int numCubesY = WorldManager.Instance.WorldSetting.NumCubesY;
        int pointsPerAxisXZ = numCubesXZ + 1;
        int pointsPerAxisY = numCubesY + 1;

        DataBuffer buffer = GetBuffer();
        GenerateDensity(buffer, chunk.Position, pointsPerAxisXZ, pointsPerAxisY);
        GenerateTriangles(buffer, chunk.Position, numCubesXZ, numCubesY);

        AsyncGPUReadback.Request(buffer.TriangleBuffer, (readback) => {
            // get number of triangles
            ComputeBuffer.CopyCount(buffer.TriangleBuffer, buffer.TriangleCountBuffer, 0);
            int[] triangleCount = { 0 };
            buffer.TriangleCountBuffer.GetData(triangleCount);
            int numTriangle = triangleCount[0];

            // get triangles
            Triangle[] triangles = new Triangle[numTriangle];
            buffer.TriangleBuffer.GetData(triangles, 0, 0, numTriangle);
            RequeueBuffer(buffer);
            callback(new List<Triangle>(triangles));
        });
    }

    public static void CalculateMesh(Chunk chunk, List<Triangle> triangles) {
        // remove repeating vertices and create indices
        var vertexLookup = new Dictionary<Vector3, int>();
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        foreach (Triangle triangle in triangles) {
            for (int i = 2; i >= 0; i--) {
                Vector3 vertex = triangle[i];
                if (!vertexLookup.ContainsKey(vertex)) {
                    vertexLookup[vertex] = vertices.Count;
                    vertices.Add(vertex);
                }
                indices.Add(vertexLookup[vertex]);
            }
        }
        chunk.SetData(vertices, indices);
    }

    private void GenerateDensity(DataBuffer buffer, Vector3 pos, int pointsPerAxisXZ, int pointsPerAxisY) {
        int numGroupsXZ = Mathf.CeilToInt(pointsPerAxisXZ / (float)_threadGroupSize);
        int numGroupsY = Mathf.CeilToInt(pointsPerAxisY / (float)_threadGroupSize);

        var rng = new System.Random(NoiseSetting.Seed);
        var offsets = new Vector3[NoiseSetting.NumOctaves];
        float offsetsScale = 1000;
        for (int i = 0; i < NoiseSetting.NumOctaves; i++) {
            offsets[i] = (new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()) * 2 - Vector3.one) * offsetsScale;
        }
        buffer.OffsetBuffer.SetData(offsets);

        DensityShader.SetBuffer(0, "densities", buffer.DensityBuffer);
        DensityShader.SetBuffer(0, "offsets", buffer.OffsetBuffer);

        DensityShader.SetVector("spaceOffset", NoiseSetting.SpaceOffset);

        float chunkHeight = WorldManager.ChunkHeight();
        DensityShader.SetInt("numOctaves", NoiseSetting.NumOctaves);
        DensityShader.SetFloat("frequency", NoiseSetting.Frequency / 100);
        DensityShader.SetFloat("lacunarity", NoiseSetting.Lacunarity);
        DensityShader.SetFloat("amplitude", NoiseSetting.Amplitude);
        DensityShader.SetFloat("persistence", NoiseSetting.Persistence);

        DensityShader.SetFloat("offsetY", NoiseSetting.OffsetY);
        DensityShader.SetFloat("yWeight", NoiseSetting.YWeight);
        DensityShader.SetFloat("noiseWeight", NoiseSetting.NoiseWeight);
        DensityShader.SetFloat("vWeight", NoiseSetting.VWeight);

        DensityShader.SetFloat("ceilingY", NoiseSetting.CeilingY * WorldManager.Instance.WorldSetting.NumCubesY);
        DensityShader.SetFloat("ceilingWeight", NoiseSetting.CeilingWeight / WorldManager.Instance.WorldSetting.NumCubesY);
        DensityShader.SetFloat("hardCeilingY", NoiseSetting.HardCeilingY * WorldManager.Instance.WorldSetting.NumCubesY);
        DensityShader.SetFloat("hardCeilingWeight", NoiseSetting.HardCeilingWeight);

        DensityShader.SetFloat("floorY", NoiseSetting.FloorY * WorldManager.Instance.WorldSetting.NumCubesY);
        DensityShader.SetFloat("floorWeight", NoiseSetting.FloorWeight / WorldManager.Instance.WorldSetting.NumCubesY);
        DensityShader.SetFloat("hardFloorY", NoiseSetting.HardFloorY * WorldManager.Instance.WorldSetting.NumCubesY);
        DensityShader.SetFloat("hardFloorWeight", NoiseSetting.HardFloorWeight);

        DensityShader.SetFloat("warpFrequency", NoiseSetting.WarpFrequency / 1000);
        DensityShader.SetFloat("warpAmplitude", NoiseSetting.WarpAmplitude);

        DensityShader.SetVector("terracing", new Vector4(NoiseSetting.Terracing.x, NoiseSetting.Terracing.y / WorldManager.Instance.WorldSetting.NumCubesY));

        DensityShader.SetInt("pointsPerAxisXZ", pointsPerAxisXZ);
        DensityShader.SetInt("pointsPerAxisY", pointsPerAxisY);
        DensityShader.SetFloat("scale", WorldManager.CubeSize());
        DensityShader.SetVector("worldPos", new Vector4(pos.x, 0, pos.z));

        DensityShader.Dispatch(0, numGroupsXZ, numGroupsY, numGroupsXZ);

        // DEBUG : check densities
        //float[] data = new float[pointsPerAxisXZ * pointsPerAxisY * pointsPerAxisXZ];
        //buffer.DensityBuffer.GetData(data, 0, 0, pointsPerAxisXZ * pointsPerAxisY * pointsPerAxisXZ);
        //for (int i = 0; i < 16; i++) {
        //    Debug.Log($"[{i}] {data[i]}");
        //}

        //for (int i = pointsPerAxisXZ * pointsPerAxisY * pointsPerAxisXZ / 2; i > pointsPerAxisXZ * pointsPerAxisY * pointsPerAxisXZ / 2 - 16; i--) {
        //    Debug.Log($"[{i}] {data[i]}");
        //}
    }

    private void GenerateTriangles(DataBuffer buffer, Vector3 pos, int numCubesXZ, int numCubesY) {
        int pointsPerAxisXZ = numCubesXZ + 1;
        int pointsPerAxisY = numCubesY + 1;
        int numGroupsXZ = Mathf.CeilToInt(numCubesXZ / (float)_threadGroupSize);
        int numGroupsY = Mathf.CeilToInt(numCubesY / (float)_threadGroupSize);

        buffer.TriangleBuffer.SetCounterValue(0);
        MeshShader.SetBuffer(0, "densities", buffer.DensityBuffer);
        MeshShader.SetBuffer(0, "triangles", buffer.TriangleBuffer);
        MeshShader.SetInt("pointsPerAxisXZ", pointsPerAxisXZ);
        MeshShader.SetInt("pointsPerAxisY", pointsPerAxisY);
        MeshShader.SetFloat("threshold", WorldManager.Instance.WorldSetting.Threshold);
        MeshShader.SetFloat("scale", WorldManager.CubeSize());
        MeshShader.SetVector("worldPos", new Vector4(pos.x, pos.y, pos.z));

        MeshShader.Dispatch(0, numGroupsXZ, numGroupsY, numGroupsXZ);
    }

    #region POOLING
    private DataBuffer GetBuffer() {
        if (_availableDataBuffers.TryDequeue(out DataBuffer buffer)) {
            if (!buffer.Initialized) {
                buffer.Initialize();
            }
            return buffer;
        } else {
            var newBuffer = CreateNewBuffer(false); // consider a mechanism of buffer reduction
            newBuffer.Initialize();
            return newBuffer;
        }
    }

    private DataBuffer CreateNewBuffer(bool enqueue) {
        var buffer = new DataBuffer();
        // buffer.Initialize(); - turned off, data buffers will be lazy loaded
        _allDataBuffers.Add(buffer);

        if (enqueue) {
            _availableDataBuffers.Enqueue(buffer);
        }
        _bufferNumber++;

        return buffer;
    }

    private void RequeueBuffer(DataBuffer buffer) {
        if (!Application.isPlaying) {
            buffer.ReleaseBuffers();
        }
        _availableDataBuffers.Enqueue(buffer);
    }
    #endregion POOLING

    public void DisposeAll() {
        if (_allDataBuffers == null) { return; }
        foreach (DataBuffer buffer in _allDataBuffers) {
            buffer.Dispose();
        }
        _allDataBuffers = null;
#if UNITY_EDITOR
        EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect();
#endif
    }

    #region DATA_BUFFER
    private class DataBuffer : IDisposable {
        public ComputeBuffer DensityBuffer;
        public ComputeBuffer TriangleBuffer;
        public ComputeBuffer TriangleCountBuffer;
        public ComputeBuffer OffsetBuffer;

        public bool Initialized;

        public void Initialize() {
            if (Initialized) { return; }

            int numCubesXZ = WorldManager.Instance.WorldSetting.NumCubesXZ;
            int numCubesY = WorldManager.Instance.WorldSetting.NumCubesY;
            int pointsPerAxisXZ = numCubesXZ + 1;
            int pointsPerAxisY = numCubesY + 1;

            int numPoints = pointsPerAxisXZ * pointsPerAxisY * pointsPerAxisXZ;
            int numCubes = pointsPerAxisXZ * pointsPerAxisY * pointsPerAxisXZ;
            int maxTriangleCount = numCubes * 5;

            if (!Application.isPlaying || (DensityBuffer == null || numPoints != DensityBuffer.count)) {
                if (Application.isPlaying) {
                    DensityBuffer?.Release();
                }
                DensityBuffer = new ComputeBuffer(numPoints, sizeof(float));
            }
            if (!Application.isPlaying || (TriangleBuffer == null || maxTriangleCount != TriangleBuffer.count)) {
                if (Application.isPlaying) {
                    TriangleBuffer?.Release();
                }
                TriangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
            }
            if (!Application.isPlaying || (TriangleCountBuffer == null || 1 != TriangleCountBuffer.count)) {
                if (Application.isPlaying) {
                    TriangleCountBuffer?.Release();
                }
                TriangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            }
            if (!Application.isPlaying || (OffsetBuffer == null || Instance.NoiseSetting.MaxNumOctaves != OffsetBuffer.count)) {
                if (Application.isPlaying) {
                    OffsetBuffer?.Release();
                }
                OffsetBuffer = new ComputeBuffer(Instance.NoiseSetting.MaxNumOctaves, sizeof(float) * 3);
            }

            Initialized = true;
        }

        public void ReleaseBuffers() {
            DensityBuffer?.Release();
            TriangleBuffer?.Release();
            TriangleCountBuffer?.Release();
            OffsetBuffer?.Release();

            Initialized = false;
        }

        public void Dispose() {
            ReleaseBuffers();
            DensityBuffer?.Dispose();
            TriangleBuffer?.Dispose();
            TriangleCountBuffer?.Dispose();
            OffsetBuffer?.Dispose();
        }
    }
    #endregion DATA_BUFFER
}
