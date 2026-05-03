using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private ProceduralSettings _settings;
    public ProceduralSettings Settings => _settings;
    
    public Mesh[] treeMeshes;
    public Material treeTrunkMaterial;
    public Material treeLeavesMaterial;

    [SerializeField] private int chunkSize = 16;
    
    [SerializeField] private int radius0 = 2;
    [SerializeField] private int radius1 = 4;
    [SerializeField] private int radius2 = 8;
    [SerializeField] private int radius3 = 16;
    [SerializeField] private int radius4 = 32;
    [SerializeField] private int radius5 = 64;
    [SerializeField] private int radius6 = 128;

    [SerializeField] private Material mat;

    private Dictionary<Vector2Int, Chunk> chunkDict;
    private Vector2Int lastChunkCoord;

    [SerializeField] private Transform player;
    private Stack<GameObject> chunkPool;
    private List<Vector2Int> chunksToRemoveCache = new List<Vector2Int>();

    private void Start()
    {
        chunkDict = new Dictionary<Vector2Int, Chunk>();
        chunkPool = new Stack<GameObject>();
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
        Vector2Int chunkCoord = GetChunkCoordFromWorldPos(player.position);
        if (chunkCoord == lastChunkCoord) return;
        lastChunkCoord = chunkCoord;
        chunksToRemoveCache.Clear();


        foreach (var chunk in chunkDict)
        {
            if (Vector2Int.Distance(chunk.Key, chunkCoord) > radius6 + 1)
            {
                chunksToRemoveCache.Add(chunk.Key);
            }
        }

        foreach (var chunk in chunksToRemoveCache)
        {
            chunkPool.Push(chunkDict[chunk].gameObject);
            chunkDict[chunk].gameObject.SetActive(false);
            chunkDict.Remove(chunk);
        }


        for (int y = chunkCoord.y - radius6; y <= chunkCoord.y + radius6; y++)
        {
            for (int x = chunkCoord.x - radius6; x <= chunkCoord.x + radius6; x++)
            {
                Vector2Int visibleChunkCoord = new Vector2Int(x, y);

                float dist = Vector2Int.Distance(chunkCoord, visibleChunkCoord);
                if (dist > radius6) continue;

                int lod = 0;
                if (dist < radius0) lod = 0;
                else if (dist < radius1) lod = 1;
                else if (dist < radius2) lod = 2;
                else if (dist < radius3) lod = 3;
                else if (dist < radius4) lod = 4;
                else if (dist < radius5) lod = 5;
                else if (dist < radius6) lod = 6;

                if (!chunkDict.ContainsKey(visibleChunkCoord))
                {
                    LoadChunk(visibleChunkCoord, lod);
                }
                else
                {
                    Chunk existingChunk = chunkDict[visibleChunkCoord];

                    if (existingChunk.CurrentLOD != lod)
                    {
                        existingChunk.GenerateMesh(lod);
                    }
                }
            }
        }
    }

    private void LoadChunk(Vector2Int coord, int lod)
    {
        Chunk chunk;

        if (chunkPool.Count > 0)
        {
            GameObject chunkGO = chunkPool.Pop();
            chunk = chunkGO.GetComponent<Chunk>();
            chunkGO.SetActive(true);
            chunk.gameObject.name = "Chunk" + coord;
        }
        else
        {
            GameObject chunkGO = new GameObject("Chunk" + coord);
            chunkGO.transform.parent = transform;
            chunk = chunkGO.AddComponent<Chunk>();
            chunk.gameObject.GetComponent<MeshRenderer>().material = mat;
            //chunk.gameObject.AddComponent<MeshCollider>();
        }

        chunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        chunk.Init(coord, chunkSize, this);
        chunk.GenerateMesh(lod);
        chunkDict.Add(coord, chunk);
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

    [Header("Global")]
    public float heightMultiplier;
    public int nbTrees;
    public float treesDist;
}
