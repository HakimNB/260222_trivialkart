# LevelUp Multi-Login Server

This is the Node.js / Express backend authentication and verification server for the TrivialKart game. It verifies credentials from third-party identity providers (Google and Facebook) and generates custom JSON Web Tokens (JWT) for secure client-server sessions.

## Responsibilities

The server is responsible for:
- **Connection Checks**: Performs handshakes with the game client to verify client/server matching (`WEB_CLIENT_ID`).
- **Google Authentication (PGS v1 & v2)**:
  - Supports V1 sign-in by validating client-provided ID tokens.
  - Supports V2 sign-in by exchanging one-time authorization codes for Google access tokens, refreshing/exchanging tokens, fetching player IDs from the Google Games REST API (`/players/me`), and mapping them to in-game IDs.
  - Links and migrates legacy V1 Player IDs to modern Google IDs (GAIA `sub` identifier).
- **Facebook Authentication**: Validates client Facebook access tokens using the Facebook Graph API (`debug_token`) and maps Facebook user IDs to unique in-game accounts.
- **In-Game Database Tracking**: Maintains an in-memory database of game scores/counts mapped to internal in-game account IDs.
- **Session Management**: Signs custom, short-lived JWT tokens on successful verification, which the client must present to authorize state-modifying requests (like uploading scores).

## Codebase Structure

- **[server.js](server.js)**: Main server application code containing endpoint routing, Google and Facebook API integrations, in-memory data structures, and middleware for JWT validation.
- **[commands.txt](commands.txt)**: Deployment and testing reference commands list, detailing how to deploy the server to Google Cloud Run, set environment variables, and run `curl` test requests.

## Endpoints

### Unauthenticated Endpoints

- `POST /connection_check`
  - Validates client's `webClientId` configuration.
- `POST /verify_and_link_google`
  - Verifies a PGS v1 `idToken` and links/creates an in-game account based on `playerID`.
- `POST /exchange_authcode_and_link`
  - Exchanges a PGS v2 `authCode` for Google tokens, verifies the player ID, performs account migration if necessary, and returns a session JWT.
- `POST /exchange_authcode_for_tokens`
  - Exchanges a PGS v2 `authCode` for Google credentials (access, ID, and refresh tokens).
- `POST /verify_and_link_facebook`
  - Verifies a Facebook `accessToken` and links/creates a Facebook-prefixed in-game account.

### Authenticated Endpoints (Requires `Authorization: Bearer <JWT>`)

- `POST /post_count`
  - Updates the score/count for the authenticated player's in-game account.
- `POST /send_single_event`
  - Sends a game event statistics report directly to the Google Play Games services REST API for the player.

## Setup and Configuration

1. **Environment Variables**: Create a `.env` file (e.g., `env_staging.env` or `env_prod.env`) containing:
   ```env
   WEB_CLIENT_ID="<your-google-web-client-id>"
   WEB_CLIENT_SECRET="<your-google-web-client-secret>"
   FB_APP_ID="<your-facebook-app-id>"
   FB_APP_SECRET="<your-facebook-app-secret>"
   JWT_SECRET="<your-random-jwt-signing-secret>"
   SERVER_NAME="TrivialKart-Auth-Server"
   ```
2. **Run Locally**:
   ```bash
   npm install
   npm start
   ```
3. **Deploy to Google Cloud Run**:
   Refer to the commands in `commands.txt` for step-by-step instructions on enabling Google Cloud services, granting service account permissions, and executing the deployment command:
   ```bash
   gcloud run deploy trivialkart-auth-server --source . --allow-unauthenticated
   ```

## Getting Started for Developers

Follow these steps to spin up the server locally for development:

1. **Install Prerequisites**:
   - Install **Node.js** (v18 or higher is recommended).
2. **Install Dependencies**:
   - Run the following command in the server project root:
     ```bash
     npm install
     ```
