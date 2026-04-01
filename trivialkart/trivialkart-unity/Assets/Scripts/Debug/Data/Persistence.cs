using UnityEngine;
using System;

public class Persistence : MonoBehaviour
{

    public static Persistence Instance { get; private set; }

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
