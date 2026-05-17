using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SphereGenerator : MonoBehaviour
{
    public int radius;

    public FaceCube[] faces;
    public int LOD = 0;
    public float noiseAmplitude = 2.0f;
    public float noiseFrequency = 1.5f;

    private void Start()
    {
        FaceCube faceA = new FaceCube(radius, Vector3.up, transform, noiseAmplitude, noiseFrequency);
        FaceCube faceB = new FaceCube(radius, Vector3.down, transform, noiseAmplitude, noiseFrequency);
        FaceCube faceC = new FaceCube(radius, Vector3.left, transform, noiseAmplitude, noiseFrequency);
        FaceCube faceD = new FaceCube(radius, Vector3.right, transform, noiseAmplitude, noiseFrequency);
        FaceCube faceE = new FaceCube(radius, Vector3.forward, transform, noiseAmplitude, noiseFrequency);
        FaceCube faceF = new FaceCube(radius, Vector3.back, transform, noiseAmplitude, noiseFrequency);

        faces = new FaceCube[] { faceA, faceB, faceC, faceD, faceE, faceF };

        foreach (FaceCube face in faces)
        {
            face.GenerateMesh(LOD);
        }
    }

}

public class FaceCube
{
    private Transform parent;
    private float radius;
    private Vector3 normal;
    float noiseAmplitude;
    float noiseFrequency;

    public FaceCube(float radius, Vector3 normal, Transform parent, float noiseAmplitude, float noiseFrequency)
    {
        this.radius = radius;
        this.normal = normal;
        this.parent = parent;
        this.noiseAmplitude = noiseAmplitude;
        this.noiseFrequency = noiseFrequency;
    }

    public void GenerateMesh(int maxLOD)
    {
        GameObject go = new GameObject("Face");
        go.transform.parent = parent;

        Mesh mesh = new Mesh();

        Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
        Vector3 axisB = Vector3.Cross(normal, axisA);

        Vector3 faceCenter = normal * radius;
        float initialSize = radius * 2f;

        // Préparation des listes dynamiques pour le Quadtree
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // APPEL DE LA MÉTHODE RÉCURSIVE AU LIEU DU QUAD EN DUR
        SubdivideFace(faceCenter, initialSize, 0, maxLOD, axisA, axisB, vertices, triangles);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshRenderer rd = go.AddComponent<MeshRenderer>();
        MeshFilter filter = go.AddComponent<MeshFilter>();

        filter.mesh = mesh;
        rd.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }

    private void SubdivideFace(Vector3 nodeCenter, float nodeSize, int currentLOD, int maxLOD, Vector3 axisA, Vector3 axisB, List<Vector3> vertices, List<int> triangles)
    {
        if (currentLOD < maxLOD)
        {
            // ON SUBDIVISE EN 4 NOUVEAUX QUADS
            float halfSize = nodeSize / 2f;
            float offset = nodeSize / 4f;

            // Enfant Bas-Gauche
            SubdivideFace(nodeCenter - axisA * offset - axisB * offset, halfSize, currentLOD + 1, maxLOD, axisA, axisB, vertices, triangles);
            // Enfant Haut-Gauche
            SubdivideFace(nodeCenter - axisA * offset + axisB * offset, halfSize, currentLOD + 1, maxLOD, axisA, axisB, vertices, triangles);
            // Enfant Bas-Droite
            SubdivideFace(nodeCenter + axisA * offset - axisB * offset, halfSize, currentLOD + 1, maxLOD, axisA, axisB, vertices, triangles);
            // Enfant Haut-Droite
            SubdivideFace(nodeCenter + axisA * offset + axisB * offset, halfSize, currentLOD + 1, maxLOD, axisA, axisB, vertices, triangles);
        }
        else
        {
            // ON A ATTEINT LA LIMITE, ON DESSINE LE QUAD
            int startIndex = vertices.Count;

            Vector3 v0 = nodeCenter - axisA * (nodeSize / 2f) - axisB * (nodeSize / 2f);
            Vector3 v1 = nodeCenter - axisA * (nodeSize / 2f) + axisB * (nodeSize / 2f);
            Vector3 v2 = nodeCenter + axisA * (nodeSize / 2f) - axisB * (nodeSize / 2f);
            Vector3 v3 = nodeCenter + axisA * (nodeSize / 2f) + axisB * (nodeSize / 2f);

            // 1. Obtenir les directions normalisées (points sur une sphère de rayon 1)
            Vector3 dir0 = v0.normalized;
            Vector3 dir1 = v1.normalized;
            Vector3 dir2 = v2.normalized;
            Vector3 dir3 = v3.normalized;


            // 2. Calculer l'élévation avec le bruit
            float elevation0 = noise.snoise(new float3(dir0.x, dir0.y, dir0.z) * noiseFrequency) * noiseAmplitude + noise.snoise(new float3(dir0.x, dir0.y, dir0.z) * noiseFrequency / 2) * noiseAmplitude / 2 + noise.snoise(new float3(dir0.x, dir0.y, dir0.z) * noiseFrequency / 4) * noiseAmplitude / 4;
            float elevation1 = noise.snoise(new float3(dir1.x, dir1.y, dir1.z) * noiseFrequency) * noiseAmplitude + noise.snoise(new float3(dir1.x, dir1.y, dir1.z) * noiseFrequency / 2) * noiseAmplitude / 2 + noise.snoise(new float3(dir1.x, dir1.y, dir1.z) * noiseFrequency / 4) * noiseAmplitude / 4;
            float elevation2 = noise.snoise(new float3(dir2.x, dir2.y, dir2.z) * noiseFrequency) * noiseAmplitude + noise.snoise(new float3(dir2.x, dir2.y, dir2.z) * noiseFrequency / 2) * noiseAmplitude / 2 + noise.snoise(new float3(dir2.x, dir2.y, dir2.z) * noiseFrequency / 4) * noiseAmplitude / 4; ;
            float elevation3 = noise.snoise(new float3(dir3.x, dir3.y, dir3.z) * noiseFrequency) * noiseAmplitude + noise.snoise(new float3(dir3.x, dir3.y, dir3.z) * noiseFrequency / 2) * noiseAmplitude / 2 + noise.snoise(new float3(dir3.x, dir3.y, dir3.z) * noiseFrequency / 4) * noiseAmplitude / 4; ;

            // 3. Appliquer : Direction * (Rayon de base + Élévation)
            vertices.Add(dir0 * (radius + elevation0));
            vertices.Add(dir1 * (radius + elevation1));
            vertices.Add(dir2 * (radius + elevation2));
            vertices.Add(dir3 * (radius + elevation3));

            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 0);

            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 3);
            triangles.Add(startIndex + 1);
        }
    }
}
