/*
 * Klient RetailAI
 * ---------------
 * Przykładowy/Testowy komponent AI, który cyklicznie wysyła zapytania do API.
 * UWAGA: Jest to klasa testowa, główna logika AI znajduje się w NPCBuyer.cs.
 */
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections;

public class RetailAI : MonoBehaviour
{
    void Start()
    {
        // Rozpocznij pętlę
        StartCoroutine(PredictLoop());
    }

    IEnumerator PredictLoop()
    {
        while (true)
        {
            yield return Predict();
            yield return new WaitForSeconds(9999f);
        }
    }

    // Wysyła zapytanie POST z przykładowymi danymi do serwera Python
    public IEnumerator Predict()
    {
        var json = @"
        {
            ""holiday_flag"": 1,
            ""precpt"": 0,
            ""avg_temperature"": 18,
            ""stock_hour6_22_cnt"": 50,
            ""hours_stock_status"": 1,
            ""first_category_id"": 12,
            ""second_category_id"": 5,
            ""third_category_id"": 21
        }";

        UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:8000/predict", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<AIResult>(request.downloadHandler.text);
            Debug.Log("Prediction: " + result.prediction + " | Prob BUY: " + result.final_buy_prob);
        }
        else
        {
            Debug.LogError("AI request failed: " + request.error);
        }
    }
}


