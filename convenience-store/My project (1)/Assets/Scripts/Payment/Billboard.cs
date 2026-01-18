/*
 * Skrypt Billboard
 * ----------------
 * Prosty komponent sprawiający, że obiekt UI (np. tekst nad głową NPC)
 * zawsze jest obrócony przodem do kamery gracza.
 */
using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
    }
}
