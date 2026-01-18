"""
Definicje Modeli Sieci Neuronowych
----------------------------------
Ten plik zawiera definicje architektur dwóch sieci neuronowych używanych w projekcie:
1. RetailNet - sieć przewidująca ogólne prawdopodobieństwo zakupu.
2. PersonalityNet - sieć korygująca decyzję na podstawie cech osobowości.

Oraz definicje kolumn wejściowych i logikę ładowania wytrenowanego modelu RetailNet.
"""
import torch
import torch.nn as nn
import torch.nn.functional as F

# -------------------------------
# Feature columns (te same co przy treningu)
# -------------------------------
# Lista cech używanych przez model RetailNet
FEATURE_COLUMNS = [
    "precpt",
    "avg_temperature",
    "stock_hour6_22_cnt",
    "hours_stock_status",
    "first_category_id",
    "second_category_id",
    "third_category_id",
]

# Lista cech używanych przez model PersonalityNet
PERSONALITY_COLUMNS = [
    "base_buy_prob",
    "impulsiveness",
    "generosity",
    "is_impulse"
]

# -------------------------------
# Klasy Modeli (Model Classes)
# -------------------------------
class RetailNet(nn.Module):
    """
    Sieć RetailNet: Prosta sieć feed-forward.
    Służy do oceny 'bazowej' szansy na zakup na podstawie czynników zewnętrznych.
    Architektura:
      - Wejście: len(FEATURE_COLUMNS)
      - Warstwa ukryta 1: 64 neurony (ReLU)
      - Warstwa ukryta 2: 64 neurony (ReLU)
      - Wyjście: 1 neuron (Logits)
    """
    def __init__(self, input_size):
        super().__init__()
        self.fc1 = nn.Linear(input_size, 64)
        self.fc2 = nn.Linear(64, 64)
        self.fc3 = nn.Linear(64, 1)

    def forward(self, x):
        """Przepływ danych przez sieć (Forward Pass)."""
        x = F.relu(self.fc1(x))
        x = F.relu(self.fc2(x))
        return self.fc3(x)


class PersonalityNet(nn.Module):
    """
    Sieć PersonalityNet: Sieć korygująca decyzję.
    Bierze pod uwagę bazowe prawdopodobieństwo oraz cechy osobowości agenta.
    Architektura:
      - Wejście: len(PERSONALITY_COLUMNS)
      - Warstwa ukryta 1: 32 neurony (ReLU)
      - Warstwa ukryta 2: 16 neuronów (ReLU)
      - Wyjście: 1 neuron (Logits - ostateczna decyzja)
    """
    def __init__(self, input_size):
        super().__init__()
        self.fc1 = nn.Linear(input_size, 32)
        self.fc2 = nn.Linear(32, 16)
        self.fc3 = nn.Linear(16, 1)

    def forward(self, x):
        """Przepływ danych przez sieć (Forward Pass)."""
        x = F.relu(self.fc1(x))
        x = F.relu(self.fc2(x))
        return self.fc3(x)

# -------------------------------
# Urządzenie Obliczeniowe (Device)
# -------------------------------
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# -------------------------------
# Ładowanie wytrenowanego modelu
# -------------------------------
# Inicjalizacja i próba załadowania wytrenowanego modelu RetailNet przy starcie modułu
model = RetailNet(len(FEATURE_COLUMNS)).to(device)
try:
    checkpoint = torch.load("retail_ai_full.pth", map_location=device, weights_only=True)
    model.load_state_dict(checkpoint["model_state"])
    model.eval()
except Exception as e:
    print(f"Warning: Could not load retail_ai_full.pth: {e}")
