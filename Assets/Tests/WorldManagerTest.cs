using NUnit.Framework;
using UnityEngine;

public class WorldManagerTest {
    [SetUp]
    public void SetUp() {
        //var manager = new GameObject().AddComponent<WorldManager>();
        //manager.Awake();
        var worldSetting = ScriptableObject.CreateInstance<WorldSetting>();
        worldSetting.ChunkSize = 64;
        worldSetting.NumCubesXZ = 32;
        worldSetting.NumCubesY = 64;
        worldSetting.Threshold = 0.5f;
        WorldManager.Instance.WorldSetting = worldSetting;
    }

    [Test]
    public void WithWorldSetting_CubeSize_CorrectSize() {
        // Arrange
        float expected = 2f;
        // Act
        float result = WorldManager.CubeSize();
        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void WithWorldSetting_ChunkHeight_CorrectHeight() {
        // Arrange
        float expected = 128f;
        // Act
        float result = WorldManager.ChunkHeight();
        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void WithWorldSetting_PosToChunkPos_CorrectTransformation() {
        // Arrange
        var vec = Vector3.one;
        var expected = new Vector3(0, WorldManager.Instance.GroundLevel, 0);
        // Act
        Vector3 result = WorldManager.PosToChunkPos(vec);
        // Assert
        Assert.AreEqual(expected, result);
    }

}
