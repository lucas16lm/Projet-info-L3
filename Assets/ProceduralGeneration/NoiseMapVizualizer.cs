using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.noise;

public class NoiseMapVizualizer : MonoBehaviour
{
    [SerializeField] private int size = 100;

    [Header("Mask parameters")]
    [SerializeField] [Range(1, 6)] private int p = 1;
    [SerializeField] float radius = 1;
    [SerializeField] int centerX = 0;
    [SerializeField] int centerY = 0;

    [Header("Height parameters")]
    public float scale;
    public float xOffset;
    public float yOffset;

    public int octaves;
    public float persistence;
    public float lacunarity;

    private Texture2D texture;


    private void Start()
    {
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }

    public void GenerateTexture()
    {
        NativeArray<float> maskMap = new NativeArray<float>(size * size, Allocator.TempJob);
        NativeArray<float> heightMap = new NativeArray<float>(size * size, Allocator.TempJob);

        var maskJob = new FallOfJob
        {
            result = maskMap,
            p = p,
            size = size,
            centerX = centerX,
            centerY = centerY,
            radius = radius,
        };

        var heightJob = new FractalSimplexJob
        {
            result = heightMap,
            width = size,
            scale = scale,
            xOffset = xOffset,
            yOffset = yOffset,

            octaves = octaves,
            persistence = persistence,
            lacunarity = lacunarity

        };

        NativeArray<Color32> colorMap = texture.GetRawTextureData<Color32>();

        var colorJob = new ColorConverterJob
        {
            input = heightMap,
            output = colorMap
        };

        var multiplicationJob = new ApplyMaskJob
        {
            result = heightMap,
            mask = maskMap
        };

        JobHandle maskHandle = maskJob.Schedule(maskMap.Length, 64);
        JobHandle heightHandle = heightJob.Schedule(heightMap.Length, 64, maskHandle);
        JobHandle applyMask = multiplicationJob.Schedule(heightMap.Length, 64, heightHandle);
        JobHandle colorHandle = colorJob.Schedule(colorMap.Length, 64, applyMask);
        

        colorHandle.Complete();
        texture.Apply();
        maskMap.Dispose();
        heightMap.Dispose();
    }

    private void Update()
    {
        GenerateTexture();
    }


}
