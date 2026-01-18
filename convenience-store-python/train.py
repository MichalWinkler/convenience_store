"""
Trening Modelu RetailNet
------------------------
Skrypt odpowiedzialny za pobieranie danych zewnętrznych (Hugging Face),
przetwarzanie ich i trening sieci RetailNet.
Celem jest nauczenie modelu przewidywania, czy klient dokona zakupu
na podstawie warunków pogodowych i stanu magazynowego.
"""
import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data import Dataset, DataLoader
import pandas as pd
import numpy as np
from huggingface_hub import login

# Logowanie do HuggingFace (wymagane do pobrania prywatnych datasetów, jeśli takie są)
login("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")

FEATURE_COLUMNS = [
    "precpt",
    "avg_temperature",
    "stock_hour6_22_cnt",
    "hours_stock_status",
    "first_category_id",
    "second_category_id",
    "third_category_id",
]

# --------------------------------------------------------
# Definicja Datasetu
# --------------------------------------------------------
class RetailDataset(Dataset):
    """
    Klasa Dataset dla PyTorch, owijająca ramkę danych Pandas.
    Przygotowuje wektory cech oraz etykiety (target).
    """
    def __init__(self, dataframe, feature_columns):
        self.feature_columns = [c for c in feature_columns if c in dataframe.columns]
        self.data = dataframe.copy()
        # Wypełnianie brakujących danych zerami
        self.data[self.feature_columns] = self.data[self.feature_columns].fillna(0)
        # Tworzenie etykiety binarnej: 1 jeśli kwota sprzedaży > 0, w przeciwnym razie 0
        self.data["target"] = (self.data["sale_amount"] > 0).astype(int)

    def __len__(self):
        return len(self.data)

    def __getitem__(self, idx):
        """Zwraca pojedynczą próbkę danych (features, target)."""
        row = self.data.iloc[idx]
        feature_values = np.array(
            [row[col] if np.isscalar(row[col]) else np.float32(np.mean(row[col]))
             for col in self.feature_columns],
            dtype=np.float32
        )
        return torch.from_numpy(feature_values), torch.tensor(row["target"], dtype=torch.long)

class RetailNet(nn.Module):
    """
    Definicja modelu używanego w treningu (musi być zgodna z network.py).
    """
    def __init__(self, input_size):
        super().__init__()
        self.fc1 = nn.Linear(input_size, 64)
        self.fc2 = nn.Linear(64, 64)
        self.fc3 = nn.Linear(64, 1)  # BUY / SKIP

    def forward(self, x):
        x = F.relu(self.fc1(x))
        x = F.relu(self.fc2(x))
        return self.fc3(x)

def main():
    """Główna funkcja treningowa."""
    print("\nLoading dataset...")
    # Pobieranie datasetu z Hugging Face
    df = pd.read_parquet("hf://datasets/Dingdong-Inc/FreshRetailNet-50K/data/train.parquet")

    # Próbkowanie dla szybszego treningu (opcjonalne)
    df = df.sample(10000)

    dataset = RetailDataset(df, FEATURE_COLUMNS)
    dataloader = DataLoader(
        dataset,
        batch_size=2048,
        shuffle=True,
        num_workers=0,
        pin_memory=True
    )

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print("Using device:", device)

    model = RetailNet(len(FEATURE_COLUMNS)).to(device)
    optimizer = torch.optim.Adam(model.parameters(), lr=1e-3)
    criterion = nn.BCEWithLogitsLoss()

    NUM_EPOCHS = 250

    
    history = {"loss": [], "accuracy": []}
    
    # Pętla treningowa
    for epoch in range(NUM_EPOCHS):
        model.train()
        total_loss = 0
        correct = 0
        total = 0

        for X_batch, y_batch in dataloader:
            X_batch = X_batch.to(device)
            y_batch = y_batch.to(device).float().unsqueeze(1)

            optimizer.zero_grad()
            logits = model(X_batch)
            loss = criterion(logits, y_batch)
            loss.backward()
            optimizer.step()

            preds = (torch.sigmoid(logits) > 0.5).float()
            total_loss += loss.item() * X_batch.size(0)
            correct += (preds == y_batch).sum().item()
            total += X_batch.size(0)
            
        epoch_loss = total_loss/total
        epoch_acc = correct/total
        history["loss"].append(epoch_loss)
        history["accuracy"].append(epoch_acc)

        print(f"Epoch {epoch+1}: loss={epoch_loss:.4f} | acc={epoch_acc:.4f}")
        
    # Zapis historii treningu
    import json
    with open("retail_history.json", "w") as f:
        json.dump(history, f)
    print("Saved training history to retail_history.json")

    # Zapis modelu
    torch.save(model.state_dict(), "retail_ai_model.pth")
    torch.save({
        "model_state": model.state_dict(),
        "feature_columns": FEATURE_COLUMNS
    }, "retail_ai_full.pth")

    print("\nModel saved: retail_ai_model.pth and retail_ai_full.pth")

    # Prosty test weryfikacyjny po treningu
    model.eval()
    sample = {
        "precpt": 0,
        "avg_temperature": 18,
        "stock_hour6_22_cnt": 50,
        "hours_stock_status": 1,
        "first_category_id": 12,
        "second_category_id": 5,
        "third_category_id": 21,
    }

    x = torch.tensor([sample.get(col, 0) for col in FEATURE_COLUMNS], dtype=torch.float32).to(device)
    with torch.no_grad():
        logits = model(x.unsqueeze(0))
        prob = torch.sigmoid(logits).item()
        print(f"Sample test probability: {prob:.4f}")
        print("Prediction:", "BUY" if prob > 0.5 else "SKIP")

if __name__ == "__main__":
    main()
