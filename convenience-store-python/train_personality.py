"""
Trening Modelu PersonalityNet
-----------------------------
Skrypt odpowiedzialny za generowanie syntetycznych danych osobowościowych
i trening sieci neuronowej PersonalityNet.
Model uczy się korygować bazową chęć zakupu w oparciu o cechy psychologiczne:
impulsywność, szczodrość oraz flagę produktu impulsowego.
"""
import torch
import torch.nn as nn
import torch.optim as optim
import numpy as np
from torch.utils.data import Dataset, DataLoader
from network import PersonalityNet, PERSONALITY_COLUMNS, device

class SyntheticPersonalityDataset(Dataset):
    """
    Syntetyczny zbiór danych do treningu sieci osobowości.
    Symuluje wpływ cech charakteru na decyzję zakupową.
    """
    def __init__(self, num_samples=10000):
        self.num_samples = num_samples
        
        # Generowanie losowych cech
        self.base_buy_probs = np.random.rand(num_samples).astype(np.float32)
        self.impulsiveness = np.random.rand(num_samples).astype(np.float32)
        self.generosity = np.random.rand(num_samples).astype(np.float32)
        self.is_impulse = np.random.randint(0, 2, num_samples).astype(np.float32)
        
        self.targets = []
        for i in range(num_samples):
            # Prosta logika symulująca decyzję
            score = self.base_buy_probs[i] * 2.0
            
            # Produkty impulsowe trudniej kupić, chyba że jest się impulsywnym
            if self.is_impulse[i] > 0.5:
                score -= 1.0
                score += self.impulsiveness[i] * 2.5
            else:
                score += 0.0
            
            # Szczodrość lekko zwiększa szansę zakupu
            score += self.generosity[i] * 0.3
            
            # Szum losowy
            score += np.random.normal(0, 0.1)
            
            # Próg decyzyjny
            buy = 1 if score > 1.2 else 0
            self.targets.append(buy)
            
        self.targets = np.array(self.targets, dtype=np.float32)

    def __len__(self):
        return self.num_samples

    def __getitem__(self, idx):
        """Pobranie próbki wektora cech osobowościowych."""
        x = np.array([
            self.base_buy_probs[idx],
            self.impulsiveness[idx],
            self.generosity[idx],
            self.is_impulse[idx]
        ], dtype=np.float32)
        
        y = np.array([self.targets[idx]], dtype=np.float32)
        
        return torch.from_numpy(x), torch.from_numpy(y)

def main():
    """Główna pętla treningowa."""
    print("Generating synthetic data...")
    dataset = SyntheticPersonalityDataset(num_samples=10000)
    dataloader = DataLoader(dataset, batch_size=64, shuffle=True)
    
    model = PersonalityNet(len(PERSONALITY_COLUMNS)).to(device)
    optimizer = optim.Adam(model.parameters(), lr=0.001)
    criterion = nn.BCEWithLogitsLoss()
    
    
    history = {"loss": [], "accuracy": []}
    
    print("Training PersonalityNet...")
    for epoch in range(100):
        model.train()
        total_loss = 0
        correct = 0
        total = 0
        
        for X_batch, y_batch in dataloader:
            X_batch = X_batch.to(device)
            y_batch = y_batch.to(device)
            
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
        
        print(f"Epoch {epoch+1}: Loss = {epoch_loss:.4f}, Acc = {epoch_acc:.4f}")
        
    import json
    with open("personality_history.json", "w") as f:
        json.dump(history, f)
    print("Saved training history to personality_history.json")
        
    torch.save(model.state_dict(), "personality_model.pth")
    print("Saved personality_model.pth")

if __name__ == "__main__":
    main()
