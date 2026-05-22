using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EventChainManager : MonoBehaviour
{
    public static EventChainManager Instance;

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

    [Header("Portal Escena 1 → 2")]
    public GameObject portal1Mesh;
    public ParticleSystem portal1Particles;
    public Light portal1Light;
    public AudioClip portal1Sound;
    public string scene2Name = "CamaraActivacion";

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

    public void ActivateP1()
    {
        portal1Active = true;
        SetPortalVisible(portal1Mesh, portal1Particles, portal1Light, true);
    }

    public void TriggerFinalPortal() => StartCoroutine(Event5_FinalPortal());

    public void TriggerEvent(int index)
    {
        switch (index)
        {
            case -1: break;
            case 0: StartCoroutine(Event1_OpenDoor()); break;
            case 1: StartCoroutine(Event2_RaisePlatform()); break;
            case 2: StartCoroutine(Event3_LightsOn()); break;
            case 3: StartCoroutine(Event4_SpinGear()); break;
        }
    }

    // ── Portales ──
    public void TryPortal1()
    {
        if (transitioning || !portal1Active) return;
        if (GameManager.Instance == null || !GameManager.Instance.CanUsePortal1()) return;
        transitioning = true;
        StartCoroutine(GoToScene(scene2Name, portal1Sound, 2f));
    }

    public void TryPortal2()
    {
        Debug.Log("TryPortal2 llamado - portal2Active: " + portal2Active);
        if (transitioning || !portal2Active) return;
        transitioning = true;
        StartCoroutine(GoToScene(victorySceneName, portal2WhooshSound, 1.5f));
    }

    IEnumerator GoToScene(string sceneName, AudioClip sound, float fadeDuration)
    {
        if (sound != null) audioSource.PlayOneShot(sound);
        GameManager.Instance?.Save();

        if (fadeCanvasGroup != null)
        {
            // Cambia a blanco para efecto ceguera
            var img = fadeCanvasGroup.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = Color.white;
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
        Vector3 bottomPos = elevator.position;
        Vector3 topPos = elevator.position + Vector3.up * elevatorRiseDistance;

        while (true)
        {
            yield return MoveOverTime(elevator, topPos, 2f);
            yield return new WaitForSeconds(1f);
            yield return MoveOverTime(elevator, bottomPos, 2f);
            yield return new WaitForSeconds(1f);
        }
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

        Quaternion startRot = gear.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 90f, 0f);
        float t = 0f;

        while (t < 2f)
        {
            t += Time.deltaTime;
            gear.rotation = Quaternion.Lerp(startRot, endRot, t / 2f);
            yield return null;
        }
        gear.rotation = endRot;
    }

    IEnumerator Event5_FinalPortal()
    {
        yield return new WaitForSeconds(0.5f);
        portal2Active = true;
        Debug.Log("Portal 2 activado - mesh: " + (portal2Mesh != null));
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