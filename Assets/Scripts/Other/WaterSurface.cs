using UnityEngine;

public class WaterSurfaceManager : MonoBehaviour {
    [Header("Target to follow")]
    public Transform PlayerTransform;
    [Header("Cameras")]
    public Camera MainCamera;
    public Camera OverWaterCamera;

    private bool _isPlayerOverWater = true;
    private int _mask;

    private void Start() {
        _mask =
        (1 << LayerMask.NameToLayer("Terrain")) |
        (1 << LayerMask.NameToLayer("Plants")) |
        (1 << LayerMask.NameToLayer("Boids"));

        Vector3 pos = transform.position;
        pos.y = WorldManager.Instance.SurfaceLevel;
        transform.position = pos;
    }

    void Update() {
        // Update position
        transform.position = new(PlayerTransform.position.x, transform.position.y, PlayerTransform.transform.position.z);

        // Check for player position
        if (!_isPlayerOverWater && transform.position.y < PlayerTransform.transform.position.y) {
            _isPlayerOverWater = true;
            SetCamerasCullingMasks();
        } else if (_isPlayerOverWater && transform.position.y > PlayerTransform.transform.position.y) {
            _isPlayerOverWater = false;
            SetCamerasCullingMasks();
        }
    }

    private void SetCamerasCullingMasks() {
        if (_isPlayerOverWater) {
            MainCamera.cullingMask |= _mask;
            OverWaterCamera.cullingMask &= ~_mask;
        } else {
            MainCamera.cullingMask &= ~_mask;
            OverWaterCamera.cullingMask |= _mask;
        }
    }
}
