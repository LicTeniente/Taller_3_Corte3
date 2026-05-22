using UnityEngine;

public class ClickTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            Debug.Log("Click detectado en: " + Input.mousePosition);
    }
}