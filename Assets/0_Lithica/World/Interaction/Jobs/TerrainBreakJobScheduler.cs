using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class TerrainBreakJobScheduler
{
    public static TerrainBreakJobResult ScheduleBrush(
        WorldManager world,
        Vector2 worldPos,
        float breakRadius)
    {
        int minPx = world.WorldToDamagePixelX(worldPos.x - breakRadius);
        int maxPx = world.WorldToDamagePixelX(worldPos.x + breakRadius);
        int minPy = world.WorldToDamagePixelY(worldPos.y - breakRadius);
        int maxPy = world.WorldToDamagePixelY(worldPos.y + breakRadius);

        minPx = Mathf.Clamp(minPx, 0, world.DamageResolutionX - 1);
        maxPx = Mathf.Clamp(maxPx, 0, world.DamageResolutionX - 1);
        minPy = Mathf.Clamp(minPy, 0, world.DamageResolutionY - 1);
        maxPy = Mathf.Clamp(maxPy, 0, world.DamageResolutionY - 1);

        int width = maxPx - minPx + 1;
        int height = maxPy - minPy + 1;
        int total = width * height;

        NativeArray<int2> pixelCoords = new NativeArray<int2>(total, Allocator.Persistent);
        NativeArray<byte> hitMask = new NativeArray<byte>(total, Allocator.Persistent);

        int index = 0;
        for (int py = minPy; py <= maxPy; py++)
        {
            for (int px = minPx; px <= maxPx; px++)
            {
                pixelCoords[index] = new int2(px, py);
                index++;
            }
        }

        float pixelWorldSize = world.cellSize / world.TilePixelSize;

        DirectionalBrush job = new DirectionalBrush
        {
            PixelCoords = pixelCoords,
            BrushWorldPos = new float2(worldPos.x, worldPos.y),
            RadiusSq = breakRadius * breakRadius,
            PixelWorldSize = pixelWorldSize,
            WorldOrigin = new float2(world.transform.position.x, world.transform.position.y),
            Result = hitMask
        };

        JobHandle handle = job.Schedule(total, 64);

        return new TerrainBreakJobResult
        {
            Handle = handle,
            PixelCoords = pixelCoords,
            HitMask = hitMask,
            Count = total
        };
    }
}