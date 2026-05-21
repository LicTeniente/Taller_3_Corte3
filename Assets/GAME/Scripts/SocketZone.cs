using System.Collections;
using UnityEngine;

// ────────────────────────────────────────────────────────────
// SocketZone  (zona de colocación + detección de proximidad)
// ────────────────────────────────────────────────────────────
public class SocketZone : MonoBehaviour
{
    [Header("Configuración")]
    public string acceptedType;
    public int eventIndex;

    [Header("Efectos visuales")]
    public Renderer auraRenderer;
    public Material readyMaterial;
    public Material correctMaterial;
    public Material wrongMaterial;
    public Light socketLight;

    [Header("Audio")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip readySound;

    bool filled;
    AudioSource audioSource;
    Material defaultMaterial;

    void Start()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        if (socketLight != null) socketLight.enabled = false;
        if (auraRenderer != null) defaultMaterial = auraRenderer.material;
    }

    // Llamado desde PlayerController cada frame
    void Update()
    {
        if (PlayerController.Instance == null) return;
        GrabbableObject held = PlayerController.Instance.HeldObject;
        float dist = Vector3.Distance(PlayerController.Instance.transform.position, transform.position);
        if (dist <= 2.5f && held != null && Accepts(held)) ShowReady();
        else HideReady();
    }

    public bool Accepts(GrabbableObject obj) => !filled && obj != null && obj.objectType == acceptedType;

    public void PlaceObject(GrabbableObject obj)
    {
        filled = true;
        obj.SetHeld(false);
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider c = obj.GetComponent<Collider>();
        if (c != null) c.enabled = false;
        obj.transform.position = transform.position + Vector3.up * 0.1f;
        obj.transform.rotation = Quaternion.identity;

        if (correctSound != null) audioSource.PlayOneShot(correctSound);
        if (socketLight != null) socketLight.enabled = true;
        if (auraRenderer != null && correctMaterial != null) auraRenderer.material = correctMaterial;

        GameManager.Instance?.RegisterMechanism(acceptedType);
        EventChainManager.Instance?.TriggerEvent(eventIndex);
        StartCoroutine(AnimatePlace(obj.transform));
    }

    public void RejectObject()
    {
        if (wrongSound != null) audioSource.PlayOneShot(wrongSound);
        StartCoroutine(FlashWrong());
    }

    public void ShowReady()
    {
        if (filled) return;
        if (auraRenderer != null && readyMaterial != null) auraRenderer.material = readyMaterial;
        if (readySound != null && !audioSource.isPlaying) audioSource.PlayOneShot(readySound);
    }

    public void HideReady()
    {
        if (filled || auraRenderer == null || defaultMaterial == null) return;
        auraRenderer.material = defaultMaterial;
    }

    IEnumerator AnimatePlace(Transform obj)
    {
        if (obj == null) yield break;
        Vector3 start = obj.position, end = transform.position + Vector3.up * 0.1f;
        float t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; if (obj != null) obj.position = Vector3.Lerp(start, end, t / 0.3f); yield return null; }
    }

    IEnumerator FlashWrong()
    {
        if (auraRenderer == null || wrongMaterial == null) yield break;
        Material prev = auraRenderer.material;
        auraRenderer.material = wrongMaterial;
        yield return new WaitForSeconds(0.5f);
        if (auraRenderer != null) auraRenderer.material = filled ? correctMaterial : prev;
    }
}