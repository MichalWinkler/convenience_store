import json
import matplotlib.pyplot as plt
import os

def plot_history(history_file, title, output_file):
    if not os.path.exists(history_file):
        print(f"File {history_file} not found. Please run the corresponding training script first.")
        return

    with open(history_file, 'r') as f:
        data = json.load(f)

    epochs = range(1, len(data['loss']) + 1)
    
    plt.figure(figsize=(12, 5))
    
    # Loss
    plt.subplot(1, 2, 1)
    plt.plot(epochs, data['loss'], 'r-o', label='Loss', markersize=2)
    plt.title(f'{title} - Loss')
    plt.xlabel('Epochs')
    plt.ylabel('Loss')
    plt.grid(True, which='both', linestyle='--', linewidth=0.5)
    plt.legend()

    # Accuracy
    plt.subplot(1, 2, 2)
    plt.plot(epochs, data['accuracy'], 'b-o', label='Accuracy', markersize=2)
    plt.title(f'{title} - Accuracy')
    plt.xlabel('Epochs')
    plt.ylabel('Accuracy')
    plt.grid(True, which='both', linestyle='--', linewidth=0.5)
    plt.legend()

    plt.tight_layout()
    plt.savefig(output_file, dpi=300)
    print(f"Saved plot to {output_file}")
    plt.close()

if __name__ == "__main__":
    print("Generating training plots for Retail and Personality networks...")
    plot_history("retail_history.json", "Retail Network", "retail_training_plot.png")
    plot_history("personality_history.json", "Personality Network", "personality_training_plot.png")
