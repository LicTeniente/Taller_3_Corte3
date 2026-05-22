using UnityEngine;

// ── DangerZone ──────────────────────────────────────────────
public class DangerZone : MonoBehaviour
{
    public enum ZoneType { Spikes, Arrow, Fire }
    public ZoneType zoneType = ZoneType.Spikes;
    public float fireDamageInterval = 2f;

    float fireTimer;
    bool playerInside;

    void Start() { Collider col = GetComponent<Collider>(); if (col != null) col.isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger: " + other.name + " tag: " + other.tag);
        if (!other.CompareTag("Player")) return;
        if (zoneType == ZoneType.Fire) { playerInside = true; fireTimer = 0f; }
        PlayerController.Instance?.TakeDamage();
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player") || zoneType != ZoneType.Fire || !playerInside) return;
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireDamageInterval) { fireTimer = 0f; PlayerController.Instance?.TakeDamage(); }
    }

    void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) playerInside = false; }
}