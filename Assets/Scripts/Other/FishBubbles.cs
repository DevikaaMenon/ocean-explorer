using UnityEngine;

public class FishBubbles : MonoBehaviour {
    [HideInInspector]
    public ParticleSystem Prefab;
    [HideInInspector]
    public Transform Parent;

    void Start() {
        Prefab.gameObject.layer = LayerMask.NameToLayer("Bubbles");
        ParticleSystem sys = Instantiate(Prefab, Parent.position, Quaternion.identity);
        sys.transform.parent = Parent;
        sys.Play();
    }
}
