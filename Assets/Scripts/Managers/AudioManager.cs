using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }
    public AudioSource MusicSource;
    public AudioClip Background;

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
        MusicSource.clip = Background;
        MusicSource.loop = true;
        MusicSource.volume = 0.5f;
    }
    void Start() {
        MusicSource.Play();
    }



}
