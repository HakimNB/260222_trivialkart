using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;

// #if PGS_V1 || PGS_V2
// using Facebook.Unity;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
// #endif

public class AuthManager : MonoBehaviour
{
// #if PGS_V1 || PGS_V2
    // --- UI REFERENCES ---
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
    
    // --- STATE VARIABLES ---
    private string customJwtToken;

// #if PGS_V2
    // V2 (CredMan) Specific Variables
    private volatile bool googleTaskComplete = false;
    private string authCodeToExchange = null;
    private string credManError = null;
// #endif

    public string serverUrl;
    public string webClientId;

    // --- ENDPOINTS ---
// #if PGS_V1
//     private const string verify_and_link_google = "http://192.168.0.101:3000/verify_and_link_google";
//     private const string verify_and_link_facebook = "http://192.168.0.101:3000/verify_and_link_facebook";
//     private const string post_count = "http://192.168.0.101:3000/post_count";
// #elif PGS_V2
    private string exchange_authcode_and_link;
    private string verify_and_link_facebook;
    private string post_count;
    private string connection_check_url;
// #endif

    // --- REQUEST/RESPONSE OBJECTS ---
    [System.Serializable]
    private class GoogleAuthRequest
    {
// #if PGS_V1
//         public string idToken;
//         public string playerID;
// #elif PGS_V2
        public string authCode;
// #endif
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

    private void Awake()
    {
        Debug.Log("AuthManager.Awake ");
        // --- 1. ENDPOINT SETUP ---
// #if PGS_V2
        exchange_authcode_and_link = serverUrl + "/exchange_authcode_and_link";
        verify_and_link_facebook = serverUrl + "/verify_and_link_facebook";
        post_count = serverUrl + "/post_count";
        connection_check_url = serverUrl + "/connection_check";
// #endif
        
        // --- 2. UI SETUP ---
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

        // --- 3. PLATFORM INITIALIZATION ---

// #if PGS_V1
//         // [RESTORED] V1 Initialization Logic
//         statusText.text = "Initializing PGS v1...";
//         var config = new PlayGamesClientConfiguration.Builder()
//             .RequestEmail()
//             .RequestIdToken() // Required for ID Token flow
//             .Build();

//         PlayGamesPlatform.InitializeInstance(config);
//         PlayGamesPlatform.DebugLogEnabled = true;
//         PlayGamesPlatform.Activate();
// #elif PGS_V2
        // V2 Initialization
        statusText.text = "Initializing...";
        PlayGamesPlatform.DebugLogEnabled = true;
// #endif

        // Facebook Init (Common)
        // if (!FB.IsInitialized) FB.Init(OnInitComplete, OnHideUnity);
        // else FB.ActivateApp();

        // --- 4. BUTTON LISTENERS ---
        getStartedButton.onClick.AddListener(GetStartedClicked);
        iAlreadyHaveButton.onClick.AddListener(IAlreadyHaveButtonClicked);
        signInWithGoogleButton.onClick.AddListener(OnSignInWithGoogleClicked);
        signInWithFacebookButton.onClick.AddListener(OnSignInWithFacebookClicked);
        signOutButton.onClick.AddListener(OnSignOutClicked);
        incButton.onClick.AddListener(OnIncButtonClicked);
        unlockAchievementButton.onClick.AddListener(OnAchievementUnlockButtonClicked);
        showAchievementButton.onClick.AddListener(OnShowAchievementsButtonClicked);

        // --- 5. STARTUP AUTH LOGIC ---
        statusText.text = "Checking credentials...";

// #if PGS_V1
//         // [RESTORED] V1 Silent Sign-In
//         PlayGamesPlatform.Instance.Authenticate(OnSilentSignInFinished, true);
// #elif PGS_V2
        // V2 Session Check / Silent CredMan
        // if (TryLoadSession())
        // {
        //     Debug.Log("Valid session found. Skipping CredMan.");
        //     ShowGamePanel();
        //     SignInToPlayGamesServices(); // Achievements only
        // }
        // else if (PlayerPrefs.GetInt("UserSignedOut", 0) == 0)
        // {
        //     Debug.Log("Attempting CredMan Silent Sign-In...");
        //     StartSignIn(false); // Silent Mode
        // }
        // else
        // {
        //     ShowStartPanel();
        // }
        // StartCoroutine(PostScore());
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
// #endif
    }
    
