/*
 * Generator Klientów (Spawner)
 * ----------------------------
 * Zarządza procesem tworzenia nowych klientów (NPC) w sklepie.
 * Kontroluje upływ czasu w grze (godziny otwarcia 8-22), prawdopodobieństwo
 * pojawienia się klienta w zależności od godziny oraz przydzielanie
 * losowych cech i przedmiotów impulsowych nowym agentom.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HourlyClientSegment
{
    public string name = "Segment Name"; 
    public int hour; // np. 8 dla 8:00-9:00
    public List<GameObject> specificPrefabs;
}

public class ClientSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Default List of NPC Prefabs to choose from randomly.")]
    // Domyślna lista prefabrykatów NPC do losowego wyboru.
    public List<GameObject> clientPrefabs;

    [Tooltip("Specific configurations for certain hours. If an entry exists for the current hour, it overrides the default list.")]
    // Specyficzne konfiguracje dla określonych godzin.
    public List<HourlyClientSegment> hourlyClientConfig;

    [Tooltip("Transform where the NPC will be spawned.")]
    // Punkt (Transform), w którym pojawi się NPC.
    public Transform spawnPoint;

    [Header("Time Settings")]
    [Tooltip("How many real seconds equal one game hour.")]
    // Ile sekund rzeczywistych odpowiada jednej godzinie w grze.
    public float realSecondsPerHour = 10.0f; 
    
    [Tooltip("Current hour of the day (0-24).")]
    // Aktualna godzina w grze (0-24).
    public float currentHour = 8.0f; // Start o 8:00

    // Godziny otwarcia 8:00 - 22:00
    private const float OPENING_HOUR = 8.0f;
    private const float CLOSING_HOUR = 22.0f;

    [Header("Spawn Settings")]
    [Tooltip("Spawn chance per hour. Index 0 is 8:00-9:00, Index 1 is 9:00-10:00, etc. up to 22:00.")]
    public float[] hourlySpawnChance; // Uproszczone: szansa na spawn w każdym interwale sprawdzania
    
    [Tooltip("Interval in seconds to check for spawning.")]
    // Interwał w sekundach do sprawdzania spawnu.
    public float spawnCheckInterval = 2.0f;

    [Header("Impulse Buying")]
    [Tooltip("List of items customers might impulsively decide to buy.")]
    // Lista przedmiotów, które klienci mogą kupić pod wpływem impulsu.
    public List<Product> globalImpulseList;

    // Lista wszystkich dostępnych produktów (do generowania listy podstawowej)
    private List<Product> allShopProducts = new List<Product>();

    [Header("Shopping List Settings")]
    [Tooltip("Maximum number of items in the basic shopping list.")]
    // Maksymalna liczba przedmiotów na podstawowej liście zakupów.
    public int maxShoppingListItems = 5;
    
    [Tooltip("Maximum extra items a customer can pick.")]
    // Maksymalna liczba dodatkowych przedmiotów.
    public int maxImpulseItems = 2;
    
    [Tooltip("Chance (0-1) that a customer picks up impulse items.")]
    // Szansa (0-1), że klient wybierze przedmioty impulsowe.
    public float impulseChance = 0.5f;

    [Header("Animation Settings")]
    [Tooltip("Globalny kontroler animacji dla wszystkich spawnowanych NPC (opcjonalne). Jeśli przypisany, nadpisze ustawienia prefabu.")]
    public RuntimeAnimatorController globalNpcController;

    [Header("Scene References for NPC")]
    // Referencje do kluczowych punktów nawigacyjnych przekazywanych agentom
    public Transform sidewalkPoint;
    public Transform storeEntrance;
    public Transform checkoutPoint;
    public Transform exitPoint;

    private float _timer;

    public static ClientSpawner Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (hourlySpawnChance == null || hourlySpawnChance.Length == 0)
        {
            // Domyślna konfiguracja na 14 godzin (8 do 22)
            hourlySpawnChance = new float[14]; 
            for(int i=0; i<14; i++) hourlySpawnChance[i] = 0.5f; // 50% szans domyślnie
        }
        
        StartCoroutine(TimeLoop());
        StartCoroutine(TimeLoop());
        StartCoroutine(SpawnLoop());

        // Inicjalizacja listy produktów
        // Znajdź wszystkie produkty w sklepie, aby móc je przydzielać klientom
        allShopProducts.AddRange(FindObjectsOfType<Product>());
        // Debug.Log($"ClientSpawner: Znaleziono {allShopProducts.Count} produktów w sklepie.");
    }

    // Pętla zarządzająca czasem gry
    IEnumerator TimeLoop()
    {
        while (true)
        {
            yield return null; 
            
            // Zwiększ czas tylko jeśli jest przed godziną zamknięcia
            if (currentHour < CLOSING_HOUR)
            {
                currentHour += (Time.deltaTime / realSecondsPerHour);
                
                // Ogranicz do godziny zamknięcia
                if (currentHour >= CLOSING_HOUR)
                {
                    currentHour = CLOSING_HOUR;
                }
            }
            // else: Czas zatrzymany na godzinie zamknięcia
        }
    }

    // Pętla sprawdzająca czy należy zespawnować klienta
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnCheckInterval);

            // Spawnowanie tylko w godzinach otwarcia
            if (currentHour >= OPENING_HOUR && currentHour < CLOSING_HOUR)
            {
                AttemptSpawn();
            }
        }
    }

    // Logika próby stworzenia nowego agenta na podstawie szansy godzinowej
    void AttemptSpawn()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("ClientSpawner: SpawnPoint is not assigned!");
            return;
        }

        // Określ indeks dla tablicy godzinowej
        // 8:00 to indeks 0
        int hourIndex = Mathf.FloorToInt(currentHour) - (int)OPENING_HOUR;
        
        // Sprawdzenie bezpieczeństwa
        if (hourIndex < 0 || hourIndex >= hourlySpawnChance.Length) 
        {
            // Zabezpieczenie lub ignorowanie przy błędnej konfiguracji
             return; 
        }

        float chance = hourlySpawnChance[hourIndex];
        float roll = Random.value;
        
        if (roll < chance)
        {
            SpawnClient();
        }
    }

    // Fizyczne utworzenie obiektu klienta i jego konfiguracja
    void SpawnClient()
    {
        List<GameObject> poolToUse = clientPrefabs;

        // Sprawdzenie czy dla danej godziny jest dedykowany zestaw modeli (np. biznesmeni rano)
        int currentHourInt = Mathf.FloorToInt(currentHour);
        if (hourlyClientConfig != null)
        {
            foreach(var seg in hourlyClientConfig)
            {
                if (seg.hour == currentHourInt && seg.specificPrefabs != null && seg.specificPrefabs.Count > 0)
                {
                    poolToUse = seg.specificPrefabs;
                    Debug.Log($"ClientSpawner: Using hourly override for hour {currentHourInt}. Pool size: {poolToUse.Count}");
                    break;
                }
            }
        }

        if (poolToUse == null || poolToUse.Count == 0) 
        {
            Debug.LogWarning($"ClientSpawner: No prefabs available to spawn for hour {currentHourInt}!");
            return;
        }

        // 1. Wybór losowego prefabu
        GameObject prefab = poolToUse[Random.Range(0, poolToUse.Count)];
        if (prefab == null)
        {
            Debug.LogError("ClientSpawner: Selected prefab is null!");
            return;
        }
        
        // 2. Instancjacja
        // Próba znalezienia poprawnej pozycji na NavMesh w pobliżu punktu spawnu
        Vector3 startPos = spawnPoint.position;
        bool foundMesh = false;
        UnityEngine.AI.NavMeshHit hit;
        
        // Szukaj w promieniu 5 jednostek
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPoint.position, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            startPos = hit.position;
            foundMesh = true;
        }

        GameObject newClient = Instantiate(prefab, startPos, spawnPoint.rotation);
        // Debug.Log($"ClientSpawner: Instantiated {newClient.name} at {startPos} (OnMesh: {foundMesh})");
        
        // Sprawdzenie poprawności NavMeshAgent
        UnityEngine.AI.NavMeshAgent agent = newClient.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"ClientSpawner: Spawned object {newClient.name} MISSING NavMeshAgent component!");
        }
        else
        {
             // Wymuś pozycję na siatce, jeśli znaleziono
             if (foundMesh)
             {
                 agent.Warp(startPos);
             }
             else if (!agent.isOnNavMesh)
             {
                 Debug.LogWarning("ClientSpawner: Spawn point is OFF NavMesh and SamplePosition failed. Checking wider range...");
                 if (UnityEngine.AI.NavMesh.SamplePosition(spawnPoint.position, out hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
                 {
                     agent.Warp(hit.position);
                 }
             }
        }

        // 3. Konfiguracja skryptu NPCBuyer
        NPCBuyer buyerScript = newClient.GetComponent<NPCBuyer>();
        if (buyerScript != null)
        {
            Debug.Log($"ClientSpawner: NPCBuyer script found on {newClient.name}. Configuring...");

            // Konfiguracja animatora
            if (globalNpcController != null)
            {
                Animator anim = newClient.GetComponent<Animator>();
                if (anim == null) anim = newClient.GetComponentInChildren<Animator>();

                if (anim != null)
                {
                    anim.runtimeAnimatorController = globalNpcController;
                    // Debug.Log($"ClientSpawner: Assigned global Animator Controller to {newClient.name}");
                    
                    // Upewnij się, że NPCBuyer też o tym wie
                    if (buyerScript.npcAnimator == null) buyerScript.npcAnimator = anim;
                }
                else
                {
                    Debug.LogWarning($"ClientSpawner: Global Controller assigned but NO Animator found on {newClient.name}!");
                }
            }

            // Przypisanie punktów nawigacyjnych
            if (sidewalkPoint != null) buyerScript.sidewalkPoint = sidewalkPoint;
            if (storeEntrance != null) buyerScript.storeEntrance = storeEntrance;
            if (checkoutPoint != null) buyerScript.checkoutPoint = checkoutPoint;
            if (exitPoint != null) buyerScript.exitPoint = exitPoint;
            
            // Losowanie cech osobowości
            buyerScript.impulsiveness = Random.Range(0.0f, 1.0f);
            buyerScript.generosity = Random.Range(0.0f, 1.0f);
            Debug.Log($"ClientSpawner: {newClient.name} Stats -> Impulsiveness: {buyerScript.impulsiveness:F2}, Generosity: {buyerScript.generosity:F2}");

            // Logika zakupów impulsowych
            if (globalImpulseList != null && globalImpulseList.Count > 0)
            {
                // Losowanie szansy na impuls
                float impulseRoll = Random.value;
                if (impulseRoll <= impulseChance)
                {
                    int itemsToAdd = Random.Range(1, maxImpulseItems + 1);
                    Debug.Log($"ClientSpawner: {newClient.name} decided to impulse buy {itemsToAdd} items (Roll: {impulseRoll:F2} <= {impulseChance:F2})");
                    
                    for (int i = 0; i < itemsToAdd; i++)
                    {
                        Product randomItem = globalImpulseList[Random.Range(0, globalImpulseList.Count)];
                        if (randomItem != null)
                        {
                            buyerScript.shoppingList.Add(randomItem);
                        }
                    }
                }
            }

            // Logika generowania podstawowej listy zakupów
            // Zawsze generuj listę podstawową, aby zapewnić, że klient ma co najmniej jeden produkt.
            if (allShopProducts.Count > 0)
            {
                // Jeśli prefab nie ma przypisanych produktów, wylosuj je z dostępnych w sklepie.
                // Losujemy ilość od 1 do maxShoppingListItems.
                int itemsCount = Random.Range(1, maxShoppingListItems + 1);
                
                for (int i = 0; i < itemsCount; i++)
                {
                    Product randomProd = allShopProducts[Random.Range(0, allShopProducts.Count)];
                    if (randomProd != null && !buyerScript.shoppingList.Contains(randomProd))
                    {
                        buyerScript.shoppingList.Add(randomProd);
                    }
                }
                // Debug.Log($"ClientSpawner: Przypisano {buyerScript.shoppingList.Count} podstawowych produktów dla {newClient.name}");
            }
            // -------------------------------------------------------------

            // Inicjalizacja zachowania
            // Wywołujemy dopiero teraz, gdy mamy pewność co do NavMesh i konfiguracji
            buyerScript.Initialize();
        }
        else
        {
             Debug.LogError($"ClientSpawner: Spawned object {newClient.name} is MISSING NPCBuyer script! It won't move or buy anything.");
        }
    }

    void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.forward * 2);
        }

        if (sidewalkPoint != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(sidewalkPoint.position, 0.3f); }
        if (storeEntrance != null) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(storeEntrance.position, 0.3f); }
        if (checkoutPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(checkoutPoint.position, 0.3f); }
        if (exitPoint != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(exitPoint.position, 0.3f); }
    }
}
