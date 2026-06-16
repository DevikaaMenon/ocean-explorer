// credits:
// Pixel Rayn's Unity Voxels - Procedural Generation Tutorial: https://www.youtube.com/watch?v=EubjobNVJdM&list=PLxI8V1bns4ExV7K6DIrP8BByNSKDCRivo&index=1
// Sebastian Lague's Coding Adventure: Marching Cubes: https://www.youtube.com/watch?v=M3iI2l0ltbE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Task = System.Threading.Tasks.Task;

[ExecuteInEditMode]
public class WorldManager : MonoBehaviour {
    #region SINGLETON
    public static WorldManager Instance { get; private set; }

    public void Awake() {
        if (Instance == null) {
            Instance = this;
            Initialize();
#if UNITY_EDITOR
            WorldSetting.OnWorldSettingChanged += SettingsChanged;
            NoiseSetting.OnNoiseSettingChanged += SettingsChanged;
            _settingsChanged = false;
#endif
        } else if (Instance != this) {
            if (Application.isPlaying) {
                Destroy(this);
            } else {
                DestroyImmediate(this);
            }
        }
    }

#if UNITY_EDITOR
    private void SettingsChanged() {
        _settingsChanged = true;
    }

    private void OnValidate() {
        SettingsChanged();
    }

    private void OnDestroy() {
        WorldSetting.OnWorldSettingChanged -= SettingsChanged;
        NoiseSetting.OnNoiseSettingChanged -= SettingsChanged;
    }
#endif

    private void OnApplicationQuit() {
        _tokenSource.Cancel();
        _killThreads = true;
        _checkActiveChunksLoop?.Abort();
        Clean();
    }
    #endregion

    public Transform Player;
    private Vector3 _lastPos;
    private Vector3 _lastCheckedPos;

    private GameObject _chunkHolder;
    const string _chunkHolderName = "Chunk Holder";

    private const int _sleepFor = 250;
    private int _mainThreadID;
    private Thread _checkActiveChunksLoop;
    private bool _killThreads = false;
    private bool _performedFirstPass = false;
    private CancellationTokenSource _tokenSource;

    public ConcurrentDictionary<Vector3, Chunk> ActiveChunks;
    private Dictionary<Vector3, Chunk> _chunkPool;
    private ConcurrentQueue<Vector3> _chunksToActivate;
    private ConcurrentQueue<Vector3> _chunksToDeactivate;
    private ConcurrentQueue<(List<Triangle> triangles, Chunk chunk)> _trianglesToProcess;
    private Queue<(Chunk chunk, Task task)> _chunksToSet;

    private bool _settingsChanged;
    [Header("Settings")]
    public WorldSetting WorldSetting;
    [Range(1, 10)]
    public int RenderDistance = 3;
    [Range(0, 5)]
    public int RenderDistanceOffset = 2;
    public Vector2Int FixedMapSize = new(3, 3);

    public float GroundLevel = 0;
    [SerializeField]
    private float SurfaceLevelOffset = 30;

    [HideInInspector]
    public float SurfaceLevel {
        get => GroundLevel + ChunkHeight() + SurfaceLevelOffset;
    }

    //[Header("Gizmos")]
    //public bool ShowGizmos;
    //public Color GizmosColor;

    private void Update() {
        if (Application.isPlaying) {
            if (Player.transform.position != _lastPos) {
                _lastPos = PosToChunkPos(Player.transform.position);
            }
        }
#if UNITY_EDITOR
        else if (_settingsChanged) {
            _settingsChanged = false;
            RestoreWorld();
        }
#endif
        Run();
    }

