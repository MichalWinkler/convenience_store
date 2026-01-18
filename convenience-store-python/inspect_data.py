import pandas as pd
try:
    print("Reading parquet...")
    df = pd.read_parquet("hf://datasets/Dingdong-Inc/FreshRetailNet-50K/data/train.parquet")
    print("Columns:", df.columns.tolist())
    print("First 5 rows:")
    print(df.head())
    
    # Check for category names
    possible_names = [c for c in df.columns if 'name' in c or 'desc' in c]
    if possible_names:
        print("Found name columns:", possible_names)
        print(df[possible_names + ['first_category_id']].drop_duplicates().head(20))
    else:
        print("No name columns found.")
        
except Exception as e:
    print(f"Error: {e}")