    internal void ProcessAuthentication(SignInStatus status) {
        if (status == SignInStatus.Success) {
            Debug.Log("AuthManager Authenticate Success");
            StartCoroutine(VerifyServerConnectionRoutine());
        }
    }

    private void SilentCredMan() {
        // V2 Session Check / Silent CredMan
        if (TryLoadSession())
        {
            Debug.Log("Valid session found. Skipping CredMan.");
            ShowGamePanel();
            SignInToPlayGamesServices(); // Achievements only
        }
        else if (PlayerPrefs.GetInt("UserSignedOut", 0) == 0)
        {
            Debug.Log("Attempting CredMan Silent Sign-In...");
            StartSignIn(false); // Silent Mode
        }
        else
        {
            ShowStartPanel();
        }
        //StartCoroutine(PostScore());
    }

    private IEnumerator VerifyServerConnectionRoutine()
    {
        Debug.Log("AuthManager.VerifyServerConnectionRoutine ");
        Debug.Log($"<color=yellow>[AuthManager]</color> Testing connection to: {serverUrl}");
        
        // We send our local webClientId so the server can warn us if there's a mismatch
        string jsonPayload = $"{{\"webClientId\":\"{webClientId}\"}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(connection_check_url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("AuthManager.VerifyServerConnectionRoutine Connection Failed: " + request.error);
            Debug.LogError($"<color=red>[AuthManager]</color> Connection Failed: {request.error}");
            statusText.text = "Server Offline";
        }
        else
        {
            // Parse a simple response from the server
            var response = JsonUtility.FromJson<ConnectionResponse>(request.downloadHandler.text);
            
            Debug.Log($"<color=cyan>[AuthManager]</color> Connected to Server: <b>{response.serverName}</b>");
            Debug.Log($"<color=cyan>[AuthManager]</color> Server Status: {response.status}");
            
            if (response.webClientIdMatch) {
                statusText.text = $"Connected to {response.serverName} status: {response.status}";
                // SilentCredMan(); 
                ShowGamePanel();
            } else {
                Debug.LogWarning("<color=orange>[AuthManager]</color> Warning: webClientId mismatch between Client and Server!");
                statusText.text = "Config Mismatch Detected";
            }
        }
    }

    [System.Serializable]
    private class ConnectionResponse {
        public string serverName;
        public string status;
        public bool webClientIdMatch;
    }
    
    // --- MAIN UPDATE LOOP ---
    private void Update()
    {
        // Debug.Log("AuthManager.Update ");
// #if PGS_V2
        // V2 Main Thread Dispatcher
        if (googleTaskComplete)
        {
            Debug.Log("AuthManager.Update googleTaskComplete");
            googleTaskComplete = false;
            if (!string.IsNullOrEmpty(credManError))
            {
                Debug.LogError("CredMan Error: " + credManError);
                statusText.text = "Sign-in Failed.";
                ShowStartPanel();
            }
            else if (!string.IsNullOrEmpty(authCodeToExchange))
            {
                Debug.Log("Got Auth Code. Exchanging...");
                statusText.text = "Connecting to server...";
                StartCoroutine(ExchangeAuthcodeAndLink(authCodeToExchange));
            }
            credManError = null;
            authCodeToExchange = null;
        }
// #endif
    }

