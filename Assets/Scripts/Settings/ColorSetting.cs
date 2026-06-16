using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Color Setting", menuName = "Settings/Color Setting")]
public class ColorSetting : ScriptableObject {
    [Range(32, 256)]
    public int TextureWidth = 128;
    [Range(32, 256)]
    public int TextureHeight = 64;

    public Gradient Gradient;
    public Vector4 OctaveFrequencies;
    public Vector4 OctaveAmplitudes;
    public Vector2 Offset;
    [Range(0f, 1f)]
    public float RWeigth = 0.5f;
    [Range(0f, 1f)]
    public float GWeigth = 0.5f;
    [Range(0f, 1f)]
    public float BWeigth = 0.5f;

    public static event Action OnColorSettingChanged;

    private void OnValidate() {
        OnColorSettingChanged?.Invoke();
    }
}
