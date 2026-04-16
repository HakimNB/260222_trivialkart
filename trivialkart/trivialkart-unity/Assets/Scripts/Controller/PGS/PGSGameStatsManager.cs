using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class PGSGameStatsManager : MonoBehaviour
{
    public void SingleEventLog()
    {
        Debug.Log("Recording single event...");
        PlayerGameEvent singleEvent = new PlayerGameEvent.Builder("event_single")
            .AddProperty("score_event", GameDataController.GetGameData().distanceTraveled)
            .Build();
        PlayGamesPlatform.Instance.RecordEvent(singleEvent);
    }

    public void MultiEventLog()
    {
        Debug.Log("Recording multiple events...");
        List<PlayerGameEvent> multipleEvents = new List<PlayerGameEvent>
        {
            new PlayerGameEvent.Builder("event_multiple_01")
                .AddProperty("score_event", GameDataController.GetGameData().distanceTraveled)
                .Build(),
            new PlayerGameEvent.Builder("event_multiple_02")
                .AddProperty("run_time_event", GameDataController.GetGameData().coinsOwned)
                .Build()
        };
        PlayGamesPlatform.Instance.RecordEvents(multipleEvents);
    }
}	
