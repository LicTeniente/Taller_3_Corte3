using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ── HUDManager ────────────────
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("Escena 1 - Artefactos")]
    public TextMeshProUGUI artifactCountText;
    public Slider artifactProgressBar;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI pickupPopupText;
    public GameObject gameOverPanel;
    public Button retryButton;

    [Header("Escena 2 - Mecanismos")]
    public TextMeshProUGUI mechanismsText;
    public Slider mechanismsBar;
    public TextMeshProUGUI failedAttemptsText;
    public TextMeshProUGUI timerText;

    [Header("Transición Fade")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1.5f;

    void Awake() => Instance = this;

    void Start()
    {
        if (pickupPopupText != null) pickupPopupText.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (retryButton != null) retryButton.onClick.AddListener(RetryScene);

        // Fade in al entrar a la escena
        if (fadePanel != null) StartCoroutine(FadeIn());

        // Notificar inicio de escena 2
        if (SceneManager.GetActiveScene().name == "CamaraDeActivacion")
            GameManager.Instance?.OnScene2Start();

        Refresh();
    }

    void Update() => UpdateTimer();

    // ── Refresh general (escena 1 y 2) ──
    public void Refresh()
    {
        if (GameManager.Instance == null) return;
        var gm = GameManager.Instance;

        // Escena 1
        if (artifactCountText != null) artifactCountText.text = $"Artefactos: {gm.artifactsCollected}/{gm.totalArtifacts}";
        if (artifactProgressBar != null) artifactProgressBar.value = (float)gm.artifactsCollected / gm.totalArtifacts;
        if (livesText != null)
        {
            string hearts = "";
            for (int i = 0; i < gm.lives; i++) hearts += "♥ ";
            livesText.text = hearts.TrimEnd();
        }
        int remaining = Mathf.Max(0, gm.artifactsRequired - gm.artifactsCollected);
        if (statusText != null)
            statusText.text = remaining > 0 ? $"Recolecta {remaining} artefacto(s) más para continuar" : "¡Portal activo! Búscalo al noreste.";

        // Escena 2
        if (mechanismsText != null) mechanismsText.text = $"Mecanismos activados: {gm.mechanismsActivated}/{gm.totalMechanisms}";
        if (mechanismsBar != null) mechanismsBar.value = (float)gm.mechanismsActivated / gm.totalMechanisms;
        if (failedAttemptsText != null) failedAttemptsText.text = $"Intentos fallidos: {gm.failedAttempts}";
    }

    void UpdateTimer()
    {
        if (timerText == null || GameManager.Instance == null) return;
        float e = GameManager.Instance.GetScene2Elapsed();
        timerText.text = $"Tiempo: {(int)(e / 60f)}:{(int)(e % 60f):D2}";
    }

    public void ShowPickupFeedback()
    {
        StopCoroutine("ShowPopup");
        StartCoroutine("ShowPopup");
    }

    IEnumerator ShowPopup()
    {
        if (pickupPopupText == null) yield break;
        pickupPopupText.text = "[+] Artefacto recolectado";
        pickupPopupText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        pickupPopupText.gameObject.SetActive(false);
    }

    public void TriggerGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void RetryScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator FadeIn()
    {
        fadePanel.alpha = 1f;
        float t = 0f;
        while (t < fadeDuration) { t += Time.deltaTime; fadePanel.alpha = 1f - Mathf.Clamp01(t / fadeDuration); yield return null; }
        fadePanel.alpha = 0f;
    }

    public IEnumerator FadeOut(float duration)
    {
        float t = 0f;
        while (t < duration) { t += Time.deltaTime; fadePanel.alpha = Mathf.Clamp01(t / duration); yield return null; }
    }
}