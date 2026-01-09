package com.wickedcube.trivialkart;

import android.accounts.Account;
import android.content.Context;
import android.util.Log;

// Credential Manager Imports
import androidx.credentials.CredentialManager;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;
import android.os.CancellationSignal;

// Google ID Token Import
import com.google.android.libraries.identity.googleid.GetGoogleIdOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

// Authorization Client Imports
import com.google.android.gms.auth.api.identity.AuthorizationClient;
import com.google.android.gms.auth.api.identity.AuthorizationRequest;
import com.google.android.gms.auth.api.identity.AuthorizationResult;
import com.google.android.gms.auth.api.identity.Identity;
import com.google.android.gms.common.api.Scope;
import com.google.android.gms.common.api.ApiException;

import com.unity3d.player.UnityPlayer;

import java.util.Collections;
import java.util.List;
import java.util.concurrent.Executor;
import java.util.concurrent.Executors;

public class CredManBridge {

    public static void signIn(Context context, String webClientId) {
        CredentialManager credentialManager = CredentialManager.create(context);

        // 1. Configure Request for CredMan (to get the Account/Email)
        GetGoogleIdOption googleIdOption = new GetGoogleIdOption.Builder()
            .setFilterByAuthorizedAccounts(false)
            .setServerClientId(webClientId)
            .setAutoSelectEnabled(false)
            .build();

        GetCredentialRequest request = new GetCredentialRequest.Builder()
            .addCredentialOption(googleIdOption)
            .build();

        CancellationSignal cancellationSignal = new CancellationSignal();
        Executor executor = Executors.newSingleThreadExecutor();

        Log.d("CredMan", "Starting CredMan Sign-In...");

        credentialManager.getCredentialAsync(
            context,
            request,
            cancellationSignal,
            executor,
            new androidx.credentials.CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                @Override
                public void onResult(GetCredentialResponse result) {
                    try {
                        Log.d("CredMan", "CredMan Success. Parsing result...");

                        // 2. Extract Email from CredMan result
                        GoogleIdTokenCredential credential = GoogleIdTokenCredential.createFrom(result.getCredential().getData());
                        String email = credential.getId();
                        
                        Log.d("CredMan", "Got Email: " + email);

                        // 3. Construct the Account Object
                        Account account = new Account(email, "com.google");
                        
                        // 4. Build Authorization Request
                        // Use requestOfflineAccess to get the Server Auth Code
                        List<Scope> requestedScopes = Collections.singletonList(new Scope("https://www.googleapis.com/auth/games_lite"));
                        
                        AuthorizationRequest authRequest = new AuthorizationRequest.Builder()
                            .setRequestedScopes(requestedScopes)
                            .setAccount(account)
                            .requestOfflineAccess(webClientId) // CORRECTED: Replaces setServerClientId
                            .build();

                        // 5. Call the Authorization API
                        AuthorizationClient authClient = Identity.getAuthorizationClient(context);
                        
                        authClient.authorize(authRequest)
                            .addOnSuccessListener(authorizationResult -> {
                                // CORRECTED: Check for null instead of hasServerAuthCode()
                                if (authorizationResult.getServerAuthCode() != null) {
                                    String authCode = authorizationResult.getServerAuthCode();
                                    Log.d("CredMan", "Authorization Success! Auth Code retrieved.");
                                    
                                    // Send code to Unity to trigger the Node.js backend call
                                    UnityPlayer.UnitySendMessage("AuthManager", "OnSignInSuccess", authCode);
                                } else {
                                    Log.e("CredMan", "Authorization Success, but no Server Auth Code returned.");
                                    UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", "No Auth Code returned");
                                }
                            })
                            .addOnFailureListener(e -> {
                                Log.e("CredMan", "Authorization Failed", e);
                                UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", "Authorization Failed: " + e.getMessage());
                            });

                    } catch (Exception e) {
                        Log.e("CredMan", "Error parsing CredMan result", e);
                        UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", "Parsing Error: " + e.getMessage());
                    }
                }

                @Override
                public void onError(GetCredentialException e) {
                    Log.e("CredMan", "CredMan UI Error/Cancellation", e);
                    UnityPlayer.UnitySendMessage("AuthManager", "OnSignInError", e.getMessage());
                }
            }
        );
    }
}