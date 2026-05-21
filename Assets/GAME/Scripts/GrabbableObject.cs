using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class GrabbableObject : MonoBehaviour
{
    [Header("Tipo (debe coincidir con el socket)")]
    public string objectType;

    Rigidbody rb;

    void Start() => rb = GetComponent<Rigidbody>();

    public void SetHeld(bool held)
    {
        rb.isKinematic = held;
        rb.useGravity = !held;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = !held;
    }

    public void BounceBack(Vector3 playerPos)
    {
        SetHeld(false);
        Vector3 dir = (transform.position - playerPos).normalized;
        dir.y = 0.5f;
        rb.AddForce(dir * 4f, ForceMode.Impulse);
    }
}