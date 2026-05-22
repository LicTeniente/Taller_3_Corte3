using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable] public class Scene1Data { public int artifactsCollected, totalArtifacts, livesRemaining, livesLost, deathCount; public bool completed; public float completionTime; }
[Serializable] public class Scene2Data { public int objectsPlacedCorrectly, totalObjectsToPlace, failedAttempts; public string[] eventsTriggered; public bool portalUnlocked, completed; public float completionTime; }
[Serializable] public class SessionStats { public float totalPlayTime; public int totalAttempts, gamesCompleted; }
[Serializable] public class GameData { public string playerName, lastScenePlayed, timestamp; public Scene1Data scene1; public Scene2Data scene2; public SessionStats sessionStats; }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Escena 1")]
    public int artifactsCollected = 0;
    public int totalArtifacts = 10;
    public int artifactsRequired = 6;
    public int lives = 3;
    public int livesLost = 0;
    public int deathCount = 0;

    [Header("Escena 2")]
    public int mechanismsActivated = 0;
    public int totalMechanisms = 5;
    public int failedAttempts = 0;

    [HideInInspector] public float scene1StartTime, scene2StartTime, scene1CompletionTime;

    string filePath;
    GameData currentData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            scene1StartTime = Time.time;
            filePath = Path.Combine(Application.persistentDataPath, "GameData.json");
        }
        else Destroy(gameObject);
    }

    public void CollectArtifact()
    {
        artifactsCollected++;
        var hud = FindFirstObjectByType<HUDManager>();
        if (hud != null) hud.Refresh();
        if (CanUsePortal1()) EventChainManager.Instance?.ActivateP1();
    }

    public void TakeDamage()
    {
        lives--; livesLost++; deathCount++;
        var hud = FindFirstObjectByType<HUDManager>();
        if (hud != null) hud.Refresh();
        if (lives <= 0) HUDManager.Instance?.TriggerGameOver();
    }

    public bool CanUsePortal1() => artifactsCollected >= artifactsRequired && lives >= 1;

    public void RegisterMechanism(string eventName)
    {
        mechanismsActivated++;
        Debug.Log("Mecanismo registrado: " + mechanismsActivated + "/" + totalMechanisms);
        var hud = FindFirstObjectByType<HUDManager>();
        if (hud != null) hud.Refresh();
        if (CanUsePortal2()) EventChainManager.Instance?.TriggerFinalPortal();
    }
    public void RegisterFailedAttempt()
    {
        failedAttempts++;
        var hud = FindFirstObjectByType<HUDManager>();
        if (hud != null) hud.Refresh();
    }

    public bool CanUsePortal2() => mechanismsActivated >= totalMechanisms;

    public float GetScene1Elapsed() => Time.time - scene1StartTime;
    public float GetScene2Elapsed() => Time.time - scene2StartTime;
    public void OnScene2Start() { scene1CompletionTime = GetScene1Elapsed(); scene2StartTime = Time.time; }

    public void Save()
    {
        if (currentData == null) currentData = new GameData();
        currentData.playerName = "Arqueólogo";
        currentData.lastScenePlayed = SceneManager.GetActiveScene().name;
        currentData.timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        currentData.scene1 = new Scene1Data
        {
            artifactsCollected = artifactsCollected,
            totalArtifacts = totalArtifacts,
            livesRemaining = lives,
            livesLost = livesLost,
            deathCount = deathCount,
            completed = CanUsePortal1(),
            completionTime = scene1CompletionTime > 0 ? scene1CompletionTime : GetScene1Elapsed()
        };

        string[] eventNames = { "door_open", "platform_activate", "light_on", "gear_spin", "portal_unlocked" };
        string[] triggered = new string[Mathf.Min(mechanismsActivated, eventNames.Length)];
        for (int i = 0; i < triggered.Length; i++) triggered[i] = eventNames[i];

        currentData.scene2 = new Scene2Data
        {
            objectsPlacedCorrectly = mechanismsActivated,
            totalObjectsToPlace = totalMechanisms,
            failedAttempts = failedAttempts,
            eventsTriggered = triggered,
            portalUnlocked = CanUsePortal2(),
            completed = CanUsePortal2(),
            completionTime = GetScene2Elapsed()
        };

        float total = scene1CompletionTime + GetScene2Elapsed();
        currentData.sessionStats = new SessionStats
        {
            totalPlayTime = total,
            totalAttempts = failedAttempts + mechanismsActivated,
            gamesCompleted = CanUsePortal2() ? 1 : 0
        };

        File.WriteAllText(filePath, JsonUtility.ToJson(currentData, true));
    }

    public GameData Load()
    {
        if (!File.Exists(filePath)) return null;
        currentData = JsonUtility.FromJson<GameData>(File.ReadAllText(filePath));
        return currentData;
    }
}