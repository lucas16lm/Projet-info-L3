using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.noise;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] int seed;
    [SerializeField] float scale = 10f;
    [SerializeField] int offset = 0;
    [SerializeField] int width = 256;
    [SerializeField] int height = 256;
    public GameObject quad;

    [Header("Fractal Noise (fBm)")]
    [SerializeField, Range(1, 8)] int octaves = 4;
    [SerializeField, Range(0f, 1f)] float persistence = 0.5f;
    [SerializeField] float lacunarity = 2f;

    private Texture2D texture;

    private void Start()
    {
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        quad.GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }

    public void GenerateMap()
    {
        NativeArray<Color32> pixelData = texture.GetRawTextureData<Color32>();

        var job = new GenerateNoiseMapJob
        {
            results = pixelData,
            width = width,
            height = height,
            scale = scale,
            offset = offset,
            octaves = octaves,
            persistence = persistence,
            lacunarity = lacunarity
        };

        JobHandle handle = job.Schedule(pixelData.Length, 64);
        handle.Complete();
        texture.Apply();
    }

    private void Update()
    {
        GenerateMap();
    }


}

[BurstCompile]
public struct GenerateNoiseMapJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<Color32> results;
    public int width;
    public int height;
    public float scale;
    public float offset;

    public int octaves;
    public float persistence;
    public float lacunarity;

    public void Execute(int index)
    {
        int y = index / width;
        int x = index % width;

        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;
        float maxPossibleHeight = 0f;

        float baseX = (x / (float)width) * scale;
        float baseY = (y / (float)height) * scale;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (baseX + offset) * frequency;
            float sampleY = (baseY + offset) * frequency;

            float noiseValue = snoise(new float2(sampleX, sampleY));
            noiseHeight += noiseValue * amplitude;
            maxPossibleHeight += amplitude;
            
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        float normalizedHeight = noiseHeight / maxPossibleHeight;
        normalizedHeight = (normalizedHeight + 1f) * 0.5f;

        byte colorValue = (byte)(math.saturate(normalizedHeight) * 255);
        results[index] = new Color32(colorValue, colorValue, colorValue, 255);
    }
}
