using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : Menu {
    public GameObject StartMenu;
    public GameObject SettingsMenu;
    public GameObject ManualMenu;

    void Awake() {
        PushView(StartMenu);
        SettingsMenu.SetActive(false);
        ManualMenu.SetActive(false);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) && 
            !StartMenu.activeSelf) {
            PopView();
        }
    }

    public void PlayGame() {
        SceneManager.LoadScene("MainScene");
    }

    public void QuitGame() {
        Application.Quit();
    }

    public void GoToSettings() {
        PushView(SettingsMenu);
        UnselectButton();
    }

    public void GoToManual() {
        PushView(ManualMenu);
        UnselectButton();
    }
}
