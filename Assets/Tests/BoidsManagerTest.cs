using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEditor.VersionControl;
using System.Collections.Generic;

public class BoidsManagerTests {
    private BoidsManager _boidsManager;

    [SetUp]
    public void SetUp() {
        _boidsManager = new GameObject().AddComponent<BoidsManager>();
        _boidsManager.Setting = ScriptableObject.CreateInstance<BoidsSetting>();

        InitManager();

        _boidsManager.Start();
    }

    private void InitManager() {
        _boidsManager.Models = new List<ModelData>();
        const int numberOfSpecies = 2;

        for(int i = 0; i < numberOfSpecies; i++) {
            _boidsManager.Models.Add(new ModelData{
                Count = 100,
                MinScale = 1.0f,
                MaxScale = 1.0f
            });
        }

        _boidsManager.Setting.BoidsSettings = new List<BoidsData>();
        for(int i = 0; i < numberOfSpecies; i++) {
            _boidsManager.Setting.BoidsSettings.Add(new BoidsData{
                MaxSpeed = 1.0f,
                MaxSteerForce = 1.0f,
                HalfFOVCosine = 0.0f,
                Interactions = new List<InteractionData>(),
            });
            for(int j = 0; j < numberOfSpecies; j++) {
                _boidsManager.Setting.BoidsSettings[i].Interactions.Add(new InteractionData{
                   SpeciesId = j,
                   Behaviour = new BehaviourData(){
                        cohesion = new ComponetsData() {
                            factor = 1.0f,
                            radius = 1.0f
                        },
                        alignment = new ComponetsData() {
                            factor = 1.0f,
                            radius = 1.0f
                        },
                        separation = new ComponetsData() {
                            factor = 1.0f,
                            radius = 1.0f
                        },
                   } 
                });   
            }
        }
    }

    [TestCase(123, 1)]
    [TestCase(312, 2)]
    public void GetNumberOfGroups_ReturnsProperNumberOfThreadGroups_WhenAtLeastOneThreadWillBeExcuted(int numberOfThreads, int expected)
    {
        // Arrange
        // Act
        int numberOfGroups = _boidsManager.GetNumberOfGroups(numberOfThreads);
        // Assert
        Assert.AreEqual(expected, numberOfGroups);
    }

    [TestCase(0)]
    [TestCase(-2330)]
    public void GetNumberOfGroups_ThrowsArgumentException_WhenInvalidNumberOfThreadsIsPassed(int numberOfThreads) 
    {
        // Arrange
        // Act and Assert
        Assert.Throws<ArgumentException>(() => { _boidsManager.GetNumberOfGroups(numberOfThreads); });
    }

    [Test]
    public void SteerTowards_ReturnsCorrectVector_WhenValidParametersArePassed()
    {
        // Arrange
        Vector3 vector = new Vector3(10.0f, 0.0f, 0.0f);
        Vector3 velocity = new Vector3(1.0f, 0.0f, 0.0f);
        int speciesId = 0;
        _boidsManager.Setting.BoidsSettings[0] = new BoidsData() {
            MaxSpeed = 3.0f,
            MaxSteerForce = 2.0f,
            HalfFOVCosine = _boidsManager.Setting.BoidsSettings[speciesId].HalfFOVCosine,
            Interactions = _boidsManager.Setting.BoidsSettings[speciesId].Interactions
        };
        _boidsManager.SetSpeciesParameters();

        // Act
        Vector3 result = _boidsManager.SteerTowards(vector, velocity, speciesId);
        // Assert
        Assert.AreEqual(2.0f, result.x);
        Assert.AreEqual(0.0f, result.y);
        Assert.AreEqual(0.0f, result.z);
    }
    
    public void SteerTowards_ThrowsArgumentException_WhenFirstVectorIsZero()
    {
        // Arrange
        Vector3 vector = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 velocity = new Vector3(1.0f, 1.1f, 3.09f);
        int speciesId = 0;
        _boidsManager.SetSpeciesParameters();
        _boidsManager.Setting.BoidsSettings[0] = new BoidsData() {
            MaxSpeed = 1.0f,
            MaxSteerForce = 34.01f,
            HalfFOVCosine = _boidsManager.Setting.BoidsSettings[speciesId].HalfFOVCosine,
            Interactions = _boidsManager.Setting.BoidsSettings[speciesId].Interactions
        };
        // Act and Assert
        Assert.Throws<ArgumentException>(() => {_boidsManager.SteerTowards(vector, velocity, speciesId); });
    }

    [TestCase(0, 0, 0)]
    [TestCase(0, 1, 1)]
    [TestCase(1, 0, 2)]
    [TestCase(1, 1, 3)]
    public void SpeciesToIndex_ReturnsProperId_WhenValidDataPassed(int species1, int species2, int expected)
    {
        // Arrange
        // Act
        int result = _boidsManager.SpeciesToIndex(species1, species2);
        // Assert
        Assert.AreEqual(expected, result);
    }   

    [TestCase(-1, 0)]
    [TestCase(123, -2)]
    public void SpeciesToIndex_ThrowsArgumentOutOfRangeException_WhenInvlidDataIsPassed(int species1, int species2)
    {
        // Arrange
        // Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => { _boidsManager.SpeciesToIndex(species1, species2); });
    }
}
