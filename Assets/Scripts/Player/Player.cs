using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {
    [SerializeField] private GameObject Model;
    [SerializeField] private GameObject ModelBottom;
    [SerializeField] private PlayerCamera Camera;

    public float WaterSurfaceOffset = 1.5f;

    [Header("Swimming Settings")]
    public float BaseSwimmingForce = 5f;
    public float SpeedingMultiplayer = 2.5f;
    public float VerticalSurfacingForce = 5f;
    public float DragInWater = 2f;
    public float Buoyancy = 8f;

    [Header("General Settings")]
    public float PitchRollThreshold = 0.1f;
    public float DeltaTimePitchRoll = 6f;
    public float DeltaTimeYaw = 18f;

    [Header("Effects")]
    [SerializeField] private GameObject Algae;

    private Rigidbody _body;
    public Flashlight Flashlight;

    private void Awake() {
        _body = GetComponent<Rigidbody>();
        transform.position = new Vector3(0, WorldManager.Instance.SurfaceLevel + 10, 0);
    }

    private void Update() {
        TurnModel();
    }

    private void FixedUpdate() {
        bool isInWater = IsInWater();
        if (isInWater) {
            _body.drag = DragInWater;
            ApplyBuoyancy();
            Surface();
            Swim();
            StopAtGroundLevel();
        } else {
            _body.drag = 0;
            Swim();
        }
        Effects(isInWater);
    }

    private bool IsInWater() {
        return ModelBottom.transform.position.y < WorldManager.Instance.SurfaceLevel + WaterSurfaceOffset;
    }

    private void Effects(bool enable) {
        Algae.SetActive(enable);
        if (enable) {
            BubbleManager.TrySpawning(transform.position, Camera.GetCameraDirection());
        }
    }

    private void ApplyBuoyancy() {
        _body.AddForce(Vector3.up * Buoyancy, ForceMode.Acceleration);
        _body.drag = DragInWater;
    }

    private void Swim() {
        Vector3 moveDirection = GetDirection();
        if (moveDirection.magnitude > 0) {
            var rotation = Quaternion.Euler(Camera.Transform.eulerAngles);
            moveDirection = rotation * moveDirection;
            if (!IsInWater() && moveDirection.y > 0) {
                moveDirection.y = 0;
            }
            _body.AddForce(moveDirection * GetSpeed(), ForceMode.Force);
        }
    }

    private void Surface() {
        bool surfacing = VRInputManager.Instance != null
            ? VRInputManager.Instance.IsSurfacing()
            : InputManager.Instance.IsSurfacing();
        if (surfacing) {
            _body.AddForce(Vector3.up * VerticalSurfacingForce, ForceMode.Force);
        }
    }

    private void TurnModel() {
        Vector3 currentEuler = Model.transform.rotation.eulerAngles;
        Vector3 targetEuler = Quaternion.LookRotation(Camera.Transform.forward).eulerAngles;

        float pitchDifference = Mathf.Abs(Mathf.DeltaAngle(currentEuler.x, targetEuler.x));
        float rollDifference = Mathf.Abs(Mathf.DeltaAngle(currentEuler.z, targetEuler.z));

        float newYaw = Mathf.LerpAngle(currentEuler.y, targetEuler.y, Time.deltaTime * DeltaTimeYaw);

        float newPitch = currentEuler.x;
        if (pitchDifference > PitchRollThreshold) {
            newPitch = Mathf.LerpAngle(currentEuler.x, targetEuler.x, Time.deltaTime * DeltaTimePitchRoll);
        }

        float newRoll = currentEuler.z;
        if (rollDifference > PitchRollThreshold) {
            newRoll = Mathf.LerpAngle(currentEuler.z, targetEuler.z, Time.deltaTime * DeltaTimePitchRoll);
        }

        Model.transform.rotation = Quaternion.Euler(newPitch, newYaw, newRoll);
    }

    private void StopAtGroundLevel() {
        var halfHeight = GetPlayerHeight() / 2;
        if (transform.position.y - halfHeight < WorldManager.Instance.GroundLevel) {
            Vector3 pos = transform.position;
            pos.y = WorldManager.Instance.GroundLevel + halfHeight;
            transform.position = pos;

            Vector3 vel = _body.velocity;
            vel.y = 0;
            _body.velocity = vel;
        }
    }

    private float GetSpeed() {
        bool speeding = VRInputManager.Instance != null
            ? VRInputManager.Instance.Speeding()
            : InputManager.Instance.Speeding();
        float speed = BaseSwimmingForce;
        if (speeding) {
            speed *= SpeedingMultiplayer;
        }
        return speed;
    }

    private Vector3 GetDirection() {
        Vector2 move = VRInputManager.Instance != null
            ? VRInputManager.Instance.GetPlayerMovement()
            : InputManager.Instance.GetPlayerMovement();
        return new Vector3(move.x, 0, move.y).normalized;
    }

    private float GetPlayerHeight() {
        return GetComponent<Collider>().bounds.size.y;
    }
}
