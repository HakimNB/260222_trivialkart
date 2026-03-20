const express = require('express');
const { OAuth2Client } = require('google-auth-library');
const jwt = require('jsonwebtoken');

const envFile = process.env.NODE_ENV === 'production' ? 'env_prod.env' : 'env_staging.env';
require('dotenv').config({ path: envFile });
console.log(`Loaded environment from ${envFile}`);

const WEB_CLIENT_ID = process.env.WEB_CLIENT_ID;
const WEB_CLIENT_SECRET = process.env.WEB_CLIENT_SECRET;
const FB_APP_ID = process.env.FB_APP_ID;
const FB_APP_SECRET = process.env.FB_APP_SECRET;

const JWT_SECRET = process.env.JWT_SECRET;

const PORT = 3000;

if (!WEB_CLIENT_ID || !WEB_CLIENT_SECRET || !FB_APP_ID || !FB_APP_SECRET || !JWT_SECRET) {
    console.error("FATAL ERROR: Google or Facebook .env or JWT_SECRET variables are not set.");
    process.exit(1);
}

const FB_APP_ACCESS_TOKEN = `${FB_APP_ID}|${FB_APP_SECRET}`;
const client = new OAuth2Client(WEB_CLIENT_ID, WEB_CLIENT_SECRET);

const app = express();
app.use(express.json());

//{ "openID": "ingame-1001" }
const userDatabase_v2 = new Map();

//{ "playedID": "ingame-1001" }
const userDatabase_v1 = new Map();

//{ "ingame-1001": 10 }
const inGameDatabase = new Map();

const verifyToken = (req, res, next) => {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.split(' ')[1]; // Get just the token part

    if (token == null) {
        return res.status(401).json({ error: "No token provided" }); // 401 Unauthorized
    }

    // Verify the token is valid and was signed by us
    jwt.verify(token, JWT_SECRET, (err, userPayload) => {
        if (err) {
            console.error("Token verification failed:", err.message);
            return res.status(403).json({ error: "Invalid token" }); // 403 Forbidden
        }

        // Token is valid!
        req.user = userPayload;
        next();
    });
};

let nextInGameAccountId = 1001;

const SERVER_NAME = process.env.SERVER_NAME;

app.post('/connection_check', (req, res) => {
    const clientProvidedId = req.body.webClientId;

    // Check if the client's webClientId matches the server's environment variable
    const isMatch = clientProvidedId === WEB_CLIENT_ID;

    console.log(`[Handshake] Connection check from client. ID Match: ${isMatch}`);

    res.status(200).json({
        serverName: SERVER_NAME,
        status: "Online",
        webClientIdMatch: isMatch
    });
});


app.post('/verify_and_link_google', async (req, res) => {
    const { idToken, playerID } = req.body;

    if (!idToken || !playerID) {
        return res.status(400).json({ error: "idToken and playerId are required" });
    }

    try {
        const ticket = await client.verifyIdToken({
            idToken: idToken,
            audience: WEB_CLIENT_ID,
        });

        const payload = ticket.getPayload();
        const email = payload.email;

        console.log(`(PGS v1) Successfully verified idToken for: ${email}`);
        console.warn(`(PGS v1) Using playerID from client: ${playerID}`);

        // 3. Find or create the in-game account using GAIA ID
        let inGameAccountID
        if (userDatabase_v1.has(playerID)) {
            // User already exists, retrieve their ID
            inGameAccountID = userDatabase_v1.get(playerID);
            console.log(`(PGS v1) Existing user. In-Game ID: ${inGameAccountID}`);
        } else {
            // New user, create a new in-game ID and store it
            inGameAccountID = `ingame-${nextInGameAccountId++}`;

            userDatabase_v1.set(playerID, inGameAccountID);
            inGameDatabase.set(inGameAccountID, 0);

            console.log(`(PGS v1) New user. Created In-Game ID: ${inGameAccountID}`);
        }

        const tokenPayload = {
            playerID: playerID,
            inGameAccountID: inGameAccountID
        };
        const customJwtToken = jwt.sign(tokenPayload, JWT_SECRET, { expiresIn: '7d' });

        // 4. Send the success response back to the client
        res.status(200).json({
            playerID: playerID,
            email: email,
            inGameAccountID: inGameAccountID,
            inGameCount : inGameDatabase.get(inGameAccountID),
            jwtToken: customJwtToken
        });

    } catch (error) {
        console.error("Error during token verification:", error.message);
        res.status(500).json({ error: "Failed to verify authentication" });
    }
});

