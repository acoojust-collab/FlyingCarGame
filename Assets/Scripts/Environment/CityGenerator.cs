using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedurally places Kenney City Kit prefabs (roads, buildings) around the player.
/// Chunks load/unload based on player position for performance on Android.
/// 
/// SETUP:
/// 1. Import Kenney City Kit from kenney.nl/assets/city-kit-commercial
/// 2. Place road and building prefabs into the arrays below
/// 3. Assign the player Transform
/// </summary>
public class CityGenerator : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Kenney Road Prefabs (drag from Project window)")]
    public GameObject[] roadStraightPrefabs;    // kenney: road_straight
    public GameObject[] roadCurvedPrefabs;      // kenney: road_curve
    public GameObject[] roadIntersectionPrefabs;// kenney: road_intersection

    [Header("Kenney Building Prefabs")]
    public GameObject[] buildingLowPrefabs;     // small shops, houses
    public GameObject[] buildingTallPrefabs;    // skyscrapers, offices
    public GameObject[] propPrefabs;            // trees, benches, streetlights

    [Header("City Layout")]
    public int   chunkSize        = 48;         // meters per city block
    public int   renderDistance   = 3;          // chunks around player to keep loaded
    public float buildingDensity  = 0.7f;       // 0-1, chance a lot has a building
    public float buildingMaxHeight = 60f;       // random Y scale cap

    [Header("Roads")]
    public bool generateRoadGrid = true;
    public int  blockCount       = 2;           // road every N chunks

    // ── Chunk tracking ───────────────────────────────────────────────────────
    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk = new Vector2Int(int.MaxValue, 0);

    void Start()
    {
        if (player == null)
        {
            var car = FindObjectOfType<FlyingCarController>();
            if (car != null) player = car.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector2Int currentChunk = WorldToChunk(player.position);
        if (currentChunk == lastPlayerChunk) return;

        lastPlayerChunk = currentChunk;
        UpdateChunks(currentChunk);
    }

    // ─── CHUNK MANAGEMENT ───────────────────────────────────────────────────

    void UpdateChunks(Vector2Int center)
    {
        var needed = new HashSet<Vector2Int>();

        for (int x = -renderDistance; x <= renderDistance; x++)
        for (int z = -renderDistance; z <= renderDistance; z++)
        {
            var coord = new Vector2Int(center.x + x, center.y + z);
            needed.Add(coord);
            if (!loadedChunks.ContainsKey(coord))
                LoadChunk(coord);
        }

        // Unload far chunks
        var toRemove = new List<Vector2Int>();
        foreach (var kv in loadedChunks)
            if (!needed.Contains(kv.Key)) toRemove.Add(kv.Key);

        foreach (var key in toRemove)
        {
            Destroy(loadedChunks[key]);
            loadedChunks.Remove(key);
        }
    }

    void LoadChunk(Vector2Int coord)
    {
        Vector3 worldPos = ChunkToWorld(coord);
        var chunk = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunk.transform.position = worldPos;

        // Seed random per-chunk so layout is consistent
        var rand = new System.Random(coord.x * 10000 + coord.y);

        // Road grid
        if (generateRoadGrid && IsRoadChunk(coord))
            PlaceRoad(chunk, worldPos, rand);
        else
            PlaceBlock(chunk, worldPos, rand);

        loadedChunks[coord] = chunk;
    }

    // ─── ROAD PLACEMENT ─────────────────────────────────────────────────────

    bool IsRoadChunk(Vector2Int coord)
    {
        return coord.x % blockCount == 0 || coord.y % blockCount == 0;
    }

    void PlaceRoad(GameObject parent, Vector3 pos, System.Random rand)
    {
        bool xRoad = (Mathf.RoundToInt(pos.x / chunkSize) % blockCount == 0);
        bool zRoad = (Mathf.RoundToInt(pos.z / chunkSize) % blockCount == 0);

        GameObject prefab = null;
        float yRot = 0f;

        if (xRoad && zRoad && roadIntersectionPrefabs?.Length > 0)
            prefab = roadIntersectionPrefabs[rand.Next(roadIntersectionPrefabs.Length)];
        else if (zRoad && roadStraightPrefabs?.Length > 0)
            prefab = roadStraightPrefabs[rand.Next(roadStraightPrefabs.Length)];
        else if (xRoad && roadStraightPrefabs?.Length > 0)
        {
            prefab = roadStraightPrefabs[rand.Next(roadStraightPrefabs.Length)];
            yRot = 90f;
        }

        if (prefab != null)
            Instantiate(prefab, pos, Quaternion.Euler(0, yRot, 0), parent.transform);
    }

    // ─── BUILDING BLOCK ─────────────────────────────────────────────────────

    void PlaceBlock(GameObject parent, Vector3 blockPos, System.Random rand)
    {
        int gridN = 2;
        float cellSize = chunkSize / gridN;

        for (int x = 0; x < gridN; x++)
        for (int z = 0; z < gridN; z++)
        {
            if (rand.NextDouble() > buildingDensity) continue;

            Vector3 cellPos = blockPos + new Vector3(x * cellSize, 0f, z * cellSize);

            bool tall = rand.NextDouble() > 0.5f;
            GameObject[] pool = tall ? buildingTallPrefabs : buildingLowPrefabs;
            if (pool == null || pool.Length == 0) continue;

            var prefab = pool[rand.Next(pool.Length)];
            var building = Instantiate(prefab, cellPos, Quaternion.Euler(0, rand.Next(4) * 90f, 0), parent.transform);

            // Random height scale for variety
            if (tall)
            {
                float scaleY = (float)(rand.NextDouble() * (buildingMaxHeight / 10f - 1f) + 1f);
                building.transform.localScale = new Vector3(1f, scaleY, 1f);
            }

            // Scatter props nearby
            if (propPrefabs?.Length > 0 && rand.NextDouble() > 0.6f)
            {
                var prop = propPrefabs[rand.Next(propPrefabs.Length)];
                Vector3 propPos = cellPos + new Vector3((float)(rand.NextDouble() - 0.5f) * cellSize,
                                                        0f,
                                                        (float)(rand.NextDouble() - 0.5f) * cellSize);
                Instantiate(prop, propPos, Quaternion.identity, parent.transform);
            }
        }
    }

    // ─── HELPERS ────────────────────────────────────────────────────────────

    Vector2Int WorldToChunk(Vector3 worldPos) =>
        new Vector2Int(Mathf.RoundToInt(worldPos.x / chunkSize),
                       Mathf.RoundToInt(worldPos.z / chunkSize));

    Vector3 ChunkToWorld(Vector2Int chunk) =>
        new Vector3(chunk.x * chunkSize, 0f, chunk.y * chunkSize);
}
