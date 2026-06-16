using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour {
    [HideInInspector] public List<Vector3> Vertices { get; private set; }
    [HideInInspector] public List<int> Triangles { get; private set; }

    private Mesh _mesh;
    private MeshFilter _filter;
    private MeshRenderer _renderer;
    private MeshCollider _collider;

    [HideInInspector] public bool IsDataGenerated;
    public Vector3 Position;

    public void Initialize() {
        IsDataGenerated = false;

        _filter = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<MeshCollider>();
        _renderer.sharedMaterial = ColorManager.Instance.Terrain;
        gameObject.layer = LayerMask.NameToLayer("Terrain");
        gameObject.tag = "Chunk";
    }

    public void SetData(List<Vector3> vertices, List<int> triangles) {
        Vertices = vertices;
        Triangles = triangles;
        IsDataGenerated = true;
    }

    public void SetMesh() {
        _mesh = _filter.sharedMesh;
        if (_mesh == null) {
            _mesh = new();
            _filter.sharedMesh = _mesh;
        }

        _mesh.SetVertices(Vertices);
        _mesh.SetTriangles(Triangles, 0);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _mesh.Optimize();
        _mesh.UploadMeshData(false);

        _collider.sharedMesh = _mesh;

        if (!gameObject.activeInHierarchy) {
            gameObject.SetActive(true);
            //Debug.Log($"Chunk [{Position.x}, {Position.y}, {Position.z}] activated.\"");
        }

        _collider.sharedMesh = _mesh; // for now collider mesh is the same as filter
    }

    public bool RaycastMesh(Ray ray, out RaycastHit hit, float maxDistance) {
        return _collider.Raycast(ray, out hit, maxDistance);
    }

    private void DeleteChildren() {
        foreach (Transform child in transform) {
            if (Application.isPlaying) {
                Destroy(child.gameObject);
            } else {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    public void DisposeMesh() {
        //Debug.Log($"Chunk [{Position.x}, {Position.y}, {Position.z}] disposed.\"");
        _filter.sharedMesh = null;
        _collider.sharedMesh = null;

        if (_mesh != null) {
            _mesh.Clear();
            if (Application.isPlaying) {
                Destroy(_mesh);
            } else {
                DestroyImmediate(_mesh);
            }
        }
        _mesh = null;

        DeleteChildren();
    }

    public void DisposeMeshAndData() {
        DisposeMesh();
        Vertices.Clear();
        Vertices.Capacity = 0;
        Triangles.Clear();
        Triangles.Capacity = 0;
        IsDataGenerated = false;

        DeleteChildren();
    }

    //#if UNITY_EDITOR
    //    private void OnDrawGizmos() {
    //        float size = WorldManager.Instance.WorldSetting.ChunkSize;
    //        float height = WorldManager.ChunkHeight();

    //        if (WorldManager.Instance.ShowGizmos && Application.isPlaying && Selection.activeObject == gameObject) {
    //            Gizmos.color = WorldManager.Instance.GizmosColor;
    //            var vec = new Vector3(size, height, size);
    //            Gizmos.DrawCube(Position + vec / 2f, vec);
    //        }
    //    }
    //#endif
}
