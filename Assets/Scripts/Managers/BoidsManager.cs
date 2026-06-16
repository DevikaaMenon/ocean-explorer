using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct ComponetsData {
    [Range(-100.0f, 100.0f)]
    public float factor;
    [Range(0, 100.0f)]
    public float radius;
}

[Serializable]
public struct BehaviourData {
    public ComponetsData cohesion;
    public ComponetsData alignment;
    public ComponetsData separation;
}

[Serializable]
public struct InteractionData {
    public int SpeciesId;
    public BehaviourData Behaviour;
}

[Serializable]
public struct BoidsData {

    [Range(BoidsConstants.MinSpeed, 25.0f)]
    public float MaxSpeed;
    [Range(1.0f, 10.0f)]
    public float MaxSteerForce;
    [Range(-1.0f, 1.0f)]
    public float HalfFOVCosine;
    public List<InteractionData> Interactions;
}

[Serializable]
public struct ModelData {
    public GameObject Model;
    [Range(0, 512)]
    public int Count;
    [Range(0.001f, 10.0f)]
    public float MaxScale;
    [Range(0.001f, 10.0f)]
    public float MinScale;
    public bool EmitsBubbles;
    public FishData Info;
}
public class BoidsManager : MonoBehaviour {
    public LayerMask ObstacleMask;
    public GameObject Player;

    [Header("Prefabs")]
    public List<ModelData> Models;
    public ParticleSystem Bubbles;

    [Header("Settings")]
    public BoidsSetting Setting;
    [Header("Computations")]
    public bool ShouldSpawnBubbles = true;
    public bool ShouldUpdate = true;
    public bool ShouldUpdateParameters = false;

    [Header("Fish objects")]
    public GameObject[] Units;
    private int _flockSize = 0;
    private int _numberOfSpecies = 0;
    private Vector3[] ObstacleAvoidanceVectors;
    private int[] _lastAvoidanceIndices;

    [Header("Host buffers")]
    private Vector3[] _positions;
    private Vector3[] _velocities;
    private Vector3[] _accelerations;
    private int[] _speciesId;
    private BehaviourData[] _speciesData;

    // The three buffers below could be packed into a
    // single one, but they are sent to only GPU once.
    private float[] _maxSpeeds;
    private float[] _halfFOVCosines;
    private float[] _maxSteerForces;

    [Header("Shaders")]
    public ComputeShader BoidShader;

    [Header("Compute buffers")]
    private ComputeBuffer _computePositions;
    private ComputeBuffer _computeVelocities;
    private ComputeBuffer _computeAccelerations;
    private ComputeBuffer _computeSpeciesId;
    private ComputeBuffer _computeSpeciesData;
    private ComputeBuffer _computeMaxSpeeds;
    private ComputeBuffer _computeHalfFOVCosines;
    private ComputeBuffer _computeMaxSteerForces;

