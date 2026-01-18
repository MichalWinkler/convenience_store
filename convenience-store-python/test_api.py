"""
Test API Endpoint
-----------------
Prosty skrypt do testowania funkcjonalności endpointu /predict.
Wysyła przykładowy ładunek JSON do lokalnego serwera i wypisuje odpowiedź.
Przydatny do szybkiego sprawdzania statusu serwera.
"""
import requests
import json

def test_api():
    """Wysyła żądanie POST do lokalnego API w celu testu."""
    url = "http://127.0.0.1:8000/predict"
    payload = {
        "precpt": 0,
        "avg_temperature": 20,
        "stock_hour6_22_cnt": 1,
        "hours_stock_status": 1,
        "first_category_id": 44,
        "second_category_id": 44,
        "third_category_id": 44,
        "impulsiveness": 0.5,
        "generosity": 0.5,
        "is_impulse": 0
    }
    
    try:
        response = requests.post(url, json=payload)
        print(f"Status Code: {response.status_code}")
        print(f"Response: {response.text}")
    except Exception as e:
        print(f"Failed to connect: {e}")

if __name__ == "__main__":
    test_api()
