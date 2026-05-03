using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk : MonoBehaviour
{
    private ChunkManager manager;
    private int size;
    private Vector2Int coordinates;
    private Unity.Mathematics.Random rd;
    private uint localSeed;
    public int CurrentLOD { get; private set; } = -1;

    private List<Matrix4x4[]> treeMatricesBatches = new List<Matrix4x4[]>();

    private void Awake()
    {
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshCollider>().enabled = false;
    }

    public void Init(Vector2Int coordinates, int size, ChunkManager manager)
    {
        this.manager = manager;
        this.size = size;
        this.coordinates = coordinates;
        localSeed = math.hash(new int2(coordinates.x, coordinates.y));
        rd = new Unity.Mathematics.Random(localSeed);
    }

    private void Update()
    {
        if (CurrentLOD > 2) return;

        if (treeMatricesBatches.Count > 0)
        {
            for (int i = 0; i < treeMatricesBatches.Count; i++)
            {
                
                Mesh mesh = manager.treeMeshes[CurrentLOD];

                Graphics.DrawMeshInstanced(
                    mesh,
                    CurrentLOD == 0 ? 1 : 0,
                    manager.treeTrunkMaterial,
                    treeMatricesBatches[i]
                );

                Graphics.DrawMeshInstanced(
                    mesh,
                    CurrentLOD == 0 ? 0 : 1,
                    manager.treeLeavesMaterial,
                    treeMatricesBatches[i]
                );
            }
        }
    }


    public void GenerateMesh(int lod)
    {
        CurrentLOD = lod;
        int meshSimplificationIncrement = (int)Mathf.Pow(2, lod);
        int verticesPerLine = (size / meshSimplificationIncrement) + 1;

        int numVertices = verticesPerLine * verticesPerLine;
        int numQuads = (verticesPerLine - 1) * (verticesPerLine - 1);
        int numTriangles = numQuads * 6;

        NativeArray<float> heightMap = new NativeArray<float>(numVertices, Allocator.TempJob);
        NativeArray<float3> vertices = new NativeArray<float3>(numVertices, Allocator.TempJob);
        NativeArray<int> triangles = new NativeArray<int>(numTriangles, Allocator.TempJob);


        var heightJob = new HeightMapJob
        {
            result = heightMap,
            width = verticesPerLine,
            xOffset = coordinates.x * size,
            yOffset = coordinates.y * size,
            meshSimplificationIncrement = meshSimplificationIncrement,

            islandsScale = manager.Settings.islandsScale,
            islandsOctaves = manager.Settings.islandsOctaves,
            islandsPersistance = manager.Settings.islandsPersistance,
            islandsLacunarity = manager.Settings.islandsLacunarity,
            islandspower = manager.Settings.islandsPower,
            islandsProximityFactor = manager.Settings.islandsProximityFactor,


            warpScale = manager.Settings.islandsWarpScale,
            warpStrength = manager.Settings.islandsWarpStrength,

            mountainsScale = manager.Settings.mountainsScale,
            mountainsOctaves = manager.Settings.mountainsOctaves,
            mountainsPersistance = manager.Settings.mountainsPersistance,
            mountainsLacunarity = manager.Settings.mountainsLacunarity,
            mountainsPower = manager.Settings.mountainsPower,
        };


        var meshJob = new MeshMakerJob
        {
            heightMap = heightMap,
            vertices = vertices,
            triangles = triangles,
            width = verticesPerLine,
            heightMultiplier = manager.Settings.heightMultiplier,
            meshSimplificationIncrement = meshSimplificationIncrement
        };

        

        JobHandle heightHandle = heightJob.Schedule(heightMap.Length, 64);
        JobHandle meshHandle = meshJob.Schedule(heightMap.Length, 64, heightHandle);
        
        meshHandle.Complete();

        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        if(mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }
        
        mesh.SetVertices(vertices);
        mesh.SetIndices(triangles, MeshTopology.Triangles, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        int gridWidth = 30;
        int totalCells = gridWidth * gridWidth;
        float cellSize = size / (float)gridWidth;

        

        if(CurrentLOD <= 2)
        {
            NativeArray<float2> treesPos = new NativeArray<float2>(totalCells, Allocator.TempJob);
            var treePosJob = new JitteredGridParallelJob
            {
                results = treesPos,
                chunkSize = size,
                cellSize = cellSize,
                gridWidth = gridWidth,
                randomness = 0.8f,
                treeProbability = 0.1f,
                chunkSeed = localSeed
            };

            JobHandle treeHandle = treePosJob.Schedule(treesPos.Length, 64);
            treeHandle.Complete();


            if (treeMatricesBatches.Count == 0 && treesPos.Length > 0)
            {
                List<Matrix4x4> currentBatch = new List<Matrix4x4>();

                foreach (float2 pos in treesPos)
                {
                    if (pos.x < 0 || pos.y < 0) continue;

                    float gridX = pos.x / meshSimplificationIncrement;
                    float gridZ = pos.y / meshSimplificationIncrement;

                    int x0 = Mathf.FloorToInt(gridX);
                    int x1 = Mathf.Min(x0 + 1, verticesPerLine - 1);
                    int z0 = Mathf.FloorToInt(gridZ);
                    int z1 = Mathf.Min(z0 + 1, verticesPerLine - 1);

                    float tx = gridX - x0;
                    float tz = gridZ - z0;

                    float h00 = vertices[z0 * verticesPerLine + x0].y;
                    float h10 = vertices[z0 * verticesPerLine + x1].y;
                    float h01 = vertices[z1 * verticesPerLine + x0].y;
                    float h11 = vertices[z1 * verticesPerLine + x1].y;

                    float heightY = Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
                    if (heightY <= 0.15f * manager.Settings.heightMultiplier) continue;

                    Vector3 slopeX = new Vector3(meshSimplificationIncrement, h10 - h00, 0);
                    Vector3 slopeZ = new Vector3(0, h01 - h00, meshSimplificationIncrement);
                    Vector3 surfaceNormal = Vector3.Cross(slopeZ, slopeX).normalized;
                    float slopeAngle = Vector3.Angle(Vector3.up, surfaceNormal);
                    if (slopeAngle > 35) continue;

                    float worldX = pos.x + (coordinates.x * size);
                    float worldZ = pos.y + (coordinates.y * size);

                    Vector3 position = new Vector3(worldX, heightY, worldZ);
                    Quaternion rotation = Quaternion.Euler(0, rd.NextFloat(0f, 360f), 0);
                    Vector3 scale = Vector3.one * rd.NextFloat(0.8f, 1.2f);

                    Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
                    currentBatch.Add(matrix);

                    if (currentBatch.Count == 1023)
                    {
                        treeMatricesBatches.Add(currentBatch.ToArray());
                        currentBatch.Clear();
                    }
                }

                if (currentBatch.Count > 0)
                {
                    treeMatricesBatches.Add(currentBatch.ToArray());
                }
            }
            treesPos.Dispose();
        }
        

        heightMap.Dispose();
        vertices.Dispose();
        triangles.Dispose();
        

        GetComponent<MeshFilter>().sharedMesh = mesh;

        MeshCollider col = GetComponent<MeshCollider>();
        if (lod == 0)
        {
            Physics.BakeMesh(mesh.GetEntityId(), false);
            col.sharedMesh = mesh;
            col.enabled = true;
        }
        else
        {
            col.enabled = false;
        }
    }
}
