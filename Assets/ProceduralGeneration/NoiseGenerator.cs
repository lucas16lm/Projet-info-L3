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
    [WriteOnly]
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<float2> uvs;

    public int width;
    public float heightMultiplier;

    public void Execute(int index)
    {
        int x = index % width;
        int z = index / width;

        float y = heightMap[index] * heightMultiplier;
        vertices[index] = new float3(x, y, z);

        float u = math.saturate(heightMap[index]);
        uvs[index] = new float2(u, 0f);

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
    public float innerRadius;
    public float power;

    public float noiseScale;
    public float noiseStrength;

    public void Execute(int index)
    {
        if (radius == 0)
        {
            result[index] = 0;
            return;
        }

        int x = index % size;
        int y = index / size;

        float2 pos = new float2(x, y);
        float offsetX = noise.snoise(pos * noiseScale) * noiseStrength;
        float offsetY = noise.snoise(new float2(pos.x + 1000f, pos.y + 1000f) * noiseScale) * noiseStrength;

        float dx = math.abs((x + offsetX) - centerX);
        float dy = math.abs((y + offsetY) - centerY);


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

        float falloffRange = math.max(0.0001f, radius - innerRadius);
        float noiseValue = 1f - math.saturate((d - innerRadius) / falloffRange);

        result[index] = math.pow(noiseValue, power);
    }
}

[BurstCompile]
public struct CoastalErosionJob : IJobParallelFor
{
    public NativeArray<float> map;

    public float seaLevel;
    public float waveRange;
    public float erosionForce; // Ex: 0.05f pour une érosion douce par itération
    public float sedimentationRate; // Ex: 0.01f pour combler lentement les abysses

    public void Execute(int index)
    {
        float height = map[index];
        float distToSeaLevel = math.abs(height - seaLevel);

        // 1. Action des vagues (Création de plages et hauts-fonds)
        if (distToSeaLevel < waveRange)
        {
            // La force est de 1.0 exactement au niveau de la mer, et tombe à 0 sur les bords.
            float force = 1.0f - (distToSeaLevel / waveRange);

            // On adoucit la courbe d'impact pour un résultat plus naturel
            force = force * force;

            // On "tire" le terrain vers le niveau de la mer.
            // Cela rabote les falaises et remblaie les zones juste sous l'eau, formant une plage.
            map[index] = math.lerp(height, seaLevel, force * erosionForce);
        }
        // 2. Sédimentation des fonds marins (Loin de l'agitation des vagues)
        else if (height < seaLevel - waveRange)
        {
            float depth = seaLevel - height;

            // Les sédiments s'accumulent doucement en fonction de la profondeur
            map[index] = height + (depth * sedimentationRate);
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
        //result[index] = math.saturate(result[index] * mask[index]);
        result[index] = math.saturate(result[index] - (1 - mask[index]));
    }
}