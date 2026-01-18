/*
 * System Kolejki do Kasy
 * ----------------------
 * Zarządza kolejką klientów (NPC) oczekujących na płatność przy kasie.
 * Wylicza pozycje, w których powinni stać klienci, oraz zarządza ich
 * dołączaniem i opuszczaniem kolejki.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckoutQueue : MonoBehaviour
{
    public static CheckoutQueue Instance;

    [Header("Configuration")]
    public Transform queueStartPoint; // Pozycja kasjera
    [Tooltip("Point defining the direction of the queue line (Where the line goes towards).")]
    // Punkt definiujący kierunek kolejki (w którą stronę idzie).
    public Transform queueDirectionPoint; 
    [Tooltip("Distance between people in line")]
    // Odległość między osobami w kolejce
    public float spacing = 1.5f;

    private List<NPCBuyer> queue = new List<NPCBuyer>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Dodaje NPC na koniec kolejki.
    /// Zwraca pozycję docelową, gdzie NPC powinien stanąć.
    /// </summary>
    public Vector3 JoinQueue(NPCBuyer buyer)
    {
        if (!queue.Contains(buyer))
        {
            queue.Add(buyer);
        }
        return GetPositionInQueue(buyer);
    }

    /// <summary>
    /// Usuwa NPC z kolejki (np. po zapłaceniu).
    /// </summary>
    public void LeaveQueue(NPCBuyer buyer)
    {
        if (queue.Contains(buyer))
        {
            queue.Remove(buyer);
            UpdateQueuePositions();
        }
    }

    /// <summary>
    /// Zwraca pierwszego klienta w kolejce (tego przy kasie).
    /// </summary>
    public NPCBuyer GetFirstCustomer()
    {
        if (queue.Count > 0) return queue[0];
        return null;
    }

    /// <summary>
    /// Oblicza pozycję w świecie 3D dla danego NPC na podstawie jego indeksu w kolejce.
    /// </summary>
    public Vector3 GetPositionInQueue(NPCBuyer buyer)
    {
        int index = queue.IndexOf(buyer);
        if (index == -1) return Vector3.zero; // Nie powinno się zdarzyć, jeśli dołączył

        // 0 jest w punkcie startowym
        // 1 jest oddalony o "spacing"
        if (queueStartPoint != null)
        {
            Vector3 direction = -queueStartPoint.forward; // Domyślny kierunek
            
            if (queueDirectionPoint != null)
            {
                // Kierunek OD startu DO punktu kierunkowego
                direction = (queueDirectionPoint.position - queueStartPoint.position).normalized;
            }

            return queueStartPoint.position + (direction * (index * spacing));
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// Nakazuje wszystkim czekającym NPC zaktualizowanie swojej pozycji (przesunięcie się).
    /// </summary>
    void UpdateQueuePositions()
    {
        foreach (var buyer in queue)
        {
            if (buyer != null)
            {
                Vector3 newPos = GetPositionInQueue(buyer);
                buyer.UpdateQueuePosition(newPos);
            }
        }
    }

    /// <summary>
    /// Zwraca indeks klienta w kolejce (0 = pierwszy).
    /// </summary>
    public int GetIndex(NPCBuyer buyer)
    {
        return queue.IndexOf(buyer);
    }
    
    void OnDrawGizmos()
    {
        if (queueStartPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(queueStartPoint.position, 0.5f);

            if (queueDirectionPoint != null)
            {
                 Gizmos.DrawLine(queueStartPoint.position, queueDirectionPoint.position);
                 Gizmos.DrawWireSphere(queueDirectionPoint.position, 0.3f);
            }
            else
            {
                Gizmos.DrawLine(queueStartPoint.position, queueStartPoint.position - queueStartPoint.forward * 5);
            }
        }
    }
}
