/*
 * Klasa Reprezentująca Produkt
 * ----------------------------
 * Przechowuje dane o pojedynczym produkcie w sklepie, takie jak nazwa,
 * cena, poziom zapasów oraz kategorie używane przez sieci neuronowe.
 */
using UnityEngine;

public class Product : MonoBehaviour
{
    // Nazwa produktu wyświetlana w UI
    public string productName = "Product";
    // Aktualna cena produktu
    public float price = 5.0f;
    // Cena bazowa (do ewentualnych porównań)
    public float basePrice = 5.0f; 
    
    private void Start()
    {
        // Inicjalizacja ceny bazowej jeśli nie została ustawiona
        if (basePrice == 0) basePrice = price;
    }

    // Czy produkt jest oznaczony jako impulsowy
    public bool isImpulse = false;
    // Obecny stan magazynowy produktu
    public int stockLevel = 10;

    [Header("Categories")]
    // Kategorie ID używane jako wejście do sieci neuronowej
    public int cat1;
    public int cat2;
    public int cat3;
}
