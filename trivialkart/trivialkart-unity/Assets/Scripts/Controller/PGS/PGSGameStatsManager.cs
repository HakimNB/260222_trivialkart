using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;
using UnityEngine.Networking;

public class PGSGameStatsManager : MonoBehaviour
{
    private static PGSGameStatsManager _instance = null;
    public static PGSGameStatsManager GetInstance() {
        return _instance;
    }
    
    [System.Serializable]
    private class DistanceTravelledRequest { 
        public string playerId;
        public float distance; 
    }
    
    public TMPro.TextMeshProUGUI TMP_DistanceTraveled;
    
    private void Awake() {
        if ( _instance != null ) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    public void GSAPI_ClientSingleEvent(float distanceTraveled)
    {
        Debug.Log("PGSGameStatsManager.GSAPI_ClientSingleEvent");
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

    public void GSAPI_ClientMultiEvent(float distanceTravelled, int coinsOwned, bool sedanUnlocked)
    {
        Debug.Log("PGSGameStatsManager.MultiEventLog");
        Debug.Log("Recording multiple events...");
        List<PlayerGameEvent> events = new List<PlayerGameEvent>
        {
            new PlayerGameEvent.Builder("event_multiple_1")
                .AddProperty("Dist", distanceTravelled)
                .Build(),
            new PlayerGameEvent.Builder("event_multiple_2")
                .AddProperty("Coins", coinsOwned)
                .Build(),
            new PlayerGameEvent.Builder("event_multiple_3")
            .AddProperty("SedanUnlocked", sedanUnlocked)
            .Build()
        };
        PlayGamesPlatform.Instance.RecordEvents(events);
    }

    public void GSAPI_ServerSingleEvent(float distanceTraveled) {
        Debug.Log("PGSGameStatsManager.GSAPI_ServerSingleEvent");
        StartCoroutine(SendSingleEventLog(distanceTraveled));
    }

    private IEnumerator SendSingleEventLog(float distanceTraveled)
    {
        Debug.Log("PGSGameStatsManager.SendSingleEventLog");
        string playerId = PlayGamesPlatform.Instance.GetUserId();
        Debug.Log("PGSGameStatsManager.SendSingleEventLog.Player ID: " + playerId);
        DistanceTravelledRequest requestData = new DistanceTravelledRequest { 
            playerId = playerId,
            distance = distanceTraveled 
        };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
        UnityWebRequest request = new UnityWebRequest(AuthManager.GetInstance().serverUrl + "/send_single_event", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + AuthManager.GetInstance().PgsAccessToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("PGSGameStatsManager.SendSingleEventLog success: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("PGSGameStatsManager.SendSingleEventLog failed. Error: " + request.error);
        }   
    }
}
