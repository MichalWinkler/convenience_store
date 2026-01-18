/*
 * Menedżer Powiadomień (Singleton)
 * --------------------------------
 * Odpowiada za wyświetlanie tymczasowych powiadomień na ekranie
 * (np. "Płatność zaakceptowana"). Tworzy własne Canvas i kontenery UI
 * w kodzie, jeśli nie są dostępne.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    private static NotificationManager _instance;
    public static NotificationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NotificationManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("NotificationManager");
                    _instance = go.AddComponent<NotificationManager>();
                }
            }
            return _instance;
        }
    }

    private GameObject notificationContainer;
    private Canvas notificationsCanvas;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetupUI();
    }

    // Inicjalizacja struktury UI dla powiadomień
    void SetupUI()
    {
        if (notificationContainer != null) return;

        // Utwórz dedykowany Canvas dla powiadomień, aby były na wierzchu
        GameObject canvasGO = new GameObject("NotificationCanvas");
        canvasGO.transform.SetParent(transform); // Dziecko tego menedżera
        
        notificationsCanvas = canvasGO.AddComponent<Canvas>();
        notificationsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        notificationsCanvas.sortingOrder = 100; // Wysoki porządek sortowania dla widoczności
        
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f; // Dopasuj równomiernie

        canvasGO.AddComponent<GraphicRaycaster>();

        // Utwórz Kontener
        notificationContainer = new GameObject("NotificationContainer");
        notificationContainer.transform.SetParent(notificationsCanvas.transform, false);
        
        RectTransform rt = notificationContainer.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); // Góra Środek
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -100); // 100px od góry
        rt.sizeDelta = new Vector2(500, 0); // Szerokość 500

        VerticalLayoutGroup vlg = notificationContainer.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.spacing = 15;
        
        ContentSizeFitter csf = notificationContainer.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // Metoda publiczna do wywołania powiadomienia
    public void ShowNotification(string message, float duration = 3.0f)
    {
        StartCoroutine(CreateNotificationRoutine(message, duration));
    }

    // Korutyna tworząca, wyświetlająca i usuwająca powiadomienie
    IEnumerator CreateNotificationRoutine(string message, float duration)
    {
        // Upewnij się, że UI istnieje (w przypadku dostępu przed pełną inicjalizacją Awake)
        if (notificationContainer == null) SetupUI(); 

        // Utwórz obiekt powiadomienia
        GameObject notifObj = new GameObject("Notification");
        notifObj.transform.SetParent(notificationContainer.transform, false);

        // Tło (Obraz)
        Image bg = notifObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Ciemniejsze, nieprzezroczyste
        // Optional: If you have a sprite 'UISprite' or similar, load it. For now, simple rectangle.
        
        // Element układu
        LayoutElement le = notifObj.AddComponent<LayoutElement>();
        le.minHeight = 60;
        
        // Tekst
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(notifObj.transform, false);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        // Rozciągnij tekst
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero; // Wypełnij rodzica
        textRT.offsetMin = new Vector2(20, 0); // Margines
        textRT.offsetMax = new Vector2(-20, 0);

        // Animacja: Pojawianie się (Fade In)
        CanvasGroup cg = notifObj.AddComponent<CanvasGroup>();
        cg.alpha = 0;
        float timer = 0;
        float fadeTime = 0.3f;
        
        while(timer < fadeTime)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, timer / fadeTime);
            yield return null;
        }
        cg.alpha = 1;

        // Czekaj
        yield return new WaitForSeconds(duration);

        // Animacja: Zanikanie (Fade Out)
        timer = 0;
        while(timer < fadeTime)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, timer / fadeTime);
            yield return null;
        }

        Destroy(notifObj);
    }
}
