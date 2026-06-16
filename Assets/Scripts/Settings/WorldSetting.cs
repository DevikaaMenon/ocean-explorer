using System;
using UnityEngine;

[CreateAssetMenu(fileName = "World Setting", menuName = "Settings/World Setting")]
public class WorldSetting : ScriptableObject {
    [Header("Chunk")]
    [Range(8f, 256f)]
    public float ChunkSize = 32f;
    [Range(2, 64)]
    public int NumCubesXZ = 32;
    [Range(2, 128)]
    public int NumCubesY = 64;

    [Header("Generator")]
    public float Threshold = 6f;

    public static event Action OnWorldSettingChanged;

    private void OnValidate() {
        OnWorldSettingChanged?.Invoke();
    }
}
