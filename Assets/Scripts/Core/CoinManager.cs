using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton that tracks collected coins and spawns coin rings/clusters in the air.
/// Raises events so the HUD can react without tight coupling.
/// </summary>
public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Coin Prefab")]
    public GameObject coinPrefab;

    [Header("Spawn Settings")]
    public int coinsPerRing     = 8;
    public float ringRadius     = 6f;
    public float spawnInterval  = 4f;        // seconds between new rings
    public float spawnAltMin    = 20f;
    public float spawnAltMax    = 200f;
    public float spawnRangeXZ   = 150f;      // radius around player

    [Header("Scoring")]
    public int totalCoins       = 0;
    public int highScore        = 0;

    public delegate void CoinsChanged(int total);
    public event CoinsChanged OnCoinsChanged;

    private Transform player;
    private float spawnTimer;
    private List<GameObject> activeCoins = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    void Start()
    {
        var car = FindObjectOfType<FlyingCarController>();
        if (car != null) player = car.transform;
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnCoinRing();
        }
    }

    // ─── SPAWN ──────────────────────────────────────────────────────────────

    void SpawnCoinRing()
    {
        if (coinPrefab == null || player == null) return;

        // Pick a random point ahead of player
        Vector3 forward = player.forward;
        Vector3 center  = player.position + forward * Random.Range(30f, spawnRangeXZ);
        center.y = Random.Range(spawnAltMin, spawnAltMax);

        // Coin ring — evenly spaced in a circle
        for (int i = 0; i < coinsPerRing; i++)
        {
            float angle = i * (360f / coinsPerRing) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
            Vector3 spawnPos = center + offset;
            var coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            activeCoins.Add(coin);
        }

        // Cleanup destroyed coins from list
        activeCoins.RemoveAll(c => c == null);

        // Despawn oldest ring if too many coins in scene
        if (activeCoins.Count > 80)
        {
            int toRemove = activeCoins.Count - 80;
            for (int i = 0; i < toRemove; i++)
            {
                if (activeCoins[0] != null) Destroy(activeCoins[0]);
                activeCoins.RemoveAt(0);
            }
        }
    }

    // ─── SCORING ────────────────────────────────────────────────────────────

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        if (totalCoins > highScore)
        {
            highScore = totalCoins;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
        OnCoinsChanged?.Invoke(totalCoins);
    }

    public void ResetCoins()
    {
        totalCoins = 0;
        OnCoinsChanged?.Invoke(totalCoins);
    }
}
