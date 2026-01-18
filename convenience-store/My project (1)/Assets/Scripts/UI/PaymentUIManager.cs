/*
 * UI Systemu Płatności (Kasa)
 * ---------------------------
 * Obsługuje interfejs kasy fiskalnej, w tym wyświetlacz, klawiaturę numeryczną
 * i logikę wprowadzania ceny przez gracza. Zarządza również blokowaniem
 * kursora i ruchu gracza podczas obsługi kasy.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class PaymentUIManager : MonoBehaviour
{
    private static PaymentUIManager _instance;
    public static PaymentUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PaymentUIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PaymentUIManager");
                    _instance = go.AddComponent<PaymentUIManager>();
                }
            }
            return _instance;
        }
    }

    public bool IsVisible => uiContainer != null && uiContainer.activeSelf;

    private GameObject uiContainer;
    private TextMeshProUGUI inputDisplay;
    private string currentInput = "";
    private float expectedPrice = 0;
    private Action onPaymentSuccess;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Obsługa Esc do zamknięcia
        if (uiContainer != null && uiContainer.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseUI();
            }
        }
    }

    // Otwiera panel płatności z oczekiwaną kwotą
    public void OpenPaymentUI(float price, Action onSuccess)
    {
        if (uiContainer == null) SetupUI();

        expectedPrice = price;
        onPaymentSuccess = onSuccess;
        currentInput = "";
        UpdateDisplay();
        
        uiContainer.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Zablokowanie ruchu gracza podczas korzystania z kasy
        FirstPersonController fpc = FindObjectOfType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.cameraCanMove = false;
            fpc.playerCanMove = false;
            fpc.lockCursor = false; // Upewnij się, że FPC nie próbuje go zablokować
            fpc.enableHeadBob = false;
        }
    }

    // Zamknięcie panelu i przywrócenie kontroli graczowi
    public void CloseUI()
    {
        if (uiContainer != null) uiContainer.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        FirstPersonController fpc = FindObjectOfType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.cameraCanMove = true;
            fpc.playerCanMove = true;
            fpc.lockCursor = true;
            fpc.enableHeadBob = true;
        }
    }

    // Dynamiczne tworzenie UI kasy w kodzie
    private void SetupUI()
    {
        // Canvas Rodzic
        GameObject canvasGO = new GameObject("PaymentCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // Na samym wierzchu
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel Kontenera (Środek)
        uiContainer = new GameObject("Panel_Payment");
        uiContainer.transform.SetParent(canvasGO.transform, false);
        Image bg = uiContainer.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Ciemne tło
        RectTransform rt = uiContainer.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 650);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // Tytuł
        CreateText(uiContainer.transform, "ENTER PRICE", new Vector2(0, 275), 36);

        // Ekran Wyświetlacza
        GameObject displayObj = new GameObject("Display");
        displayObj.transform.SetParent(uiContainer.transform, false);
        Image dispBg = displayObj.AddComponent<Image>();
        dispBg.color = Color.black;
        RectTransform dispRT = displayObj.GetComponent<RectTransform>();
        dispRT.sizeDelta = new Vector2(350, 80);
        dispRT.anchoredPosition = new Vector2(0, 200);

        inputDisplay = CreateText(displayObj.transform, "$0.00", Vector2.zero, 48);
        inputDisplay.alignment = TextAlignmentOptions.Right;

        // Siatka Klawiatury Numerycznej
        GameObject gridObj = new GameObject("NumpadGrid");
        gridObj.transform.SetParent(uiContainer.transform, false);
        RectTransform gridRT = gridObj.AddComponent<RectTransform>();
        gridRT.sizeDelta = new Vector2(300, 400);
        gridRT.anchoredPosition = new Vector2(0, -50);
        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(90, 90);
        grid.spacing = new Vector2(10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        // Przyciski 1-9
        for (int i = 1; i <= 9; i++)
        {
            int num = i;
            CreateButton(gridObj.transform, num.ToString(), () => OnNumberPress(num));
        }

        // Wiersz 4: Kropka, 0, Czyszczenie
        CreateButton(gridObj.transform, ".", OnDecimalPress);
        CreateButton(gridObj.transform, "0", () => OnNumberPress(0));
        CreateButton(gridObj.transform, "C", OnClearPress, Color.red);

        // Przycisk Zapłać (Pod siatką)
        CreateButton(uiContainer.transform, "PAY", OnEnterPress, Color.green);
        // Przesuwamy ręcznie, bo nie jest w siatce
        Transform payBtn = uiContainer.transform.Find("Btn_PAY");
        if (payBtn != null)
        {
            RectTransform payRT = payBtn.GetComponent<RectTransform>();
            payRT.anchoredPosition = new Vector2(-5, -275);
            payRT.sizeDelta = new Vector2(290, 60); // Szeroki przycisk
        }
        
        uiContainer.SetActive(false);
    }

    private void OnDecimalPress()
    {
        if (!currentInput.Contains("."))
        {
            if (currentInput == "") currentInput = "0"; // "0." jeśli puste
            currentInput += ".";
            UpdateDisplay();
        }
    }

    private void OnNumberPress(int num)
    {
        if (currentInput.Length < 10) // Lekko zwiększona max długość
        {
            currentInput += num.ToString();
            UpdateDisplay();
        }
    }

    private void OnClearPress()
    {
        currentInput = "";
        UpdateDisplay();
    }

    // Zatwierdzenie płatności
    private void OnEnterPress()
    {
        if (float.TryParse(currentInput, out float amount))
        {
            // Walidacja kwoty - czy zgadza się z oczekiwaną
            if (Mathf.Approximately(amount, expectedPrice))
            {
                // Sukces
                onPaymentSuccess?.Invoke();
                CloseUI();
                NotificationManager.Instance.ShowNotification("Payment Accepted!", 2f);
            }
            else
            {
                // Błąd
                StartCoroutine(FlashError());
            }
        }
    }

    System.Collections.IEnumerator FlashError()
    {
        inputDisplay.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        inputDisplay.color = Color.white;
        currentInput = "";
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (string.IsNullOrEmpty(currentInput))
            inputDisplay.text = "$0";
        else
            inputDisplay.text = "$" + currentInput;
    }

    // --- Funkcje Pomocnicze ---

    private TextMeshProUGUI CreateText(Transform parent, string content, Vector2 pos, float fontSize)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 100); // rozmiar ogólny

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        return tmp;
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Color? color = null)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(90, 90); // Domyślny dla siatki, może być nadpisany

        Image img = btnObj.AddComponent<Image>();
        img.color = color ?? Color.white;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 40;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = (color.HasValue) ? Color.white : Color.black; 
    }
}
