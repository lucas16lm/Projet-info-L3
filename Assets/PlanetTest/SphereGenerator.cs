using UnityEngine;

public class SphereGenerator : MonoBehaviour
{
    [SerializeField] private int radius;

    private void Start()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    private void Generate()
    {
        for (int x = -radius; x < radius; x++)
        {
            for (int y = -radius; y < radius; y++)
            {
                for (int z = -radius; z < radius; z++)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.position = new Vector3(x, y, z).normalized * radius;
                }
            }
        }
    }
}
