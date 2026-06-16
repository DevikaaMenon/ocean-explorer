using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour {
    private bool _fpCameraEnabled;
    [Header("Cameras")]
    [SerializeField] private GameObject FPCamera;
    [SerializeField] private GameObject FPVolume;
    [SerializeField] private GameObject TPCamera;
    [SerializeField] private GameObject TPVolume;

    [HideInInspector] public Transform Transform;

    private void Awake() {
        _fpCameraEnabled = true;
        SetCameras();
        SetTransform();
    }

    private void Start() {
        InputManager.Instance.Controls.Keys.CameraSwitch.performed += SwitchCameras;
    }

    private void OnDisable() {
        InputManager.Instance.Controls.Keys.CameraSwitch.performed -= SwitchCameras;
    }

    private void SetCameras() {
        FPCamera.SetActive(_fpCameraEnabled);
        FPVolume.SetActive(_fpCameraEnabled);
        TPCamera.SetActive(!_fpCameraEnabled);
        TPVolume.SetActive(!_fpCameraEnabled);
    }

    private void SetTransform() {
        if (_fpCameraEnabled) {
            Transform = FPCamera.transform;
        } else {
            Transform = TPCamera.transform;
        }
    }

    public float GetCameraYaw() => Transform.eulerAngles.y;

    public Vector3 GetCameraDirection() => Transform.forward;

    public void SwitchCameras(InputAction.CallbackContext context) {
        _fpCameraEnabled = !_fpCameraEnabled;
        SetCameras();
        SetTransform();
    }
}
