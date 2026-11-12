using UnityEngine;

public class SoundManager : MonoBehaviour
{
    
    public static SoundManager Instance { get; private set; }

    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("--- Audio Clips ---")]
    public AudioClip backgroundMusic;
    public AudioClip arrowShoot;
    public AudioClip arrowMidAir;
    public AudioClip playerWalk;
    public AudioClip enemyAttack;
    public AudioClip hitEffect;

    [Header("--- HEALTH & COMBAT AUDIO ---")]
    public AudioClip playerHurt;
    public AudioClip playerDeath;
    public AudioClip enemyDeath;

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

    // Changes or starts BG music
    public void PlayMusic(AudioClip clip)
    {
        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // Main text-string play handler to cleanly match the other script calls
    public void Play(string name)
    {
        switch (name.ToLower())
        {
            case "shoot":
                PlaySFX(arrowShoot);
                StartCoroutine(PlayDelayedMidAirSound());
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
            case "playerhurt":
                PlaySFX(playerHurt);
                break;
            case "playerdeath":
                PlaySFX(playerDeath);
                break;
            case "enemydeath":
                PlaySFX(enemyDeath);
                break;
            default:
                Debug.LogWarning($"[Sound] Clip name string '{name}' not recognized!");
                break;
        }
    }

    // Coroutine sequence to handle the brief delayed flying arrow audio
    private System.Collections.IEnumerator PlayDelayedMidAirSound()
    {
        yield return new WaitForSeconds(0.15f);
        if (arrowMidAir != null)
        {
            PlaySFX(arrowMidAir);
        }
    }
}
