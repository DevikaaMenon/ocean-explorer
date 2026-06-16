using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class ColorManager : MonoBehaviour {
    #region SINGLETON
    public static ColorManager Instance { get; private set; }

    public void Awake() {
        if (Instance == null) {
            Instance = this;
#if UNITY_EDITOR
            ColorSetting.OnColorSettingChanged += SettingsChanged;
            _settingsChanged = true;
#endif
            _textureChanged = true;
        } else if (Instance != this) {
            if (Application.isPlaying) {
                Destroy(this);
            } else {
                DestroyImmediate(this);
            }
        }
    }

#if UNITY_EDITOR
    private void SettingsChanged() {
        _settingsChanged = true;
    }

    private void OnValidate() {
        SettingsChanged();
    }

    private void OnDestroy() {
        ColorSetting.OnColorSettingChanged -= SettingsChanged;
    }
#endif
    #endregion SINGLETON

    private Texture2D _terrainTexture;
    private bool _textureChanged;
    private bool _settingsChanged;

    [Header("Assets")]
    public Material Terrain;
    public string TexturePath = "Assets/Textures/GeneratedTexture.png";

    [Header("Settings")]
    public ColorSetting ColorSetting;

    private void Update() {
#if UNITY_EDITOR
        if (_settingsChanged) {
            _settingsChanged = false;

            UpdateTexture();
        }
#endif
        Terrain.SetFloat("_BoundYMin", WorldManager.Instance.GroundLevel);
        Terrain.SetFloat("_BoundYMax", WorldManager.Instance.SurfaceLevel);
        if (_textureChanged) {
            _textureChanged = false;
            Terrain.SetTexture("_TerrainTexture", _terrainTexture);
        }
    }

#if UNITY_EDITOR
    private void UpdateTexture() {
        _textureChanged = true;
        if (_terrainTexture == null || _terrainTexture.width != ColorSetting.TextureWidth || _terrainTexture.height != ColorSetting.TextureHeight) {
            _terrainTexture = new(ColorSetting.TextureWidth, ColorSetting.TextureHeight, TextureFormat.RGBA32, false);
        }

        for (int y = 0; y < ColorSetting.TextureHeight; y++) {
            for (int x = 0; x < ColorSetting.TextureWidth; x++) {
                float t = (float)x / (ColorSetting.TextureWidth - 1);
                Color gradientColor = ColorSetting.Gradient.Evaluate(t);

                float u = (float)x / ColorSetting.TextureWidth;
                float v = (float)y / ColorSetting.TextureHeight;

                float noise = 0;
                for (int i = 0; i < 4; i++) {
                    noise += Mathf.PerlinNoise(
                        u * ColorSetting.OctaveFrequencies[i] + ColorSetting.Offset.x,
                        v * ColorSetting.OctaveFrequencies[i] + ColorSetting.Offset.y
                    ) * ColorSetting.OctaveAmplitudes[i];
                }
                noise = Mathf.Sin((u + v) * 0.5f + noise * Mathf.PI);
                noise *= Mathf.Sign(noise) * noise;
                gradientColor.r = Mathf.Clamp01(gradientColor.r + noise * ColorSetting.RWeigth);
                gradientColor.g = Mathf.Clamp01(gradientColor.g + noise * ColorSetting.GWeigth);
                gradientColor.b = Mathf.Clamp01(gradientColor.b + noise * ColorSetting.BWeigth);

                _terrainTexture.SetPixel(x, y, gradientColor);
            }
        }
        _terrainTexture.Apply();

        if (_terrainTexture != null && _terrainTexture.isReadable) {
            byte[] bytes = _terrainTexture.EncodeToPNG();
            if (bytes != null) {
                System.IO.File.WriteAllBytes(TexturePath, bytes);
                Debug.Log($"Saved texture to {TexturePath}");
                AssetDatabase.ImportAsset(TexturePath);
            } else {
                Debug.LogError("Failed to encode texture to .png format.");
            }
        } else {
            Debug.Log("Texture is not readable or not a Texture2D.");
        }
    }
#endif
}
