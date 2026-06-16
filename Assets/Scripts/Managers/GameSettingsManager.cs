using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

// The class should be instantiated after AudioManager
public class GameSettingsManager : MonoBehaviour {
    private string SavePath;
    public static GameSettingsManager Instance { get; private set; }
    // public NoiseSetting NoiseSettings;
    public SettingsData Settings { get; set; }
    public void Awake() {
        if (Instance == null) {
            DontDestroyOnLoad(this.gameObject);
            Instance = this;
            Initialize();
        } else if (Instance != this) {
            if (Application.isPlaying) {
                Destroy(this);
            } else {
                DestroyImmediate(this);
            }
        }
    }
    void Initialize() {
        SavePath = Application.persistentDataPath + "/SavedSettings.dat";
        Settings = new SettingsData();
        Settings.Resolution = new ResolutionItem();
        Load();
    }
    void OnApplicationQuit() {
        Save();
    }
    void Save() {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file;

        if (File.Exists(SavePath)) {
            file = File.Open(SavePath, FileMode.Open);
        } else {
            file = File.Create(SavePath);
        }
        try {
            bf.Serialize(file, Settings);
        } catch (Exception ex) {
            Debug.Log(ex.Message);
        }

        file.Close();
    }
    void Load() {
        bool saveExists = File.Exists(SavePath);
        bool loadSucceeded = true;
        if (saveExists) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(SavePath, FileMode.Open);
            try {
                Settings = (SettingsData)bf.Deserialize(file);
            } catch (Exception ex) {
                Debug.Log(ex.Message);
                loadSucceeded = false;
            }
            file.Close();

            if (loadSucceeded) {
                ApplyGraphics();
                ApplyVolume();
            }
        }
        if (!saveExists || !loadSucceeded) {
            Settings.FullScreen = Screen.fullScreen;
            Settings.VSyncCount = QualitySettings.vSyncCount;
            Settings.Resolution.Width = Screen.currentResolution.width;
            Settings.Resolution.Height = Screen.currentResolution.height;
            Settings.SoundVolume = AudioManager.Instance.MusicSource.volume;
        }
    }
    public void ApplyVolume() {
        AudioManager.Instance.MusicSource.volume = Settings.SoundVolume;
    }
    public void ApplyGraphics() {
        Screen.SetResolution(Settings.Resolution.Width, Settings.Resolution.Height, Settings.FullScreen);
        QualitySettings.vSyncCount = Settings.VSyncCount;
    }
}

[Serializable]
public class SettingsData {
    public string Seed;
    public bool FullScreen;
    public int VSyncCount;
    public ResolutionItem Resolution;
    public float SoundVolume;
}

[Serializable]
public class ResolutionItem {
    public int Width, Height;
}
