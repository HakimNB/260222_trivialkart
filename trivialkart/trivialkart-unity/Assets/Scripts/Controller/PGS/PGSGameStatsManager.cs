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
        public string packageId;
        public string playerId;
        public float scoreEvent; 
        public float runTimeEvent;
    }
    
    public TMPro.TextMeshProUGUI TMP_DistanceTraveled;
    
    private void Awake() {
        if ( _instance != null ) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void BtnClick_ClientSingleEvent(){
        Debug.Log("BtnClick_ClientSingleEvent");
        GSAPI_ClientSingleEvent(100.0f);
    }

    public void BtnClick_ClientMultipleEvent(){
        Debug.Log("BtnClick_ClientMultipleEvent");
        GSAPI_ClientMultiEvent(100.0f, 200, false);
    }

    public void BtnClick_ServerSingleEvent(){
        Debug.Log("BtnClick_ServerSingleEvent");
        GSAPI_ServerSingleEvent(100.0f);
    }

    public void BtnClick_ServerMultipleEvent(){
        Debug.Log("BtnClick_ServerMultipleEvent");
        GSAPI_ServerMultiEvent(100.0f, 200.0f);
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
        Debug.Log("Recording single event...");
        PlayerGameEvent singleEvent = new PlayerGameEvent.Builder("event_single")
            .AddProperty("score_event", distanceTraveled)
            .Build();
        PlayGamesPlatform.Instance.RecordEvent(singleEvent);
        PlayGamesPlatform.Instance.RequestEventsUpload();
        // ++ KIM 260316 ORIGINAL
    }

    public void GSAPI_ClientMultiEvent(float distanceTravelled, int coinsOwned, bool sedanUnlocked)
    {
        Debug.Log("PGSGameStatsManager.MultiEventLog");
        Debug.Log("Recording multiple events...");
        List<PlayerGameEvent> multipleEvents = new List<PlayerGameEvent>
        {
            new PlayerGameEvent.Builder("event_multiple_01")
                .AddProperty("score_event", distanceTravelled)
                .Build(),
            new PlayerGameEvent.Builder("event_multiple_02")
                .AddProperty("run_time_event", coinsOwned)
                .Build()
        };
        PlayGamesPlatform.Instance.RecordEvents(multipleEvents);
        PlayGamesPlatform.Instance.RequestEventsUpload();
    }

    public void GSAPI_ServerSingleEvent(float distanceTraveled) {
        Debug.Log("PGSGameStatsManager.GSAPI_ServerSingleEvent");
        StartCoroutine(SendSingleEventLog(distanceTraveled));
    }

    public void GSAPI_ServerMultiEvent(float distanceTraveled, float runTimeEvent) {
        Debug.Log("PGSGameStatsManager.GSAPI_ServerMultiEvent");
        StartCoroutine(SendMultipleEventLog(distanceTraveled, runTimeEvent));
    }

    private IEnumerator SendSingleEventLog(float distanceTraveled)
    {
        Debug.Log("PGSGameStatsManager.SendSingleEventLog packageId: " + Application.identifier);
        string playerId = PlayGamesPlatform.Instance.GetUserId();
        Debug.Log("PGSGameStatsManager.SendSingleEventLog.Player ID: " + playerId);
        DistanceTravelledRequest requestData = new DistanceTravelledRequest {
            packageId = Application.identifier, 
            playerId = playerId,
            scoreEvent = distanceTraveled
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

    private IEnumerator SendMultipleEventLog(float scoreEvent, float runTimeEvent)
    {
        Debug.Log("PGSGameStatsManager.SendMultipleEventLog packageId: " + Application.identifier);
        string playerId = PlayGamesPlatform.Instance.GetUserId();
        Debug.Log("PGSGameStatsManager.SendMultipleEventLog.Player ID: " + playerId);
        DistanceTravelledRequest requestData = new DistanceTravelledRequest {
            packageId = Application.identifier, 
            playerId = playerId,
            scoreEvent = scoreEvent,
            runTimeEvent = runTimeEvent
        };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
        UnityWebRequest request = new UnityWebRequest(AuthManager.GetInstance().serverUrl + "/send_multiple_event", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + AuthManager.GetInstance().PgsAccessToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("PGSGameStatsManager.SendMultipleEventLog success: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("PGSGameStatsManager.SendMultipleEventLog failed. Error: " + request.error);
        }   
    }
}