3. **Environment Setup**:
   - Create a local configuration file named `env_staging.env` in the root of the project:
     ```env
     WEB_CLIENT_ID="your-google-web-client-id"
     WEB_CLIENT_SECRET="your-google-web-client-secret"
     FB_APP_ID="your-facebook-app-id"
     FB_APP_SECRET="your-facebook-app-secret"
     JWT_SECRET="local-development-secret-key-12345"
     SERVER_NAME="Local-TrivialKart-Auth-Server"
     ```
   - **Where to retrieve/generate these values**:
     - **`WEB_CLIENT_ID` & `WEB_CLIENT_SECRET`**:
       1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
       2. Select your Google Cloud project and navigate to **APIs & Services -> Credentials**.
       3. Under **OAuth 2.0 Client IDs**, select/create a client ID of type **Web application**.
       4. Copy the *Client ID* (for `WEB_CLIENT_ID`) and the *Client secret* (for `WEB_CLIENT_SECRET`).
       > [!NOTE]
       > The backend server requires a **Web Application** OAuth credential (not an Android OAuth credential) to successfully perform the OAuth `authCode` exchange for tokens.
     - **`FB_APP_ID` & `FB_APP_SECRET`**:
       1. Log into the [Meta for Developers Portal](https://developers.facebook.com/).
       2. Select your application, and go to **App Settings -> Basic**.
       3. Copy the *App ID* (for `FB_APP_ID`) and *App secret* (for `FB_APP_SECRET`).
     - **`JWT_SECRET`**:
       - A secure secret key used to sign and verify session JWTs. You can use any string for local testing, but for production, generate a secure random secret using:
         ```bash
         openssl rand -hex 32
         ```
     - **`SERVER_NAME`**:
       - An arbitrary string name used to identify this backend environment instance (e.g., `Local-TrivialKart-Auth-Server` or `Staging-TrivialKart-Auth-Server`).
4. **Run the Server**:
   - Run the start command:
     ```bash
     npm start
     ```
   - The server will start listening at `http://localhost:3000`.
5. **Testing Endpoints Locally**:
   - You can test connection health with a quick `curl` handshake:
     ```bash
     curl -X POST http://localhost:3000/connection_check \
       -H "Content-Type: application/json" \
       -d '{"webClientId": "your-mock-web-client-id"}'
     ```

## Deployment to Google Cloud Run

To deploy the authentication server to **Google Cloud Run**, follow these steps:

1. **Prerequisites & Login**:
   - Ensure the Google Cloud SDK (`gcloud`) is installed.
   - Authenticate with your Google account:
     ```bash
     gcloud auth login
     ```
2. **Configure Defaults**:
   - Set the default active project ID:
     ```bash
     gcloud config set project project-257ff62a-786a-4eec-aec
     ```
   - Set your preferred compute region:
     ```bash
     gcloud config set run/region us-central1
     ```
3. **Enable Cloud APIs**:
   - Enable the Cloud Run and Cloud Build APIs:
     ```bash
     gcloud services enable run.googleapis.com cloudbuild.googleapis.com
     ```
4. **Deploy**:
   - Deploy the source code directly. You can pass the required environment variables inline during deployment:
     ```bash
     gcloud run deploy trivialkart-auth-server \
       --source . \
       --allow-unauthenticated \
       --update-env-vars WEB_CLIENT_ID="<your-google-web-client-id>",WEB_CLIENT_SECRET="<your-google-web-client-secret>",FB_APP_ID="<your-facebook-app-id>",FB_APP_SECRET="<your-facebook-app-secret>",JWT_SECRET="<your-jwt-secret>",SERVER_NAME="TrivialKart-Auth-Server"
     ```
   - Once completed, the CLI output will print your live **Service URL** (e.g. `https://trivialkart-auth-server-181838787985.us-central1.run.app`).

---

## Detailed Testing Guide

### 1. Tail Server Logs in Real-time
To monitor live requests and debug issues, stream your service logs to the terminal *before* testing:
```bash
gcloud beta run services logs tail trivialkart-auth-server --region=us-central1 --project=project-257ff62a-786a-4eec-aec
```

### 2. Test Exchange Auth Code for Tokens
Once you obtain a PGS `authCode` from the Unity client, exchange it for access and refresh tokens:
```bash
curl -X POST https://<YOUR_SERVICE_URL>/exchange_authcode_for_tokens \
  -H "Content-Type: application/json" \
  -d '{
    "authCode": "YOUR_ONE_TIME_AUTH_CODE"
  }'
```
This returns the `playerID` along with an `accessToken` and `refreshToken`.

### 3. Test `gameStats:batchRecordEvents`
You can utilize the returned credentials to test game events reporting.

#### Option A: Direct Google API Request
Send the events directly to the Google Play Games Services endpoint:
```bash
curl -X POST "https://games.googleapis.com/games/v1/players/me/gameStats:batchRecordEvents" \
  -H "Authorization: Bearer <YOUR_ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "playerId": "<YOUR_PLAYER_ID>",
    "packageName": "com.WickedCube.TrivialKart",
    "requestTime": {
      "seconds": 1773994812,
      "nanos": 140000000
    },
    "events": [
      {
        "eventId": "0213c1c9-e1a1-42e3-94a4-b81ba5e94ba5",
        "eventName": "eName",
        "eventProperties": {
          "distance": {
            "intValue": "13"
          }
        },
        "eventTime": {
          "seconds": 1773994812,
          "nanos": 140000000
        }
      }
    ]
  }'
```

#### Option B: Request via Server's Proxy Endpoint
Test the server's event submission proxy wrapper (`/send_single_event`):
```bash
curl -X POST https://<YOUR_SERVICE_URL>/send_single_event \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <YOUR_ACCESS_TOKEN>" \
  -d '{
    "distance": 15.5,
    "playerId": "<YOUR_PLAYER_ID>"
  }'
```


