using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class TerrainDestructionController
{
    public delegate bool TryDestroyPixelDelegate(int px, int py);

    public static bool BrushDestroy(
        WorldManager world,
        Vector2 worldPos,
        float breakRadius,
        TryDestroyPixelDelegate tryDestroyPixel,
        out Vector2 minWorld,
        out Vector2 maxWorld)
    {
        minWorld = Vector2.zero;
        maxWorld = Vector2.zero;

        if (world == null)
            return false;

        world.EnsureDamageMask();

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

        if (total <= 0)
            return false;

        NativeArray<int2> pixelCoords = new NativeArray<int2>(total, Allocator.TempJob);
        NativeArray<byte> result = new NativeArray<byte>(total, Allocator.TempJob);

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
            BrushWorldPos = worldPos,
            RadiusSq = breakRadius * breakRadius,
            PixelWorldSize = pixelWorldSize,
            WorldOrigin = new float2(world.transform.position.x, world.transform.position.y),
            Result = result
        };

        JobHandle handle = job.Schedule(total, 64);
        handle.Complete();

        bool changed = false;
        Vector2 minChanged = Vector2.positiveInfinity;
        Vector2 maxChanged = Vector2.negativeInfinity;

        for (int i = 0; i < total; i++)
        {
            if (result[i] == 0)
                continue;

            int px = pixelCoords[i].x;
            int py = pixelCoords[i].y;

            if (!IsSolidDamagePixel(world, px, py))
                continue;

            if (tryDestroyPixel(px, py))
            {
                changed = true;

                Vector2 pixelWorld = TerrainSamplingHelper.DamagePixelToWorldCenter(world, px, py);
                minChanged = Vector2.Min(minChanged, pixelWorld);
                maxChanged = Vector2.Max(maxChanged, pixelWorld);
            }
        }

        pixelCoords.Dispose();
        result.Dispose();

        if (!changed)
            return false;

        Vector2 padding = Vector2.one * pixelWorldSize;
        minWorld = minChanged - padding;
        maxWorld = maxChanged + padding;
        return true;
    }

    public static bool IsSolidDamagePixel(WorldManager world, int px, int py)
    {
        if (world == null)
            return false;

        if (px < 0 || px >= world.DamageResolutionX || py < 0 || py >= world.DamageResolutionY)
            return false;

        Vector2 worldPos = TerrainSamplingHelper.DamagePixelToWorldCenter(world, px, py);

        float density = TerrainSamplingHelper.SampleDensityAtWorld(world, worldPos);
        if (!world.IsSolid(density))
            return false;

        if (world.IsDestroyedPixel(px, py))
            return false;

        return true;
    }
}