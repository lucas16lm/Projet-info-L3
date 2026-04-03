using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk : MonoBehaviour
{
    public void GenerateMesh(int chunkSize, float scale, float xOffset, float yOffset, float power, float heightMultiplier, float islandSize, int centerX, int centerY)
    {
        // 1. Définition des tailles (TRES IMPORTANT)
        int vertexDimension = chunkSize + 1;
        int vertexCount = vertexDimension * vertexDimension;
        int faceCount = chunkSize * chunkSize;

        // 2. Allocation du MeshData
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        // 3. Configuration des attributs (Position seulement pour l'instant)
        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp);
        vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        meshData.SetIndexBufferParams(faceCount * 6, IndexFormat.UInt32);

        NativeArray<float3> verts = meshData.GetVertexData<float3>();
        NativeArray<int> tris = meshData.GetIndexData<int>();

        // CORRECTION 1 : Le masque doit avoir la taille des sommets (vertexCount)
        NativeArray<float> maskMap = new NativeArray<float>(vertexCount, Allocator.TempJob);
        // CORRECTION 2 : heightMap a été retiré car inutilisé (évite le Memory Leak)

        var maskJob = new FallOfJob
        {
            result = maskMap,
            p = 2,
            width = vertexDimension, // On utilise la dimension des sommets
            centerX = centerX,
            centerY = centerY,
            radius = islandSize,
            warpScale = 0.05f,
            warpStrength = 1
        };

        var meshJob = new MeshMakerJob
        {
            heightMap = maskMap, // Le masque sert de HeightMap ici
            vertices = verts,
            triangles = tris,
            width = vertexDimension, // On passe la dimension des sommets
            heightMultiplier = heightMultiplier
        };

        // 4. Exécution des Jobs
        JobHandle noiseHandle = maskJob.Schedule(maskMap.Length, 64);
        JobHandle meshHandle = meshJob.Schedule(vertexCount, 64, noiseHandle);
        meshHandle.Complete();

        // 5. Nettoyage mémoire
        maskMap.Dispose();
        vertexAttributes.Dispose();

        // 6. Finalisation du Mesh
        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, faceCount * 6));

        Mesh mesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // CORRECTION 3 : Vérification des composants pour éviter de les empiler
        if (!TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (!TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = mesh;
    }
}
