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
    [SerializeField] private Material mat;
    private Texture tex;

    private Dictionary<Vector2Int, Chunk> chunkDict;
    private Vector2Int lastChunkCoord;

    [Header("Islands")]
    [SerializeField] private int centerX;
    [SerializeField] private int centerY;
    [SerializeField] private int p = 1;
    public Gradient colorGradient;

    private void Start()
    {
        chunkDict = new Dictionary<Vector2Int, Chunk>();
        tex = GenerateGradientTexture();
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
                    //chunk.GetComponent<MeshRenderer>().material.mainTexture = tex;
                    chunkGO.AddComponent<MeshCollider>();
                }
            }
        }
    }

    Texture2D GenerateGradientTexture()
    {
        int width = 500;
        Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] colors = new Color[width];

        for (int i = 0; i < width; i++)
        {
            float t = (float)i / (width - 1);
            colors[i] = colorGradient.Evaluate(t);
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }
}

[System.Serializable]
public struct ProceduralSettings
{
    [Header("Islands shape")]
    public float islandsScale;
    public int islandsOctaves;
    public float islandsPersistance;
    public float islandsLacunarity;
    public float islandsPower;
    public float islandsProximityFactor;

    [Header("Islands warp")]
    public float islandsWarpScale;
    public float islandsWarpStrength;

    [Header("Mountain settings")]
    public float mountainsScale;
    public int mountainsOctaves;
    public float mountainsPersistance;
    public float mountainsLacunarity;
    public float mountainsPower;

    [Header("Flat settings")]
    public float flatScale;
    public int flatOctaves;
    public float flatPersistance;
    public float flatLacunarity;
    public float flatPower;


    [Header("Hydrolic erosion")]
    public float ravineScale;
    public float erosionStrength;
    public float slopeThreshold;


    [Header("Global")]
    public float heightMultiplier;
}
