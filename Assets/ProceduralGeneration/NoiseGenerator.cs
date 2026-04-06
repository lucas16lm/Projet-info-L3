using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

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
    public float power;

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
            float simplex = noise.snoise(worldPos * (1f/scale) * frequency);
            simplex = (simplex + 1) * 0.5f;

            noiseHeight += simplex * amplitude;
            totalAmplitude += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        float finalValue = noiseHeight / totalAmplitude;
        finalValue = math.pow(finalValue, power);
        result[index] = math.saturate(finalValue);
    }
}

[BurstCompile]
public struct FallOfJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<float> result;
    public int p;
    public int size;
    public int centerX;
    public int centerY;
    public float radius;
    public float power;

    public void Execute(int index)
    {
        int x = index % size;
        int y = index / size;

        float dx = math.abs(x - centerX);
        float dy = math.abs(y - centerY);


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
        noiseValue = math.pow(noiseValue, power);

        result[index] = math.saturate(noiseValue);
    }
}

[BurstCompile]
public struct CellularHydraulicErosionJob : IJob
{
    public NativeArray<float> map;
    public int mapSize;
    public int numDroplets;
    public Unity.Mathematics.Random random;


    public void Execute()
    {
        for (int i = 0; i < numDroplets; i++)
        {
            int currentIndex = random.NextInt(0, map.Length);
            float water = 1.0f;
            float sediment = 0.0f;

            while (water > 0)
            {
                int x = currentIndex % mapSize;
                int y = currentIndex / mapSize;

                int nextIndex = currentIndex;
                float lowestHeight = map[currentIndex];

                if (x > 0)
                {
                    int leftNeighbor = currentIndex - 1;
                    if (map[leftNeighbor] < lowestHeight)
                    {
                        lowestHeight = map[leftNeighbor];
                        nextIndex = leftNeighbor;
                    }
                }

   
                if (x < mapSize - 1)
                {
                    int rightNeighbor = currentIndex + 1;
                    if (map[rightNeighbor] < lowestHeight)
                    {
                        lowestHeight = map[rightNeighbor];
                        nextIndex = rightNeighbor;
                    }
                }


                if (y < mapSize - 1)
                {
                    int upNeighbor = currentIndex + mapSize;
                    if (map[upNeighbor] < lowestHeight)
                    {
                        lowestHeight = map[upNeighbor];
                        nextIndex = upNeighbor;
                    }
                }


                if (y > 0)
                {
                    int downNeighbor = currentIndex - mapSize;
                    if (map[downNeighbor] < lowestHeight)
                    {
                        lowestHeight = map[downNeighbor];
                        nextIndex = downNeighbor;
                    }
                }

                if (nextIndex == currentIndex)
                {
                    map[currentIndex] += sediment;
                    if (currentIndex + 1 >= 0 && currentIndex + 1 < map.Length) map[currentIndex + 1] += sediment / 2;
                    if (currentIndex - 1 >= 0 && currentIndex - 1 < map.Length) map[currentIndex - 1] += sediment / 2;
                    if (currentIndex + mapSize >= 0 && currentIndex + mapSize < map.Length) map[currentIndex + mapSize] += sediment / 2;
                    if (currentIndex - mapSize >= 0 && currentIndex - mapSize < map.Length) map[currentIndex - mapSize] += sediment / 2;
                    break;
                }

                float heightDiff = map[currentIndex] - map[nextIndex];
                float erosionAmount = heightDiff * 0.04f;

                map[currentIndex] -= erosionAmount;
                if (currentIndex + 1 >= 0 && currentIndex + 1 < map.Length) map[currentIndex + 1] -= erosionAmount / 2;
                if (currentIndex - 1 >= 0 && currentIndex - 1 < map.Length) map[currentIndex - 1] -= erosionAmount / 2;
                if (currentIndex + mapSize >= 0 && currentIndex + mapSize < map.Length) map[currentIndex + mapSize] -= erosionAmount / 2;
                if (currentIndex - mapSize >= 0 && currentIndex - mapSize < map.Length) map[currentIndex - mapSize] -= erosionAmount / 2;

                sediment += erosionAmount;

                currentIndex = nextIndex;
                water -= 0.05f;
            }
            map[currentIndex] += sediment;
            if(currentIndex + 1 >= 0 && currentIndex + 1 < map.Length) map[currentIndex + 1] += sediment / 2;
            if (currentIndex - 1 >= 0 && currentIndex - 1 < map.Length) map[currentIndex - 1] += sediment / 2;
            if (currentIndex + mapSize >= 0 && currentIndex + mapSize < map.Length) map[currentIndex + mapSize] += sediment / 2;
            if (currentIndex - mapSize >= 0 && currentIndex - mapSize < map.Length) map[currentIndex - mapSize] += sediment / 2;
        }    
    }
}

[BurstCompile]
public struct ApplyMaskJob : IJobParallelFor
{
    public NativeArray<float> result;
    [ReadOnly] public NativeArray<float> mask;

    public void Execute(int index)
    {
        result[index] = math.saturate(result[index] - (1 - mask[index]));
    }
}