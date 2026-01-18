"""
API Serwera Backend
-------------------
Ten plik implementuje serwer REST API przy użyciu frameworka FastAPI.
Jest odpowiedzialny za obsługę żądań przychodzących z silnika Unity,
przetwarzanie danych o pogodzie i postaci, a następnie zwracanie przewidywań
modeli RetailNet i PersonalityNet.

Główne funkcje:
- Obsługa endpointu /predict dla żądań POST.
- Logowanie danych wejściowych i wyjściowych do pliku game_logs.log.
- Ładowanie i uruchamianie modeli sieci neuronowych.
"""
from fastapi import FastAPI
from pydantic import BaseModel
import torch
import torch.nn.functional as F
import numpy as np
from network import model, FEATURE_COLUMNS, device, PersonalityNet, PERSONALITY_COLUMNS

import logging
import json

# Konfiguracja loggera
logger = logging.getLogger()
logger.setLevel(logging.INFO)

fh = logging.FileHandler('game_logs.log')
fh.setLevel(logging.INFO)

formatter = logging.Formatter('%(asctime)s - %(message)s', datefmt='%Y-%m-%d %H:%M:%S')
fh.setFormatter(formatter)

logger.addHandler(fh)

app = FastAPI()

# ==============================
# 1) Dane wejściowe (Schema Pydantic)
# ==============================
class InputData(BaseModel):
    """
    Model danych wejściowych (Schema).
    definiuje strukturę danych oczekiwanych w żądaniu JSON.
    """
    # Cechy dla RetailNet (sieci popytu ogólnego)
    precpt: float = 0
    avg_temperature: float = 0
    stock_hour6_22_cnt: float = 0
    hours_stock_status: float = 0
    first_category_id: int = 0
    second_category_id: int = 0
    third_category_id: int = 0

    # Cechy dla PersonalityNet (sieci personalnej)
    impulsiveness: float = 0
    generosity: float = 0
    is_impulse: float = 0


# ==============================
# 2) Prediction endpoint
# ==============================
# Load Personality Model (Ładowanie modelu osobowości)
personality_model = PersonalityNet(len(PERSONALITY_COLUMNS)).to(device)
# Usunięto try/except aby wymusić widoczność błędów
print("Loading personality_model.pth...")
personality_model.load_state_dict(torch.load("personality_model.pth", map_location=device, weights_only=True))
personality_model.eval()
print("Model loaded successfully!")


# ==============================
# 2) Prediction endpoint
# ==============================
@app.post("/predict")
async def predict(data: InputData):
    """
    Główny endpoint przewidywania zakupów.
    
    Argumenty:
        data (InputData): Dane wejściowe przesłane przez Unity (JSON).
        
    Zwraca:
        dict: Słownik zawierający prawdopodobieństwo zakupu i ostateczną decyzję (0 lub 1).
    """
    # Ręczne logowanie przychodzącego żądania
    try:
        with open("game_logs.log", "a") as f:
            f.write(f"RECEIVED REQUEST: {data.json()}\n")
    except Exception as e:
        print(f"Failed to write log: {e}")

    # Zamiana wejścia na tensor zgodnie z FEATURE_COLUMNS
    x_values = [getattr(data, col, 0) for col in FEATURE_COLUMNS]

    x = torch.tensor(x_values, dtype=torch.float32).to(device).unsqueeze(0)

    with torch.no_grad():
        # 1. RetailNet prediction (Predykcja popytu ogólnego)
        # Przewidywanie ogólnego popytu na podstawie danych sklepowych/pogodowych
        logits = model(x)
        base_buy_prob = float(torch.sigmoid(logits).item())

        # 2. PersonalityNet prediction (Predykcja korekty osobowościowej)
        # Wejście: [bazowe_prawd, impulsywność, szczodrość, czy_impulsowy]
        # Przewidywanie indywidualne na podstawie cech osobowości agenta
        p_values = [
            base_buy_prob,
            data.impulsiveness,
            data.generosity,
            data.is_impulse
        ]
        
        # Logowanie tensora wejściowego
        try:
             with open("game_logs.log", "a") as f:
                f.write(f"DEBUG TENSOR: {p_values}\n")
        except: pass
        
        p_x = torch.tensor(p_values, dtype=torch.float32).to(device).unsqueeze(0)

        p_logits = personality_model(p_x)
        final_buy_prob = float(torch.sigmoid(p_logits).item())
        pred = 1 if final_buy_prob > 0.5 else 0

    response = {
        "base_buy_prob": base_buy_prob,
        "final_buy_prob": final_buy_prob,
        "prediction": pred,     # 1 = kupi, 0 = nie kupi
        "debug_check": "alive"
    }
    
    # Ręczne logowanie odpowiedzi
    try:
        with open("game_logs.log", "a") as f:
            f.write(f"SENDING RESPONSE: {json.dumps(response)}\n")
    except Exception as e:
        print(f"Failed to write log: {e}")
    
    return response
