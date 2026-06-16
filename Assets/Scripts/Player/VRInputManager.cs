using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

// Reads Quest 3 controller input and bridges it to the same interface
// that PlayerController and Flashlight already use via InputManager.
// Left thumbstick  → swim direction (replaces WASD)
// Right thumbstick Y > 0 → surface      (replaces Space)
// Right trigger held → speed boost       (replaces Shift)
// Left primary button (X) → flashlight   (replaces F)
public class VRInputManager : MonoBehaviour {
    public static VRInputManager Instance { get; private set; }

    private InputDevice _leftController;
    private InputDevice _rightController;

    private Vector2 _moveInput;
    private bool _isSurfacing;
    private bool _isSpeeding;
    private bool _flashlightPressed;
    private bool _flashlightPressedLastFrame;

    void Awake() {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Update() {
        EnsureDevices();
        ReadInputs();
    }

    void EnsureDevices() {
        if (!_leftController.isValid) {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
            if (devices.Count > 0) _leftController = devices[0];
        }
        if (!_rightController.isValid) {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
            if (devices.Count > 0) _rightController = devices[0];
        }
    }

    void ReadInputs() {
        _leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out _moveInput);

        _rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightStick);
        _isSurfacing = rightStick.y > 0.3f;

        _rightController.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
        _isSpeeding = trigger > 0.5f;

        _leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool xButton);
        _flashlightPressed = xButton && !_flashlightPressedLastFrame;
        _flashlightPressedLastFrame = xButton;
    }

    public Vector2 GetPlayerMovement() => _moveInput;
    public bool IsSurfacing() => _isSurfacing;
    public bool Speeding() => _isSpeeding;
    public bool FlashlightToggled() => _flashlightPressed;
}
