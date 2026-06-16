using UnityEngine;

public class InputManager : MonoBehaviour {
    #region SINGLETON
    public static InputManager Instance { get; private set; }

    public void Awake() {
        if (Instance == null) {
            Instance = this;
            Controls = new Controls();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        } else if (Instance != this) {
            if (Application.isPlaying) {
                Destroy(this);
            } else {
                DestroyImmediate(this);
            }
        }
    }
    #endregion

    public Controls Controls;

    private void OnEnable() {
        Controls?.Enable();
    }

    private void OnDisable() {
        Controls?.Disable();
    }

    public Vector2 GetPlayerMovement() {
        return Controls.Player.Swimming.ReadValue<Vector2>();
    }

    // currently unused
    //public Vector2 GetMouseMovement() {
    //    return Controls.Player.Looking.ReadValue<Vector2>();
    //}

    public bool IsSurfacing() {
        return Controls.Player.Surfacing.ReadValue<float>() > 0;
    }

    public bool Speeding() {
        return Controls.Player.Speeding.ReadValue<float>() > 0;
    }
}