    // ========================================================================
    //                          PGS V1 LOGIC [RESTORED]
    // ========================================================================
// #if PGS_V1
//     private void OnSilentSignInFinished(bool success)
//     {
//         Debug.Log("AuthManager.OnSilentSignInFinished success:" + success);
//         if (success)
//         {
//             Debug.Log("PGS Silent sign-in successful. Verifying...");
//             statusText.text = "Verifying with server...";
//             ProcessAuthenticationResult(true);
//         }
//         else
//         {
//             Debug.Log("PGS Silent sign-in failed. Showing start panel.");
//             statusText.text = "Please sign in.";
//             ShowStartPanel();
//         }
//     }
    
//     private void ProcessAuthenticationResult(bool success)
//     {
//         Debug.Log("AuthManager.ProcessAuthenticationResult success:" + success);
//         if (success)
//         {
//             statusText.text = "Success! Getting ID Token...";
//             string idToken = PlayGamesPlatform.Instance.GetIdToken();
//             string playerID = PlayGamesPlatform.Instance.GetUserId();

//             if (!string.IsNullOrEmpty(idToken))
//             {
//                 StartCoroutine(VerifyAndLinkGoogleAccount(idToken, playerID));
//             }
//             else
//             {
//                 Debug.LogError("Failed to get ID Token.");
//                 statusText.text = "Failed to get ID Token.";
//                 ShowStartPanel();
//             }
//         }
//         else
//         {
//             Debug.LogError("PGS Sign-in failed/cancelled.");
//             statusText.text = "Sign-in failed.";
//             ShowStartPanel();
//         }
//     }
    
//     private IEnumerator VerifyAndLinkGoogleAccount(string idToken, string playerID)
//     {
//         Debug.Log("AuthManager.VerifyAndLinkGoogleAccount idToken:" + idToken + " playerID:" + playerID);
//         GoogleAuthRequest requestData = new GoogleAuthRequest { idToken = idToken, playerID = playerID };
//         byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));

//         UnityWebRequest request = new UnityWebRequest(verify_and_link_google, "POST");
//         request.uploadHandler = new UploadHandlerRaw(bodyRaw);
//         request.downloadHandler = new DownloadHandlerBuffer();
//         request.SetRequestHeader("Content-Type", "application/json");

//         yield return request.SendWebRequest();

//         if (request.result != UnityWebRequest.Result.Success)
//         {
//             Debug.LogError($"Error: {request.error}");
//             statusText.text = "Server Link Failed.";
//             ShowStartPanel();
//         }
//         else
//         {
//             var response = JsonUtility.FromJson<LinkResponse>(request.downloadHandler.text);
//             statusText.text = $"Signed in as: {response.email}";
//             incText.text = response.inGameCount.ToString("000");
//             customJwtToken = response.jwtToken;
//             ShowGamePanel();
//         }
//     }
// #endif

    // ========================================================================
    //                          PGS V2 LOGIC (CredMan + Caching)
    // ========================================================================
// #if PGS_V2
    private bool TryLoadSession()
    {
        Debug.Log("AuthManager.TryLoadSession ");
        string token = PlayerPrefs.GetString("Cached_JWT", null);
        if (string.IsNullOrEmpty(token)) return false;

        this.customJwtToken = token;
        string email = PlayerPrefs.GetString("Cached_Email", "");
        int count = PlayerPrefs.GetInt("Cached_Count", 0);

        statusText.text = $"Signed in as: {email}";
        incText.text = count.ToString("000");
        return true;
    }

    private void SaveSession(LinkResponse data)
    {
        Debug.Log("AuthManager.SaveSession data:" + data);
        PlayerPrefs.SetString("Cached_JWT", data.jwtToken);
        PlayerPrefs.SetString("Cached_Email", data.email);
        PlayerPrefs.SetString("Cached_ID", data.inGameAccountID);
        PlayerPrefs.SetInt("Cached_Count", data.inGameCount);
        PlayerPrefs.SetInt("UserSignedOut", 0);
        PlayerPrefs.Save();
        this.customJwtToken = data.jwtToken;
    }

