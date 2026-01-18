import torch
import numpy as np
from network import PersonalityNet, PERSONALITY_COLUMNS, device

def check_logic():
    print("Loading model...")
    model = PersonalityNet(len(PERSONALITY_COLUMNS)).to(device)
    try:
        model.load_state_dict(torch.load("personality_model.pth", map_location=device, weights_only=True))
    except:
        print("Could not load model, creating new one (untrained logic won't match, but script will run)")
        
    model.eval()
    
    # Scenario: High Base Prob, Regular Item (User JSON), Impulsiveness 0.5
    base_buy_prob = 0.99
    impulsiveness = 0.5
    generosity = 0.5
    is_impulse = 0.0    # Regular Item
    
    print(f"\nScenario: Base={base_buy_prob}, Impulsiveness={impulsiveness}, IsImpulse={is_impulse}")
    
    inp = torch.tensor([[base_buy_prob, impulsiveness, generosity, is_impulse]], dtype=torch.float32).to(device)
    with torch.no_grad():
        out = model(inp)
        prob = torch.sigmoid(out).item()
        
    print(f"Final Probability: {prob:.4f}")
    
    if prob < 0.1:
        print("CONCLUSION: The low probability is EXPECTED behavior for Impulse items given low impulsiveness.")
    else:
        print("CONCLUSION: The model did NOT learn the penalty correctly.")

if __name__ == "__main__":
    check_logic()
