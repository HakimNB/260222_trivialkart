// Copyright 2022 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

// #if PGS_V1 || PGS_V2
// using Facebook.Unity;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
// #endif

public class AuthManager : MonoBehaviour
{   
    // --- STATE VARIABLES ---
    private string customJwtToken;

    public string PgsPlayerId;
    public string PgsAuthCode;
    public string PgsAccessToken;
    public string PgsRefreshToken;

    private volatile bool googleTaskComplete = false;
    private string authCodeToExchange = null;
    private string credManError = null;

    public string serverUrl;
    public string webClientId;
    public string webClientSecret = "TODO-CLIENT-SECRET"

    // --- ENDPOINTS ---
    private string exchange_authcode_and_link;
    private string exchange_authcode_for_tokens;
    private string verify_and_link_facebook;
    private string post_count;
    private string connection_check_url;

    // --- REQUEST/RESPONSE OBJECTS ---
    [System.Serializable]
    private class GoogleAuthRequest
    {
        public string authCode;
        public string clientId;
        public string clientSecret;
    }

    [System.Serializable]
    private class FacebookAuthRequest { public string accessToken; }

    [System.Serializable]
    private class PostCountRequest { public int count; }

    [System.Serializable]
    private class LinkResponse
    {
        public string email;
        public string inGameAccountID;
        public int inGameCount;
        public string jwtToken;
    }

    [System.Serializable]
    private class PgsAuthTokenResponse
    {
        public string playerID;
        public string accessToken;
        public string refreshToken;
    }

    private static AuthManager _instance = null;
    public static AuthManager GetInstance() {
        return _instance;
    }

    private void Awake() {
        if ( _instance != null ) {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        initialize();
    }

    private void initialize() {
        exchange_authcode_and_link = serverUrl + "/exchange_authcode_and_link";
        exchange_authcode_for_tokens = serverUrl + "/exchange_authcode_for_tokens";
        verify_and_link_facebook = serverUrl + "/verify_and_link_facebook";
        post_count = serverUrl + "/post_count";
        connection_check_url = serverUrl + "/connection_check";

        PlayGamesPlatform.DebugLogEnabled = true;
    }

    [System.Serializable]
    private class ConnectionResponse {
        public string serverName;
        public string status;
        public bool webClientIdMatch;
    }

    // --- PGS ---
    public void PGS_SignIn(Action<string> onSuccess, Action<string> onFailure) {
        Debug.Log("AuthManager.PGS_SignIn ");
        PlayGamesPlatform.Instance.Authenticate((SignInStatus status) => { 
            Debug.Log("PGS Auth: " + status); 
            if (status == SignInStatus.Success) {
                // *** [Play Games Plugin 2.1.0] 03/19/26 12:40:24 +08:00 ERROR: Requesting server side access task failed - com.google.android.gms.common.api.ApiException: 10: 
                PGS_RequestServerSideAccess(onSuccess);
            } else {
                onFailure("PGS Auth: " + status);
            }
        });
    }

    public void PGS_RequestServerSideAccess(Action<string> onCompleted) {
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, (string serverAuthCode) => {
            Debug.Log("Server Auth Code: " + serverAuthCode); // Server Auth Code: ""
            PgsAuthCode = serverAuthCode;
            onCompleted(serverAuthCode);
        });
    }

    public void PGS_ExchangeAuthCode(string authCode, Action<string> onSuccess, Action<string> onFailure) {
        StartCoroutine(PGS_ExchangeAuthcodeForTokens(authCode, onSuccess, onFailure));
    }

    private IEnumerator PGS_ExchangeAuthcodeForTokens(string serverAuthCode, Action<string> onSuccess, Action<string> onFailure)
    {
        Debug.Log("AuthManager.PGS_ExchangeAuthcodeForTokens serverAuthCode:" + serverAuthCode);
        GoogleAuthRequest requestData = new GoogleAuthRequest { 
            authCode = serverAuthCode,
            clientId = webClientId,
            clientSecret = webClientSecret
        };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));

        UnityWebRequest request = new UnityWebRequest(exchange_authcode_for_tokens, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error: {request.error}");
            onFailure(request.error);
        }
        else
        {
            var response = JsonUtility.FromJson<PgsAuthTokenResponse>(request.downloadHandler.text);
            Debug.Log("AuthManager.ExchangeAuthcodeForTokens response:" + response);
            PgsPlayerId = response.playerID;
            PgsAccessToken = response.accessToken;
            PgsRefreshToken = response.refreshToken;
            onSuccess(PgsPlayerId);
        }
    }

    public bool PGS_IsAuthenticated() {
        return PlayGamesPlatform.Instance.IsAuthenticated();
    }

    public void PGS_ShowAchievementUI() {
        PlayGamesPlatform.Instance.ShowAchievementsUI();
    }

    public string PGS_GetLocalPlayerId() {
        return PlayGamesPlatform.Instance.localUser.id;
    }

    public void PGS_ShowAchievementUI_Delay() {
        StartCoroutine(PGS_ShowAchievementUI_DelayInternal());
    }

    private IEnumerator PGS_ShowAchievementUI_DelayInternal() {
        PlayGamesPlatform.Instance.ShowAchievementsUI();
        yield return new WaitForSeconds(1.0f);
    }

    // --- UTILS ---

}