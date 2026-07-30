# TrivialKart Unity Client

This is the Unity client project for **TrivialKart**, a sample racing/driving game that demonstrates the integration of Google Play Games Services (PGS), Unity In-App Purchases (IAP), Play Integrity, and account linking features.

## Responsibilities

The Unity project is responsible for:
- **Game Mechanics & Rendering**: Renders the game scenes, handles user input (car driving/tapping, screen navigation), and implements the core gameplay loop (vehicle choice, consuming gas, store purchases).
- **Authentication**: Integrates with the Google Play Games Services (PGS) SDK and Facebook SDK to sign in users.
- **Backend Communication (Account Linking & Verification)**: Sends third-party credentials (like PGS auth codes/ID tokens or Facebook access tokens) to the game's authentication backend (`LevelUp_MultiLogin_Server`) to obtain a secure JWT token, establish an in-game session, and link accounts.
- **In-App Purchases (IAP)**: Demonstrates buying in-game currency (coins) and unlocking items/cars using the Unity IAP package.
- **Play Integrity**: Queries device integrity signals and Play license status.
- **Input SDK**: Integrates keyboard controls support for Google Play Games on PC.

## Codebase Structure

- **[Assets/Scripts/AuthManager.cs](Assets/Scripts/AuthManager.cs)**: Central component managing user sign-in flows (Google PGS V1 & V2, Facebook), account linking/migration on the backend server, and maintaining session state (`jwtToken`).
- **[Assets/Scripts/Controller/PGS/](Assets/Scripts/Controller/PGS/)**: Modules handling Google Play Games Services specific features:
  - Achievements (`PGSAchievementManager.cs`, `AchievementsPageController.cs`)
  - Cloud Saves (`PGSCloudSaveManager.cs`)
  - Game Stats (`PGSGameStatsManager.cs`)
  - Recall API (`PGSRecallManager.cs` for player retrieval/migration)
- **[Assets/Scenes/](Assets/Scenes/)**:
  - `MockSignIn.unity`: Scene for testing multi-login/authentication flows.
  - `TrivialKartScene.unity`: Main game scene containing gameplay and store features.

## Setup and Configuration

1. **Platform Settings**:
   - Open the project in Unity and switch the build target to **Android** (`File -> Build Profiles...`).
   - Enter your unique package identifier in `Player Settings -> Other Settings -> Package Name`.
   - Setup app signing (e.g. upload key keystore) under `Player Settings -> Publishing Settings`.

2. **Backend Authentication Server Configuration**:
   - Ensure the `LevelUp_MultiLogin_Server` is running (e.g. locally on Node or deployed on Google Cloud Run).
   - In Unity, select the GameObject holding the `AuthManager` component (typically in `MockSignIn` or `TrivialKartScene`).
   - Configure the following fields in the Unity Inspector:
     - **Server Url**: The address of the authentication server (e.g., `http://<server-ip>:3000` or the Cloud Run URL).
     - **Web Client Id**: The Web Client ID from your Google Cloud Console credential settings.

3. **Google Play Games Services (PGS) Setup**:
   - Import your Play Games Android XML resources under `Window -> Google Play Games -> Setup -> Android Setup...`.
   - Toggle PGS features on via `TrivialKart -> BuildOptions -> Build with Google Play Games Services`.

4. **In-App Purchases (IAP) Setup**:
   - Toggle IAP on via `TrivialKart -> BuildOptions -> Build with IAP`.
   - Paste the Google Play licensing key under `Services -> Unity IAP -> Receipt Validation Obfuscation`.

## Getting Started for Developers

Follow these steps to get up and running quickly:

1. **Open in Unity**:
   - Open Unity Hub, click **Add**, and select this `trivialkart-unity` folder.
   - Use **Unity 6.0 LTS** or newer to open the project.
2. **Switch Platform**:
   - Go to `File -> Build Profiles...` (or `File -> Build Settings...`).
   - Select **Android** and click **Switch Platform**.
3. **Run Backend Server**:
   - Set up and run the `LevelUp_MultiLogin_Server` locally (refer to its [README.md](../LevelUp_MultiLogin_Server/README.md) for instructions).
4. **Configure Local Host URL**:
   - Open `Assets/Scenes/MockSignIn.unity`.
   - Select the `AuthManager` GameObject in the Hierarchy.
   - In the Inspector, update **Server Url** to point to your local backend server:
     - **`http://localhost:3000`**: If running in a WebGL / Editor build directly.
     - **`http://10.0.2.2:3000`**: If testing in a standard Android Emulator (10.0.2.2 points to host machine).
     - **`http://<your-host-ip>:3000`**: If testing on a physical Android device on the same local network.
5. **Enter Play Mode**:
   - Press the **Play** button in the Unity Editor to run, test UI clicks, and check the network handshake output in the console.

