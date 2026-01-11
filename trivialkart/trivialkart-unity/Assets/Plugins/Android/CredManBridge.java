package com.wickedcube.trivialkart;

import android.accounts.Account;
import android.content.Context;
import android.util.Log;
import android.os.CancellationSignal;

import androidx.credentials.CredentialManager;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.exceptions.NoCredentialException;

import com.google.android.libraries.identity.googleid.GetGoogleIdOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

import com.google.android.gms.auth.api.identity.AuthorizationClient;
import com.google.android.gms.auth.api.identity.AuthorizationRequest;
import com.google.android.gms.auth.api.identity.Identity;
import com.google.android.gms.common.api.Scope;

import com.unity3d.player.UnityPlayer;

import java.util.Collections;
import java.util.List;
import java.util.concurrent.Executor;
import java.util.concurrent.Executors;

public class CredManBridge {

    // --- MODE 1: SILENT SIGN-IN (Called on Awake) ---
    // Tries to auto-select an authorized account. If it fails, it does NOT show UI.
    public static void signInSilent(Context context, String webClientId) {
        CredentialManager credentialManager = CredentialManager.create(context);
        CancellationSignal cancellationSignal = new CancellationSignal();
        Executor executor = Executors.newSingleThreadExecutor();

        Log.d("CredMan", "Attempting Silent Sign-In...");

        GetGoogleIdOption silentOption = new GetGoogleIdOption.Builder()
            .setFilterByAuthorizedAccounts(true) // Strict: Only authorized accounts
            .setServerClientId(webClientId)
            .setAutoSelectEnabled(true)          // Auto-select if possible
            .build();

        GetCredentialRequest silentRequest = new GetCredentialRequest.Builder()
            .addCredentialOption(silentOption)
            .build();

        credentialManager.getCredentialAsync(
            context,
            silentRequest,
            cancellationSignal,
            executor,
            new androidx.credentials.CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                @Override
                public void onResult(GetCredentialResponse result) {
                    Log.d("CredMan", "Silent Sign-In Successful!");
                    handleSignInResult(context, result, webClientId);
                }

                @Override
                public void onError(GetCredentialException e) {
                    // Send a specific error code so Unity knows to just stay on the Start Screen
                    Log.d("CredMan", "Silent sign-in failed. Keeping UI hidden.");
                    UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", "SilentFailed");
                }
            }
        );
    }

    // --- MODE 2: INTERACTIVE SIGN-IN (Called on Button Click) ---
    // Forces the Account Selection / "Add Account" sheet to appear.
    public static void signInInteractive(Context context, String webClientId) {
        CredentialManager credentialManager = CredentialManager.create(context);
        CancellationSignal cancellationSignal = new CancellationSignal();
        Executor executor = Executors.newSingleThreadExecutor();

        Log.d("CredMan", "Starting Interactive Sign-In...");

        GetGoogleIdOption interactiveOption = new GetGoogleIdOption.Builder()
            .setFilterByAuthorizedAccounts(false) // Show ALL accounts (and "Add Account")
            .setServerClientId(webClientId)
            .setAutoSelectEnabled(false)          // Force the UI to show
            .build();

        GetCredentialRequest interactiveRequest = new GetCredentialRequest.Builder()
            .addCredentialOption(interactiveOption)
            .build();

        credentialManager.getCredentialAsync(
            context,
            interactiveRequest,
            cancellationSignal,
            executor,
            new androidx.credentials.CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                @Override
                public void onResult(GetCredentialResponse result) {
                    Log.d("CredMan", "Interactive Sign-In Successful!");
                    handleSignInResult(context, result, webClientId);
                }

                @Override
                public void onError(GetCredentialException e) {
                    Log.e("CredMan", "Interactive Sign-In Canceled or Failed", e);
                    UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", "Canceled");
                }
            }
        );
    }

    // --- SHARED: Process Result & Authorize ---
    private static void handleSignInResult(Context context, GetCredentialResponse result, String webClientId) {
        try {
            GoogleIdTokenCredential credential = GoogleIdTokenCredential.createFrom(result.getCredential().getData());
            String email = credential.getId();
            
            Account account = new Account(email, "com.google");
            List<Scope> requestedScopes = Collections.singletonList(new Scope("https://www.googleapis.com/auth/games_lite"));
            
            AuthorizationRequest authRequest = new AuthorizationRequest.Builder()
                .setRequestedScopes(requestedScopes)
                .setAccount(account)
                .requestOfflineAccess(webClientId)
                .build();

            AuthorizationClient authClient = Identity.getAuthorizationClient(context);
            
            authClient.authorize(authRequest)
                .addOnSuccessListener(authorizationResult -> {
                    if (authorizationResult.getServerAuthCode() != null) {
                        UnityPlayer.UnitySendMessage("AuthManager", "OnSignInSuccess", authorizationResult.getServerAuthCode());
                    } else {
                        UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", "No Auth Code returned");
                    }
                })
                .addOnFailureListener(e -> {
                    UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", "Authorization Failed: " + e.getMessage());
                });

        } catch (Exception e) {
            UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", "Parsing Error: " + e.getMessage());
        }
    }
}