    private void ClearSession()
    {
        Debug.Log("AuthManager.ClearSession ");
        PlayerPrefs.DeleteKey("Cached_JWT");
        PlayerPrefs.DeleteKey("Cached_Email");
        PlayerPrefs.DeleteKey("Cached_ID");
        PlayerPrefs.DeleteKey("Cached_Count");
        PlayerPrefs.SetInt("UserSignedOut", 1);
        PlayerPrefs.Save();
        this.customJwtToken = null;
    }

    public void StartSignIn(bool interactive)
    {
        Debug.Log("AuthManager.StartSignIn interactive:" + interactive);
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaClass bridge = new AndroidJavaClass("com.wickedcube.trivialkart.CredManBridge");
        
        string methodName = interactive ? "signInInteractive" : "signInSilent";
        bridge.CallStatic(methodName, currentActivity, webClientId);
    }
    
    public void OnSignInSuccess(string token) { Debug.Log("AuthManager.OnSignInSuccess token:" + token); authCodeToExchange = token; googleTaskComplete = true; }
    public void OnSignInError(string error) 
    { 
        Debug.Log("AuthManager.OnSignInError error:" + error);
        if (error == "SilentFailed") { Debug.Log("AuthManager.Silent failed. Idle."); return; }
        credManError = error; googleTaskComplete = true; 
    }

    private IEnumerator ExchangeAuthcodeAndLink(string serverAuthCode)
    {
        Debug.Log("AuthManager.ExchangeAuthcodeAndLink serverAuthCode:" + serverAuthCode);
        GoogleAuthRequest requestData = new GoogleAuthRequest { authCode = serverAuthCode };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));

        UnityWebRequest request = new UnityWebRequest(exchange_authcode_and_link, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error: {request.error}");
            statusText.text = "Server Link Failed.";
            ShowStartPanel();
        }
        else
        {
            var response = JsonUtility.FromJson<LinkResponse>(request.downloadHandler.text);
            SaveSession(response);
            statusText.text = $"Signed in as: {response.email}";
            incText.text = response.inGameCount.ToString("000");
            ShowGamePanel();
            SignInToPlayGamesServices();
        }
    }
    
    private void SignInToPlayGamesServices()
    {
        Debug.Log("AuthManager.SignInToPlayGamesServices ");
        PlayGamesPlatform.Instance.Authenticate((SignInStatus status) => { Debug.Log("PGS Auth: " + status); });
    }
