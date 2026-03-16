using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class PGSGameStatsManager : MonoBehaviour
{
    public static PGSGameStatsManager Instance { get; private set; }
    public float distanceTraveled = 0.0f;
    public TMPro.TextMeshProUGUI TMP_DistanceTraveled;
    private void Awake()
    {
        Debug.Log("PGSGameStatsManager.Awake");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        distanceTraveled += Time.deltaTime;
        if (TMP_DistanceTraveled != null ) {
            TMP_DistanceTraveled.text = distanceTraveled.ToString("F1");
        }
        SingleEventLog();
    }
    
    public void SingleEventLog()
    {
        Debug.Log("PGSGameStatsManager.SingleEventLog");
        Debug.Log("Recording single event...");
        // ++ KIM 260316 ORIGINAL
        // PlayerGameEvent playerGameEvent = new PlayerGameEvent.Builder("event_Single")
        //     .AddProperty("Dist", GameDataController.GetGameData().distanceTraveled)
        //     .Build();
        // PlayGamesPlatform.Instance.RecordEvent(playerGameEvent);
        PlayerGameEvent playerGameEvent = new PlayerGameEvent.Builder("event_Single")
            .AddProperty("Dist", distanceTraveled)
            .Build();
        PlayGamesPlatform.Instance.RecordEvent(playerGameEvent);
        PlayGamesPlatform.Instance.RequestEventsUpload();
        // ++ KIM 260316 ORIGINAL
    }

    public void MultiEventLog()
    {
        Debug.Log("PGSGameStatsManager.MultiEventLog");
        Debug.Log("Recording multiple events...");
        List<PlayerGameEvent> events = new List<PlayerGameEvent>
        {
            new PlayerGameEvent.Builder("event_multiple_1")
                .AddProperty("Dist", GameDataController.GetGameData().distanceTraveled)
                .Build(),
            new PlayerGameEvent.Builder("event_multiple_2")
                .AddProperty("Coins", GameDataController.GetGameData().coinsOwned)
                .Build(),
            new PlayerGameEvent.Builder("event_multiple_3")
            .AddProperty("SedanUnlocked", GameDataController.GetGameData().carIndexToOwnership[0] == Ownership.Owned)
            .Build()
        };
        PlayGamesPlatform.Instance.RecordEvents(events);
    }
}