// ---
// NEW: PGS v2 (v0.11.x+) ENDPOINT
// This endpoint receives a one-time Auth Code from the client,
// exchanges it for tokens, verifies the token, and links the account
// ---
app.post('/exchange_authcode_and_link', async (req, res) => {
    const { authCode } = req.body;
    if (!authCode) {
        return res.status(400).json({ error: "authCode is required" });
    }
    try {
        const { tokens } = await client.getToken(authCode);
        console.log(`tokens: ${JSON.stringify(tokens)}`); // tokens: {"access_token":"","scope":"https://www.googleapis.com/auth/drive.appdata https://www.googleapis.com/auth/games_lite","token_type":"Bearer","expiry_date":1773902295498}
        const idToken = tokens.id_token;
        const accessToken = tokens.access_token;
        if (!idToken) {
            throw new Error("Failed to retrieve id_token from authCode exchange.");
        }
        const ticket = await client.verifyIdToken({
            idToken: idToken,
            audience: WEB_CLIENT_ID,
        });
        const payload = ticket.getPayload();
        const email = payload.email;
        const googleId = payload.sub;
        const playerInfo = await getPlayerInfo(accessToken);
        const playerID = playerInfo.playerId;
        
        if (!playerID) {
            console.error("Payload dump:", payload);
            throw new Error("player_id not found in token payload. Ensure client requested 'https://www.googleapis.com/auth/games_lite' scope.");
        }
        
        let inGameAccountID;
        if (userDatabase_v1.has(playerID)) {
            inGameAccountID = userDatabase_v1.get(playerID);
            userDatabase_v2.set(googleId, inGameAccountID);
            userDatabase_v1.delete(playerID);
        } 
        else if(userDatabase_v2.has(googleId))
        {
            inGameAccountID = userDatabase_v2.get(googleId);
        }
        else {
            inGameAccountID = `ingame-${nextInGameAccountId++}`;
            userDatabase_v2.set(googleId, inGameAccountID);
            inGameDatabase.set(inGameAccountID, 0);
        }
        
        const tokenPayload = {
            playerID: googleId,
            inGameAccountID: inGameAccountID
        };
        
        const customJwtToken = jwt.sign(tokenPayload, JWT_SECRET, { expiresIn: '7d' });
        res.status(200).json({
            playerID: playerID,
            email: email,
            inGameAccountID: inGameAccountID,
            inGameCount: inGameDatabase.get(inGameAccountID),
            jwtToken: customJwtToken
        });
    } catch (error) {
        console.error("Error during authCode exchange:", error.message);
        res.status(500).json({ error: "Failed to verify authentication" });
    }
});


