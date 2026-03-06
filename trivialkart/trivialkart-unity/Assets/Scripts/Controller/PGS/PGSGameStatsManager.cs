using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public class PGSGameStatsManager : MonoBehaviour
{
    public void SingleEventLog()
    {
        Debug.Log("Recording single event...");
        PlayerGameEvent playerGameEvent = new PlayerGameEvent.Builder("event_Single")
            .AddProperty("Dist", GameDataController.GetGameData().distanceTraveled)
            .Build();
        PlayGamesPlatform.Instance.RecordEvent(playerGameEvent);
    }

    public void MultiEventLog()
    {
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
