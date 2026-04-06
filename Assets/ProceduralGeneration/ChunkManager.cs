using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private ProceduralSettings _settings;
    public ProceduralSettings Settings => _settings;

    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int radius = 4;

    [SerializeField] private float scale = 10f;

    [SerializeField] int heightMultiplier = 20;
    [SerializeField] float power = 1;
    [SerializeField] Material mat;

    private Dictionary<Vector2Int, Chunk> chunkDict;
    private Vector2Int lastChunkCoord;

    [Header("Islands")]
    [SerializeField] private int centerX;
    [SerializeField] private int centerY;
    [SerializeField] private int p = 1;

    private void Start()
    {
        chunkDict = new Dictionary<Vector2Int, Chunk>();
        UpdateChunks();
    }

    public Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / chunkSize);
        int z = Mathf.FloorToInt(worldPos.z / chunkSize);
        return new Vector2Int(x, z);
    }

    private void Update()
    {
        UpdateChunks();
    }

    private void UpdateChunks()
    {
        Vector3 worldPos = Camera.main.transform.position;
        Vector2Int chunkCoord = GetChunkCoordFromWorldPos(worldPos);
        if (chunkCoord == lastChunkCoord) return;
        lastChunkCoord = chunkCoord;


        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        foreach (var chunk in chunkDict)
        {
            if (Vector2Int.Distance(chunk.Key, chunkCoord) > radius + 1)
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (var chunk in chunksToRemove)
        {
            Destroy(chunkDict[chunk].gameObject);
            chunkDict.Remove(chunk);
        }


        for (int y = chunkCoord.y - radius; y < chunkCoord.y + radius; y++)
        {
            for (int x = chunkCoord.x - radius; x < chunkCoord.x + radius; x++)
            {
                Vector2Int visibleChunkCoord = new Vector2Int(x, y);
                if (!chunkDict.ContainsKey(visibleChunkCoord))
                {
                    GameObject chunkGO = new GameObject("Chunk" + visibleChunkCoord);
                    chunkGO.transform.parent = transform;
                    chunkGO.transform.position = new Vector3(visibleChunkCoord.x * chunkSize, 0, visibleChunkCoord.y * chunkSize);
                    Chunk chunk = chunkGO.AddComponent<Chunk>();
                    chunk.Init(visibleChunkCoord, chunkSize, this);

                    chunkDict.Add(visibleChunkCoord, chunk);

                    float worldXOffset = visibleChunkCoord.x * chunkSize;
                    float worldZOffset = visibleChunkCoord.y * chunkSize;
                    chunk.GenerateMesh();
                    chunk.GetComponent<MeshRenderer>().material = mat;
                }
            }
        }
    }
}

[System.Serializable]
public struct ProceduralSettings
{
    public float scale;
    public int octaves;
    public float persistence;
    public float lacunarity;
    public float heightMultiplier;
    [Range(0.5f, 10)] public float power;
    [Range(0,1)] public float islandProbability;
    [Header("erosion")]
    public int numDroplets;
    public float erosionRate;
    public float depositionRate;
    public float evaporationRate;
    public float inertia;
    public float capacityMultiplier;
}