    private void Run() {
        Vector3 workChunkPos;
        while (_chunksToDeactivate.TryDequeue(out workChunkPos)) {
            DeactivateChunk(workChunkPos);
        }

        for (int i = 0; i < ComputeManager.Instance.MaxChunksPerFrame; i++) {
            if (_chunksToActivate.TryDequeue(out workChunkPos)) {
                Chunk chunk = GetChunk(workChunkPos);
                ComputeManager.Instance.GetTriangles(chunk, false, (triangles) => {
                    if (triangles != null) {
                        _trianglesToProcess.Enqueue((triangles, chunk));
                        // Debug.Log($"Generated chunk [{chunk.Position.x}, {chunk.Position.y}, {chunk.Position.z}]!");
                    }
                });
            } else { break; }
        }

        int j = 0;
        while (j < ComputeManager.Instance.MaxChunksPerFrame && _trianglesToProcess.TryDequeue(out var tuple)) {
            var task = Task.Run(() => {
                _tokenSource.Token.ThrowIfCancellationRequested();
                ComputeManager.CalculateMesh(tuple.chunk, tuple.triangles);
            }, _tokenSource.Token);
            _chunksToSet.Enqueue((tuple.chunk, task));
            j++;
        }

        j = 0;
        while (j < ComputeManager.Instance.MaxChunksPerFrame && _chunksToSet.TryDequeue(out var tuple)) {
            tuple.task.Wait();
            tuple.chunk.SetMesh();
            CoralManager.Instance.SpawnResources(tuple.chunk);
            if (!ActiveChunks.TryAdd(tuple.chunk.Position, tuple.chunk)) {
                Debug.Log($"Failed to add chunk [{tuple.chunk.Position.x}, {tuple.chunk.Position.y}, {tuple.chunk.Position.z}] to active chunks!");
            }
            j++;
        }
    }

    private void Initialize() {
        ActiveChunks = new ConcurrentDictionary<Vector3, Chunk>();
        _chunkPool = new Dictionary<Vector3, Chunk>();
        _chunksToActivate = new ConcurrentQueue<Vector3>();
        _chunksToDeactivate = new ConcurrentQueue<Vector3>();
        _trianglesToProcess = new ConcurrentQueue<(List<Triangle>, Chunk)>();
        _chunksToSet = new Queue<(Chunk, Task)>();
        _mainThreadID = Thread.CurrentThread.ManagedThreadId;
        _tokenSource = new();
    }

    private void Start() {
        MakeFreshChunkHolder();

        ComputeManager.Instance.Initialize();

        if (_checkActiveChunksLoop != null && _checkActiveChunksLoop.IsAlive) {
            _checkActiveChunksLoop.Abort();
        }
        _checkActiveChunksLoop = new Thread(CheckActiveChunks) {
            Priority = System.Threading.ThreadPriority.BelowNormal
        };
        if (Application.isPlaying) {
            _checkActiveChunksLoop.Start();

            // initialize some chunks
            int renderDist = RenderDistance + RenderDistanceOffset;
            int totalChunks = renderDist * renderDist;
            for (int i = 0; i < totalChunks; i++) {
                CreateChunk(new Vector3(i, i, i), true);
            }
        }
#if UNITY_EDITOR
        else {
            SetFixedMap();
        }
#endif
    }

#if UNITY_EDITOR
    private void RestoreWorld() {
        Initialize();
        MakeFreshChunkHolder();
        SetFixedMap();
    }

    private void SetFixedMap() {
        var down = -FixedMapSize / 2;
        var up = FixedMapSize + down;
        for (int x = down.x; x < up.x; x++) {
            for (int z = down.y; z < up.y; z++) {
                Chunk chunk = CreateChunk(new Vector3(x * WorldSetting.ChunkSize, GroundLevel, z * WorldSetting.ChunkSize), true);
                if (!ActiveChunks.ContainsKey(chunk.Position)) {
                    _chunksToActivate.Enqueue(chunk.Position);
                }
            }
        }
    }
#endif

    private Chunk CreateChunk(Vector3 pos, bool add) {
        if (Thread.CurrentThread.ManagedThreadId != _mainThreadID) {
            _chunksToActivate.Enqueue(pos);
            return null;
        }

        var chunkObj = new GameObject($"Chunk ({pos.x}, {pos.y}, {pos.z})");
        chunkObj.transform.parent = _chunkHolder.transform;
        Chunk newChunk = chunkObj.AddComponent<Chunk>();
        newChunk.Position = pos;
        newChunk.name = $"Chunk ({newChunk.Position.x}, {newChunk.Position.y}, {newChunk.Position.z})";
        newChunk.Initialize();

        if (add) {
            MoveToPool(newChunk);
        }
        return newChunk;
    }

