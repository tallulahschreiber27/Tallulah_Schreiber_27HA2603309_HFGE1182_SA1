using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions InputAction;

    [Header("ANIMATION PARAMETERS")]
    private Animator anim;
    private ShootHandler shootHandler;

    [Header("MOVEMENT PARAMETERS")]
    public float walkSpeed;
    public float sprintSpeed;
    public float jumpForce;
    public Rigidbody rb;
    public bool isSprinting;
    [Range(1f, 3f)]
    public float joystickCurve = 1.5f;
    private Vector2 moveDirection;

    [Header("CAMERA PARAMETERS")]
    [SerializeField] private Camera playerCamera;
    public float mouseSensitivity = 20f;
    public float touchSensitivity = 0.25f;
    [Range(1f, 30f)]
    public float lookSmoothness = 18f;
    public float lookLimitX = 80f;
    private float cameraAngle = 0f;
    private Vector2 lookInput;
    private Vector2 currentLookDelta;

    [Header("CROUCHING")]
    public CapsuleCollider hitbox;
    [SerializeField] private bool isCrouching;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float cameraHeight = 1.5f;

    [Header("GROUND DETECTION")]
    [SerializeField] private bool isGrounded = true;
    public float groundCheckDistance;
    public LayerMask groundLayer;

    [Header("MELEE / PUNCH PARAMETERS")]
    public float punchRange = 2.5f;
    public float punchDamage = 25f;
    public float punchCooldown = 0.6f;
    public LayerMask enemyLayer;
    private float nextPunchTime = 0f;

    [Header("AUDIO FEEDBACK")]
    [Tooltip("Time between footstep audio clips while walking/running.")]
    [SerializeField] private float stepRate = 0.5f;
    private float stepTimer;

    private Pickup currentPickupInRange = null;

    #region BostonCity Campus
    /* This code is property of Boston City Campus, and should not be used, or copied for any other subject.
     If you would copy and or use this in an assessment outside the assessment that this was supplied for
     action will be taken against you for plagiarism.
    */
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        shootHandler = GetComponent<ShootHandler>();

        InputAction = new PlayerInputActions();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        InputAction.Player.Enable();

        InputAction.Player.Move.performed += OnMovePerformed;
        InputAction.Player.Move.canceled += OnMoveCanceled;

        InputAction.Player.Look.performed += OnLookPerformed;
        InputAction.Player.Look.canceled += OnLookCanceled;

        InputAction.Player.Jump.performed += OnJump;
        InputAction.Player.Crouch.performed += OnCrouch;
        InputAction.Player.Sprint.performed += OnSprint;
        InputAction.Player.Sprint.canceled += OnSprint;
    }

    private void OnDisable()
    {
        InputAction.Player.Move.performed -= OnMovePerformed;
        InputAction.Player.Move.canceled -= OnMoveCanceled;

        InputAction.Player.Look.performed -= OnLookPerformed;
        InputAction.Player.Look.canceled -= OnLookCanceled;

        InputAction.Player.Jump.performed -= OnJump;
        InputAction.Player.Crouch.performed -= OnCrouch;
        InputAction.Player.Sprint.performed -= OnSprint;
        InputAction.Player.Sprint.canceled -= OnSprint;

        InputAction.Player.Disable();
        EnhancedTouchSupport.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext context) => moveDirection = context.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext context) => moveDirection = Vector2.zero;
    private void OnLookPerformed(InputAction.CallbackContext context) => lookInput = context.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext context) => lookInput = Vector2.zero;

    void Start()
    {
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (shootHandler == null)
        {
            Debug.LogError($"<b>[{gameObject.name}]</b> PlayerController failed to find the ShootHandler script on this GameObject!");
        }
    }

    void Update()
    {
        LookHandler();

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[MANUAL INPUT] Physical 'E' key pressed down!");
            ExecuteManualInteraction();
        }

        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        GroundDetection();
        MoveHandler();
    }

    
    private void UpdateAnimatorParameters()
    {
        if (anim == null) return;

        float flatSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        anim.SetFloat("Speed", flatSpeed);

        float crouchWeight = isCrouching ? 1f : 0f;
        anim.SetFloat("isCrouching", crouchWeight);

        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);

        if (shootHandler != null)
        {
            float ammoWeight = (shootHandler.GetCurrentAmmo() > 0) ? 1f : 0f;
            anim.SetFloat("hasAmmo", ammoWeight);
        }
    }

    public void AddAmmunition(int amount)
    {
        if (shootHandler != null)
        {
            shootHandler.AddAmmo(amount);
        }
    }

    public void ExecuteManualInteraction()
    {
        if (currentPickupInRange != null)
        {
            Debug.Log("[DEBUG 2] Forwarding collect command to pickup: " + currentPickupInRange.gameObject.name);
            currentPickupInRange.Collect();
        }
        else
        {
            Debug.LogWarning("[DEBUG 3] 'E' pressed, but no pickup is currently detected near the player.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Pickup pickup = other.GetComponent<Pickup>();
        if (pickup != null)
        {
            currentPickupInRange = pickup;
            Debug.Log("[DEBUG 4] Player entered trigger of pickup: " + other.gameObject.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Pickup pickup = other.GetComponent<Pickup>();
        if (pickup != null && currentPickupInRange == pickup)
        {
            currentPickupInRange = null;
            Debug.Log("[DEBUG 5] Player exited trigger of pickup: " + other.gameObject.name);
        }
    }

    public void MoveHandler()
    {
        float inputMagnitude = Mathf.Clamp01(moveDirection.magnitude);
        float curvedMagnitude = Mathf.Pow(inputMagnitude, joystickCurve);
        Vector2 smoothInput = inputMagnitude > 0.001f ? (moveDirection.normalized * curvedMagnitude) : Vector2.zero;

        Vector3 moveDir = transform.right * smoothInput.x + transform.forward * smoothInput.y;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 targetVelocity = moveDir * currentSpeed;

        currentVelocity.x = targetVelocity.x;
        currentVelocity.z = targetVelocity.z;

        rb.linearVelocity = currentVelocity;

        if (isGrounded && smoothInput.magnitude > 0.1f)
        {
            float currentRate = isSprinting ? (stepRate * 0.6f) : stepRate;

            stepTimer += Time.deltaTime;
            if (stepTimer >= currentRate)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.Play("Walk");
                }
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = !isSprinting;
    }

    public void LookHandler()
    {
        Vector2 targetLookDelta = Vector2.zero;

        if (Touchscreen.current != null && Touch.activeTouches.Count > 0)
        {
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.startScreenPosition.x < Screen.width * 0.5f)
                    continue;

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId))
                    continue;

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    float dpiScale = Screen.dpi > 0 ? (Screen.dpi / 160f) : 1f;
                    targetLookDelta = (touch.delta / dpiScale) * touchSensitivity;
                    break;
                }
            }
        }
        else if (!Application.isMobilePlatform)
        {
            targetLookDelta = lookInput * (mouseSensitivity * 0.02f);
        }

        currentLookDelta = Vector2.Lerp(currentLookDelta, targetLookDelta, Time.deltaTime * lookSmoothness);

        if (currentLookDelta.sqrMagnitude < 0.0001f)
            return;

        cameraAngle -= currentLookDelta.y;
        cameraAngle = Mathf.Clamp(cameraAngle, -lookLimitX, lookLimitX);

        playerCamera.transform.localRotation = Quaternion.Euler(cameraAngle, 0f, 0f);
        transform.Rotate(Vector3.up * currentLookDelta.x);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            if (anim != null) anim.SetTrigger("JumpTrigger");

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.Play("Jump");
            }
        }
    }

    public void Crouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        hitbox.height = Mathf.Lerp(hitbox.height, targetHeight, 0.2f);
        Vector3 camLocalPos = playerCamera.transform.localPosition;
        camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetHeight - 0.25f, Time.deltaTime * 5f);
        hitbox.height = targetHeight;
        playerCamera.transform.localPosition = camLocalPos;
    }
    public void OnCrouch(InputAction.CallbackContext context)
    {
        isCrouching = !isCrouching;
        Crouch();
    }
    public void GroundDetection()
    {
        float currentScaleY = transform.localScale.y;
        Vector3 start = transform.position + (Vector3.up * 0.1f * currentScaleY);
        float dynamicCheckDistance = (groundCheckDistance + 0.1f) * currentScaleY;
        isGrounded = Physics.Raycast(start, Vector3.down, dynamicCheckDistance, groundLayer);
    }
    //mobile UI buttons
    public void MobileJump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            if (anim != null) anim.SetTrigger("JumpTrigger");
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.Play("Jump");
            }
        }
    }
    public void MobileCrouch()
    {
        isCrouching = !isCrouching;
        Crouch();
    }
    public void MobileSprint()
    {
        isSprinting = !isSprinting;
    }
    public void MobileInteract()
    {
        ExecuteManualInteraction();
    }
    public void MobilePunch()
    {
        if (Time.time < nextPunchTime) return;
        nextPunchTime = Time.time + punchCooldown;
        if (anim != null)
        {
            anim.SetTrigger("PunchTrigger");
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.Play("Punch");
        }
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, punchRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.root == transform.root) return;
            if (hit.collider.CompareTag("Crate"))
            {
                Debug.Log($"[PUNCH] Smashed crate: {hit.collider.name}!");
                hit.collider.SendMessageUpwards("TakeDamage", punchDamage, SendMessageOptions.DontRequireReceiver);
                hit.collider.SendMessageUpwards("Break", SendMessageOptions.DontRequireReceiver);
                hit.collider.SendMessageUpwards("DestroyCrate", SendMessageOptions.DontRequireReceiver);
            }
            else if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
            {
                Debug.Log($"[PUNCH] Hit enemy {hit.collider.name} for {punchDamage} damage!");
                hit.collider.SendMessageUpwards("TakeDamage", punchDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}