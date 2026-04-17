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
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;


using GooglePlayGames;
using GooglePlayGames.BasicApi;

using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    private GameObject startPanel;
    private GameObject loginButtonsPanel;
    private GameObject gamePanel;
    private Button getStartedButton;
    private Button iAlreadyHaveButton;
    private Button signInWithGoogleButton;
    private Button signInWithFacebookButton;
    private Button signOutButton;
    private Button incButton;
    private Button unlockAchievementButton;
    private Button showAchievementButton;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI incText;
    private string customJwtToken;


    // private GoogleSignInUser googleUser;
    
    // --- NEW VARIABLES for main-thread dispatching ---
    private volatile bool googleTaskComplete = false;
    private Exception googleSignInException = null;
    private string authCodeToExchange = null;

    public string serverUrl;
    public string webClientId;
    private string verify_and_link_google;
    private string exchange_authcode_and_link;
    private string verify_and_link_facebook;
    private string post_count;

    [Serializable]
    private class GoogleAuthRequest
    {
        public string authCode;
    }

    [Serializable]
    private class FacebookAuthRequest
    {
        public string accessToken;
    }

    [Serializable]
    private class PostCountRequest
    {
        public int count;
    }

    [Serializable]
    private class LinkResponse
    {
        public string email;
        public string inGameAccountID;
        public int inGameCount;
        public string jwtToken;
    }

    private void Awake()
    {
        verify_and_link_google = serverUrl + "/verify_and_link_google";
        verify_and_link_facebook = serverUrl + "/verify_and_link_facebook";
        exchange_authcode_and_link = serverUrl + "/exchange_authcode_and_link";
        post_count = serverUrl + "/post_count";
        
        // UI setup
        startPanel = GameObject.Find("Canvas").transform.Find("StartPanel").gameObject;
        loginButtonsPanel = GameObject.Find("Canvas").transform.Find("LoginPanel").gameObject;
        gamePanel = GameObject.Find("Canvas").transform.Find("GamePanel").gameObject;
        statusText = GameObject.Find("Canvas").transform.Find("StatusText").GetComponent<TextMeshProUGUI>();

        getStartedButton = startPanel.transform.Find("GetStarted").GetComponent<Button>();
        iAlreadyHaveButton = startPanel.transform.Find("IAlreadyHave").GetComponent<Button>();
        signInWithGoogleButton = loginButtonsPanel.transform.Find("SIWG").GetComponent<Button>();
        signInWithFacebookButton = loginButtonsPanel.transform.Find("FB").GetComponent<Button>();
        signOutButton = gamePanel.transform.Find("SignOut").GetComponent<Button>();
        incText = gamePanel.transform.Find("IncText").GetComponent<TextMeshProUGUI>();
        incButton = gamePanel.transform.Find("Inc").GetComponent<Button>();
        unlockAchievementButton = gamePanel.transform.Find("UnlockAchievement").GetComponent<Button>();
        showAchievementButton = gamePanel.transform.Find("ShowAchievement").GetComponent<Button>();

        // --- V2 INITIALIZATION ---
        statusText.text = "Initializing Google Sign-In...";
        // GoogleSignIn.Configuration = new GoogleSignInConfiguration
        // {
        //     WebClientId = webClientId,
        //     ForceTokenRefresh = true,
            
        //     UseGameSignIn = false,
        //     RequestEmail = true,
        //     RequestAuthCode = true,
        // };

        PlayGamesPlatform.DebugLogEnabled = true;

        // // Facebook initialization
        // if (!FB.IsInitialized)
        // {
        //     FB.Init(OnInitComplete, OnHideUnity);
        // }
        // else
        // {
        //     FB.ActivateApp();
        // }

        // Button listeners
        getStartedButton.onClick.AddListener(GetStartedClicked);
        iAlreadyHaveButton.onClick.AddListener(IAlreadyHaveButtonClicked);
        // signInWithGoogleButton.onClick.AddListener(OnSignInWithGoogleClicked);
        // signInWithFacebookButton.onClick.AddListener(OnSignInWithFacebookClicked);
        signOutButton.onClick.AddListener(OnSignOutClicked);
        incButton.onClick.AddListener(OnIncButtonClicked);
        unlockAchievementButton.onClick.AddListener(OnAchievementUnlockButtonClicked);
        showAchievementButton.onClick.AddListener(OnShowAchievementsButtonClicked);

        statusText.text = "Checking credentials...";

        // GoogleSignIn.DefaultInstance.SignInSilently().ContinueWith(OnGoogleSignInComplete);
    }
    
