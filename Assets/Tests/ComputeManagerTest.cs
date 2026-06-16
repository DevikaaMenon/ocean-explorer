using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ComputeManagerTest {
    private Chunk _chunk;

    [SetUp]
    public void SetUp() {
        _chunk = new GameObject().AddComponent<Chunk>();
        _chunk.Position = Vector3.zero;
    }

    [Test]
    public void CorrectTriangle_CalculateMesh_ThreeVerticesWithAccordingIndices() {
        // Arrange
        var triangle = new Triangle() {
            v1 = new Vector3(0, 0, 0),
            v2 = new Vector3(1, 0, 0),
            v3 = new Vector3(0, 1, 0)
        };
        var triangles = new List<Triangle>() { triangle };
        var expVertices = new List<Vector3>() { triangle.v3, triangle.v2, triangle.v1 };
        var expTriangles = new List<int>() { 0, 1, 2 };
        // Act
        ComputeManager.CalculateMesh(_chunk, triangles);
        // Assert
        Assert.AreEqual(expVertices, _chunk.Vertices);
        Assert.AreEqual(expTriangles, _chunk.Triangles);
    }

    [Test]
    public void ZeroTriangle_CalculateMesh_OneVertexWithThreeIdenticalIndices() {
        // Arrange
        var triangle = new Triangle() {
            v1 = Vector3.zero,
            v2 = Vector3.zero,
            v3 = Vector3.zero
        };
        var triangles = new List<Triangle>() { triangle };
        var expVertices = new List<Vector3>() { triangle.v1 };
        var expTriangles = new List<int>() { 0, 0, 0 };
        // Act
        ComputeManager.CalculateMesh(_chunk, triangles);
        // Assert
        Assert.AreEqual(expVertices, _chunk.Vertices);
        Assert.AreEqual(expTriangles, _chunk.Triangles);
    }
}

