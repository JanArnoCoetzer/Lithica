using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct DirectionalBrush : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> PixelCoords;
    [ReadOnly] public float2 BrushWorldPos;
    [ReadOnly] public float RadiusSq;
    [ReadOnly] public float PixelWorldSize;
    [ReadOnly] public float2 WorldOrigin;
    [WriteOnly] public NativeArray<byte> Result;

    public void Execute(int index)
    {
        int2 pixel = PixelCoords[index];

        float2 pixelWorldCenter = new float2(
            WorldOrigin.x + (pixel.x + 0.5f) * PixelWorldSize,
            WorldOrigin.y + (pixel.y + 0.5f) * PixelWorldSize
        );

        float2 delta = pixelWorldCenter - BrushWorldPos;
        float distSq = math.dot(delta, delta);

        Result[index] = distSq <= RadiusSq ? (byte)1 : (byte)0;
    }

    public static void DebugPrintResults(
        NativeArray<int2> pixelCoords,
        NativeArray<byte> result,
        int maxCount = 32)
    {
        int count = math.min(pixelCoords.Length, maxCount);

        for (int i = 0; i < count; i++)
        {
            Debug.Log(
                $"[DirectionalBrush] index={i} pixel=({pixelCoords[i].x},{pixelCoords[i].y}) result={result[i]}"
            );
        }
    }
}