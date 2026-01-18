/*
 * Wyzwalacz Płatności (Trigger)
 * -----------------------------
 * Wykrywa obecność gracza w strefie kasy. Jeśli gracz znajduje się przy kasie,
 * a pierwszy klient w kolejce oczekuje na zapłatę, skrypt otwiera UI płatności.
 */
using UnityEngine;
using System.Collections;

public class PaymentTrigger : MonoBehaviour
{
    private bool playerInRange = false;
    private bool uiOpen = false;

    // Wykrywanie wejścia gracza w strefę kasy
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PaymentTrigger] OnTriggerEnter: {other.name}, Tag: {other.tag}");
        if (other.CompareTag("Player") || other.GetComponent<FirstPersonController>())
        {
            Debug.Log("[PaymentTrigger] Player detected inside trigger.");
            playerInRange = true;
            CheckForPayment();
        }
    }

    // Wykrywanie wyjścia gracza ze strefy kasy
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<FirstPersonController>())
        {
            Debug.Log("[PaymentTrigger] Player exited trigger.");
            playerInRange = false;
            if (uiOpen)
            {
                PaymentUIManager.Instance.CloseUI();
                uiOpen = false;
            }
        }
    }

    void Update()
    {
        // Ciągłe sprawdzanie, na wypadek gdyby NPC dotarł, gdy gracz już tam stoi
        if (playerInRange && !uiOpen)
        {
            CheckForPayment();
        }
    }

    // Sprawdza czy można rozpocząć proces płatności
    void CheckForPayment()
    {
        if (CheckoutQueue.Instance == null) return;

        NPCBuyer activeNPC = CheckoutQueue.Instance.GetFirstCustomer();
        
        if (activeNPC == null) 
        {
             // Debug.Log("[PaymentTrigger] CheckForPayment: No active payer in queue.");
             return; 
        }

        // Otwieramy UI tylko jeśli klient jest na początku kolejki I wyraził gotowość (IsWaitingToPay)
        if (activeNPC.IsWaitingToPay)
        {
            Debug.Log($"[PaymentTrigger] Opening Payment UI. Cost: {activeNPC.TotalCost}");
            uiOpen = true;
            PaymentUIManager.Instance.OpenPaymentUI(activeNPC.TotalCost, () => 
            {
                // Callback po pomyślnej płatności
                Debug.Log("[PaymentTrigger] Payment Success Callback triggered.");
                activeNPC.OnPaymentReceived();
                uiOpen = false;
            });
        }
        else
        {
             // Debug.Log($"[PaymentTrigger] NPC found at front but not ready? Waiting: {activeNPC.IsWaitingToPay}");
        }
    }
}
