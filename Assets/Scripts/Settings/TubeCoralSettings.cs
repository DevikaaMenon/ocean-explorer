using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Tube Coral Setting", menuName = "Settings/Tube Coral Setting")]
public class TubeCoralSetting : ScriptableObject {
    [Header("Models")]
    public GameObject[] Prefabs;
    [Min(0.1f)]
    public float ModelScale = 1f;

    [Header("Spawning")]
    [Range(0f, 1f)]
    public float Density = 0.3f;
    [Range(-1f, 1f)]
    public float VerticalOffset = 0f;

    [Header("Rotation")]
    [Range(0f, 1f)]
    public float NormalAndUpSlerp;

    [Header("Slope")]
    [Range(0f, 90f)]
    public float MaxAngle = 45f;

    [Header("Edge checking")]
    [Min(2)]
    public uint SamplePoints = 4;
    [Min(0f)]
    public float MaxHeightDifference = 1f;

    [Header("Noise")]
    [Min(0.1f)]
    public float NoiseScale = 0.5f;
    public Vector3 Offset;

    public event Action OnBranchCoralSettingChanged;

    private void OnValidate() {
        OnBranchCoralSettingChanged?.Invoke();
    }
}
