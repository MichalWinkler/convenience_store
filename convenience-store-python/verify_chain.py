"""
Weryfikacja Łańcucha Decyzyjnego
--------------------------------
Skrypt testowy służący do manualnej weryfikacji poprawności działania
szeregowego połączenia modeli RetailNet i PersonalityNet.
Uruchamia predefiniowane przypadki testowe (scenariusze), aby sprawdzić,
czy model osobowości poprawnie modyfikuje decyzje zakupowe.
"""
import torch
import numpy as np
from network import model, RetailNet, PersonalityNet, FEATURE_COLUMNS, PERSONALITY_COLUMNS, device

def verify():
    """Funkcja wykonująca testy weryfikacyjne."""
    print("Loading models")
    # Ładowanie modelu RetailNet (popyt ogólny)
    retail_model = RetailNet(len(FEATURE_COLUMNS)).to(device)
    retail_model.load_state_dict(torch.load("retail_ai_model.pth", map_location=device, weights_only=True))
    retail_model.eval()

    # Ładowanie modelu PersonalityNet (korekta osobowościowa)
    personality_model = PersonalityNet(len(PERSONALITY_COLUMNS)).to(device)
    personality_model.load_state_dict(torch.load("personality_model.pth", map_location=device, weights_only=True))
    personality_model.eval()
    
    # Przypadek Testowy 1: Wysokie prawd. zakupu + Wysoka Impulsywność + Produkt Impulsowy -> Powinien kupić (BUY)
    # --------------------------------------------------------------------------
    print("\nTest 1: Strong BUY signal (High Base Prob + Impulsive + Impulse Item)")
    # Symulujemy sytuację, gdzie RetailNet dał wysokie prawdopodobieństwo
    base_prob = 0.8
    impulsiveness = 0.9
    generosity = 0.5
    is_impulse = 1.0  # Produkt impulsowy (normalnie trudniejszy do kupienia, ale tu klient jest impulsywny)
    
    # Wejście: [bazowe_prawd, impulsywność, szczodrość, czy_impulsowy]
    p_input = torch.tensor([[base_prob, impulsiveness, generosity, is_impulse]], dtype=torch.float32).to(device)
    p_out = personality_model(p_input)
    final_prob = torch.sigmoid(p_out).item()
    print(f"Base Prob: {base_prob}, Impulsiveness: {impulsiveness}, IsImpulse: {is_impulse} -> Final Prob: {final_prob:.4f} (Expected > 0.5)")
    
    print("\nTest 2: Weak signal (Medium Base Prob + Low Impulsiveness + Impulse Item)")
    # Przypadek Testowy 2: Średnie prawd. zakupu + Niska Impulsywność + Produkt Impulsowy -> Powinien pominąć (SKIP)
    # --------------------------------------------------------------------------
    base_prob = 0.4
    impulsiveness = 0.1
    generosity = 0.5
    is_impulse = 1.0 # Produkt impulsowy, ale klient mało impulsywny
    
    p_input = torch.tensor([[base_prob, impulsiveness, generosity, is_impulse]], dtype=torch.float32).to(device)
    p_out = personality_model(p_input)
    final_prob = torch.sigmoid(p_out).item()
    print(f"Base Prob: {base_prob}, Impulsiveness: {impulsiveness}, IsImpulse: {is_impulse} -> Final Prob: {final_prob:.4f} (Expected < 0.5)")

if __name__ == "__main__":
    try:
        verify()
        print("\nVerification successful!")
    except Exception as e:
        print(f"\nVerification failed: {e}")