app.post('/verify_and_link_facebook', async (req, res) => {
    const { accessToken } = req.body;

    if (!accessToken) {
        return res.status(400).json({ error: "accessToken is required" });
    }

    try {
        // 1. Verify the client's token by calling Facebook's debug_token endpoint
        const debugUrl = `https://graph.facebook.com/debug_token?input_token=${accessToken}&access_token=${FB_APP_ACCESS_TOKEN}`;

        const debugResponse = await fetch(debugUrl);
        const debugData = await debugResponse.json();

        if (!debugData.data || !debugData.data.is_valid) {
            console.error("Facebook token is invalid:", debugData);
            throw new Error("Invalid Facebook token.");
        }

        // 2. Token is valid, get the unique Facebook User ID
        const facebookUserId = debugData.data.user_id;

        // 3. (Optional but recommended) Get the user's name and email
        const userUrl = `https://graph.facebook.com/${facebookUserId}?fields=name,email&access_token=${FB_APP_ACCESS_TOKEN}`;
        const userResponse = await fetch(userUrl);
        const userData = await userResponse.json();

        const email = userData.email || null; // Email is not always guaranteed
        const name = userData.name || "Facebook User";
        console.log(`Successfully verified FB user: ${name} (ID: ${facebookUserId})`);

        // 4. Find or create the in-game account
        // We MUST prefix the ID to avoid collisions with Google IDs
        const prefixedFacebookId = `fb-${facebookUserId}`;

        let inGameAccountID;
        if (userDatabase_v2.has(prefixedFacebookId)) {
            // User already exists
            inGameAccountID = userDatabase_v2.get(prefixedFacebookId);
            console.log(`Existing FB user. In-Game ID: ${inGameAccountID}`);
        } else {
            // New user, create a new in-game ID
            inGameAccountID = `ingame-${nextInGameAccountId++}`;
            userDatabase_v2.set(prefixedFacebookId, inGameAccountID);
            inGameDatabase.set(inGameAccountID, 0); // Default score
            console.log(`New FB user. Created In-Game ID: ${inGameAccountID}`);
        }

        const tokenPayload = {
            facebookId: prefixedFacebookId,
            inGameAccountID: inGameAccountID
        };
        const customJwtToken = jwt.sign(tokenPayload, JWT_SECRET, { expiresIn: '7d' });

        // 5. Send the success response back to the client
        res.status(200).json({
            playerID: prefixedFacebookId, // We re-use this field
            email: email,
            inGameAccountID: inGameAccountID,
            inGameCount : inGameDatabase.get(inGameAccountID),
            jwtToken: customJwtToken
        });

    } catch (error) {
        console.error("Error during Facebook token verification:", error.message);
        res.status(500).json({ error: "Failed to verify Facebook authentication" });
    }
});

app.post('/post_count', verifyToken, async (req, res) => {
    // 1. Get the count from the body
    const { count } = req.body;

    // 2. Get the user's info FROM THE MIDDLEWARE (req.user)
    // This is 100% secure because the token was already verified.
    const { inGameAccountID, playerID, facebookId } = req.user;

    if (typeof count === 'undefined') {
        return res.status(400).json({ error: "count is required" });
    }

    try {
        // 3. Update the database for the correct user
        inGameDatabase.set(inGameAccountID, count);

        console.log(`Updated count for ${inGameAccountID} to: ${count}`);

        // 4. Send back the success response
        res.status(200).json({
            playerID: playerID || facebookId,
            email: "",
            inGameAccountID: inGameAccountID,
            inGameCount : inGameDatabase.get(inGameAccountID)
        });

    } catch (error) {
        console.error("Error during posting count:", error.message);
        res.status(500).json({ error: "Failed to post count" });
    }
});

