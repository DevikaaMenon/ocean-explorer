using System;
using UnityEngine;

// https://pastebin.com/pzfqGmFZ
// https://www.youtube.com/watch?v=gfD8S32xzYI

[ExecuteInEditMode]
public class CoralManager : MonoBehaviour {
    #region SINGLETON
    public static CoralManager Instance { get; private set; }

    //private void SettingsChanged() { _settingsChanged = true; }

    public void Awake() {
        if (Instance == null) {
            Instance = this;
            //_settingsChanged = false;
        } else if (Instance != this) {
            if (Application.isPlaying) {
                Destroy(this);
            } else {
                DestroyImmediate(this);
            }
        }
    }

    //private void OnValidate() {}

    //private void OnDestroy() {}
    #endregion

    //private bool _settingsChanged;

    [Header("Branch coral settings")]
    public BranchCoralSetting BranchCoralSetting;
    public bool ShouldSpawnBranchCorals = true;

    [Header("Kelp settings")]
    public KelpSetting KelpSetting;
    public bool ShouldSpawnKelp = true;

    [Header("Tube coral settings")]
    public TubeCoralSetting TubeCoralSetting;
    public bool ShouldSpawnTubeCorals = true;

    [Header("World settings")]
    public WorldSetting WorldSetting;

    [Header("Light")]
    public GameObject DirectionalLight;

    [Header("Raycast setup")]
    [Range(1, 100)]
    public int GridResolution = 10;
    [Range(0, 1)]
    public float RandomOffsetMultiplier = 1f;

    public void Start() {
        float BCScale = BranchCoralSetting.ModelScale;
        foreach (var bc in BranchCoralSetting.Prefabs) {
            bc.layer = LayerMask.NameToLayer("Plants");
            bc.transform.localScale = new Vector3(BCScale, BCScale, BCScale);
            Renderer renderer = bc.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        float TCScale = TubeCoralSetting.ModelScale;
        foreach (var tc in TubeCoralSetting.Prefabs) {
            tc.layer = LayerMask.NameToLayer("Plants");
            tc.transform.localScale = new Vector3(TCScale, TCScale, TCScale);
            Renderer renderer = tc.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        float KScale = KelpSetting.ModelScale;
        foreach (var k in KelpSetting.Prefabs) {
            k.layer = LayerMask.NameToLayer("Plants");
            k.transform.localScale = new Vector3(KScale, KScale, KScale);
            Renderer renderer = k.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    private bool CheckTubeCoralSpawn(Chunk chunk, RaycastHit hit, GameObject coral) {
        Bounds bounds = coral.GetComponent<Renderer>().bounds;
        for (int i = 0; i < TubeCoralSetting.SamplePoints; i++) {
            // Calculate the angle and position of the base check point
            float angle = (i / (float)TubeCoralSetting.SamplePoints) * Mathf.PI * 2f;
            Vector3 offset = 0.5f * bounds.size.x * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 checkPoint = hit.point + offset;

            // Check the terrain height at the base point
            Vector3 origin = new(checkPoint.x, chunk.Position.y + WorldManager.ChunkHeight(), checkPoint.z);
            Ray ray = new(origin, Vector3.down);
            if (chunk.RaycastMesh(ray, out RaycastHit checkHit, WorldSetting.ChunkSize)) {
                // Ensure the base point is not hanging over
                if (checkPoint.y - checkHit.point.y > TubeCoralSetting.MaxHeightDifference) {
                    // If any point is not supported, reject the spawn
                    return false;
                }
            } else {
                return false;
            }
        }
        return true;
    }

    private void SpawnBranchCoral(Chunk chunk, RaycastHit hit, GameObject coral) {
        Quaternion originalRotation = Quaternion.identity;
        Quaternion normalRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.right, hit.normal), hit.normal);
        Quaternion blendedRotation = Quaternion.Slerp(originalRotation, normalRotation, BranchCoralSetting.NormalAndUpSlerp);
        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        Quaternion randomUpRotation = Quaternion.AngleAxis(randomAngle, blendedRotation * Vector3.up);
        Quaternion finalRotation = randomUpRotation * blendedRotation;
        GameObject obj = Instantiate(coral, hit.point, finalRotation, chunk.transform);
        obj.transform.localScale *= UnityEngine.Random.Range(0.8f, 1.2f);
    }

    private void SpawnTubeCoral(Chunk chunk, RaycastHit hit, GameObject coral) {
        Quaternion originalRotation = Quaternion.identity;
        Quaternion normalRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.right, hit.normal), hit.normal);
        Quaternion blendedRotation = Quaternion.Slerp(originalRotation, normalRotation, TubeCoralSetting.NormalAndUpSlerp);
        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        Quaternion randomUpRotation = Quaternion.AngleAxis(randomAngle, blendedRotation * Vector3.up);
        Quaternion finalRotation = randomUpRotation * blendedRotation;
        Vector3 pos = hit.point + TubeCoralSetting.VerticalOffset * hit.normal;
        GameObject obj = Instantiate(coral, pos, finalRotation, chunk.transform);
        obj.transform.localScale *= UnityEngine.Random.Range(0.8f, 1.2f);
    }

