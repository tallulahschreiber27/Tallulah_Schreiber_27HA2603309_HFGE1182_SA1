using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HealthHandler : MonoBehaviour
{
    [Header("HEALTH PARAMETERS")]
    [Tooltip("Add tags here to define what this character is vulnerable to. Leave empty to accept ALL damage.")]
    [SerializeField] private string[] damageTags;
    [SerializeField] private float health;
    [SerializeField] private float healthMax = 100f;

    [Header("UI ELEMENTS")]
    [Tooltip("Drag and drop your health bar Slider here!")]
    [SerializeField] private UnityEngine.UI.Slider healthSlider;

    [Header("HEALTHBAR POSITIONING")]
    [Tooltip("How high above the character's pivot point should the health bar float?")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2.2f, 0f);
    private Transform canvasTransform;
    private Transform mainCameraTransform;

    [Header("DECAY PARAMETERS")]
    [SerializeField] private bool doesDecay = false;
    [SerializeField] private float decayTickRate = 1f;
    [SerializeField] private float decayDamage = 2f;
    private float decayTimer;

    [Header("DEATH PARAMETERS")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyOnDeathDelay = 0f;

    [Header("PLAYER VISUAL FEEDBACK (SA1 CUSTOM)")]
    [Tooltip("ONLY FOR THE PLAYER: Drag your Red Damage Flash UI Image here!")]
    [SerializeField] private UnityEngine.UI.Image damageFlashImage;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.4f); // Red with 40% transparency

    private bool isDead = false;

    #region BostonCity Campus
    /* This code is property of Boston City Campus, and should not be used, or copied for any other subject.
     If you would copy and or use this in an assessment outside the assessment that this was supplied for
     action will be taken against you for plagiarism.
    */
    #endregion
    void Start()
    {
        if (health <= 0 || health == healthMax)
        {
            health = healthMax;
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<UnityEngine.UI.Slider>();
        }

        if (healthSlider != null)
        {
            Canvas parentCanvas = healthSlider.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasTransform = parentCanvas.transform;
                canvasTransform.SetParent(null);
            }
        }

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        UpdateHealthBarUI();
    }

    void Update()
    {
        if (isDead) return;

        if (doesDecay)
        {
            decayTimer += Time.deltaTime;
            if (decayTimer >= decayTickRate)
            {
                DamageHandler(decayDamage);
                decayTimer = 0f;
            }
        }

        // SMOOTHLY FADE OUT THE RED DAMAGE FLASH OVER TIME
        if (gameObject.CompareTag("Player") && damageFlashImage != null && damageFlashImage.color.a > 0)
        {
            damageFlashImage.color = Color.Lerp(damageFlashImage.color, Color.clear, flashDuration * Time.deltaTime * 10f);
        }
    }

    void LateUpdate()
    {
        if (isDead || canvasTransform == null) return;

        canvasTransform.position = transform.position + healthBarOffset;

        if (mainCameraTransform != null)
        {
            canvasTransform.LookAt(canvasTransform.position + mainCameraTransform.rotation * Vector3.forward,
                                   mainCameraTransform.rotation * Vector3.up);
        }
    }

    private void UpdateHealthBarUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Clamp01(health / healthMax);
        }
    }

    public void DamageHandler(string damageTag, float damageAmount)
    {
        if (isDead) return;

        if (damageTag == "Arrow" || damageTag == "Enemy" || (damageTags != null && damageTags.Contains(damageTag)))
        {
            ApplyDamage(damageAmount);
        }
        else
        {
            Debug.Log($"[{gameObject.name}] blocked damage from tag: {damageTag} (Not in accepted list).");
        }
    }

    public void DamageHandler(float damageAmount)
    {
        if (isDead) return;

        ApplyDamage(damageAmount);
    }

    private void ApplyDamage(float amount)
    {
        if (amount <= health)
        {
            health -= amount;
        }
        else
        {
            health = 0;
        }

        UpdateHealthBarUI();

        // --- AUDIO FEEDBACK: DAMAGE TAKEN ---
        if (SoundManager.Instance != null && health > 0)
        {
            if (gameObject.CompareTag("Player"))
            {
                SoundManager.Instance.Play("playerhurt");
            }
            else if (gameObject.CompareTag("Enemy"))
            {
                SoundManager.Instance.Play("hit");
            }
        }

        // --- VISUAL FEEDBACK: PLAYER FLASHES RED SCREEN ---
        if (gameObject.CompareTag("Player") && damageFlashImage != null && health > 0)
        {
            damageFlashImage.color = flashColor;
        }

        if (health <= 0)
        {
            DeathHandler();
        }
    }

    public void HealHandler(float healAmount)
    {
        if (isDead) return;

        if (health + healAmount >= healthMax)
        {
            health = healthMax;
        }
        else
        {
            health += healAmount;
        }

        UpdateHealthBarUI();
    }

    public void DeathHandler()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[{gameObject.name}] has died.");

        // --- AUDIO FEEDBACK: CHARACTER DEATH ---
        if (SoundManager.Instance != null)
        {
            if (gameObject.CompareTag("Player"))
            {
                SoundManager.Instance.Play("playerdeath");
            }
            else
            {
                SoundManager.Instance.Play("enemydeath");
            }
        }

        // --- GAME OVER UI SYSTEM TRIPPED ---
        if (gameObject.CompareTag("Player"))
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.TriggerGameOver();
            }
            else
            {
                Debug.LogError("[HEALTH HANDLER] Critical: UIManager instance missing from scene hierarchy!");
            }
        }

        if (gameObject.CompareTag("Enemy") && GameManager.Instance != null)
        {
            GameManager.Instance.CheckLevelStatus();
        }

        AnimalAI aiScript = GetComponent<AnimalAI>();
        if (aiScript != null) aiScript.Die();

        // FIX: Prevent deleting the player's main UI canvas!
        if (canvasTransform != null && !gameObject.CompareTag("Player"))
        {
            Destroy(canvasTransform.gameObject);
        }

        if (destroyOnDeath)
        {
            // For the player, disable components instead of destroying the GameObject entirely
            if (gameObject.CompareTag("Player"))
            {
                var controller = GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;

                // Disable player visual rendering if necessary, but keep the object alive for the camera!
            }
            else
            {
                Destroy(gameObject, destroyOnDeathDelay);
            }
        }
        else
        {
            if (!gameObject.CompareTag("Player"))
            {
                gameObject.SetActive(false);
            }
        }
    }


    public float GetMaxHealth()
    {
        return healthMax;
    }

    public float GetHealth()
    {
        return health;
    }

    public void SetHealth(float healthValue)
    {
        health = Mathf.Clamp(healthValue, 0f, healthMax);
        UpdateHealthBarUI();

        if (health <= 0)
        {
            DeathHandler();
        }
    }
}
