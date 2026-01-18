/*
 * AIResult - Model Danych
 * -----------------------
 * Klasa pomocnicza (DTO) służąca do deserializacji odpowiedzi JSON
 * otrzymanej z serwera Python. Zawiera ostateczną decyzję i prawdopodobieństwo zakupu.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AIResult
{
    public float final_buy_prob;
    public int prediction;
}