    private void SpawnKelp(Chunk chunk, RaycastHit hit, GameObject kelp) {
        Quaternion originalRotation = kelp.transform.rotation;
        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        Quaternion randomUpRotation = Quaternion.AngleAxis(randomAngle, originalRotation * Vector3.up);
        Quaternion finalRotation = originalRotation * randomUpRotation;
        GameObject obj = Instantiate(kelp, hit.point, finalRotation, chunk.transform);
        obj.transform.localScale *= UnityEngine.Random.Range(0.8f, 1.2f);
    }

    public void SpawnResources(Chunk chunk) {
        UnityEngine.Random.InitState((int)(chunk.Position.x) + (int)(chunk.Position.z));

        float gridTileSize = WorldSetting.ChunkSize / GridResolution;
        float randomOffset = RandomOffsetMultiplier * gridTileSize;

        float offsetX = chunk.Position.x / WorldSetting.ChunkSize;
        float offsetZ = chunk.Position.z / WorldSetting.ChunkSize;

        Vector3 position = chunk.Position;
        position.y += WorldManager.ChunkHeight();

        for (int i = 0; i < GridResolution; i++) {
            for (int j = 0; j < GridResolution; j++) {
                Vector3 positionOffset = new(
                    UnityEngine.Random.Range(-randomOffset, randomOffset),
                    0,
                    UnityEngine.Random.Range(-randomOffset, randomOffset));
                Ray ray = new(position + positionOffset, Vector3.down);
                if (chunk.RaycastMesh(ray, out RaycastHit hit, WorldManager.ChunkHeight())) {
                    // Branch Coral
                    if (ShouldSpawnBranchCorals &&
                        BranchCoralSetting.Density > Mathf.PerlinNoise(BranchCoralSetting.NoiseScale * i / GridResolution + offsetX + BranchCoralSetting.Offset.x, BranchCoralSetting.NoiseScale * j / GridResolution + offsetZ + BranchCoralSetting.Offset.z) &&
                        Vector3.Angle(Vector3.up, hit.normal) < BranchCoralSetting.MaxAngle) {
                        int index = UnityEngine.Random.Range(0, BranchCoralSetting.Prefabs.Length);
                        SpawnBranchCoral(chunk, hit, BranchCoralSetting.Prefabs[index]);
                    }
                    // Tube Coral
                    else if (ShouldSpawnTubeCorals &&
                        TubeCoralSetting.Density > Mathf.PerlinNoise(TubeCoralSetting.NoiseScale * i / GridResolution + offsetX + TubeCoralSetting.Offset.z, TubeCoralSetting.NoiseScale * j / GridResolution + offsetZ + TubeCoralSetting.Offset.z) &&
                        Vector3.Angle(Vector3.up, hit.normal) < TubeCoralSetting.MaxAngle) {
                        int index = UnityEngine.Random.Range(0, TubeCoralSetting.Prefabs.Length);
                        if (CheckTubeCoralSpawn(chunk, hit, TubeCoralSetting.Prefabs[index])) {
                            SpawnTubeCoral(chunk, hit, TubeCoralSetting.Prefabs[index]);
                        }
                    }
                    // Kelp
                    else if (ShouldSpawnKelp &&
                        KelpSetting.Density > Mathf.PerlinNoise(KelpSetting.NoiseScale * i / GridResolution + offsetX + KelpSetting.Offset.z, KelpSetting.NoiseScale * j / GridResolution + offsetZ + KelpSetting.Offset.z) &&
                        Vector3.Angle(Vector3.up, hit.normal) < KelpSetting.MaxAngle) {
                        Ray rayToLightSource = new(hit.point, -DirectionalLight.transform.forward);
                        if (!Physics.Raycast(rayToLightSource, WorldManager.ChunkHeight(), LayerMask.GetMask("Terrain"))) {
                            int index = UnityEngine.Random.Range(0, KelpSetting.Prefabs.Length);
                            SpawnKelp(chunk, hit, KelpSetting.Prefabs[index]);
                        }
                    }
                }

                position.z += gridTileSize;
            }

            position.x += gridTileSize;
            position.z = chunk.Position.z;
        }
    }
}
