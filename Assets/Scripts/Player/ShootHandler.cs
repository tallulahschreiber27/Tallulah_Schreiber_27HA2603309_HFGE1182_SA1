using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

public class ShootHandler : MonoBehaviour
{
    [Header("UI ELEMENTS")]
    [SerializeField] private TMP_Text ammoText;

    [Header("WEAPON VISUALS")]
    [SerializeField] private GameObject bowVisual;
    [SerializeField] private GameObject fistVisual;

    [Header("ACTIONS")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileForce = 20f;
    [SerializeField] private Transform projectileSpawn;
    [SerializeField] private float maxChargeTime = 1f;
    private bool isCharging = false;
    private float chargeTime = 0f;

    [Header("PUNCH SYSTEM CONFIGURATION")]
    [SerializeField] private GameObject punchHitBox;
    [SerializeField] private float punchDamage = 15f;

    [Header("AMMO SYSTEM CONFIGURATION")]
    [SerializeField] private int currentAmmo = 10;
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private int totalAmmo = 30;
    [SerializeField] private int maxReserveAmmo = 50;

    [Header("CAMERA")]
    [SerializeField] private Camera cam;
    [SerializeField] private float defaultCamFOV = 60f;
    [SerializeField] private float zoomCamFOV = 40f;
    private bool isZooming;

    private bool isPunching = false;
    private Coroutine punchCoroutine;
    private Animator playerAnim;

    #region BostonCity Campus
    /* This code is property of Boston City Campus, and should not be used, or copied for any other subject.
     If you would copy and or use this in an assessment outside the assessment that this was supplied for
     action will be taken against you for plagiarism.
    */
    #endregion
    private void Awake()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    private void Start()
    {
        playerAnim = GetComponent<Animator>();
        if (playerAnim == null)
        {
            Debug.LogWarning($"<b>[{gameObject.name}]</b> ShootHandler could not find an Animator component on this GameObject!");
        }

        if (punchHitBox != null) punchHitBox.SetActive(false);
        CheckWeaponAndUIState();
    }

    private void Update()
    {
        if (Time.timeScale == 0f || (UIManager.Instance != null && UIManager.Instance.IsPaused))
        {
            if (isCharging)
            {
                isCharging = false;
                chargeTime = 0f;
            }
            return; 
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (isCharging)
            {
                isCharging = false;
                chargeTime = 0f;
            }
            return;
        }

        if (Input.GetMouseButtonDown(0) && !isCharging && !isPunching)
        {
            if (currentAmmo > 0)
            {
                OnBeginShootManual();

                //Tells animator to pull bow back
                if (playerAnim != null) playerAnim.SetTrigger("ShootTrigger");
            }
            else
            {
                punchCoroutine = StartCoroutine(PunchObject());

                //Tells animator to punch
                if (playerAnim != null) playerAnim.SetTrigger("PunchTrigger");
            }
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            OnShootManual();
        }

        if (Input.GetMouseButton(1) && currentAmmo > 0)
        {
            isZooming = true;
        }
        else
        {
            isZooming = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            OnReloadManual();
        }

        ZoomIn();
        ChargeShot();
    }

    private void CheckWeaponAndUIState()
    {
        bool bowIsReady = currentAmmo > 0;

        if (bowVisual != null) bowVisual.SetActive(bowIsReady);
        if (fistVisual != null) fistVisual.SetActive(!bowIsReady);

        if (ammoText != null)
        {
            if (bowIsReady)
            {
                // Stops previous flickering bug encountered
                int totalCombinedArrows = currentAmmo + totalAmmo;
                ammoText.text = $"{totalCombinedArrows} / 10";
            }
            else
            {
                ammoText.text = "WEAPON: FISTS";
            }
        }
    }


    public void ChargeShot()
    {
        if (isCharging)
        {
            chargeTime = Mathf.MoveTowards(chargeTime, maxChargeTime, Time.deltaTime);
        }
    }

    private void OnBeginShootManual()
    {
        isCharging = true;
        chargeTime = 0f;
    }

    public void OnShootManual()
    {
        isCharging = false;

        if (currentAmmo <= 0)
        {
            CheckWeaponAndUIState();
            return;
        }

        currentAmmo--;
        CheckWeaponAndUIState();

        // Audio feedback for shooting
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play("shoot");
        }

        if (projectilePrefab != null && projectileSpawn != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawn.position, projectileSpawn.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();

            if (rb != null)
            {
                float combinedCharge = Mathf.Max(0.2f, chargeTime / maxChargeTime);
                rb.AddForce(projectileSpawn.forward * projectileForce * combinedCharge, ForceMode.Impulse);
            }
        }
    }

