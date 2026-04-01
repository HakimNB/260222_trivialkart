# Step-by-Step Guide: Deploying `trivialkart-auth-server` to Google Cloud Run

This guide will walk you through deploying your authentication server to Google Cloud Run, enabling testing from any device.

## Prerequisites

1.  **Google Cloud Platform (GCP) Account**: Access to a GCP project.
2.  **Google Cloud CLI (`gcloud`)**: Installed on your local machine (where you run this command).
3.  **Authentication**: Authenticated with your GCP account via `gcloud auth login`.

---

## Step 1: Initialize Project and Region

Open your local terminal (not the workspace terminal) and set your project ID:

```bash
# Set your GCP Project ID
gcloud config set project [YOUR_PROJECT_ID]

# Set your default region (e.g., us-central1)
gcloud config set run/region us-central1
```

Replace `[YOUR_PROJECT_ID]` with your actual project ID.

---

## Step 2: Enable Required APIs

Run the following command to enable **Cloud Run** and **Cloud Build** in your project:

```bash
gcloud services enable run.googleapis.com cloudbuild.googleapis.com
```

---

## Step 3: Deploy to Cloud Run

We can use `gcloud run deploy` to build and deploy your application. Cloud Run will use Google Buildpacks to automatically detect and build your Node environment.

### Option A: Standard Deploy (with prompts)

Run this command in the `trivialkart-auth-server` directory on your local machine:

```bash
gcloud run deploy trivialkart-auth-server \
  --source . \
  --allow-unauthenticated
```

- `--source .`: Tells Cloud Run to build the container from the current directory.
- `--allow-unauthenticated`: Makes the endpoint public so your Unity client can reach it.

### Option B: Non-Interactive Deploy (with Environment Variables)

If you want to configure environment variables inline, use:

```bash
gcloud run deploy trivialkart-auth-server \
  --source . \
  --allow-unauthenticated \
  --set-env-vars="WEB_CLIENT_ID=[YOUR_WEB_CLIENT_ID],WEB_CLIENT_SECRET=[YOUR_WEB_CLIENT_SECRET],FB_APP_ID=[YOUR_FB_APP_ID],FB_APP_SECRET=[YOUR_FB_APP_SECRET],JWT_SECRET=[YOUR_JWT_SECRET],NODE_ENV=production"
```

> [!CAUTION]
> Hardcoding secrets in commands is not recommended for production. For production, consider using Google Secret Manager or setting them via the GCP Console UI.

---

## Troubleshooting: Permission Errors & Sandbox Restrictions

If your GCP environment is restricted, you might encounter `Permission Denied` or `Invalid Build Request` errors during `gcloud run deploy`. Run these commands in your local terminal to grant the Cloud Build system the necessary privileges:

### 1. Fix Logs Writing
If you see errors about "does not have permission to write logs":
```bash
gcloud projects add-iam-policy-binding [YOUR_PROJECT_ID] \
  --member="serviceAccount:[PROJECT_NUMBER]-compute@developer.gserviceaccount.com" \
  --role="roles/logging.logWriter"
```

### 2. Fix Container Registry Read/Write
If you see errors about `storage.objects.get` or `artifactregistry.repositories.uploadArtifacts` denied:
```bash
gcloud projects add-iam-policy-binding [YOUR_PROJECT_ID] \
  --member="serviceAccount:[PROJECT_NUMBER]-compute@developer.gserviceaccount.com" \
  --role="roles/artifactregistry.writer"
```

### 3. View Build Logs via CLI
If the browser console logs are empty or blocked:
```bash
gcloud builds log [BUILD_ID] --region=us-central1
```

---

## Step 4: Verify the Deployment

Once the deployment finishes, the terminal will output a **Service URL** (e.g., `https://trivialkart-auth-server-abcde-uc.a.run.app`).

You can test if the server is running by sending a `POST` request to the health check endpoint using `curl`:

```bash
curl -X POST https://[YOUR_SERVICE_URL]/connection_check \
  -H "Content-Type: application/json" \
  -d '{"webClientId": "[YOUR_WEB_CLIENT_ID]"}'
```

If successful, it will return the JSON response `{"serverName": "TrivialKart Auth Server", "status": "Online", "webClientIdMatch": true}`.

---

## Step 5: Update Unity Client

Finally, update the `ServerUrl` field in your Unity client's `AuthManager` Inspector to point to this new HTTPS URL!