// #endif

    // ========================================================================
    //                          COMMON / UI HANDLERS
    // ========================================================================
    private void GetStartedClicked()
    {
        Debug.Log("AuthManager.GetStartedClicked ");
        statusText.text = "Signing in...";
// #if PGS_V1
//         PlayGamesPlatform.Instance.Authenticate(ProcessAuthenticationResult, false);
// #elif PGS_V2
        StartSignIn(true);
// #endif
    }
    
    private void OnSignInWithGoogleClicked()
    {
        Debug.Log("AuthManager.OnSignInWithGoogleClicked ");
        statusText.text = "Signing in with Google...";
        loginButtonsPanel.SetActive(false);
// #if PGS_V1
//         PlayGamesPlatform.Instance.Authenticate(ProcessAuthenticationResult, false);
// #elif PGS_V2
        StartSignIn(true);
// #endif
    }

    private void OnSignOutClicked()
    {
        Debug.Log("AuthManager.OnSignOutClicked ");
        statusText.text = "Signing out...";
// #if PGS_V1
//         if (PlayGamesPlatform.Instance.IsAuthenticated()) PlayGamesPlatform.Instance.SignOut();
// #elif PGS_V2
        ClearSession();
// #endif
        // if (FB.IsLoggedIn) FB.LogOut();
        customJwtToken = null;
        ShowStartPanel();
    }
    
    private void OnIncButtonClicked()
    {
        Debug.Log("AuthManager.OnIncButtonClicked ");
        int curr = 0;
        int.TryParse(incText.text, out curr);
        curr++;
        incText.text = curr.ToString("000");
        StartCoroutine(PostScore());
    }
    
    // --- SERVER (COMMON) ---
    private IEnumerator PostScore()
    {
        Debug.Log("AuthManager.PostScore ");
        if (string.IsNullOrEmpty(customJwtToken)) yield break;
        PostCountRequest requestData = new PostCountRequest { count = int.Parse(incText.text) };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));

        UnityWebRequest request = new UnityWebRequest(post_count, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + this.customJwtToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 401 || request.responseCode == 403)
            {
                statusText.text = "Session expired.";
// #if PGS_V2
                ClearSession();
// #endif
                ShowStartPanel();
            }
        }
        else
        {
            var response = JsonUtility.FromJson<LinkResponse>(request.downloadHandler.text);
            incText.text = response.inGameCount.ToString("000");
// #if PGS_V2
            PlayerPrefs.SetInt("Cached_Count", response.inGameCount);
// #endif
        }
    }

    // --- FACEBOOK (COMMON) ---
    private void OnSignInWithFacebookClicked()
    {
        Debug.Log("AuthManager.OnSignInWithFacebookClicked ");
        // if (!FB.IsInitialized) { FB.Init(OnInitComplete, OnHideUnity); return; }
        // loginButtonsPanel.SetActive(false);
        // FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email" }, OnFacebookLoginComplete);
    }
    // private void OnFacebookLoginComplete(ILoginResult result) 
    // {
    //     if (FB.IsLoggedIn) StartCoroutine(VerifyAndLinkFacebookAccount(AccessToken.CurrentAccessToken.TokenString));
    //     else ShowStartPanel();
    // }
    private IEnumerator VerifyAndLinkFacebookAccount(string accessToken)
    {
        Debug.Log("AuthManager.VerifyAndLinkFacebookAccount accessToken:" + accessToken);
        FacebookAuthRequest requestData = new FacebookAuthRequest { accessToken = accessToken };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData));
        UnityWebRequest request = new UnityWebRequest(verify_and_link_facebook, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<LinkResponse>(request.downloadHandler.text);
// #if PGS_V2
            SaveSession(response);
// #endif
            customJwtToken = response.jwtToken;
            statusText.text = $"Signed in as: {response.email}";
            incText.text = response.inGameCount.ToString("000");
            ShowGamePanel();
        }
        else
        {
            statusText.text = "Facebook Link Failed.";
            ShowStartPanel();
        }
    }

    // --- UTILS ---
    private void OnInitComplete() { Debug.Log("AuthManager.OnInitComplete "); //if (FB.IsInitialized) FB.ActivateApp();
    }
    private void OnHideUnity(bool isGameShown) { Debug.Log("AuthManager.OnHideUnity isGameShown:" + isGameShown); Time.timeScale = isGameShown ? 1 : 0; }
    private void IAlreadyHaveButtonClicked() { Debug.Log("AuthManager.IAlreadyHaveButtonClicked "); startPanel.SetActive(false); loginButtonsPanel.SetActive(true); }
    private void ShowGamePanel() { Debug.Log("AuthManager.ShowGamePanel "); gamePanel.SetActive(true); startPanel.SetActive(false); loginButtonsPanel.SetActive(false); }
    private void ShowStartPanel() { Debug.Log("AuthManager.ShowStartPanel "); gamePanel.SetActive(false); startPanel.SetActive(true); loginButtonsPanel.SetActive(false); }
    private void OnShowAchievementsButtonClicked() { Debug.Log("AuthManager.OnShowAchievementsButtonClicked "); PlayGamesPlatform.Instance.ShowAchievementsUI(); }
    private void OnAchievementUnlockButtonClicked() {
        Debug.Log("AuthManager.OnAchievementUnlockButtonClicked isAuthenticated: " + PlayGamesPlatform.Instance.IsAuthenticated());
        if (PlayGamesPlatform.Instance.IsAuthenticated())
            PlayGamesPlatform.Instance.ReportProgress(GPGSIds.achievement_tk_achievement_rand, 100f, (bool s) => {
                Debug.Log("AuthManager.OnAchievementUnlockButtonClicked s:" + s);
            });
    }
// #endif
}