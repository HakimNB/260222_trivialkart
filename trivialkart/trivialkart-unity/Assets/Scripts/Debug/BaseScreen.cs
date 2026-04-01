using UnityEngine;
using TMPro;

public class BaseScreen : MonoBehaviour
{
    public AuthManager authManager;
    public PlayerDataManager playerDataManager;

    public TextMeshProUGUI textFuel;
    public TextMeshProUGUI textDistance;
    public TextMeshProUGUI textAccessToken;
    public TextMeshProUGUI textRefreshToken;

    void Awake() {
        if ( authManager == null ) {
            authManager = Object.FindFirstObjectByType<AuthManager>();
        }
        if ( playerDataManager == null ) {
            playerDataManager = Object.FindFirstObjectByType<PlayerDataManager>();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        RefreshUI();
    }

    public void RefreshUI() {
        textFuel.text = "Fuel: " + playerDataManager.Fuel;
        textDistance.text = "Distance: " + playerDataManager.Distance;
        textAccessToken.text = "Access Token: " + authManager.PgsAccessToken;
        textRefreshToken.text = "Refresh Token: " + authManager.PgsRefreshToken;
    }

    public void BtnClick_GoToDebug() {
        UnityEngine.SceneManagement.SceneManager.LoadScene("DebugScene");
    }

    public void BtnClick_IncFuel() {
        playerDataManager.Fuel += 10;
    }

    public void BtnClick_DecFuel() {
        playerDataManager.Fuel -= 10;
    }
}
