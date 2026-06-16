using System;
using UnityEngine;
using System.Collections.Generic;

public static class BoidsConstants {
    public const int GroupSize = 256;

    public const float FlockXBoundary = 30.0f;
    public const float FlockYBoundary = 30.0f;
    public const float FlockZBoundary = 30.0f;

    public const float MinSpeed = 1.0f;
    public const int NemoId = 0;
    public const int SharkId = 1;

    public const float DefaultMaxSpeed = 5.0f; 
    public const float DefaultMaxSteerForce = 3.0f;
    public const float DefaultHalfFOVCosine = 0.0f;
}

[CreateAssetMenu(fileName = "Boids Setting", menuName = "Settings/Boids Setting")]
public class BoidsSetting : ScriptableObject {
    [Header("Flock settings")]
    [Range(10, 1000)]
    public int SpawnRadius = 100;
    public float MaxYCoordinate = 100;
    [Min(0.0f)]
    public float MaxYOffsetAvoidance = 0.0f;
    public Vector3 FlockBoundaries = new Vector3(BoidsConstants.FlockXBoundary,
                                                 BoidsConstants.FlockYBoundary,
                                                 BoidsConstants.FlockZBoundary);

    public Vector3 BoundariesTolerance = Vector3.zero;
    [Min(4)]
    public int ObstacleAvoidanceDirectionsCount = 4;
    [Range(0.0f, 50.0f)]
    public float ObstacleAvoidanceFactor = 0.0f;
    [Range(0.0f, 10.0f)]
    public float ObstacleAvoidanceVectorZComponent = 0.0f;
    [Min(0.0f)]
    public float ObstacleDistance = 10.0f;

    [Header("Boids data")]
    public List<BoidsData> BoidsSettings;

    public event Action OnBoidsSettingChanged;

    private void OnValidate() {
        OnBoidsSettingChanged?.Invoke();
    }
}
