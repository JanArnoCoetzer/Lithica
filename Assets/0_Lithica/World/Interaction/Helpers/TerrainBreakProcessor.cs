using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public static class TerrainBreakProcessor
{
    public static bool BreakBrushAtWorld(
        TerrainBreakContext context,
        Vector2 worldPos,
        float breakRadius,
        TerrainBreakGizmos gizmos,
        Object logContext,
        out int changedCount)
    {
        changedCount = 0;

        if (context.World == null || !context.World.enableDestruction)
            return false;

        if (!context.BrushTouchesSolidTerrain(worldPos, breakRadius))
            return false;

        context.World.EnsureDamageMask();

        int minPx = context.World.WorldToDamagePixelX(worldPos.x - breakRadius);
        int maxPx = context.World.WorldToDamagePixelX(worldPos.x + breakRadius);
        int minPy = context.World.WorldToDamagePixelY(worldPos.y - breakRadius);
        int maxPy = context.World.WorldToDamagePixelY(worldPos.y + breakRadius);

        minPx = Mathf.Clamp(minPx, 0, context.World.DamageResolutionX - 1);
        maxPx = Mathf.Clamp(maxPx, 0, context.World.DamageResolutionX - 1);
        minPy = Mathf.Clamp(minPy, 0, context.World.DamageResolutionY - 1);
        maxPy = Mathf.Clamp(maxPy, 0, context.World.DamageResolutionY - 1);

        int width = maxPx - minPx + 1;
        int height = maxPy - minPy + 1;
        int total = width * height;

        if (total <= 0)
            return false;

        NativeArray<int2> pixelCoords = new(total, Allocator.TempJob);
        NativeArray<byte> result = new(total, Allocator.TempJob);

        int index = 0;
        for (int py = minPy; py <= maxPy; py++)
        {
            for (int px = minPx; px <= maxPx; px++)
            {
                pixelCoords[index] = new int2(px, py);
                index++;
            }
        }

        float pixelWorldSize = context.World.cellSize / context.World.TilePixelSize;

        DirectionalBrush job = new DirectionalBrush
        {
            PixelCoords = pixelCoords,
            BrushWorldPos = new float2(worldPos.x, worldPos.y),
            RadiusSq = breakRadius * breakRadius,
            PixelWorldSize = pixelWorldSize,
            WorldOrigin = new float2(context.World.transform.position.x, context.World.transform.position.y),
            Result = result
        };

        JobHandle handle = job.Schedule(total, 64);
        handle.Complete();

        bool changed = false;

        for (int i = 0; i < total; i++)
        {
            if (result[i] == 0)
                continue;

            int px = pixelCoords[i].x;
            int py = pixelCoords[i].y;

            if (!context.IsSolidDamagePixel(px, py))
                continue;

            if (context.TryDestroyPixelAndRegisterInventory(px, py, logContext))
            {
                changed = true;
                changedCount++;
            }
        }

        pixelCoords.Dispose();
        result.Dispose();

        if (!changed)
            return false;

        Vector2 minWorld = worldPos - Vector2.one * breakRadius;
        Vector2 maxWorld = worldPos + Vector2.one * breakRadius;
        context.RebuildChangedArea(minWorld, maxWorld);
        return true;
    }

    public static bool BreakTowardMouse(
        TerrainBreakContext context,
        Vector2 targetWorldPos,
        Vector3 playerPosition,
        float breakRange,
        int breakWidth,
        TerrainBreakGizmos gizmos)
    {
        gizmos.Clear();

        if (context.World == null || !context.World.enableDestruction)
            return false;

        context.World.EnsureDamageMask();

        Vector2 start = new(playerPosition.x, playerPosition.y);
        Vector2 toMouse = targetWorldPos - start;
        gizmos.SetRay(start, targetWorldPos);

        if (toMouse.sqrMagnitude <= 0.0001f)
            return false;

        Vector2 dir = toMouse.normalized;
        Vector2 perp = new(-dir.y, dir.x);

        float pixelWorldSize = context.World.cellSize / context.World.TilePixelSize;
        float maxDistance = Mathf.Min(breakRange, toMouse.magnitude);
        int steps = Mathf.CeilToInt(maxDistance / pixelWorldSize);
        int widthPixels = Mathf.Max(1, breakWidth);

        bool changed = false;
        Vector2 changedMinWorld = Vector2.positiveInfinity;
        Vector2 changedMaxWorld = Vector2.negativeInfinity;

        for (int step = 1; step <= steps; step++)
        {
            Vector2 centerSample = start + dir * (step * pixelWorldSize);
            gizmos.RecordCenter(centerSample);

            for (int w = 0; w < widthPixels; w++)
            {
                float offsetIndex = w - (widthPixels - 1) * 0.5f;
                Vector2 sampleWorld = centerSample + perp * (offsetIndex * pixelWorldSize);

                int px = context.World.WorldToDamagePixelX(sampleWorld.x);
                int py = context.World.WorldToDamagePixelY(sampleWorld.y);

                if (px < 0 || px >= context.World.DamageResolutionX ||
                    py < 0 || py >= context.World.DamageResolutionY)
                {
                    gizmos.RecordSample(sampleWorld, false, false);
                    continue;
                }

                bool solid = context.IsSolidDamagePixel(px, py);
                bool destroyed = false;

                if (solid && context.TryDestroyPixelAndRegisterInventory(px, py))
                {
                    changed = true;
                    destroyed = true;

                    Vector2 pixelWorld = TerrainSamplingHelper.DamagePixelToWorldCenter(context.World, px, py);
                    changedMinWorld = Vector2.Min(changedMinWorld, pixelWorld);
                    changedMaxWorld = Vector2.Max(changedMaxWorld, pixelWorld);
                    gizmos.MarkHit(pixelWorld);
                }

                gizmos.RecordSample(sampleWorld, solid, destroyed);
            }
        }

        if (!changed)
            return false;

        Vector2 padding = Vector2.one * pixelWorldSize;
        changedMinWorld -= padding;
        changedMaxWorld += padding;

        context.RebuildChangedArea(changedMinWorld, changedMaxWorld);
        return true;
    }

    public static bool BreakInCone(
        TerrainBreakContext context,
        Vector2 targetWorldPos,
        Vector3 playerPosition,
        float breakRange,
        int breakWidth,
        float coneHalfAngle,
        int coneRayCount,
        int forwardRemovalPixels,
        TerrainBreakGizmos gizmos)
    {
        gizmos.Clear();

        if (context.World == null || !context.World.enableDestruction)
            return false;

        context.World.EnsureDamageMask();

        Vector2 start = new(playerPosition.x, playerPosition.y);
        Vector2 toMouse = targetWorldPos - start;
        gizmos.SetRay(start, targetWorldPos);

        if (toMouse.sqrMagnitude <= 0.0001f)
            return false;

        Vector2 baseDir = toMouse.normalized;
        float pixelWorldSize = context.World.cellSize / context.World.TilePixelSize;
        float maxDistance = Mathf.Min(breakRange, toMouse.magnitude);
        int steps = Mathf.CeilToInt(maxDistance / pixelWorldSize);
        int widthPixels = Mathf.Max(1, breakWidth);
        int rayCount = Mathf.Max(1, coneRayCount);
        int extensionPixels = Mathf.Max(1, forwardRemovalPixels);

        bool changed = false;
        Vector2 changedMinWorld = Vector2.positiveInfinity;
        Vector2 changedMaxWorld = Vector2.negativeInfinity;

        for (int ray = 0; ray < rayCount; ray++)
        {
            float angleOffset = Random.Range(-coneHalfAngle, coneHalfAngle);
            Vector2 rayDir = TerrainSamplingHelper.RotateVector(baseDir, angleOffset);
            Vector2 perp = new(-rayDir.y, rayDir.x);

            for (int step = 1; step <= steps; step++)
            {
                Vector2 centerSample = start + rayDir * (step * pixelWorldSize);
                gizmos.RecordCenter(centerSample);

                bool hitSolidThisStep = false;

                for (int w = 0; w < widthPixels; w++)
                {
                    float offsetIndex = w - (widthPixels - 1) * 0.5f;
                    Vector2 sampleWorld = centerSample + perp * (offsetIndex * pixelWorldSize);

                    int px = context.World.WorldToDamagePixelX(sampleWorld.x);
                    int py = context.World.WorldToDamagePixelY(sampleWorld.y);

                    if (px < 0 || px >= context.World.DamageResolutionX ||
                        py < 0 || py >= context.World.DamageResolutionY)
                    {
                        gizmos.RecordSample(sampleWorld, false, false);
                        continue;
                    }

                    bool solid = context.IsSolidDamagePixel(px, py);
                    bool destroyed = false;

                    if (solid)
                    {
                        hitSolidThisStep = true;

                        if (context.TryDestroyPixelAndRegisterInventory(px, py))
                        {
                            changed = true;
                            destroyed = true;

                            Vector2 pixelWorld = TerrainSamplingHelper.DamagePixelToWorldCenter(context.World, px, py);
                            changedMinWorld = Vector2.Min(changedMinWorld, pixelWorld);
                            changedMaxWorld = Vector2.Max(changedMaxWorld, pixelWorld);
                            gizmos.MarkHit(pixelWorld);
                        }

                        DestroyPixelsAfterHitNoCollisionCheck(
                            context,
                            gizmos,
                            sampleWorld,
                            rayDir,
                            extensionPixels,
                            ref changed,
                            ref changedMinWorld,
                            ref changedMaxWorld);
                    }

                    gizmos.RecordSample(sampleWorld, solid, destroyed);
                }

                if (hitSolidThisStep)
                    break;
            }
        }

        if (!changed)
            return false;

        Vector2 padding = Vector2.one * pixelWorldSize;
        changedMinWorld -= padding;
        changedMaxWorld += padding;

        context.RebuildChangedArea(changedMinWorld, changedMaxWorld);
        return true;
    }

    private static void DestroyPixelsAfterHitNoCollisionCheck(
        TerrainBreakContext context,
        TerrainBreakGizmos gizmos,
        Vector2 hitWorld,
        Vector2 forwardDir,
        int lengthPixels,
        ref bool changed,
        ref Vector2 changedMinWorld,
        ref Vector2 changedMaxWorld)
    {
        if (context.World == null)
            return;

        float pixelWorldSize = context.World.cellSize / context.World.TilePixelSize;
        Vector2 dir = forwardDir.sqrMagnitude > 0.0001f ? forwardDir.normalized : Vector2.right;
        int count = Mathf.Max(1, lengthPixels);

        for (int i = 1; i <= count; i++)
        {
            Vector2 sampleWorld = hitWorld + dir * (i * pixelWorldSize);

            int px = context.World.WorldToDamagePixelX(sampleWorld.x);
            int py = context.World.WorldToDamagePixelY(sampleWorld.y);

            if (px < 0 || px >= context.World.DamageResolutionX ||
                py < 0 || py >= context.World.DamageResolutionY)
                continue;

            bool destroyed = context.TryDestroyPixelAndRegisterInventory(px, py);
            gizmos.RecordSample(sampleWorld, true, destroyed);

            if (!destroyed)
                continue;

            changed = true;

            Vector2 pixelWorld = TerrainSamplingHelper.DamagePixelToWorldCenter(context.World, px, py);
            changedMinWorld = Vector2.Min(changedMinWorld, pixelWorld);
            changedMaxWorld = Vector2.Max(changedMaxWorld, pixelWorld);
            gizmos.MarkHit(pixelWorld);
        }
    }
}