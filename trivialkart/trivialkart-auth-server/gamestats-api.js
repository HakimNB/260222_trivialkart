const express = require('express');
const router = express.Router();

router.post('/send_single_event', async (req, res) => {
    const { scoreEvent, playerId, packageId } = req.body;

    if (!playerId) {
        return res.status(400).json({ error: "playerId is required" });
    }

    const url = `https://games.googleapis.com/games/v1/players/${playerId}/gameStats:batchRecordEvents`;

    // Dynamic Server Timestamp
    const now = new Date();
    const currentSeconds = Math.floor(now.getTime() / 1000);
    const currentNanos = (now.getTime() % 1000) * 1000000;

    const authHeader = req.headers['authorization'];

    if (!authHeader) {
        return res.status(401).json({ error: "Authorization header is missing" });
    }

    let response;
    try {
        response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authHeader
            },
            body: JSON.stringify({
                "playerId": String(playerId),
                "packageName": packageId,
                "requestTime": {
                    "seconds": currentSeconds,
                    "nanos": currentNanos
                },
                "events": [
                    {
                        "eventId": "0213c1c9-e1a1-42e3-94a4-b81ba5e94ba5", // Needs to match your achievement/event ID in GCP console
                        "eventName": "eName",
                        "eventProperties": {
                            "distance": {
                                // Google values are strictly typed as strings; use intValue, floatValue, or longValue according to your Console definition
                                "intValue": String(Math.round(scoreEvent || 0))
                            }
                        },
                        "eventTime": {
                            "seconds": currentSeconds,
                            "nanos": currentNanos
                        }
                    }
                ]
            })
        });
    } catch (err) {
        console.error("[EventsRouter] Fetch error:", err);
        return res.status(500).json({ error: "Failed to reach Google Games API" });
    }

    let responseData;
    try {
        responseData = await response.json();
    } catch {
        responseData = await response.text(); // Fallback if API returns a non-JSON error
    }

    console.log(`[EventsRouter] /send_single_event -> Status: ${response.status} | Text: ${response.statusText}`);

    return res.status(response.status).json({
        message: "Single event forwarded successfully",
        url,
        playerId,
        scoreEvent,
        googleApiResponse: responseData,
    });
});


router.post('/send_multiple_event', async (req, res) => {
    const { runTimeEvent, scoreEvent, playerId, packageId } = req.body;

    if (!playerId) {
        return res.status(400).json({ error: "playerId is required" });
    }

    const url = `https://games.googleapis.com/games/v1/players/${playerId}/gameStats:batchRecordEvents`;

    // Dynamic Server Timestamp
    const now = new Date();
    const currentSeconds = Math.floor(now.getTime() / 1000);
    const currentNanos = (now.getTime() % 1000) * 1000000;

    const authHeader = req.headers['authorization'];

    if (!authHeader) {
        return res.status(401).json({ error: "Authorization header is missing" });
    }

    let response;
    try {
        response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authHeader
            },
            body: JSON.stringify({
                "playerId": String(playerId),
                "packageName": packageId,
                "requestTime": {
                    "seconds": currentSeconds,
                    "nanos": currentNanos
                },
                "events": [
                    {
                        "eventId": "0213c1c9-e1a1-42e3-94a4-b81ba5e94ba5", // Needs to match your achievement/event ID in GCP console
                        "eventName": "event_multiple_01",
                        "eventProperties": [{
                            "key": "event_multiple_01",
                            "value": {
                                // Google values are strictly typed as strings; use intValue, floatValue, or longValue according to your Console definition
                                "intValue": String(Math.round(scoreEvent || 0))
                            }
                        }],
                        "eventTime": {
                            "seconds": currentSeconds,
                            "nanos": currentNanos
                        }
                    },
                    {
                        "eventId": "0213c1c9-e1a1-42e3-94a4-b81ba5e94ba6", // Needs to match your achievement/event ID in GCP console
                        "eventName": "event_multiple_02",
                        "eventProperties": {
                            "key": "event_multiple_02",
                            "value": {
                                // Google values are strictly typed as strings; use intValue, floatValue, or longValue according to your Console definition
                                "intValue": String(Math.round(runTimeEvent || 0))
                            }
                        },
                        "eventTime": {
                            "seconds": currentSeconds,
                            "nanos": currentNanos
                        }
                    }
                ]
            })
        });
    } catch (err) {
        console.error("[EventsRouter] Fetch error:", err);
        return res.status(500).json({ error: "Failed to reach Google Games API" });
    }

    let responseData;
    try {
        responseData = await response.json();
    } catch {
        responseData = await response.text(); // Fallback if API returns a non-JSON error
    }

    console.log(`[EventsRouter] /send_single_event -> Status: ${response.status} | Text: ${response.statusText}`);

    return res.status(response.status).json({
        message: "Multiple event forwarded successfully",
        url,
        playerId,
        scoreEvent,
        runTimeEvent,
        googleApiResponse: responseData,
    });
});

module.exports = router;
