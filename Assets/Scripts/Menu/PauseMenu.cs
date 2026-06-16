using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : Menu {
    public GameObject PauseMenuObject;
    public GameObject PausedSettingsObject;
    public GameObject Background;

    public static bool IsPaused { get; private set; } = false;

    // Start is called before the first frame update
    void Start() {
        PauseMenuObject.SetActive(false);
        PausedSettingsObject.SetActive(false);
        Background.SetActive(false);
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if(ViewStackEmpty()) {
                PauseGame();
            } else {
                PopView();
                if(ViewStackEmpty()) {
                    ResumeGame();
                }
            }
        }
    }

    public void PauseGame() {
        PushView(PauseMenuObject);
        Time.timeScale = 0.0f;
        IsPaused = true;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        UnselectButton();
        Background.SetActive(true);
    }

    public void ResumeGame() {
        PopView();
        Time.timeScale = 1.0f;
        IsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UnselectButton();
        Background.SetActive(false);
    }

    public void GoToMainMenu() {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToSettings() {
        PushView(PausedSettingsObject);
        UnselectButton();
    }

    public void QuitGame() {
        Application.Quit();
    }
}
