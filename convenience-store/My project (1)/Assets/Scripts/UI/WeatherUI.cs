/*
 * UI Pogody i Czasu
 * -----------------
 * Wyświetla aktualny czas (godzinę w grze) oraz warunki pogodowe
 * (temperatura, opady) pobrane z WeatherManager i ClientSpawner.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WeatherUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI tempText;
    public TextMeshProUGUI weatherText;

    void Update()
    {
        // 1. Aktualizacja Czasu
        if (ClientSpawner.Instance != null && timeText != null)
        {
            float hour = ClientSpawner.Instance.currentHour;
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.FloorToInt((hour - h) * 60);
            timeText.text = $"{h:00}:{m:00}";
        }

        // 2. Aktualizacja Pogody
        if (WeatherManager.Instance != null)
        {
            if (tempText != null)
            {
                tempText.text = $"{WeatherManager.Instance.CurrentTemperature:F1}°C";
            }

            if (weatherText != null)
            {
                float precip = WeatherManager.Instance.CurrentPrecipitation;
                string status = (precip > 0.5f) ? "Rainy" : "Clear";
                weatherText.text = status;
            }
        }
    }
}
