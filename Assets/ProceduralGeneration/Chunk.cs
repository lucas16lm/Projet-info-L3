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

        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream: 0);
        vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, stream: 1);

        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        meshData.SetIndexBufferParams(faceCount * 6, IndexFormat.UInt32);

        NativeArray<float3> verts = meshData.GetVertexData<float3>(0);
        NativeArray<float2> uvs = meshData.GetVertexData<float2>(1);
        NativeArray<int> tris = meshData.GetIndexData<int>();


        NativeArray<float> heightMap = new NativeArray<float>(vertexCount, Allocator.TempJob);
        NativeArray<float> erodedHeightMap = new NativeArray<float>(vertexCount, Allocator.TempJob);


        var heightJob = new HeightMapJob
        {
            result = heightMap,
            width = vertexDimension,
            xOffset = coordinates.x * size,
            yOffset = coordinates.y * size,

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

            flatScale= manager.Settings.flatScale,
            flatOctaves = manager.Settings.flatOctaves,
            flatPersistance = manager.Settings.flatPersistance,
            flatLacunarity = manager.Settings.flatLacunarity,
            flatPower = manager.Settings.flatPower,
        };

        var erosionJob = new ErosionJob
        {
            heightMap = heightMap,
            result = erodedHeightMap,
            width = vertexDimension,
            xOffset = coordinates.x * size,
            yOffset = coordinates.y * size,
        };


        var meshJob = new MeshMakerJob
        {
            heightMap = erodedHeightMap,
            vertices = verts,
            uvs = uvs,
            triangles = tris,
            width = vertexDimension,
            heightMultiplier = manager.Settings.heightMultiplier
        };

        

        JobHandle heightHandle = heightJob.Schedule(heightMap.Length, 64);
        JobHandle erosionHandle = erosionJob.Schedule(heightMap.Length, 64, heightHandle);

        JobHandle meshHandle = meshJob.Schedule(vertexCount, 64, erosionHandle);
        
        meshHandle.Complete();


        heightMap.Dispose();
        erodedHeightMap.Dispose();
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
