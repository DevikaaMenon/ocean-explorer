using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Branch Coral Setting", menuName = "Settings/Branch Coral Setting")]
public class BranchCoralSetting : ScriptableObject {
    [Header("Models")]
    public GameObject[] Prefabs;
    [Min(0.1f)]
    public float ModelScale = 1f;

    [Header("Spawning")]
    [Range(0f, 1f)]
    public float Density = 0.3f;

    [Header("Rotation")]
    [Range(0f, 1f)]
    public float NormalAndUpSlerp;

    [Header("Slope")]
    [Range(0f, 90f)]
    public float MaxAngle = 45f;

    [Header("Noise")]
    [Min(0.1f)]
    public float NoiseScale = 0.5f;
    public Vector3 Offset;

    public event Action OnBranchCoralSettingChanged;

    private void OnValidate() {
        OnBranchCoralSettingChanged?.Invoke();
    }
}