    private void OnReloadManual()
    {
        if (currentAmmo == maxAmmo || totalAmmo <= 0)
        {
            Debug.Log("[RELOAD DENIED] Clip already full or no arrows in reserve pool.");
            return;
        }

        Debug.Log("Reloading Weapon...");

        if (isPunching)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            if (punchHitBox != null) punchHitBox.SetActive(false);
            isPunching = false;
        }

        isCharging = false;

        int ammoNeeded = maxAmmo - currentAmmo;

        if (totalAmmo >= ammoNeeded)
        {
            currentAmmo += ammoNeeded;
            totalAmmo -= ammoNeeded;
        }
        else
        {
            currentAmmo += totalAmmo;
            totalAmmo = 0;
        }

        // Section B audio for reload and hit from bow
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play("hit"); // Triggers mechanical reload sound click
        }

        Debug.Log($"[RELOAD SUCCESS] Clip: {currentAmmo} | Reserves Remaining: {totalAmmo}");

        CheckWeaponAndUIState();
    }

    public void AddAmmo(int amount)
    {
        // SA1 Rule: Cap total maximum arrows on person to exactly 10
        int absoluteMaxArrows = 10;
        int combinedCurrentCount = currentAmmo + totalAmmo;

        //  BLOCK PICKUP: If you already have 10 or more arrows total, turn away!
        if (combinedCurrentCount >= absoluteMaxArrows)
        {
            Debug.Log("[PICKUP BLOCKED] Your quiver and bow are completely full of arrows!");
            return;
        }

        // Add the ammo to reserves, but cap it so combined total never crosses 10
        int spaceLeft = absoluteMaxArrows - combinedCurrentCount;
        int amountToAdd = Mathf.Min(amount, spaceLeft);

        totalAmmo += amountToAdd;

        Debug.Log($"[AMMO PICKUP] Gathered arrows. Total Reserves: {totalAmmo} | Combined Total: {currentAmmo + totalAmmo}");

        if (currentAmmo <= 0)
        {
            Debug.Log("[AUTO-RELOAD] Active weapon empty! Automatically drawing bow from new pickup items.");
            OnReloadManual();
        }
        else
        {
            CheckWeaponAndUIState();
            // Updates UI instantly
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateAmmoUI();
            }
        }
    }

    private void ZoomIn()
    {
        if (cam == null) return;
        float targetFOV = isZooming ? zoomCamFOV : defaultCamFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 10f);
    }

    public int GetCurrentAmmo() { return currentAmmo; }
    public int GetTotalAmmo() { return totalAmmo; }
    public void SetCurrentAmmo(int amount) { currentAmmo = amount; CheckWeaponAndUIState(); }
    public void SetTotalAmmo(int amount) { totalAmmo = amount; CheckWeaponAndUIState(); }

    public IEnumerator PunchObject()
    {
        isPunching = true;
        if (punchHitBox != null) punchHitBox.SetActive(true);

        Vector3 boxCenter = punchHitBox != null ? punchHitBox.transform.position : transform.position;
        Vector3 boxHalfExtents = Vector3.one * 0.5f;

        if (punchHitBox != null)
        {
            BoxCollider boxCol = punchHitBox.GetComponent<BoxCollider>();
            if (boxCol != null)
            {
                boxHalfExtents = Vector3.Scale(boxCol.size, punchHitBox.transform.lossyScale) * 0.5f;
                boxCenter = boxCol.bounds.center;
            }
        }

        Quaternion boxRotation = punchHitBox != null ? punchHitBox.transform.rotation : transform.rotation;
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, boxHalfExtents, boxRotation);

        foreach (Collider other in hitColliders)
        {
            if (other.transform.root == transform.root) continue;

            if (other.CompareTag("Enemy") || other.CompareTag("NPC") || other.CompareTag("Crate"))
            {
                HealthHandler targetHealth = other.GetComponent<HealthHandler>();
                if (targetHealth != null)
                {
                    targetHealth.DamageHandler(punchDamage);

                    // Section B hit audio 
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.Play("hit");
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.4f);
        if (punchHitBox != null) punchHitBox.SetActive(false);
        isPunching = false;
    }
}