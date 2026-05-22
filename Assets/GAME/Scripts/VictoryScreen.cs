using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ── VictoryScreen ───────────────────────────────────────────
public class VictoryScreen : MonoBehaviour
{
    [Header("Estadísticas")]
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI artifactsText;
    public TextMeshProUGUI livesRemainingText;
    public TextMeshProUGUI failedAttemptsText;
    public TextMeshProUGUI scene1TimeText;
    public TextMeshProUGUI scene2TimeText;

    [Header("Botones")]
    public Button playAgainButton;
    public Button exitButton;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);
        if (exitButton != null) exitButton.onClick.AddListener(() => Application.Quit());
        LoadStats();
    }

    void LoadStats()
    {
        GameData data = GameManager.Instance?.Load();
        if (data == null) return;

        if (totalTimeText != null) { int m = (int)(data.sessionStats.totalPlayTime / 60f); int s = (int)(data.sessionStats.totalPlayTime % 60f); totalTimeText.text = $"Tiempo total: {m}:{s:D2}"; }
        if (artifactsText != null) artifactsText.text = $"Artefactos: {data.scene1.artifactsCollected}/{data.scene1.totalArtifacts}";
        if (livesRemainingText != null) livesRemainingText.text = $"Vidas restantes: {data.scene1.livesRemaining}";
        if (failedAttemptsText != null) failedAttemptsText.text = $"Intentos fallidos: {data.scene2.failedAttempts}";
        if (scene1TimeText != null) { int m = (int)(data.scene1.completionTime / 60f); int s = (int)(data.scene1.completionTime % 60f); scene1TimeText.text = $"Tiempo Escena 1: {m}:{s:D2}"; }
        if (scene2TimeText != null) { int m = (int)(data.scene2.completionTime / 60f); int s = (int)(data.scene2.completionTime % 60f); scene2TimeText.text = $"Tiempo Escena 2: {m}:{s:D2}"; }
    }

    void PlayAgain()
    {
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        SceneManager.LoadScene("CamaraArtefactos");
    }
}