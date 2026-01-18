/*
 * Logika Kupującego NPC
 * ---------------------
 * Główny skrypt sterujący zachowaniem klienta w sklepie.
 * Realizuje maszynę stanów:
 * 1. Wejście do sklepu.
 * 2. Przejście przez listę zakupów (podchodzenie do regałów).
 * 3. Wysłanie zapytania do AI (Python) w celu podjęcia decyzji o zakupie.
 * 4. Udanie się do kolejki przy kasie i oczekiwanie na obsługę.
 * 5. Wyjście ze sklepu.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;      // zakładam że używasz NavMesh
using UnityEngine.Networking;
using System.Text;
using System.Globalization;
using TMPro; // Wymagane dla komponentów TextMeshPro

[RequireComponent(typeof(NavMeshAgent))]
public class NPCBuyer : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Movement Settings")]
    public float reachThreshold = 2.0f;

    [Header("Waypoints")]
    public Transform sidewalkPoint;      // punkt na chodniku przed sklepem
    public Transform storeEntrance;      // wejście do sklepu
    public Transform checkoutPoint;      // kasa
    public Transform exitPoint;          // wyjście ze sklepu

    [Header("Shopping")]
    public List<Product> shoppingList = new List<Product>();  // lista zakupowa NPC

    [Header("Personality")]
    [Range(0f, 1f)] public float impulsiveness = 0.5f;
    [Range(0f, 1f)] public float generosity = 0.5f;

    [Header("Animation")]
    [Tooltip("Przypisz komponent Animator tego NPC.")]
    public Animator npcAnimator;
    [Tooltip("Nazwa parametru 'Speed' w Animatorze.")]
    public string speedParameterName = "Speed";
    [Tooltip("Nazwa parametru 'IsStanding' (opcjonalne) w Animatorze.")]
    public string standingParameterName = "IsStanding";


    private bool aiFinished = false;
    private Dictionary<string, bool> aiDecisions = new Dictionary<string, bool>();

    // public static NPCBuyer CurrentPayer; // USUNIĘTO: Używamy systemu kolejkowego
    public bool IsWaitingToPay = false;
    public float TotalCost = 0f;
    private GameObject overheadUI;

    // Wywoływane przez kasjera/gracza po zakończeniu transakcji
    public void OnPaymentReceived()
    {
        // Dodaj pieniądze do gracza
        ui userInterface = FindObjectOfType<ui>();
        if (userInterface != null)
        {
            userInterface.AddMoney(TotalCost);
        }
        else
        {
            Debug.LogError("UI component not found! Money not added.");
        }

        IsWaitingToPay = false; // kontynuacja logiki
    }

    // Wyświetla cenę nad głową NPC
    void CreateOverheadUI(string text)
    {
        overheadUI = new GameObject("OverheadPrice");
        overheadUI.transform.SetParent(this.transform);
        overheadUI.transform.localPosition = new Vector3(0, 2.5f, 0); // Nad głową
        
        TextMeshPro tm = overheadUI.AddComponent<TextMeshPro>();
        tm.text = text;
        tm.fontSize = 6; // Rozmiar czcionki w przestrzeni świata
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = Color.yellow;
        
        // Zawsze przodem do kamery
        overheadUI.AddComponent<Billboard>();
    }

    // Zmieniono z Start() na Initialize(), aby Spawner mógł wywołać to ręcznie
    // dopiero po upewnieniu się, że agent jest poprawnie na NavMesh.
    public void Initialize()
    {
        // Czyszczenie stanu
        aiFinished = false;
        aiDecisions.Clear();
        IsWaitingToPay = false;
        TotalCost = 0f;
        if (overheadUI != null) Destroy(overheadUI);
        
        // Automatyczne generowanie punktów testowych
        if (sidewalkPoint == null) sidewalkPoint = CreateWaypoint("SidewalkPoint", new Vector3(0, 0, 0));
        if (storeEntrance == null) storeEntrance = CreateWaypoint("StoreEntrance", new Vector3(5, 0, 5));
        if (checkoutPoint == null) checkoutPoint = CreateWaypoint("CheckoutPoint", new Vector3(10, 0, 5));
        if (exitPoint == null) exitPoint = CreateWaypoint("ExitPoint", new Vector3(15, 0, 0));
        // ===========================================

        // 1. Auto-wyszukiwanie animatora (jeśli nie przypisany w prefabie)
        if (npcAnimator == null)
        {
            npcAnimator = GetComponent<Animator>();
            if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>();
            
            if (npcAnimator != null) Debug.Log($"[NPCBuyer] Auto-found Animator on {name}");
        }

        // inicjalizacja parametrów animatora
        if (npcAnimator != null)
        {
            if (HasParameter(speedParameterName, npcAnimator))
            {
                speedHash = Animator.StringToHash(speedParameterName);
            }
            else
            {
                Debug.LogWarning($"[NPCBuyer] Animator brakuje parametru Float o nazwie '{speedParameterName}'. Animacje chodu nie będą działać. Użyj 'Tools/Convenience Store/Create NPC Controller' aby naprawić.");
            }

            if (!string.IsNullOrEmpty(standingParameterName))
            {
                if (HasParameter(standingParameterName, npcAnimator))
                {
                    standingHash = Animator.StringToHash(standingParameterName);
                }
            }
        }

        agent = GetComponent<NavMeshAgent>();

        // Start → NPC idzie chodnikiem
        agent.SetDestination(sidewalkPoint.position);

        // Start predykcji w tle
        StartCoroutine(AI_ProcessShoppingList());

        // Po dotarciu na chodnik wejdź do sklepu
        StartCoroutine(BehaviourLoop());
    }

    // Helper do sprawdzania czy parametr istnieje w kontrolerze
    private bool HasParameter(string paramName, Animator animator)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    // Hashe parametrów (dla optymalizacji i bezpieczeństwa)
    private int speedHash = -1;
    private int standingHash = -1;
    private float debugTimer = 0f;

    void Update()
    {
        // Obsługa animacji w każdej klatce
        if (npcAnimator != null && agent != null && agent.isActiveAndEnabled)
        {
            // Pobierz aktualną prędkość agenta
            float speed = agent.velocity.magnitude;

            // Ustaw Speed (jeśli parametr istnieje)
            if (speedHash != -1)
            {
                npcAnimator.SetFloat(speedHash, speed);
            }

            // Ustaw IsStanding (jeśli parametr istnieje)
            if (standingHash != -1)
            {
                bool isStanding = speed < 0.1f;
                // Odkomentowane:
                npcAnimator.SetBool(standingHash, isStanding); 
            }

        // Wypisz status animacji raz na sekundę
            debugTimer += Time.deltaTime;
            if (debugTimer > 1.0f)
            {
                debugTimer = 0f;
                // Debug.Log($"[NPCAnim] {name}: Speed={speed:F2}, IsStanding={(speed < 0.1f)}, HasAnimator={npcAnimator != null}");
            }
        }
    }

    // Predykcja sieci wysyłana w tle podczas gdy npc idzie do sklepu
    IEnumerator AI_ProcessShoppingList()
    {
        foreach (var item in shoppingList)
        {
            if (item == null) continue;

            Debug.Log("AI checking product: " + item.productName);

             // Przygotowanie JSONa dla API
            string json = $@"
            {{
                ""precpt"": {(WeatherManager.Instance != null ? WeatherManager.Instance.CurrentPrecipitation : 0)},
                ""avg_temperature"": {(WeatherManager.Instance != null ? WeatherManager.Instance.CurrentTemperature : 20)},
                ""stock_hour6_22_cnt"": 1,
                ""hours_stock_status"": 1,
                ""first_category_id"": {item.cat1},
                ""second_category_id"": {item.cat2},
                ""third_category_id"": {item.cat3},
                ""impulsiveness"": {impulsiveness.ToString(CultureInfo.InvariantCulture)},
                ""generosity"": {generosity.ToString(CultureInfo.InvariantCulture)},
                ""is_impulse"": {(item.isImpulse ? 1 : 0)}
            }}";
            
            Debug.Log($"[Client -> Server] JSON: {json}");

            bool requestSuccess = false;
            int retryCount = 0;
            const int maxRetries = 10;

            // Pętla retry w przypadku błędu połączenia
            while (!requestSuccess && retryCount < maxRetries)
            {
                UnityWebRequest req = new UnityWebRequest("http://127.0.0.1:8000/predict", "POST");
                byte[] body = Encoding.UTF8.GetBytes(json);

                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    requestSuccess = true;
                    Debug.Log($"[Server -> Client] Response: {req.downloadHandler.text}");
                    AIResult result = JsonUtility.FromJson<AIResult>(req.downloadHandler.text);

                    // Skalowanie prawdopodobieństwa w zależności od zmiany ceny
                    float baseP = (item.basePrice > 0) ? item.basePrice : item.price;
                    float currentP = Mathf.Max(0.01f, item.price);

                    float priceRatio = baseP / currentP;

                    float rawProb = result.final_buy_prob;
                    float adjustedProb = rawProb * priceRatio;

                    bool buy = adjustedProb > 0.5f;
                    Debug.Log($"[Price Logic] Item: {item.productName}, Base: {baseP}, Curr: {currentP}, Ratio: {priceRatio:F2}, RawProb: {rawProb:F2} -> AdjProb: {adjustedProb:F2}, BUY: {buy}");

                    aiDecisions[item.productName] = buy;

                    Debug.Log($"AI: {item.productName} → {buy}");
                }
                else
                {
                    retryCount++;
                    Debug.LogWarning($"[NPCBuyer] Request failed: {req.error}. Retrying {retryCount}/{maxRetries} in 1s...");
                    yield return new WaitForSeconds(1.0f);
                    
                    if (retryCount >= maxRetries)
                    {
                         Debug.LogError($"AI error after {maxRetries} attempts: {req.error}");
                         Debug.LogError($"Server response: {req.downloadHandler.text}");
                         aiDecisions[item.productName] = false;
                    }
                }
            }
        }

        aiFinished = true;
    }

    // Główna pętla zachowania (Navigation & Shop Flow)
    IEnumerator BehaviourLoop()
    {
        yield return WalkTo(sidewalkPoint);
        yield return WalkTo(storeEntrance);

        Debug.Log("NPC entered store.");
        
        float currentRunCost = 0f;

        // Czekamy aż AI podejmie decyzje (jeśli jeszcze liczy)
        yield return new WaitUntil(() => aiFinished);

        // Chodzenie po sklepie od produktu do produktu
        for (int i = 0; i < shoppingList.Count; i++)
        {
            var item = shoppingList[i];
            if (item == null) continue;

            if (item.price == 0) item.price = 1;
            
            yield return WalkTo(item.transform);

            Debug.Log($"NPC looking at {item.productName}");

            yield return new WaitForSeconds(Random.Range(0.5f, 2f)); // Symulacja oglądania

            if (aiDecisions.ContainsKey(item.productName) && aiDecisions[item.productName])
            {
                Debug.Log($"NPC decided to BUY: {item.productName} (Impulse: {item.isImpulse}, Price: {item.price})");
                currentRunCost += item.price;
            }
            else
            {
                Debug.Log($"NPC decided to SKIP: {item.productName}");
            }
        }

        // Jeśli coś kupił, idzie do kasy
        if (currentRunCost > 0)
        {
            Debug.Log($"NPC going to checkout. Total Cost: {currentRunCost}");
            TotalCost = currentRunCost;
            if (CheckoutQueue.Instance != null)
            {
                // Dołącz do kolejki
                Vector3 queuePos = CheckoutQueue.Instance.JoinQueue(this);
                yield return WalkToPosition(queuePos);

                // Czekaj na swoją kolej
                while (CheckoutQueue.Instance.GetIndex(this) > 0)
                    yield return new WaitForSeconds(0.5f);

                // Podejdź do lady
                Vector3 frontPos = CheckoutQueue.Instance.GetPositionInQueue(this);
                yield return WalkToPosition(frontPos);

                IsWaitingToPay = true;
                CreateOverheadUI($"Total: ${TotalCost}");
                
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowNotification("Customer is waiting to pay!", 4.0f);
                }

                Debug.Log($"NPC {name} is at COUNTER waiting to pay.");

                // Czekaj na interakcję gracza
                yield return new WaitUntil(() => !IsWaitingToPay);
                
                if (overheadUI != null) Destroy(overheadUI);
                CheckoutQueue.Instance.LeaveQueue(this);
            }
            else
            {
                Debug.LogError("CheckoutQueue Instance not found! NPC stuck.");
            }

            Debug.Log("NPC payment accepted.");
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            Debug.Log("NPC bought nothing. Skipping checkout.");
        }

        Debug.Log("NPC leaving store.");
        yield return WalkTo(exitPoint);

        Destroy(gameObject);
    }

    public void UpdateQueuePosition(Vector3 newPos)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.SetDestination(newPos);
        }
    }

    IEnumerator WalkTo(Transform target)
    {
        if (target == null) yield break;
        yield return WalkToPosition(target.position);
    }

    // Pomocnicza funkcja ruchu - czekanie na dotarcie do celu
    IEnumerator WalkToPosition(Vector3 targetPos)
    {
        agent.SetDestination(targetPos);

        yield return null; 

        while (true)
        {
            // Bezpieczeństwo: Jeśli agent został wyłączony lub zniszczony, przerwij
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            {
                yield break;
            }

            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance || agent.remainingDistance <= reachThreshold)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        break;
                    }
                }
            }
            yield return null;
        }
    }

    Transform CreateWaypoint(string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        return go.transform;
    }
}



