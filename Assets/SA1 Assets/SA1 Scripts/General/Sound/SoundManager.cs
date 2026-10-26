using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Static instance allows other scripts to access this easily
    public static SoundManager Instance { get; private set; }

    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("--- Audio Clips ---")]
    public AudioClip backgroundMusic;
    public AudioClip arrowShoot;
    public AudioClip playerWalk;
    public AudioClip enemyAttack;
    public AudioClip hitEffect;

    private void Awake()
    {
        // Enforce a Single Instance (Singleton)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep music playing across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Start background music automatically
        PlayMusic(backgroundMusic);
    }

    // Call this for one-shot sound effects
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // Call this to change or start background music
    public void PlayMusic(AudioClip clip)
    {
        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // Main text-string play handler to cleanly match your other script calls
    public void Play(string name)
    {
        switch (name.ToLower())
        {
            case "shoot":
                PlaySFX(arrowShoot);
                break;
            case "walk":
                PlaySFX(playerWalk);
                break;
            case "enemyattack":
            case "enemy":
                PlaySFX(enemyAttack);
                break;
            case "hit":
                PlaySFX(hitEffect);
                break;
            default:
                Debug.LogWarning($"[Sound] Clip name string '{name}' not recognized!");
                break;
        }
    }
}