//     private void Update()
//     {
// #if PGS_V2
//         if (googleTaskComplete)
//         {
//             googleTaskComplete = false; // Reset flag

//             if (authCodeToExchange != null && googleSignInException == null)
//             {
//                 // --- Success case ---
//                 Debug.Log($"Google Sign-In successful for: {this.googleUser.Email}");
//                 Debug.Log($"Retrieved Server Auth Code. Sending to backend...");
//                 statusText.text = "Connecting to game server...";
//                 StartCoroutine(ExchangeAuthcodeAndLink(authCodeToExchange));
//             }
//             else
//             {
//                 if (googleSignInException != null)
//                 {
//                     var e = googleSignInException.GetBaseException(); 
                    
//                     if (e is GoogleSignIn.SignInException signInException) 
//                     {
//                         Debug.Log($"Google Sign-In Error: {signInException.Status}"); 
//                         if (signInException.Status == GoogleSignInStatusCode.Canceled)
//                         {
//                             statusText.text = "Sign-in cancelled.";
//                         }
//                     }
//                     else
//                     {
//                         Debug.Log($"Google Sign-In Task Error: {e.Message}"); 
//                         if (e.Message.Contains("Canceled"))
//                         {
//                              statusText.text = "Sign-in cancelled.";
//                         }
//                         else
//                         {
//                             statusText.text = "Sign-in failed. Please try again.";
//                         }
//                     }
//                 }
                
//                 ShowStartPanel();
//             }
            
