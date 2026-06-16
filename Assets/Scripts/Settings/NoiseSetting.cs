using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Noise Setting", menuName = "Settings/Noise Setting")]
public class NoiseSetting : ScriptableObject {
    [Header("Seeding")]
    public int Seed;
    public Vector3 SpaceOffset;

    [Header("Fractional Brownian Motion")]
    [Range(3, 12)]
    public int NumOctaves = 9;
    [HideInInspector]
    public readonly int MaxNumOctaves = 12;
    public float Frequency = 2f; // will divide by 100
    [Range(1.8f, 2.2f)]
    public float Lacunarity = 2f;
    public float Amplitude = 1f;
    [Range(0.3f, 0.7f)]
    public float Persistence = 0.5f;

    [Header("Scale")]
    public float OffsetY = 0f;
    public float YWeight = 1f;
    public float NoiseWeight = 1f;
    public float VWeight = 1f;

    [Header("Ceiling")]
    [Range(0f, 1f)]
    public float CeilingY = 0.8f;
    public float CeilingWeight = 1f;
    [Range(0f, 1f)]
    public float HardCeilingY = 0.95f;
    public float HardCeilingWeight = 100f;

    [Header("Floor")]
    [Range(0f, 1f)]
    public float FloorY = 0.5f;
    public float FloorWeight = 1f;
    [Range(0f, 1f)]
    public float HardFloorY = 0.05f;
    public float HardFloorWeight = -100f;

    [Header("Warping")]
    public float WarpFrequency; // will divide by 1000
    public float WarpAmplitude;

    [Header("Other")]
    public Vector2 Terracing = Vector2.zero;

    public static event Action OnNoiseSettingChanged;

    private void OnValidate() {
        OnNoiseSettingChanged?.Invoke();
    }
}
