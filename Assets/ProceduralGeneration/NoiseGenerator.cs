using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class NoiseGenerator
{
    
}


[BurstCompile]
public struct ColorConverterJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<Color32> output;
    [ReadOnly] public NativeArray<float> input;

    public void Execute(int index)
    {
        byte c = (byte)(math.saturate(input[index]) * 255);
        output[index] = new Color32(c, c, c, 255);
    }
}

[BurstCompile]
public struct MeshMakerJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> heightMap;

    [WriteOnly]
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<float3> vertices;
    [WriteOnly]
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<int> triangles;

    public int width;
    public float heightMultiplier;

    public void Execute(int index)
    {
        int x = index % width;
        int z = index / width;

        float y = heightMap[index] * heightMultiplier;
        vertices[index] = new float3(x, y, z);

        if (x < width - 1 && z < width - 1)
        {
            int triIndex = (z * (width - 1) + x) * 6;
            int v0 = index;
            int v1 = index + width;
            int v2 = index + 1;
            int v3 = index + width + 1;

            triangles[triIndex] = v0;
            triangles[triIndex + 1] = v1;
            triangles[triIndex + 2] = v2;
            triangles[triIndex + 3] = v2;
            triangles[triIndex + 4] = v1;
            triangles[triIndex + 5] = v3;
        }
    }
}

[BurstCompile]
public struct FractalSimplexJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<float> result;
    public int width;
    public float scale;
    public float xOffset;
    public float yOffset;

    public int octaves;
    public float persistence;
    public float lacunarity;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float2 worldPos = new float2(xOffset + x, yOffset + y);

        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;
        float totalAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float simplex = noise.snoise(worldPos * scale * frequency);
            noiseHeight += simplex * amplitude;
            totalAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        float finalValue = (noiseHeight / totalAmplitude + 1f) * 0.5f;
        result[index] = math.saturate(finalValue);
    }
}

[BurstCompile]
public struct WorleyMaskJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<float> result;
    public int width;
    public float scale;
    public float xOffset;
    public float yOffset;
    public float power;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float invScale = 1.0f / math.max(scale, 0.0001f);
        float2 samplePos = new float2(xOffset + x, yOffset + y) * invScale;

        float noiseValue = noise.cellular(samplePos).x;

        noiseValue = 1 - math.saturate(noiseValue);
        noiseValue = math.pow(noiseValue, power);
        result[index] = math.saturate(noiseValue);
    }
}

[BurstCompile]
public struct FallOfJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<float> result;
    public int p;
    public int width;
    public int centerX;
    public int centerY;
    public float radius;
    public float warpScale;
    public float warpStrength;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float2 noisePos = new float2(x, y) * warpScale;
        float angle = noise.snoise(noisePos) * warpStrength;

        float relX = x - centerX;
        float relY = y - centerY;

        float cosA = math.cos(angle);
        float sinA = math.sin(angle);

        float twistedX = relX * cosA - relY * sinA;
        float twistedY = relX * sinA + relY * cosA;

        float dx = math.abs(twistedX);
        float dy = math.abs(twistedY);


        float d = 0;
        if (p >= 6)
        {
            d = math.max(dx, dy);
        }
        else if (p == 2)
        {
            d = math.sqrt(dx * dx + dy * dy);
        }
        else
        {
            float a = math.pow(dx, p);
            float b = math.pow(dy, p);
            d = math.pow(a + b, 1.0f / p);
        }

        float noiseValue = 1.0f - math.smoothstep(0, radius, d);

        result[index] = math.saturate(noiseValue);
    }
}

[BurstCompile]
public struct ApplyMaskJob : IJobParallelFor
{
    public NativeArray<float> result;
    [ReadOnly] public NativeArray<float> mask;

    public void Execute(int index)
    {
        result[index] = math.saturate(result[index] * mask[index]);
    }
}