// ---
// NEW: PGS v2 (v0.11.x+) ENDPOINT
// This endpoint receives a one-time Auth Code from the client,
// exchanges it for tokens, verifies the token, and links the account
// ---
app.post('/exchange_authcode_for_tokens', async (req, res) => {
    const { authCode } = req.body;
    if (!authCode) {
        return res.status(400).json({ error: "authCode is required" });
    }
    try {
        const { tokens } = await client.getToken(authCode);
        console.log(`exchange_authcode_for_tokens tokens: ${JSON.stringify(tokens)}`); // tokens: {"access_token":"","scope":"https://www.googleapis.com/auth/drive.appdata https://www.googleapis.com/auth/games_lite","token_type":"Bearer","expiry_date":1773902295498}
        const idToken = tokens.id_token;
        const accessToken = tokens.access_token;
        const refreshToken = tokens.refresh_token;
        // if (!idToken) {
        //     throw new Error("Failed to retrieve id_token from authCode exchange.");
        // }
        // const ticket = await client.verifyIdToken({
        //     idToken: idToken,
        //     audience: WEB_CLIENT_ID,
        // });
        // const payload = ticket.getPayload();
        // const email = payload.email;
        // const googleId = payload.sub;
        const playerInfo = await getPlayerInfo(accessToken);
        const playerID = playerInfo.playerId;

        console.log(`exchange_authcode_for_tokens playerInfo: ${JSON.stringify(playerInfo)}`);
        console.log(`exchange_authcode_for_tokens playerID: ${playerID}`);

        if (!playerID) {
            console.error("Payload dump:", tokens);
            throw new Error("player_id not found in token payload. Ensure client requested 'https://www.googleapis.com/auth/games_lite' scope.");
        }

        res.status(200).json({
            playerID,
            accessToken,
            refreshToken
        });

        // let inGameAccountID;
        // if (userDatabase_v1.has(playerID)) {
        //     inGameAccountID = userDatabase_v1.get(playerID);
        //     userDatabase_v2.set(googleId, inGameAccountID);
        //     userDatabase_v1.delete(playerID);
        // }
        // else if (userDatabase_v2.has(googleId)) {
        //     inGameAccountID = userDatabase_v2.get(googleId);
        // }
        // else {
        //     inGameAccountID = `ingame-${nextInGameAccountId++}`;
        //     userDatabase_v2.set(googleId, inGameAccountID);
        //     inGameDatabase.set(inGameAccountID, 0);
        // }

        // const tokenPayload = {
        //     playerID: googleId,
        //     inGameAccountID: inGameAccountID
        // };

        // const customJwtToken = jwt.sign(tokenPayload, JWT_SECRET, { expiresIn: '7d' });
        // res.status(200).json({
        //     playerID: playerID,
        //     email: email,
        //     inGameAccountID: inGameAccountID,
        //     inGameCount: inGameDatabase.get(inGameAccountID),
        //     jwtToken: customJwtToken
        // });
    } catch (error) {
        console.error("Error during authCode exchange:", error.message);
        res.status(500).json({ error: "Failed to verify authentication" });
    }
});

app.post('/send_single_event', async (req, res) => {
    const body = req.body;
    // console.log('body: ', body); // body:  { distance: 5.758289813995361 }

    const { distance, playerId } = body;
    // console.log(`distance: ${distance} playerId: ${playerId}`);

    // Get the user's info FROM THE MIDDLEWARE (req.user)
    // const playerID = req.user?.playerID ?? ""; // a_2389139752014657965
    const url = `https://games.googleapis.com/games/v1/players/${playerId}/gameStats:batchRecordEvents`;

    let currentTime = new Date().toISOString();
    currentTime = currentTime.split('.')[0] + "Z";

    const authHeader = req.headers['authorization'];

    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': authHeader
        },
        body: JSON.stringify({
            "packageName": "com.WickedCube.TrivialKart",
            "requestTime": currentTime,
            "events": [
                {
                    "eventId": "distance",
                    "eventName": "distance",
                    "eventProperties": {
                        "distance": distance
                    },
                    "eventTime": currentTime
                }
            ]
        })
    });

    console.log(`responseStatus: ${response.status} text: ${response.statusText}`); // 400 Bad Request
    // console.log(response.data); // undefined

    // TODO: send request to Play Server
    return res.status(200).json({
        message: "Single event sent successfully",
        url,
        playerId,
        distance,
        data: response.data,
    });
});

async function getPlayerInfo(accessToken) {
    try {
        // Note: We use the v1 REST endpoint to get the legacy Player ID.
        // The client MUST have requested '[https://www.googleapis.com/auth/games_lite](https://www.googleapis.com/auth/games_lite)' scope.
        const response = await fetch('https://games.googleapis.com/games/v1/players/me', {
            headers: {
                'Authorization': `Bearer ${accessToken}`
            }
        });

        if (!response.ok) {
            throw new Error(`Games API Error: ${response.status} ${response.statusText}`);
        }

        const data = await response.json();
        return { playerId: data.playerId };
    } catch (error) {
        console.error("Failed to fetch player info:", error);
        throw error;
    }
}

app.listen(PORT, '0.0.0.0', () => {
    console.log(`Game server listening on http://localhost:${PORT}`);
    console.log('Waiting for a client to connect...');
});