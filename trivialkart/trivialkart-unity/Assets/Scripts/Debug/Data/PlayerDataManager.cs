using UnityEngine;
using System;

[Serializable]
public class PlayerData
{
    public float fuel;
    public float distance;

    public PlayerData()
    {
        // Default starting values for new players
        fuel = 100f;
        distance = 0f;
    }
}

public class PlayerDataManager : MonoBehaviour
{
    // public static PlayerDataManager Instance { get; private set; }

    private static PlayerDataManager _instance = null;
    public static PlayerDataManager GetInstance() {
        return _instance;
    }

    public PlayerData CurrentData;
    private const string SAVE_KEY = "TrivialKart_PlayerData";

    private void Awake() {
        if (_instance != null) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        LoadData(); // Automatically load saved data when game boots

    }

    // --- SAVE / LOAD SYSTEM ---
    
    public void LoadData()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            CurrentData = new PlayerData();
            Debug.Log("PlayerDataManager: Created New Save Profile");
        }
        else
        {
            try 
            {
                CurrentData = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("PlayerDataManager: Save Profile Loaded Locally");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse PlayerData. Error: " + e.Message);
                CurrentData = new PlayerData();
            }
        }
    }

    public void SaveData()
    {
        if (CurrentData
   == null) return;
        
        string json = JsonUtility.ToJson(CurrentData
  );
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        // Debug.Log("PlayerDataManager: Progress Saved!");
    }

    // --- ENCAPSULATED DATA ACCESS ---

    public float Fuel 
    { 
        get { return CurrentData.fuel; }
        set { CurrentData.fuel = value; }
    }

    public float Distance 
    { 
        get { return CurrentData.distance; }
        set { CurrentData.distance = value; }
    }

    // Safely add/subtract fuel and auto-save
    public void ModifyFuel(float amount)
    {
        CurrentData.fuel += amount;
        
        // Prevent negative fuel
        if (CurrentData.fuel < 0f) CurrentData.fuel = 0f; 
        
        SaveData();
    }

    // Safely add driven distance and auto-save
    public void AddDistance(float amount)
    {
        CurrentData.distance += amount;
        SaveData();
    }

    // Fully reset data (e.g. wiped save or starting brand new race)
    public void ResetProgress()
    {
        CurrentData = new PlayerData();
        SaveData();
        Debug.Log("PlayerDataManager: Progress Reset!");
    }
}
