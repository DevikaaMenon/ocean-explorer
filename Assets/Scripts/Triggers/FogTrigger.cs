using UnityEngine;

public class FogTrigger : MonoBehaviour {
    public float WaterSurfaceOffset = 1.5f;
    [Header("Fog")]
    [Range(0.01f, 0.05f)]
    public float BaseFogDensity = 0.03f;
    public Material VolumetricFogMaterial;

    [Header("Target to follow")]
    public Transform PlayerTransform;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            RenderSettings.fogDensity = BaseFogDensity;
            RenderSettings.fog = true;
            VolumetricFogMaterial.SetFloat("_TurnOn", 1f);
        }
    }

    //private void FixedUpdate() {
    //    float diff = WorldManager.Instance.SurfaceLevel + WaterSurfaceOffset - PlayerTransform.position.y;
    //    float scale = WorldManager.Instance.SurfaceLevel + WaterSurfaceOffset - WorldManager.Instance.GroundLevel;
    //    RenderSettings.fogDensity = 0.03f * (0.5f + diff / scale);
    //}


    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            RenderSettings.fog = false;
            VolumetricFogMaterial.SetFloat("_TurnOn", 0f);
        }
    }
}