    private void CheckActiveChunks() {
#if UNITY_EDITOR
        Profiler.BeginThreadProfiling("Chunks", "ChunkChecker");
#endif
        int renderDiameterPlusOne = 2 * RenderDistance + 1;
        var chunkBounds = new Bounds {
            size = new Vector3(renderDiameterPlusOne * WorldSetting.ChunkSize, ChunkHeight(), renderDiameterPlusOne * WorldSetting.ChunkSize)
        };

        Vector3 pos = Vector3.zero;
        // this is the main loop
        while (true && !_killThreads) {
            Thread.Sleep(_sleepFor);

            if (_lastCheckedPos != _lastPos || !_performedFirstPass) {
                _lastCheckedPos = _lastPos;

                for (int x = -RenderDistance; x < RenderDistance; x++) {
                    for (int z = -RenderDistance; z < RenderDistance; z++) {
                        pos.x = x * WorldSetting.ChunkSize + _lastCheckedPos.x;
                        pos.y = GroundLevel;
                        pos.z = z * WorldSetting.ChunkSize + _lastCheckedPos.z;

                        // add chunks to activate
                        if (!ActiveChunks.ContainsKey(pos)) {
                            _chunksToActivate.Enqueue(pos);
                        }
                    }
                }
                chunkBounds.center = _lastCheckedPos;

                // add chunks to deactivate
                foreach (Vector3 vec in ActiveChunks.Keys) {
                    if (!chunkBounds.Contains(vec)) {
                        _chunksToDeactivate.Enqueue(vec);
                    }

                }
            }

            if (!_performedFirstPass) {
                _performedFirstPass = true;
            }
        }
#if UNITY_EDITOR
        Profiler.EndThreadProfiling();
#endif
    }

    private Chunk GetChunk(Vector3 pos) {
        if (_chunkPool.TryGetValue(pos, out Chunk chunk)) {
            _chunkPool.Remove(pos);
            chunk.Position = pos;
            chunk.name = $"Chunk ({chunk.Position.x}, {chunk.Position.y}, {chunk.Position.z})";
            return chunk;
        }

        chunk = _chunkPool.Values.FirstOrDefault();
        if (chunk != null) {
            _chunkPool.Remove(chunk.Position);
            chunk.IsDataGenerated = false;
            chunk.Position = pos;
            chunk.name = $"Chunk ({chunk.Position.x}, {chunk.Position.y}, {chunk.Position.z})";
            return chunk;
        }

        return CreateChunk(pos, false);
    }

    private void MoveToPool(Chunk chunk) {
        chunk.gameObject.SetActive(false);
        _chunkPool.Add(chunk.Position, chunk);
    }

    private void DeactivateChunk(Vector3 pos) {
        if (ActiveChunks.TryRemove(pos, out Chunk chunk)) {
            chunk.DisposeMesh();
            MoveToPool(chunk);
            // Debug.Log($"Deactivated chunk [{chunk.Position.x}, {chunk.Position.y}, {chunk.Position.z}]!");
        } else {
            Debug.Log($"Chunk [{chunk.Position.x}, {chunk.Position.y}, {chunk.Position.z}] was already not active!");
        }
    }

    private void MakeFreshChunkHolder() {
        if (_chunkHolder == null) {
            _chunkHolder = GameObject.Find(_chunkHolderName);
            if (_chunkHolder == null) {
                _chunkHolder = new(_chunkHolderName);
            }
        }

        // going backwards is crucial!
        for (int i = _chunkHolder.transform.childCount - 1; i >= 0; i--) {
            if (Application.isPlaying) {
                Destroy(_chunkHolder.transform.GetChild(i).gameObject);
            } else {
                DestroyImmediate(_chunkHolder.transform.GetChild(i).gameObject);
            }
        }
    }

    private void Clean() {
        foreach (Vector3 pos in ActiveChunks.Keys) {
            if (ActiveChunks.TryRemove(pos, out Chunk chunk)) {
                chunk.DisposeMeshAndData();
                if (Application.isPlaying) {
                    Destroy(chunk);
                } else {
                    DestroyImmediate(chunk);
                }
            }
        }
#if UNITY_EDITOR
        EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect();
#endif
    }

    #region STATIC
    public static float CubeSize() {
        return Instance.WorldSetting.ChunkSize / Instance.WorldSetting.NumCubesXZ;
    }

    public static float ChunkHeight() {
        return Instance.WorldSetting.NumCubesY * CubeSize();
    }

    public static Vector3 PosToChunkPos(Vector3 pos) {
        pos /= Instance.WorldSetting.ChunkSize;
        pos = math.floor(pos) * Instance.WorldSetting.ChunkSize;
        pos.y = Instance.GroundLevel;
        return pos;
    }
    #endregion STATIC
}
