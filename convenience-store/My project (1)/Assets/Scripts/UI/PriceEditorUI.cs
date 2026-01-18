/*
 * Edytor Cen (UI)
 * ---------------
 * Obsługuje panel edycji cen produktów aktywny po naciśnięciu 'E' na produkcie.
 * Pozwala graczowi zmieniać cenę sprzedaży danego towaru, co wpływa na
 * decyzje zakupowe klientów.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PriceEditorUI : MonoBehaviour
{
    public static PriceEditorUI Instance { get; private set; }
    public bool IsVisible => uiPanel != null && uiPanel.activeSelf;

    private GameObject uiPanel;
    private TMP_InputField priceInput;
    private TextMeshProUGUI infoText;
    private Product currentProduct;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        SetupUI();
        Close();
    }

    private void Update()
    {
        if (uiPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    // Otwiera panel edycji dla konkretnego produktu
    public void Open(Product product)
    {
        currentProduct = product;
        if (infoText != null)
            infoText.text = $"<size=40>{product.productName}</size>\n<color=#AAAAAA>Base Price: ${product.basePrice:F2}</color>";
        
        if (priceInput != null)
            priceInput.text = product.price.ToString("0.00");

        uiPanel.SetActive(true);
        
        // Odblokuj Kursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Wyłącz sterowanie gracza (Próba znalezienia standardowego FPC)
        var fpc = FindObjectOfType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.enabled = false;
        }
    }

    // Zapisuje nową cenę i zamyka panel
    public void SaveAndClose()
    {
        if (currentProduct != null && priceInput != null)
        {
            if (float.TryParse(priceInput.text, out float newPrice))
            {
               currentProduct.price = Mathf.Max(0, newPrice);
            }
        }
        Close();
    }

    // Zamyka panel i przywraca sterowanie
    public void Close()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        
        // Zablokuj Kursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Włącz sterowanie gracza
        var fpc = FindObjectOfType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.enabled = true;
        }
    }

    private void SetupUI()
    {
        // Utwórz Canvas
        GameObject canvasObj = new GameObject("PriceEditorCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Utwórz Panel
        uiPanel = new GameObject("Panel");
        uiPanel.transform.SetParent(canvasObj.transform, false);
        Image bg = uiPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);
        RectTransform rt = uiPanel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 300);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // Tekst Info
        GameObject txtObj = new GameObject("InfoText");
        txtObj.transform.SetParent(uiPanel.transform, false);
        infoText = txtObj.AddComponent<TextMeshProUGUI>();
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.color = Color.white;
        RectTransform txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchoredPosition = new Vector2(0, 80);
        txtRT.sizeDelta = new Vector2(350, 100);

        // Pole Tekstowe (Input)
        GameObject inputObj = new GameObject("PriceInput");
        inputObj.transform.SetParent(uiPanel.transform, false);
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = Color.white;
        RectTransform inputRT = inputObj.GetComponent<RectTransform>();
        inputRT.sizeDelta = new Vector2(200, 50);
        inputRT.anchoredPosition = new Vector2(0, 0);
        
        priceInput = inputObj.AddComponent<TMP_InputField>();
        
        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform areaRT = textArea.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero; areaRT.anchorMax = Vector2.one; 
        areaRT.offsetMin = new Vector2(10, 0); areaRT.offsetMax = new Vector2(-10, 0);

        GameObject childText = new GameObject("Text");
        childText.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI childTmp = childText.AddComponent<TextMeshProUGUI>();
        childTmp.color = Color.black;
        childTmp.fontSize = 32;
        childTmp.alignment = TextAlignmentOptions.Center;
        RectTransform childRT = childText.GetComponent<RectTransform>();
        childRT.anchorMin = Vector2.zero; childRT.anchorMax = Vector2.one;

        priceInput.textViewport = areaRT;
        priceInput.textComponent = childTmp;
        priceInput.contentType = TMP_InputField.ContentType.DecimalNumber;

        // Przycisk Zapisz
        GameObject btnObj = new GameObject("SaveButton");
        btnObj.transform.SetParent(uiPanel.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = Color.green;
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(SaveAndClose);
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(120, 50);
        btnRT.anchoredPosition = new Vector2(0, -80);

        GameObject btnTxtObj = new GameObject("Text");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTmp = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "SAVE";
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.fontSize = 24;
        btnTmp.color = Color.black;
        RectTransform btnTxtRT = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero; btnTxtRT.anchorMax = Vector2.one;
    }
}
