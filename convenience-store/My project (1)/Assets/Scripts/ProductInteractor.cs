/*
 * Interakcja z Produktami
 * -----------------------
 * Klasa umożliwiająca graczowi/kamerze interakcję z produktami na półkach.
 * Obsługuje wyświetlanie informacji (UI) po najechaniu myszką oraz
 * otwieranie edytora ceny klawiszem 'E'.
 */
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProductInteractor : MonoBehaviour
{
    [Header("Settings")]
    public float interactionDistance = 3.0f;
    public LayerMask interactableLayer = ~0; // Domyślnie wszystko

    [Header("UI References")]
    public TextMeshProUGUI hoverInfoText;

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            Debug.LogError("ProductInteractor: No MainCamera found! Tag your camera as 'MainCamera'.");
            // Próbuj pobrać komponent z tego samego obiektu
            _cam = GetComponent<Camera>();
        }

        if (hoverInfoText == null)
        {
            CreateHoverUI();
        }
    }

    // Tworzy proste UI w kodzie, jeśli nie zostało przypisane w inspektorze
    private void CreateHoverUI()
    {
        // Próba znalezienia płótna (Canvas)
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("ProductUI_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Utwórz tekst
        GameObject txtObj = new GameObject("HoverInfoText");
        txtObj.transform.SetParent(canvas.transform, false);
        
        hoverInfoText = txtObj.AddComponent<TextMeshProUGUI>();
        hoverInfoText.fontSize = 32;
        hoverInfoText.alignment = TextAlignmentOptions.Center;
        hoverInfoText.color = Color.yellow;
        hoverInfoText.enableWordWrapping = false;
        
        // Dodaj obrys dla lepszej widoczności
        Outline outline = txtObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        // Wypozycjonuj w przybliżeniu na środku
        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, -50); 
        rt.sizeDelta = new Vector2(500, 100);

        hoverInfoText.text = ""; // Zacznij pusto
    }

    private void Update()
    {
        if (_cam == null) return;

        // Resetuj tekst domyślnie
        if (hoverInfoText != null) hoverInfoText.text = "";

        // Jeśli kursor jest widoczny, prawdopodobnie jesteśmy w menu, więc ignorujemy interakcję
        if (Cursor.visible && Cursor.lockState != CursorLockMode.Locked) 
        {
            return;
        }

        // Raycast ze środka kamery
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // Linia debugowania (widoczna w scenie)
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.green);

        // Sprawdzamy czy promień trafił w obiekt
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Collide))
        {
            // Próba pobrania komponentu Product
            Product p = hit.collider.GetComponent<Product>();
            
            // Jeśli nie znaleziono bezpośrednio, szukamy w rodzicu (dla złożonych prefabów)
            if (p == null) p = hit.collider.GetComponentInParent<Product>();

            if (p != null)
            {
                // Znaleziono produkt - aktualizacja UI
                if (hoverInfoText != null)
                {
                    hoverInfoText.text = $"{p.productName}\n<size=80%>${p.price:F2}</size>\n<color=white>[E] Edit</color>";
                }

                // Obsługa klawisza interakcji (E)
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log($"ProductInteractor: Interacting with {p.productName}");
                    OpenEditor(p);
                }
            }
        }
    }

    // Otwiera okno edycji ceny dla danego produktu
    void OpenEditor(Product p)
    {
        if (PriceEditorUI.Instance == null)
        {
            Debug.LogWarning("ProductInteractor: PriceEditorUI Instance not found! Creating one...");
            GameObject go = new GameObject("PriceEditorUI");
            go.AddComponent<PriceEditorUI>();
        }
        
        // Małe opóźnienie jeśli potrzebne lub wywołanie bezpośrednie
        PriceEditorUI.Instance.Open(p);
    }
}
