using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    public int portalIndex; // 1 = portal escena 2, 2 = portal victoria

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (portalIndex == 1) EventChainManager.Instance?.TryPortal1();
        else if (portalIndex == 2) EventChainManager.Instance?.TryPortal2();
    }
}