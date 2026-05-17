using UnityEngine;

public class ChunkQuadTree
{
    public ChunkNode root;
}

public class ChunkNode
{
    public Vector3 center;
    public float size;
    public int lodLevel;

    public ChunkNode childA;
    public ChunkNode childB;
    public ChunkNode childC;
    public ChunkNode childD;

    public bool isLeaf;

    public ChunkNode(Vector3 centerPosition, float nodeSize, int lod)
    {
        this.center = centerPosition;
        this.size = nodeSize;
        this.lodLevel = lod;
        this.isLeaf = true;
    }

    public void Subdivide()
    {
        if (!isLeaf || lodLevel >= 10) return;

        float quarterSize = size / 4f;
        float halfSize = size / 2f;

        Vector3 topLeft = center + new Vector3(-quarterSize, 0, quarterSize);
        Vector3 topRight = center + new Vector3(quarterSize, 0, quarterSize);
        Vector3 bottomLeft = center + new Vector3(-quarterSize, 0, -quarterSize);
        Vector3 bottomRight = center + new Vector3(quarterSize, 0, -quarterSize);

        childA = new ChunkNode(topLeft, halfSize, lodLevel + 1);
        childB = new ChunkNode(topRight, halfSize, lodLevel + 1);
        childC = new ChunkNode(bottomLeft, halfSize, lodLevel + 1);
        childD = new ChunkNode(bottomRight, halfSize, lodLevel + 1);

        isLeaf = false;
    }

    public void Merge()
    {
        if (isLeaf) return;

        childA = null;
        childB = null;
        childC = null;
        childD = null;

        isLeaf = true;
    }

}
