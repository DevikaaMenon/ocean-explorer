using UnityEngine;

public class AlgaeFollow : MonoBehaviour {
    [Header("Target to follow")]
    public Transform PlayerTransform;

    private void FixedUpdate() {
        transform.position = new(PlayerTransform.position.x, PlayerTransform.position.y, PlayerTransform.transform.position.z);
    }
}
