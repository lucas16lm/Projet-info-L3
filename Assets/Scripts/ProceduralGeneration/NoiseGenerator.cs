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
public struct MeshMakerJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> heightMap;
    [WriteOnly] public NativeArray<float3> vertices;
    [NativeDisableParallelForRestriction]
    [WriteOnly] public NativeArray<int> triangles;

    public int width;
    public float heightMultiplier;
    public int meshSimplificationIncrement;

    public void Execute(int index)
    {
        int x = index % width;
        int z = index / width;
        float y = heightMap[index] * heightMultiplier;
        
        vertices[index] = new float3(
            x * meshSimplificationIncrement,
            y,
            z * meshSimplificationIncrement
        );
        

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
public struct HeightMapJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<float> result;
    public int width;
    public float xOffset;
    public float yOffset;
    public int meshSimplificationIncrement;

    public float islandsScale;
    public int islandsOctaves;
    public float islandsPersistance;
    public float islandsLacunarity;
    public float islandspower;
    public float islandsProximityFactor;

    public float warpScale;
    public float warpStrength;

    public float mountainsScale;
    public int mountainsOctaves;
    public float mountainsPersistance;
    public float mountainsLacunarity;
    public float mountainsPower;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;


        float2 basePos = new float2(
            xOffset + (x * meshSimplificationIncrement),
            yOffset + (y * meshSimplificationIncrement)
        );

        float islandNoise = IslandsFractal(basePos);
        float mountainNoise = MountainFractal(basePos);

        float transitionMask = math.smoothstep(0f, 0.5f, mountainNoise);
        float landTerrain = math.lerp(0.35f, mountainNoise, transitionMask*0.8f);

        float finalHeight = landTerrain * islandNoise;
        result[index] = finalHeight;
        //result[index] = islandNoise;
    }

    private float IslandsFractal(float2 pos)
    {
        float2 warpSamplePos = pos * (1f / warpScale);

        float2 warpOffset = new float2(
            noise.snoise(warpSamplePos),
            noise.snoise(warpSamplePos + new float2(100f, 100f))
        ) * warpStrength;

        float2 worldPos = (pos + warpOffset);

        int octaves = islandsOctaves;
        float persistance = islandsPersistance;
        float lacunarity = islandsLacunarity;

        float noiseValue = 0f;
        float amplitude = 1;
        float frequency = 1f / islandsScale;
        float amplitudeMax = 0f;

        for (int i = 0; i < octaves; i++)
        {
            amplitudeMax += amplitude;

            float islandNoise = 1 - math.saturate(noise.cellular(worldPos * frequency).x * islandsProximityFactor);
            islandNoise = math.pow(islandNoise, islandspower);
            //islandNoise = math.smoothstep(0, 1, islandNoise);

            noiseValue += islandNoise * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        float finalValue = noiseValue / amplitudeMax;

        return finalValue;
    }

    private float MountainFractal(float2 pos)
    {
        int octaves = mountainsOctaves;
        float persistance = mountainsPersistance;
        float lacunarity = mountainsLacunarity;

        float noiseValue = 0f;
        float amplitude = 1;
        float frequency = 1f / mountainsScale;
        float amplitudeMax = 0f;

        for (int i = 0; i < octaves; i++)
        {
            amplitudeMax += amplitude;

            float ridge = 1f - math.abs(noise.snoise(pos * frequency));
            ridge = math.smoothstep(0,1, ridge);
            ridge = math.pow(ridge, mountainsPower);

            noiseValue += ridge * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        return noiseValue/amplitudeMax;
    }
}


[BurstCompile]
public struct JitteredGridParallelJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<float2> results;

    public float chunkSize;
    public float cellSize;
    public int gridWidth;

    public float randomness;
    public float treeProbability;
    public uint chunkSeed;

    public void Execute(int index)
    {
        int x = index % gridWidth;
        int y = index / gridWidth;

        uint cellHash = math.hash(new uint3((uint)x, (uint)y, chunkSeed));
        var cellRandom = new Unity.Mathematics.Random(cellHash == 0 ? 1 : cellHash);

        if (cellRandom.NextFloat() <= treeProbability)
        {
            float2 cellCenter = new float2(x * cellSize + cellSize * 0.5f, y * cellSize + cellSize * 0.5f);

            float maxOffset = (cellSize * 0.5f) * randomness;
            float2 offset = cellRandom.NextFloat2(new float2(-maxOffset, -maxOffset), new float2(maxOffset, maxOffset));

            float2 finalPos = cellCenter + offset;

            if (finalPos.x >= 0 && finalPos.x < chunkSize && finalPos.y >= 0 && finalPos.y < chunkSize)
            {
                results[index] = finalPos;
                return;
            }
        }

        results[index] = new float2(-1f, -1f);
    }
}