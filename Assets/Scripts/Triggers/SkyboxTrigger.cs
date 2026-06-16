using UnityEngine;

public class SkyboxTrigger : MonoBehaviour {
    [Header("Target to follow")]
    public Transform PlayerTransform;

    [Header("Skybox")]
    public Camera MainCamera;
    public Color SkyboxColor;
    public Material SkyboxMaterial;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            RenderSettings.fog = true;
            MainCamera.clearFlags = CameraClearFlags.SolidColor;
            MainCamera.backgroundColor = SkyboxColor;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            RenderSettings.fog = false;
            MainCamera.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = SkyboxMaterial;
        }
    }
}