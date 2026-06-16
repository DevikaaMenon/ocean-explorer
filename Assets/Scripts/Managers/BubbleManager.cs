using UnityEngine;
using UnityEngine.SocialPlatforms;

public class BubbleManager : MonoBehaviour {
    #region SINGLETON
    public static BubbleManager Instance { get; private set; }

    public void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            if (Application.isPlaying) {
                Destroy(this);
            } else {
                DestroyImmediate(this);
            }
        }
    }
    #endregion

    public ParticleSystem Prefab;
    public Vector3 Rotation = new(-90, 0, 0);
    [Header("Settings")]
    public string HitTag = "Chunk";
    [Range(0f, 10f)]
    public float MinRadius = 1f;
    [Range(0f, 100f)]
    public float MaxRadius = 10f;
    public float RayDistance = 50f;
    [Range(1f, 100f)]
    public float Probability = 5f;

    private GameObject _bubbleHolder;
    const string _bubbleHolderName = "Bubble Holder";

    private void Start() {
        MakeFreshBubbleHolder();
    }

    private void MakeFreshBubbleHolder() {
        if (_bubbleHolder == null) {
            _bubbleHolder = GameObject.Find(_bubbleHolderName);
            if (_bubbleHolder == null) {
                _bubbleHolder = new(_bubbleHolderName);
            }
        }

        Prefab.gameObject.layer = LayerMask.NameToLayer("Bubbles");
        _bubbleHolder.layer = LayerMask.NameToLayer("Bubbles");

        // going backwards is crucial!
        for (int i = _bubbleHolder.transform.childCount - 1; i >= 0; i--) {
            if (Application.isPlaying) {
                Destroy(_bubbleHolder.transform.GetChild(i).gameObject);
            } else {
                DestroyImmediate(_bubbleHolder.transform.GetChild(i).gameObject);
            }
        }
    }

    public static Vector3 GetRandomPosition(Vector3 pos, Vector3 dir) {
        Vector3 normDir = dir.normalized;
        float baseAngle = Mathf.Atan2(normDir.z, normDir.x) * Mathf.Rad2Deg;
        float randomAngle = Random.Range(-90f, 90f);
        float angleRad = (baseAngle + randomAngle) * Mathf.Deg2Rad;

        float radius = Random.Range(Instance.MinRadius, Instance.MaxRadius);
        float xOffset = radius * Mathf.Cos(angleRad);
        float zOffset = radius * Mathf.Sin(angleRad);

        return new Vector3(pos.x + xOffset, WorldManager.Instance.SurfaceLevel, pos.z + zOffset);
    }

    public static void TrySpawning(Vector3 origin, Vector3 direction) {
        if (Random.Range(0f, 1f) > Instance.Probability / 1000f) { return; }

        Vector3 pos = GetRandomPosition(origin, direction);
        var ray = new Ray(pos, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, Instance.RayDistance)) {
            if (hit.collider.CompareTag(Instance.HitTag)) {
                ParticleSystem sys = Instantiate(Instance.Prefab, hit.point, Quaternion.Euler(Instance.Rotation));
                sys.transform.parent = Instance._bubbleHolder.transform;
                sys.Play();
            }
        }
    }
}