    public void Start() {
        InitSizes();
        InitHostData();
        InitDeviceData();
        InitObstacleAvoidanceData();
    }
    private void FixedUpdate() {
        if (!ShouldUpdate) {
            return;
        }

        if(ShouldUpdateParameters) {
            SetSpeciesParameters();
            SetDeviceSpeciesParameters();
            SetDeviceMutables();
        }

        SetUniforms(Time.fixedDeltaTime);
        ComputeAccelerations();
        AdjustAccelerations();
        ComputePositionsAfterFixedUpdate();

        // Units can only be accessed in the main thread,
        // therefore Parallel.For is forbidden
        for (int i = 0; i < Units.Length; i++) {
            Units[i].transform.position = _positions[i];
            Units[i].transform.forward = _velocities[i];
        }
    }
    private void OnDestroy() {
        _computePositions.Release();
        _computeVelocities.Release();
        _computeAccelerations.Release();
        _computeSpeciesId.Release();
        _computeSpeciesData.Release();
        _computeMaxSpeeds.Release();
        _computeMaxSteerForces.Release();
        _computeHalfFOVCosines.Release();
    }
    private void InitObstacleAvoidanceData() {
        ObstacleAvoidanceVectors = new Vector3[4];
        ObstacleAvoidanceVectors[0] = new Vector3(0.0f, 1.0f, 0.0f);
        ObstacleAvoidanceVectors[1] = new Vector3(0.0f, -1.0f, 0.0f);
        ObstacleAvoidanceVectors[2] = new Vector3(1.0f, 0.0f, 0.0f);
        ObstacleAvoidanceVectors[3] = new Vector3(-1.0f, 0.0f, 0.0f);
    }
    private void ScaleUnit(GameObject obj, float maxScale, float minScale) {
        var scale = UnityEngine.Random.Range(minScale, maxScale);
        obj.transform.localScale = new Vector3(scale, scale, scale);
    }
    private void InitSizes() {
        _flockSize = 0;
        Models.ForEach((ModelData model) => { _flockSize += model.Count;});
        _numberOfSpecies = Models.Count;

        if(_flockSize <= 0 || _numberOfSpecies <= 0) {
            this.gameObject.SetActive(false);
            Debug.LogWarning("No boids to procees, manager deactivated");
        }
    }
    private void InstantiateBoids() {
        int i = 0;
        int speciesMaxIndex = 0;
        for(int j = 0; j < Models.Count; j++)
        {
            speciesMaxIndex += Models[j].Count;

            float maxScale = Math.Max(Models[j].MaxScale, Models[j].MinScale);
            float minScale = Math.Min(Models[j].MaxScale, Models[j].MinScale);
            for (; i < speciesMaxIndex; i++) {
                var unitVector = UnityEngine.Random.insideUnitSphere;
                var position = Vector3.zero;
                position.x = unitVector.x * Setting.FlockBoundaries.x + Player.transform.position.x;
                position.y = WorldManager.Instance.SurfaceLevel;
                position.y -= Math.Abs(unitVector.y) * (WorldManager.Instance.SurfaceLevel - WorldManager.ChunkHeight() - WorldManager.Instance.GroundLevel);
                position.z = unitVector.z * Setting.FlockBoundaries.z + Player.transform.position.z;

                var rotation = Quaternion.Euler(UnityEngine.Random.Range(-20, 0), UnityEngine.Random.Range(0, 360), 0);
                _positions[i] = position;
                _speciesId[i] = j;

                Units[i] = Instantiate(Models[j].Model, position, rotation, this.transform);
                ScaleUnit(Units[i], maxScale, minScale);

                // Add a large sphere collider to the root for click detection
                var col = Units[i].AddComponent<SphereCollider>();
                col.radius = 1.5f;
                col.isTrigger = false;
                var clickable = Units[i].AddComponent<FishClickable>();
                clickable.SpeciesIndex = j;
                clickable.Data = Models[j].Info;

                if(Models[j].EmitsBubbles) {
                    var bubbles = Units[i].AddComponent<FishBubbles>();
                    if(ShouldSpawnBubbles) {
                        bubbles.Prefab = Bubbles;
                        bubbles.Parent = Units[i].transform;
                    }
                }

                _velocities[i] = Units[i].transform.forward;
                Units[i].layer = this.gameObject.layer;

                _lastAvoidanceIndices[i] = 0;
            }
        }
    }
    private void InitHostData() {
        Units = new GameObject[_flockSize];

        _lastAvoidanceIndices = new int[_flockSize];

        _positions = new Vector3[_flockSize];
        _velocities = new Vector3[_flockSize];
        _accelerations = new Vector3[_flockSize];
        _speciesId = new int[_flockSize];
        _speciesData = new BehaviourData[_numberOfSpecies * _numberOfSpecies];
        _maxSpeeds = new float[_numberOfSpecies];
        _maxSteerForces = new float[_numberOfSpecies];
        _halfFOVCosines = new float[_numberOfSpecies];

        Setting.MaxYCoordinate = WorldManager.Instance.SurfaceLevel;

        if(Player is not null) {
            InstantiateBoids();
        }
    }
    private void InitDeviceData() {

        int sizeOfVector3 = 3 * sizeof(float);
        int sizeOfSpeciesData = 6 * sizeof(float);

        _computePositions = new ComputeBuffer(_flockSize, sizeOfVector3);
        _computeVelocities = new ComputeBuffer(_flockSize, sizeOfVector3);
        _computeAccelerations = new ComputeBuffer(_flockSize, sizeOfVector3);
        _computeSpeciesId = new ComputeBuffer(_flockSize, sizeof(int));
        _computeSpeciesData = new ComputeBuffer(_numberOfSpecies * _numberOfSpecies, sizeOfSpeciesData);
        _computeMaxSpeeds = new ComputeBuffer(_numberOfSpecies, sizeof(float));
        _computeHalfFOVCosines = new ComputeBuffer(_numberOfSpecies, sizeof(float));
        _computeMaxSteerForces = new ComputeBuffer(_numberOfSpecies, sizeof(float));

        if (BoidShader == null) {
            return;
        }

        SetSpeciesParameters();
        SetDeviceSpeciesParameters();
        SetDeviceConstants();
        SetDeviceMutables();
    }
    public int SpeciesToIndex(int speciesId1, int speciesId2) {
        if (speciesId1 < 0 || speciesId2 < 0 ||
           speciesId1 >= _numberOfSpecies || speciesId2 >= _numberOfSpecies) {
            throw new ArgumentOutOfRangeException($"species id should be between 0 and {_numberOfSpecies - 1}");
        }
        return speciesId1 * _numberOfSpecies + speciesId2;
    }
    private void SetDeviceMutables() {
        BoidShader.SetVector("flockBoundaries", Setting.FlockBoundaries);
        BoidShader.SetVector("boundariesTolerance", Setting.BoundariesTolerance);

        BoidShader.SetFloat("maxYCoordinate", Setting.MaxYCoordinate);
        BoidShader.SetFloat("maxYOffsetAvoidance", Setting.MaxYOffsetAvoidance);

        BoidShader.SetInt("numberOfSpecies", _numberOfSpecies);
    }
    private void SetDeviceConstants() {
        BoidShader.SetInt("flockSize", _flockSize);
        BoidShader.SetFloat("minSpeed", BoidsConstants.MinSpeed);
    }
    public void SetSpeciesParameters() {
        for(int i = 0; i < _numberOfSpecies; i++) {
            _maxSpeeds[i] = BoidsConstants.DefaultMaxSpeed;
            _maxSteerForces[i] = BoidsConstants.DefaultMaxSteerForce;
            _halfFOVCosines[i] = BoidsConstants.DefaultHalfFOVCosine;  
        }
        
        for(int i = 0; i < _speciesData.Length; i++) {
            _speciesData[i].cohesion.factor = 0.0f;
            _speciesData[i].cohesion.radius = 0.0f;
            _speciesData[i].alignment.factor = 0.0f;
            _speciesData[i].alignment.radius = 0.0f;
            _speciesData[i].separation.factor = 2.0f;
            _speciesData[i].separation.radius = 5.0f;
        }

        if(Setting.BoidsSettings.Count < _numberOfSpecies) {
            Debug.LogWarning("Boids data has not been provided for all species");
        }

        if(Setting.BoidsSettings.Count > _numberOfSpecies) {
            Debug.LogWarning("Too much boids data provided");
        }

        bool[] interactionsSet = new bool[_numberOfSpecies * _numberOfSpecies];

        for(int i = 0; i < Setting.BoidsSettings.Count && i < _numberOfSpecies; i++) {
            var speciesData = Setting.BoidsSettings[i];

            _maxSpeeds[i] = speciesData.MaxSpeed;
            _maxSteerForces[i] = speciesData.MaxSteerForce;
            _halfFOVCosines[i] = speciesData.HalfFOVCosine;

            if(speciesData.Interactions.Count < _numberOfSpecies) {
                Debug.LogWarning($"Some default behaviour data has been set to the species of index {i}");
            }

            foreach(var interaction in speciesData.Interactions) {
                int InterId = SpeciesToIndex(i, interaction.SpeciesId);
                _speciesData[InterId].cohesion.factor = interaction.Behaviour.cohesion.factor;
                _speciesData[InterId].cohesion.radius = interaction.Behaviour.cohesion.radius;
                _speciesData[InterId].alignment.factor = interaction.Behaviour.alignment.factor;
                _speciesData[InterId].alignment.radius = interaction.Behaviour.alignment.radius;
                _speciesData[InterId].separation.factor = interaction.Behaviour.separation.factor;
                _speciesData[InterId].separation.radius = interaction.Behaviour.separation.radius;

                if(interactionsSet[InterId]) {
                    Debug.LogWarning($"Interaction data for {i} and {interaction.SpeciesId} has been configured more than once");
                }
                interactionsSet[InterId] = true;
            }
        }           
    }
    public void SetDeviceSpeciesParameters() {
        _computeSpeciesId.SetData(_speciesId);
        _computeSpeciesData.SetData(_speciesData);
        _computeMaxSpeeds.SetData(_maxSpeeds);
        _computeMaxSteerForces.SetData(_maxSteerForces);
        _computeHalfFOVCosines.SetData(_halfFOVCosines);

        var kernelId = BoidShader.FindKernel("ComputeVelocities");

        BoidShader.SetBuffer(kernelId, "speciesId", _computeSpeciesId);
        BoidShader.SetBuffer(kernelId, "speciesData", _computeSpeciesData);
        BoidShader.SetBuffer(kernelId, "maxSpeeds", _computeMaxSpeeds);
        BoidShader.SetBuffer(kernelId, "maxSteerForces", _computeMaxSteerForces);
        BoidShader.SetBuffer(kernelId, "halfFOVCosines", _computeHalfFOVCosines);

        kernelId = BoidShader.FindKernel("UpdateBuffers");

        BoidShader.SetBuffer(kernelId, "speciesId", _computeSpeciesId);
        BoidShader.SetBuffer(kernelId, "maxSpeeds", _computeMaxSpeeds);
    }
    private void SetUniforms(float deltaTime) {
        BoidShader.SetFloat("deltaTime", deltaTime);
        BoidShader.SetVector("cameraPosition", Player.transform.position);
    }
    public int GetNumberOfGroups(int numberOfThreads) {
        if (numberOfThreads < 1) {
            throw new ArgumentException("numberOfThreads should be a positive value");
        }
        return (numberOfThreads + BoidsConstants.GroupSize - 1) / BoidsConstants.GroupSize;
    }
    private void ComputeAccelerations() {
        _computePositions.SetData(_positions);
        _computeVelocities.SetData(_velocities);

        var kernelId = BoidShader.FindKernel("ComputeVelocities");

        BoidShader.SetBuffer(kernelId, "positions", _computePositions);
        BoidShader.SetBuffer(kernelId, "velocities", _computeVelocities);
        BoidShader.SetBuffer(kernelId, "accelerations", _computeAccelerations);

        BoidShader.Dispatch(kernelId, GetNumberOfGroups(_flockSize), 1, 1);

        _computeAccelerations.GetData(_accelerations);
    }
    public Vector3 SteerTowards(Vector3 vector, Vector3 velocity, int speciesId) {
        if (speciesId < 0 || speciesId >= _numberOfSpecies) {
            throw new ArgumentOutOfRangeException($"speciesId should be between 0 and {_numberOfSpecies}");
        }
        if (vector.x == 0.0f && vector.y == 0.0f && vector.z == 0.0f) {
            throw new ArgumentException("vector parameter should not be a zero vector");
        }
        Vector3 v = vector.normalized * _maxSpeeds[speciesId] - velocity;
        return Vector3.ClampMagnitude(v, _maxSteerForces[speciesId]);
    }
    private void AdjustAccelerations() {
        for (int i = 0; i < Units.Length; i++) {
            RaycastHit Hit;
            Vector3 Position = Units[i].transform.position;
            Vector3 Forward = Units[i].transform.forward.normalized;
            Debug.DrawRay(Position, Setting.ObstacleDistance * Forward);
            if (Physics.Raycast(Position, Forward, out Hit, Setting.ObstacleDistance, ObstacleMask)) {
                float SqrDistanceToObstacle = (Hit.point - Position).sqrMagnitude;
                Vector3 SelectedDirection = Vector3.zero;
                float MaxDistance = float.MinValue;
                for (int j = 0, k = _lastAvoidanceIndices[i]; 
                     j < ObstacleAvoidanceVectors.Length; 
                     j++, k = k >= ObstacleAvoidanceVectors.Length ? 0 : k + 1) {
                    Vector3 direction = ObstacleAvoidanceVectors[j];
                    Vector3 CurrentDirection = Units[i].transform.TransformDirection(direction);
                    if (Physics.Raycast(Position, CurrentDirection.normalized, out Hit, Setting.ObstacleDistance, ObstacleMask)) {
                        float CurrentDistance = (Hit.point - Position).sqrMagnitude;
                        if (CurrentDistance > MaxDistance) {
                            MaxDistance = CurrentDistance;
                            SelectedDirection = CurrentDirection;
                            _lastAvoidanceIndices[i] = k;
                        }
                    } else {
                        SelectedDirection = CurrentDirection;
                        _lastAvoidanceIndices[i] = k;
                        break;
                    }
                }

                Vector3 AvoidanceComponent = Setting.ObstacleAvoidanceFactor * SelectedDirection - _velocities[i];
                _accelerations[i] += AvoidanceComponent / SqrDistanceToObstacle;
                Debug.DrawRay(Position, SelectedDirection);
            } else {
                _lastAvoidanceIndices[i] = 0;
            }
        }
    }
    private void ComputePositionsAfterFixedUpdate() {
        _computePositions.SetData(_positions);
        _computeVelocities.SetData(_velocities);
        _computeAccelerations.SetData(_accelerations);

        int kernelId = BoidShader.FindKernel("UpdateBuffers");

        BoidShader.SetBuffer(kernelId, "positions", _computePositions);
        BoidShader.SetBuffer(kernelId, "velocities", _computeVelocities);
        BoidShader.SetBuffer(kernelId, "accelerations", _computeAccelerations);

        BoidShader.Dispatch(kernelId, GetNumberOfGroups(_flockSize), 1, 1);

        _computePositions.GetData(_positions);
        _computeVelocities.GetData(_velocities);
    }
}

