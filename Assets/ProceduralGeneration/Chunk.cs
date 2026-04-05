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

    public void Init(Vector2Int coordinates, int size, ChunkManager manager)
    {
        this.manager = manager;
        this.size = size;
        this.coordinates = coordinates;
        localSeed = math.hash(new int2(coordinates.x, coordinates.y));
        rd = new Unity.Mathematics.Random(localSeed);
    }

    public void GenerateMesh()
    {
        int vertexDimension = size + 1;
        int vertexCount = vertexDimension * vertexDimension;
        int faceCount = size * size;

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp);
        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        meshData.SetIndexBufferParams(faceCount * 6, IndexFormat.UInt32);

        NativeArray<float3> verts = meshData.GetVertexData<float3>();
        NativeArray<int> tris = meshData.GetIndexData<int>();
        NativeArray<float> maskMap = new NativeArray<float>(vertexCount, Allocator.TempJob);
        NativeArray<float> heightMap = new NativeArray<float>(vertexCount, Allocator.TempJob);

        float islandRoll = rd.NextFloat(0, 1);
        int islandSize = islandRoll >= manager.Settings.islandProbability ? rd.NextInt(60, 150) : 0;

        var maskJob = new FallOfJob
        {
            result = maskMap,
            p = rd.NextInt(1, 6),
            size = vertexDimension,
            centerX = size / 2 + rd.NextInt((-vertexDimension / 2) + islandSize, (vertexDimension / 2) - islandSize),
            centerY = size / 2 + rd.NextInt((-vertexDimension / 2) + islandSize, (vertexDimension / 2) - islandSize),
            radius = islandSize,
            power = rd.NextFloat(0.5f, 2f)
        };

        var heightJob = new FractalSimplexJob
        {
            result = heightMap,
            width = vertexDimension,
            scale = manager.Settings.scale,
            xOffset = coordinates.x * size,
            yOffset = coordinates.y * size,
            octaves = manager.Settings.octaves,
            persistence = manager.Settings.persistence,
            lacunarity = manager.Settings.lacunarity,
            power = manager.Settings.power,
        };

        var mergeJob = new ApplyMaskJob
        {
            result = heightMap,
            mask = maskMap
        };

        var meshJob = new MeshMakerJob
        {
            heightMap = heightMap,
            vertices = verts,
            triangles = tris,
            width = vertexDimension,
            heightMultiplier = manager.Settings.heightMultiplier
        };

        

        JobHandle maskHandle = maskJob.Schedule(maskMap.Length, 64);
        JobHandle heightHandle = heightJob.Schedule(heightMap.Length, 64);

        JobHandle applyJob = mergeJob.Schedule(heightMap.Length, 64, JobHandle.CombineDependencies(maskHandle, heightHandle));

        JobHandle meshHandle = meshJob.Schedule(vertexCount, 64, applyJob);
        
        meshHandle.Complete();


        maskMap.Dispose();
        heightMap.Dispose();
        vertexAttributes.Dispose();


        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, faceCount * 6));

        Mesh mesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();


        if (!TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (!TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = mesh;
    }
}
