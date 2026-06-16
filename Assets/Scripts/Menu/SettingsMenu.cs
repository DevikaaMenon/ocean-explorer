using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : Menu {
    public Toggle FullscreenTog, VsyncTog;
    public TMP_Text ResolutionLabel;
    public Slider VolumeSlider;
    public List<ResolutionItem> Resolutions = new List<ResolutionItem>();
    private int _selectedResolution = 0;

    void Start() {
        var Settings = GameSettingsManager.Instance.Settings;

        FullscreenTog.isOn = Settings.FullScreen;

        if (Settings.VSyncCount == 0) {
            VsyncTog.isOn = false;
        } else {
            VsyncTog.isOn = true;
        }

        VolumeSlider.value = Settings.SoundVolume;

        _selectedResolution = Resolutions.FindIndex((ResolutionItem res) => {
            return res.Height == Settings.Resolution.Height &&
                   res.Width == Settings.Resolution.Width;
        });

        if (_selectedResolution < 0 || _selectedResolution >= Resolutions.Count) {
            Resolutions.Add(Settings.Resolution);
            _selectedResolution = Resolutions.Count - 1;
        }

        UpdateResolutionLabel();
    }

    private void UpdateResolutionLabel() {
        ResolutionItem res = Resolutions[_selectedResolution];
        ResolutionLabel.text = $"{res.Width} x {res.Height}";
    }

    public void SwitchResolutionRight() {
        _selectedResolution++;
        if (_selectedResolution == Resolutions.Count) {
            _selectedResolution = 0;
        }
        UpdateResolutionLabel();
        UnselectButton();
    }

    public void SwitchResolutionLeft() {
        _selectedResolution--;
        if (_selectedResolution == -1) {
            _selectedResolution = Resolutions.Count - 1;
        }
        UpdateResolutionLabel();
        UnselectButton();
    }

    public void UpdateVolume() {
        GameSettingsManager.Instance.Settings.SoundVolume = VolumeSlider.value;
        GameSettingsManager.Instance.ApplyVolume();
    }

    public void SaveOptions() {
        try {
            GameSettingsManager.Instance.Settings.FullScreen = FullscreenTog.isOn;
            GameSettingsManager.Instance.Settings.VSyncCount = VsyncTog.isOn ? 1 : 0;
            GameSettingsManager.Instance.Settings.Resolution = Resolutions[_selectedResolution];
            GameSettingsManager.Instance.ApplyGraphics();
        } catch (Exception ex) {
            Debug.Log(ex.Message);
        }
        UnselectButton();
    }
}
