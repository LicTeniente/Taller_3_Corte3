using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// ── PlayerController ──────────
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Movimiento
    [Header("Movimiento")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float gravity = -20f;

    // Cámara
    [Header("Cámara")]
    public Transform cameraHolder;
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;

    // Respawn / daño
    [Header("Vida y Respawn")]
    public Transform spawnPoint;
    public Image damageOverlay;
    public AudioClip damageSound;
    public float shakeIntensity = 0.15f;
    public float shakeDuration = 0.3f;

    // Agarre de objetos
    [Header("Agarre")]
    public float grabRange = 3f;
    public Vector3 holdOffset = new Vector3(0f, 0.8f, 1.5f);
    public TextMeshProUGUI wrongZoneMessage;
    public TextMeshProUGUI holdingText;

    public static PlayerController Instance;
    public GrabbableObject HeldObject => heldObject;

    CharacterController controller;
    Animator animator;
    AudioSource audioSource;
    float verticalVelocity, verticalLook;
    bool invincible;
    Vector3 camOriginalPos;
    GrabbableObject heldObject;

    // Input System
    Vector2 moveInput;
    Vector2 lookInput;
    bool isSprinting;

    void Awake() => Instance = this;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        if (cameraHolder != null) camOriginalPos = cameraHolder.localPosition;
        if (damageOverlay != null) damageOverlay.color = new Color(1, 0, 0, 0);
        if (wrongZoneMessage != null) wrongZoneMessage.gameObject.SetActive(false);
        if (holdingText != null) holdingText.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ── Callbacks del Input System ──
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnSprint(InputValue value) => isSprinting = value.isPressed;
    public void OnFire(InputValue value) { if (value.isPressed) HandleGrab(); }

    void Update()
    {
        Move();
        RotateCamera();
        UpdateSocketAuras();
    }

    void LateUpdate()
    {
        if (heldObject != null)
            heldObject.transform.position = transform.TransformPoint(holdOffset);
    }

    // ── Movimiento ──
    void Move()
    {
        float h = moveInput.x;
        float v = moveInput.y;
        float speed = isSprinting ? runSpeed : walkSpeed;
        Vector3 dir = (transform.right * h + transform.forward * v).normalized;

        verticalVelocity = controller.isGrounded ? -2f : verticalVelocity + gravity * Time.deltaTime;
        Vector3 move = dir * speed;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        float mag = moveInput.magnitude;
        if (animator != null)
        {
            animator.SetFloat("Speed", mag * speed);
            animator.SetBool("IsRunning", isSprinting && mag > 0.1f);
            animator.SetBool("IsGrounded", controller.isGrounded);
        }
    }

    // ── Cámara ──
    void RotateCamera()
    {
        float mx = lookInput.x * mouseSensitivity;
        float my = lookInput.y * mouseSensitivity;
        transform.Rotate(Vector3.up * mx);
        verticalLook = Mathf.Clamp(verticalLook - my, minVerticalAngle, maxVerticalAngle);
        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(verticalLook, 0f, 0f);
    }

    // ── Agarre ──
    void HandleGrab()
    {
        if (heldObject == null) TryGrab(); else TryDrop();
    }

    void TryGrab()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, grabRange);
        GrabbableObject closest = null;
        float minDist = grabRange;
        foreach (var col in nearby)
        {
            var g = col.GetComponent<GrabbableObject>();
            if (g == null) continue;
            float d = Vector3.Distance(transform.position, g.transform.position);
            if (d < minDist) { minDist = d; closest = g; }
        }
        if (closest == null) return;
        heldObject = closest;
        heldObject.SetHeld(true);
        if (holdingText != null) { holdingText.text = $"Sosteniendo: {heldObject.objectType}"; holdingText.gameObject.SetActive(true); }
    }

    void TryDrop()
    {
        SocketZone socket = GetNearestSocket();
        if (socket != null && socket.Accepts(heldObject))
        {
            socket.PlaceObject(heldObject);
        }
        else
        {
            if (socket != null) socket.RejectObject();
            GameManager.Instance?.RegisterFailedAttempt();
            heldObject.BounceBack(transform.position);
            ShowWrongZone();
        }
        heldObject = null;
        if (holdingText != null) holdingText.gameObject.SetActive(false);
    }

    SocketZone GetNearestSocket()
    {
        SocketZone[] sockets = FindObjectsByType<SocketZone>(FindObjectsSortMode.None);
        SocketZone nearest = null;
        float minDist = 2.5f;
        foreach (var s in sockets)
        {
            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d < minDist) { minDist = d; nearest = s; }
        }
        return nearest;
    }

    void UpdateSocketAuras()
    {
        SocketZone[] sockets = FindObjectsByType<SocketZone>(FindObjectsSortMode.None);
        foreach (var s in sockets)
        {
            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d <= 2.5f && heldObject != null && s.Accepts(heldObject)) s.ShowReady();
            else s.HideReady();
        }
    }

    // ── Vida / Daño ──
    public void TakeDamage()
    {
        if (invincible) return;
        if (damageSound != null) audioSource.PlayOneShot(damageSound);
        GameManager.Instance?.TakeDamage();
        if (GameManager.Instance != null && GameManager.Instance.lives > 0)
        {
            StartCoroutine(DamageRoutine());
            Respawn();
        }
    }

    void Respawn()
    {
        controller.enabled = false;
        transform.position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        transform.rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        controller.enabled = true;
    }

    IEnumerator DamageRoutine()
    {
        invincible = true;
        if (damageOverlay != null) damageOverlay.color = new Color(1, 0, 0, 0.45f);
        StartCoroutine(ShakeCamera());
        yield return new WaitForSeconds(0.4f);
        if (damageOverlay != null)
        {
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                damageOverlay.color = new Color(1, 0, 0, Mathf.Lerp(0.45f, 0f, t / 0.3f));
                yield return null;
            }
            damageOverlay.color = new Color(1, 0, 0, 0);
        }
        yield return new WaitForSeconds(1.5f);
        invincible = false;
    }

    IEnumerator ShakeCamera()
    {
        float e = 0f;
        while (e < shakeDuration)
        {
            e += Time.deltaTime;
            if (cameraHolder != null)
                cameraHolder.localPosition = camOriginalPos + Random.insideUnitSphere * shakeIntensity;
            yield return null;
        }
        if (cameraHolder != null) cameraHolder.localPosition = camOriginalPos;
    }

    void ShowWrongZone()
    {
        if (wrongZoneMessage == null) return;
        StopCoroutine("HideWrongZone");
        StartCoroutine("HideWrongZone");
    }

    IEnumerator HideWrongZone()
    {
        wrongZoneMessage.text = "Zona incorrecta";
        wrongZoneMessage.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        wrongZoneMessage.gameObject.SetActive(false);
    }
}