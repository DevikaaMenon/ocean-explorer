using System;
using UnityEngine;

public struct Triangle {
    public Vector3 v1;
    public Vector3 v2;
    public Vector3 v3;

    public readonly Vector3 this[int i] => i switch {
        0 => v1,
        1 => v2,
        2 => v3,
        _ => throw new Exception("Index out of range")
    };
}

