using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SignInPanel : MonoBehaviour
{
    public Button btnSignIn;
    public Button btnRequestServerSideAccess;
    public Button btnAchievement;

    public TextMeshProUGUI textPlayerId;
    public TextMeshProUGUI textAuthCode;
    public TextMeshProUGUI textAccessToken;
    public TextMeshProUGUI textRefreshToken;

    public void callback_signInSuccess(string serverAuthCode) {
        textAuthCode.text = "Auth Code: " + serverAuthCode;
        textPlayerId.text = "Exchanging auth code for tokens...";
        
        // Next step: Exchange Auth Code for Access / Refresh Tokens
        AuthManager.GetInstance().PGS_ExchangeAuthCode(serverAuthCode, callback_exchangeSuccess, callback_exchangeFailure);
    }

    public void callback_signInFailure(string error) {
        textPlayerId.text = "Sign In Error: " + error;
        textAuthCode.text = "";
        textAccessToken.text = "";
        textRefreshToken.text = "";
    }

    public void callback_requestServerSideAccess(string authCode) {
        textAuthCode.text = "Requesting With Auth Code: " + authCode;
        textPlayerId.text = "Request Server Side Access...";
        
        // Next step: Exchange Auth Code for Access / Refresh Tokens
        AuthManager.GetInstance().PGS_ExchangeAuthCode(authCode, callback_exchangeSuccess, callback_exchangeFailure);
    }

    public void callback_exchangeSuccess(string playerId) {
        textPlayerId.text = "Player ID: " + playerId;
        textAccessToken.text = "Access Token: " + AuthManager.GetInstance().PgsAccessToken;
        textRefreshToken.text = "Refresh Token: " + AuthManager.GetInstance().PgsRefreshToken;
    }

    public void callback_exchangeFailure(string error) {
        textPlayerId.text = "Exchange Error: " + error;
    }

    public void BtnClick_ReturnToBase() {
        UnityEngine.SceneManagement.SceneManager.LoadScene("BaseScene");
    }
    
    public void BtnClick_SignIn() {
        textPlayerId.text = "Signing in...";
        textAuthCode.text = "";
        textAccessToken.text = "";
        textRefreshToken.text = "";
        AuthManager.GetInstance().PGS_SignIn(callback_signInSuccess, callback_signInFailure);
    }

    public void BtnClick_RequestServerSideAccess() {
        AuthManager.GetInstance().PGS_RequestServerSideAccess(callback_requestServerSideAccess);
    }

    public void BtnClick_Achievement() {
        // Only works if the user is authenticated in Play Games Platform
        AuthManager.GetInstance().PGS_ShowAchievementUI_Delay();

    }

    public void BtnClick_ClientSingle() {
        float distance = PlayerDataManager.GetInstance().Distance;
        Debug.Log("SignInPanel.BtnClick_ClientSingle: " + distance);
        PGSGameStatsManager.GetInstance().GSAPI_ClientSingleEvent(distance);
    }

    public void BtnClick_ClientMulti() {
        float distance = PlayerDataManager.GetInstance().Distance;
        int coins = (int)PlayerDataManager.GetInstance().Fuel;
        bool sedanUnlocked = true;
        Debug.Log("SignInPanel.BtnClick_ClientMulti: " + distance + ", " + coins + ", " + sedanUnlocked);
        PGSGameStatsManager.GetInstance().GSAPI_ClientMultiEvent(distance, coins, sedanUnlocked);
    }

    public void BtnClick_ServerSingle() {
        float distance = PlayerDataManager.GetInstance().Distance;
        Debug.Log("SignInPanel.BtnClick_ServerSingle: " + distance);
        PGSGameStatsManager.GetInstance().GSAPI_ServerSingleEvent(distance);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bool isAuthenticated = AuthManager.GetInstance().PGS_IsAuthenticated();
        Debug.Log("SignInPanel.Start PGS_IsAuthenticated: " + isAuthenticated);
        if (isAuthenticated) 
        {
            // Load the player ID natively via the client SDK if already authenticated
            string localPlayerId = AuthManager.GetInstance().PGS_GetLocalPlayerId();
            textPlayerId.text = "Player ID: " + localPlayerId;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