//             googleSignInException = null;
//             authCodeToExchange = null;
//         }
// #endif
//     }


    private void OnShowAchievementsButtonClicked()
    {
        Debug.Log("Show achievement button");
        PlayGamesPlatform.Instance.ShowAchievementsUI();
    }

    private void OnAchievementUnlockButtonClicked()
    {
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Debug.LogWarning("Not authenticated with PGS. Cannot unlock achievement.");
            statusText.text = "Error: Not signed in to PGS.";
            //SignInToPlayGamesServices();
            return;
        }

        statusText.text = "Unlocking achievement...";

        PlayGamesPlatform.Instance.ReportProgress(
            GPGSIds.achievement_tk_achievement_rand,
            100f,
            (bool success) =>
            {
                if (success)
                {
                    Debug.Log("Achievement unlocked successfully!");
                    statusText.text = "Achievement Unlocked!";
                }
                else
                {
                    Debug.LogWarning("Failed to unlock achievement.");
                    statusText.text = "Failed to unlock achievement.";
                }
            });
    }

    private void OnIncButtonClicked()
    {
        var currNum = int.Parse(incText.text);
        currNum++;
        incText.text = currNum.ToString();

        StartCoroutine(PostScore());
    }

    // --- Facebook Methods (Unchanged) ---
    private void OnInitComplete()
    {
        // if (FB.IsInitialized)
        // {
        //     FB.ActivateApp();
        //     Debug.Log("Facebook SDK Initialized.");
        // }
        // else
        // {
        //     Debug.LogError("Failed to Initialize the Facebook SDK.");
        //     statusText.text = "Facebook SDK failed to init.";
        // }
    }

    private void OnHideUnity(bool isGameShown)
    {
        Time.timeScale = isGameShown ? 1 : 0;
    }

    
    private IEnumerator ExchangeAuthcodeAndLink(string serverAuthCode)
    {
        Debug.Log("Exchange Authcode And Link " + serverAuthCode);
        
        GoogleAuthRequest requestData = new GoogleAuthRequest { authCode = serverAuthCode };
        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(exchange_authcode_and_link, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Backend Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            statusText.text = "Failed to link account. Server error.";
            
            // GoogleSignIn.DefaultInstance.SignOut(); 
            ShowStartPanel();
        }
        else
        {
            var jsonResponse = request.downloadHandler.text;
            var response = JsonUtility.FromJson<LinkResponse>(jsonResponse);

            Debug.Log($"Successfully linked! Email: {response.email}, In-Game ID: {response.inGameAccountID}");

            statusText.text = $"Signed in as: {response.email}\nIn-Game ID: {response.inGameAccountID}";
            incText.text = response.inGameCount.ToString("000");
            customJwtToken = response.jwtToken;

            ShowGamePanel();
            
            SignInToPlayGamesServices();
        }
    }
    
    // This is called from ExchangeAuthcodeAndLink (main thread), so it's safe.
    private void SignInToPlayGamesServices()
    {
        Debug.Log("Attempting silent sign-in to Play Games Services...");
        statusText.text = "Loading game services...";
        
        PlayGamesPlatform.Instance.Authenticate((SignInStatus status) =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("Play Games Services silent sign-in successful!");
                // if (this.googleUser != null)
                // {
                //     statusText.text = $"Signed in as: {this.googleUser.Email}";
                // }
            }
            else
            {
                Debug.LogWarning("Play Games Services silent sign-in failed: " + status);
                statusText.text = "Game services (achievements) failed to load.";
            }
        });
    }

    // ---
    // == COMMON METHODS ==
    // ---
    private IEnumerator PostScore()
    {
        if (string.IsNullOrEmpty(customJwtToken))
        {
            Debug.LogError("Not logged in! (customJwtToken is null).");
            statusText.text = "Error: Not signed in. Cannot save.";
            yield break;
        }

        PostCountRequest requestData = new PostCountRequest
        {
            count = int.Parse(incText.text)
        };
        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(post_count, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + this.customJwtToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Backend Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            statusText.text = "Failed to post count. Server error.";

            if (request.responseCode == 401 || request.responseCode == 403)
            {
                statusText.text = "Session expired. Please sign out and in again.";
            }
        }
        else
        {
            var jsonResponse = request.downloadHandler.text;
            var response = JsonUtility.FromJson<LinkResponse>(jsonResponse);
            incText.text = response.inGameCount.ToString("000");
        }
    }

    private void IAlreadyHaveButtonClicked()
    {
        startPanel.SetActive(false);
        loginButtonsPanel.SetActive(true);
    }

    private void GetStartedClicked()
    {
        statusText.text = "Signing in with Google...";
        startPanel.SetActive(false);

        // This is safe because OnGoogleSignInComplete now dispatches to Update()
        // GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleSignInComplete);
    }

    private void OnSignInWithGoogleClicked()
    {
        statusText.text = "Signing in with Google...";
        loginButtonsPanel.SetActive(false);

        // This is safe because OnGoogleSignInComplete now dispatches to Update()
        // GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleSignInComplete);
    }

    private IEnumerator VerifyAndLinkFacebookAccount(string accessToken)
    {
        FacebookAuthRequest requestData = new FacebookAuthRequest { accessToken = accessToken };
        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(verify_and_link_facebook, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Backend Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            statusText.text = "Failed to link FB account. Server error.";
            ShowStartPanel();
        }
        else
        {
            var jsonResponse = request.downloadHandler.text;
            var response = JsonUtility.FromJson<LinkResponse>(jsonResponse);

            Debug.Log($"Successfully linked! Email: {response.email}, In-Game ID: {response.inGameAccountID}");

            statusText.text = $"Signed in as: {response.email ?? "Facebook User"}\nIn-Game ID: {response.inGameAccountID}";
            incText.text = response.inGameCount.ToString("000");
            customJwtToken = response.jwtToken;

            ShowGamePanel();
        }
    }
    
    private void OnSignOutClicked()
    {
        statusText.text = "Signing out...";

        // GoogleSignIn.DefaultInstance.SignOut();
        // googleUser = null; 

        // if (FB.IsLoggedIn)
        // {
        //     FB.LogOut();
        // }
        
        customJwtToken = null;
        ShowStartPanel();
    }

    // --- UI Panel Helpers (Unchanged) ---
    private void ShowGamePanel()
    {
        gamePanel.SetActive(true);
        startPanel.SetActive(false);
        loginButtonsPanel.SetActive(false);
    }

    private void ShowStartPanel()
    {
        gamePanel.SetActive(false);
        startPanel.SetActive(true);
        loginButtonsPanel.SetActive(false);
    }
}