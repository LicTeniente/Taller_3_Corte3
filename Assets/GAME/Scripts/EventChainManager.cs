using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ── EventChainManagerP ────────
public class EventChainManager : MonoBehaviour
{
    public static EventChainManager Instance;

    // ── Eventos ──
    [Header("Evento 1 - Puerta de Piedra")]
    public Transform stoneDoor;
    public AudioClip doorCreakSound;
    public float doorOpenDistance = 4f;

    [Header("Evento 2 - Plataforma Elevadora")]
    public Transform elevator;
    public float elevatorRiseDistance = 3f;

    [Header("Evento 3 - Iluminación Mágica")]
    public Light[] magicLights;
    public float lightFadeDuration = 1.5f;
    public float targetLightIntensity = 3f;

    [Header("Evento 4 - Mecanismo Giratorio")]
    public Transform gear;
    public AudioClip gearSound;

    // ── Portal Escena 1 ──
    [Header("Portal Escena 1 → 2")]
    public GameObject portal1Mesh;
    public ParticleSystem portal1Particles;
    public Light portal1Light;
    public AudioClip portal1Sound;
    public string scene2Name = "CamaraActivacion";

    // ── Portal Escena 2 ──
    [Header("Portal Escena 2 → Victoria")]
    public GameObject portal2Mesh;
    public ParticleSystem portal2Particles;
    public Light portal2Light;
    public AudioClip portal2EpicSound;
    public AudioClip portal2WhooshSound;
    public string victorySceneName = "Victoria";

    [Header("Transición")]
    public CanvasGroup fadeCanvasGroup;

    AudioSource audioSource;
    bool portal1Active, portal2Active, transitioning;

    void Awake() => Instance = this;

    void Start()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        foreach (var l in magicLights) { l.enabled = true; l.intensity = 0f; }
        SetPortalVisible(portal1Mesh, portal1Particles, portal1Light, false);
        SetPortalVisible(portal2Mesh, portal2Particles, portal2Light, false);
    }

    // ── Activar Portal 1 (llamado desde GameManager) ──
    public void ActivateP1() { portal1Active = true; SetPortalVisible(portal1Mesh, portal1Particles, portal1Light, true); }

    // ── Activar Portal 2 ──
    public void TriggerFinalPortal() => StartCoroutine(Event5_FinalPortal());

    // ── Trigger de eventos por índice ──
    public void TriggerEvent(int index)
    {
        switch (index)
        {
            case 0: StartCoroutine(Event1_OpenDoor()); break;
            case 1: StartCoroutine(Event2_RaisePlatform()); break;
            case 2: StartCoroutine(Event3_LightsOn()); break;
            case 3: StartCoroutine(Event4_SpinGear()); break;
        }
    }

    // ── Detección de colisión con portales ──
    void OnTriggerEnter(Collider other)
    {
        if (transitioning || !other.CompareTag("Player")) return;

        // Portal 1
        if (portal1Active && portal1Mesh != null && IsNear(portal1Mesh.transform, other.transform, 2f))
        {
            if (GameManager.Instance == null || !GameManager.Instance.CanUsePortal1()) return;
            transitioning = true;
            StartCoroutine(GoToScene(scene2Name, portal1Sound, 2f));
        }
        // Portal 2
        else if (portal2Active && portal2Mesh != null && IsNear(portal2Mesh.transform, other.transform, 2f))
        {
            transitioning = true;
            StartCoroutine(GoToScene(victorySceneName, portal2WhooshSound, 1.5f));
        }
    }

    bool IsNear(Transform a, Transform b, float range) => Vector3.Distance(a.position, b.position) <= range;

    IEnumerator GoToScene(string sceneName, AudioClip sound, float fadeDuration)
    {
        if (sound != null) audioSource.PlayOneShot(sound);
        GameManager.Instance?.Save();

        if (fadeCanvasGroup != null)
        {
            float t = 0f;
            while (t < fadeDuration) { t += Time.deltaTime; fadeCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration); yield return null; }
        }
        else yield return new WaitForSeconds(fadeDuration);

        SceneManager.LoadScene(sceneName);
    }

    // ── Eventos ──
    IEnumerator Event1_OpenDoor()
    {
        if (stoneDoor == null) yield break;
        if (doorCreakSound != null) audioSource.PlayOneShot(doorCreakSound);
        yield return MoveOverTime(stoneDoor, stoneDoor.position + Vector3.up * doorOpenDistance, 2f);
    }

    IEnumerator Event2_RaisePlatform()
    {
        if (elevator == null) yield break;
        yield return MoveOverTime(elevator, elevator.position + Vector3.up * elevatorRiseDistance, 2f);
    }

    IEnumerator Event3_LightsOn()
    {
        float t = 0f;
        while (t < lightFadeDuration)
        {
            t += Time.deltaTime;
            float intensity = Mathf.Lerp(0f, targetLightIntensity, t / lightFadeDuration);
            foreach (var l in magicLights) l.intensity = intensity;
            yield return null;
        }
    }

    IEnumerator Event4_SpinGear()
    {
        if (gear == null) yield break;
        if (gearSound != null) audioSource.PlayOneShot(gearSound);
        float t = 0f;
        while (t < 2f) { t += Time.deltaTime; gear.Rotate(Vector3.up, 180f * Time.deltaTime); yield return null; }
    }

    IEnumerator Event5_FinalPortal()
    {
        yield return new WaitForSeconds(0.5f);
        portal2Active = true;
        SetPortalVisible(portal2Mesh, portal2Particles, portal2Light, true);
        if (portal2EpicSound != null) audioSource.PlayOneShot(portal2EpicSound);
    }

    IEnumerator MoveOverTime(Transform obj, Vector3 target, float duration)
    {
        Vector3 start = obj.position; float t = 0f;
        while (t < duration) { t += Time.deltaTime; obj.position = Vector3.Lerp(start, target, t / duration); yield return null; }
        obj.position = target;
    }

    void SetPortalVisible(GameObject mesh, ParticleSystem ps, Light light, bool visible)
    {
        if (mesh != null) mesh.SetActive(visible);
        if (light != null) light.enabled = visible;
        if (ps != null) { if (visible) ps.Play(); else ps.Stop(); }
    }
}