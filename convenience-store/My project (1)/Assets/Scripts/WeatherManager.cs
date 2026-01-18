/*
 * Menedżer Pogody i Czasu
 * -----------------------
 * Singleton zarządzający czasem symulacji oraz warunkami pogodowymi (temperatura, opady).
 * Synchronizuje czas z ClientSpawner (jeśli istnieje) lub prowadzi własny zegar.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    [Header("Simulation Data")]
    public float CurrentTemperature = 20.0f;
    public float CurrentPrecipitation = 0.0f; // 0 = Czysto, 1 = Deszcz

    [Header("Settings")]
    public float minTempNight = 10.0f;
    public float maxTempDay = 30.0f;
    [Tooltip("Chance to change weather state every hour check.")]
    public float weatherChangeChance = 0.3f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(WeatherLoop());
    }

    // Pętla aktualizująca pogodę co sekundę rzeczywistą
    IEnumerator WeatherLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f); // Aktualizacja co każdą sekundę

            UpdateTemperature();
            UpdatePrecipitation(); // Prosta losowa logika
        }
    }

    private float internalTime = 8.0f; // Czas zapasowy

    // Oblicza temperaturę na podstawie godziny dnia (krzywa paraboliczna)
    void UpdateTemperature()
    {
        float hour = internalTime;
        if (ClientSpawner.Instance != null)
        {
            hour = ClientSpawner.Instance.currentHour;
            internalTime = hour; // Synchronizacja czasu wewnętrznego
        }
        else
        {
            // Aktualizuj czas wewnętrzny jeśli brak ClientSpawner
            // Zakładając 10 sekund rzeczywistych na godzinę gry
            internalTime += (1.0f / 10.0f); 
            if (internalTime >= 24f) internalTime -= 24f;
            hour = internalTime;
        }
        
        // Prosta paraboliczna krzywa temperatury ze szczytem o 14:00
        float distFromNoon = Mathf.Abs(hour - 14.0f);
        if (distFromNoon > 12) distFromNoon = 24 - distFromNoon;

        // distFromNoon is 0 at 14:00, 10 at 4:00/24:00, 12 at 2:00
        // (Wartość dystansu jest 0 o 14:00, 10 o 4:00/24:00, 12 o 2:00)
        float t = 1.0f - (distFromNoon / 12.0f); 
        t = Mathf.Clamp01(t);

        CurrentTemperature = Mathf.Lerp(minTempNight, maxTempDay, t);
    }

    // Losowa zmiana opadów atmosferycznych
    void UpdatePrecipitation()
    {
        // Zmiana pogody okazjonalnie
        // 5% szans na sekundę na zmianę deszczu
        if (Random.value < 0.05f) 
        {
            CurrentPrecipitation = (CurrentPrecipitation < 0.5f) ? 1.0f : 0.0f; 
            Debug.Log($"Weather Changed: Precipitation is now {CurrentPrecipitation}");
        }
    }
}
