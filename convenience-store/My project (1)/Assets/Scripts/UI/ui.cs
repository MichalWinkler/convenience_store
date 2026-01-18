/*
 * Główny Kontroler UI (HUD)
 * -------------------------
 * Zarządza głównym interfejsem użytkownika (HUD), wyświetlając czas, poziom, pieniądze,
 * pogodę oraz cel gry. Obsługuje również warunek zwycięstwa, ekran wygranej
 * i restartowanie gry.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class ui : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI weatherText;
    public Slider cleanlinessSlider;

    [Header("Win Screen References")]
    public GameObject winPanel;
    public TextMeshProUGUI winTimeText;
    public Button restartButton;
    public Button quitButton;

    [Header("Gameplay Values")]
    public int shopLevel = 1;
    public float cleanliness = 100f;
    public float money = 250f;
    public float timeSpeed = 60f;
    public float targetMoney = 500f;

    private float timeOfDay = 8f;
    
    // Logika Wygranej
    private bool gameWon = false;
    private float realTimeElapsed = 0f;

    void Start()
    {
        if (winPanel == null)
        {
            CreateWinUI();
        }
        else
        {
            winPanel.SetActive(false);
            if (restartButton) restartButton.onClick.AddListener(RestartGame);
            if (quitButton) quitButton.onClick.AddListener(QuitGame);
        }

        if (weatherText == null) Debug.LogWarning("UI: WeatherText is not assigned in the Inspector!");
        else weatherText.text = "20.0°C  Clear"; // Default value

        if (WeatherManager.Instance == null) Debug.LogWarning("UI: WeatherManager Instance is not found in the scene!");

        gameWon = false;
        Time.timeScale = 1f;
        
        EnforceHUDLayout();
    }

    // Wymusza pozycjonowanie elementów HUD w lewym górnym rogu
    void EnforceHUDLayout()
    {
        float currentY = -20f;
        float spacing = 40f;
        float leftMargin = 20f;

        // Funkcja pomocnicza do kotwiczenia tekstu
        void AnchorText(TextMeshProUGUI tmp, ref float yPos)
        {
            if (tmp == null) return;
            RectTransform rt = tmp.GetComponent<RectTransform>();
            if (rt == null) return;

            // Kotwica Lewy-Górny (Top-Left)
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            
            // Ustaw Pozycję
            rt.anchoredPosition = new Vector2(leftMargin, yPos);
            rt.sizeDelta = new Vector2(300, 50); // Zapewnij rozmiar
            tmp.alignment = TextAlignmentOptions.Left;

            yPos -= spacing;
        }

        AnchorText(timeText, ref currentY);
        AnchorText(levelText, ref currentY);
        AnchorText(moneyText, ref currentY);
        AnchorText(objectiveText, ref currentY);
        AnchorText(weatherText, ref currentY);

        // --- NOWE: Wymuszenie Canvas Scaler na rodzicu Canvas ---
        // Znajdź canvas zawierający te elementy
        Canvas parentCanvas = null;
        if (timeText != null) parentCanvas = timeText.GetComponentInParent<Canvas>();
        
        if (parentCanvas != null)
        {
            CanvasScaler scaler = parentCanvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = parentCanvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Upewnij się też o GraphicRaycaster
            if (parentCanvas.GetComponent<GraphicRaycaster>() == null) parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        // --- SKALOWANIE KONTENERA HUD (Jeśli istnieje) ---
        // Jeśli teksty są wewnątrz panelu (np. VerticalLayoutGroup), ten panel może być wyśrodkowany.
        // Spróbujmy znaleźć wspólnego rodzica tekstów i zakotwiczyć go do lewej, jeśli nie jest to Canvas.
        if (timeText != null && timeText.transform.parent != null)
        {
            RectTransform parentRT = timeText.transform.parent.GetComponent<RectTransform>();
            // Sprawdź czy rodzic to nie Canvas (czyli jest to jakiś Panel kontenerowy)
            if (parentRT != null && parentCanvas != null && parentRT.gameObject != parentCanvas.gameObject) 
            {
                 // Wyłącz LayoutGroup jeśli istnieje, bo on nadpisuje pozycje dzieci
                 UnityEngine.UI.LayoutGroup lg = parentRT.GetComponent<UnityEngine.UI.LayoutGroup>();
                 if (lg != null) 
                 {
                     lg.enabled = false; 
                     // Debug.Log("Wyłączono LayoutGroup na panelu HUD, aby wymusić pozycjonowanie ręczne.");
                 }

                 // Zakotwicz Panel HUD do lewej góry (rozciągnij na wysokość, przyklej do lewej)
                 parentRT.anchorMin = new Vector2(0, 0);
                 parentRT.anchorMax = new Vector2(0, 1); // Lewa strona, pełna wysokość
                 parentRT.pivot = new Vector2(0, 0.5f);
                 parentRT.offsetMin = Vector2.zero; // Reset offsetów
                 parentRT.offsetMax = new Vector2(400, 0); // Szerokość 400px od lewej
            }
        }

        if (cleanlinessSlider != null)
        {
            RectTransform rt = cleanlinessSlider.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(leftMargin, currentY - 10); // Extra offset
            }
        }
    }

    // Tworzenie UI wygranej w kodzie (kiedy brak prefabu)
    void CreateWinUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        GameObject panelObj = new GameObject("WinPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        winPanel = panelObj;
        
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);
        
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        
        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 20;
        CreateText(panelObj, "MISSION ACCOMPLISHED!", 60, Color.green);
        
        GameObject timeObj = CreateText(panelObj, "Time: 00:00", 40, Color.white);
        winTimeText = timeObj.GetComponent<TextMeshProUGUI>();

        restartButton = CreateButton(panelObj, "RESTART", Color.cyan);
        restartButton.onClick.AddListener(RestartGame);

        quitButton = CreateButton(panelObj, "QUIT GAME", Color.red);
        quitButton.onClick.AddListener(QuitGame);

        winPanel.SetActive(false);
    }

    GameObject CreateText(GameObject parent, string content, float size, Color color)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return go;
    }

    Button CreateButton(GameObject parent, string label, Color color)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent.transform, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        
        Button btn = btnObj.AddComponent<Button>();
        
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = 200;
        le.minHeight = 60;
        
        GameObject txt = CreateText(btnObj, label, 30, Color.black);
        
        return btn;
    }


    void Update()
    {
        if (gameWon) return;

        realTimeElapsed += Time.deltaTime;

        UpdateTimeOfDay();
        UpdateUI();
        CheckWinCondition();

        // Globalna Logika Escape (Wyjście)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Sprawdź czy otwarte są pod-menu
            bool menuOpen = false;

            if (PriceEditorUI.Instance != null && PriceEditorUI.Instance.IsVisible) menuOpen = true;
            if (PaymentUIManager.Instance != null && PaymentUIManager.Instance.IsVisible) menuOpen = true;

            // Jeśli żadne menu nie jest otwarte, Wyjdź z Gry
            if (!menuOpen)
            {
               QuitGame();
            }
            // Jeśli menu JEST otwarte, odpowiedni skrypt zajmie się jego zamknięciem (w swoim Update).
            // Zapobiega to problemowi "Podwójnego Escape", gdzie jedno naciśnięcie zamyka menu I wyłącza grę.
        }
    }

    void CheckWinCondition()
    {
        if (money >= targetMoney)
        {
            WinGame();
        }
    }

    // Aktywacja stanu wygranej i wyświetlenie panelu końcowego
    void WinGame()
    {
        gameWon = true;
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        TimeSpan t = TimeSpan.FromSeconds(realTimeElapsed);
        string timeStr = string.Format("{0:D2}m {1:D2}s", t.Minutes, t.Seconds);

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            if (winTimeText != null)
            {
                winTimeText.text = $"Time: {timeStr}";
            }
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    // Aktualizacja czasu gry
    void UpdateTimeOfDay()
    {
        if (ClientSpawner.Instance != null)
        {
            timeOfDay = ClientSpawner.Instance.currentHour;
        }
        else
        {
            timeOfDay += Time.deltaTime * (timeSpeed / 3600f);
            if (timeOfDay >= 24f) timeOfDay -= 24f;
        }
    }

    // Odświeżanie elementów UI
    void UpdateUI()
    {
        int hours = Mathf.FloorToInt(timeOfDay);
        int minutes = Mathf.FloorToInt((timeOfDay - hours) * 60);
        string ampm = hours >= 12 ? "PM" : "AM";
        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;

        if (timeText) timeText.text = $"{displayHour:00}:{minutes:00} {ampm}";
        if (levelText) levelText.text = $"Level {shopLevel}";
        if (moneyText) moneyText.text = $"${money:F0}";
        
        if (objectiveText != null)
        {
            objectiveText.text = $"Goal: ${targetMoney}";
        }

        if (weatherText != null && WeatherManager.Instance != null)
        {
            float temp = WeatherManager.Instance.CurrentTemperature;
            bool isRain = WeatherManager.Instance.CurrentPrecipitation > 0.5f;
            string weatherIcon = isRain ? "Rain" : "Clear";
            
            weatherText.text = $"{temp:F1}°C  {weatherIcon}";
        }
    }

    public void AddMoney(float amount)
    {
        money += amount;
    }

    public void ReduceCleanliness(float amount)
    {
        cleanliness = Mathf.Max(0, cleanliness - amount);
    }

    public void IncreaseCleanliness(float amount)
    {
        cleanliness = Mathf.Min(100, cleanliness + amount);
    }
}
