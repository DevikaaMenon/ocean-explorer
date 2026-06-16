using UnityEngine;

public class VolumeFollow : MonoBehaviour {
    [Header("Target to follow")]
    public Transform PlayerTransform;

    [Header("Volume Position")]
    public float Offset = 2f;
    public bool Above;

    private void Start() {
        int sign = Above ? 1 : -1;
        Vector3 pos = transform.position;
        pos.y = WorldManager.Instance.SurfaceLevel + sign * transform.localScale.y / 2 + Offset;
        transform.position = pos;
    }

    private void FixedUpdate() {
        transform.position = new(PlayerTransform.position.x, transform.position.y, PlayerTransform.transform.position.z);
    }
}
