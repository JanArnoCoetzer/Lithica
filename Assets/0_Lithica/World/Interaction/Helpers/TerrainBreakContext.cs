using UnityEngine;

public class TerrainBreakContext
{
    public WorldManager World;
    public PlayerInventoryManager PlayerInventory;
    public MaterialGenerator MaterialGenerator;
    public bool ProcessColliderImmediately;
    public DebugManager DebugManager;

    public bool DebugEnabled =>
        DebugManager != null && DebugManager.TerrainInteractionDebug;

    /// <summary>
    /// Tries to destroy a terrain damage pixel and, if successful,
    /// registers the corresponding material in the player inventory
    /// using both its id and icon sprite.
    /// </summary>
    public bool TryDestroyPixelAndRegisterInventory(int px, int py, Object logContext = null)
    {
        if (World == null)
            return false;

        // Mark pixel as destroyed in the damage mask / world.
        if (!World.SetDestroyedPixel(px, py, true))
            return false;

        if (PlayerInventory == null)
        {
            Debug.LogWarning("[Terrain] playerInventory is NULL, cannot register broken pixel", logContext);
            return true; // destruction still happened
        }

        // Resolve material id (string) for this pixel
        string materialId = TerrainInventoryHelper.ResolveMaterialIdForDamagePixel(
            World,
            MaterialGenerator,
            px,
            py);

        // Resolve the material asset so we can get its inventory icon
        TerrainPixelMaterial materialAsset =
            ResolveMaterialAssetForDamagePixel(World, MaterialGenerator, px, py);

        Sprite icon = materialAsset != null ? materialAsset.inventoryIcon : null;

        // Register in inventory with id + icon
        PlayerInventory.RegisterBrokenPixel(materialId, icon);

        return true;
    }

    /// <summary>
    /// Resolves the TerrainPixelMaterial asset for a given damage pixel.
    /// Implement this according to how your world stores per-pixel materials.
    /// </summary>
    private TerrainPixelMaterial ResolveMaterialAssetForDamagePixel(
        WorldManager world,
        MaterialGenerator materialGenerator,
        int px,
        int py)
    {
        if (world == null || materialGenerator == null)
            return null;

        // Example pattern: ask a helper / generator that already knows
        // which material is assigned to this pixel.
        //
        // Replace this with your real lookup logic.
        return TerrainInventoryHelper.ResolveMaterialAssetForDamagePixel(
            world,
            materialGenerator,
            px,
            py
        );
    }

    public bool IsSolidDamagePixel(int px, int py)
    {
        if (World == null)
            return false;

        if (px < 0 || px >= World.DamageResolutionX || py < 0 || py >= World.DamageResolutionY)
            return false;

        Vector2 worldPos = TerrainSamplingHelper.DamagePixelToWorldCenter(World, px, py);

        float density = TerrainSamplingHelper.SampleDensityAtWorld(World, worldPos);
        if (!World.IsSolid(density))
            return false;

        if (World.IsDestroyedPixel(px, py))
            return false;

        return true;
    }

    public bool BrushTouchesSolidTerrain(Vector2 worldPos, float radius)
    {
        if (World == null)
            return false;

        float pixelWorldSize = World.cellSize / World.TilePixelSize;
        int radiusSteps = Mathf.CeilToInt(radius / pixelWorldSize);

        int centerPx = World.WorldToDamagePixelX(worldPos.x);
        int centerPy = World.WorldToDamagePixelY(worldPos.y);

        for (int oy = -radiusSteps; oy <= radiusSteps; oy++)
        {
            for (int ox = -radiusSteps; ox <= radiusSteps; ox++)
            {
                int px = centerPx + ox;
                int py = centerPy + oy;

                if (px < 0 || px >= World.DamageResolutionX || py < 0 || py >= World.DamageResolutionY)
                    continue;

                Vector2 sampleWorld = TerrainSamplingHelper.DamagePixelToWorldCenter(World, px, py);
                if ((sampleWorld - worldPos).sqrMagnitude > radius * radius)
                    continue;

                if (IsSolidDamagePixel(px, py))
                    return true;
            }
        }

        return false;
    }

    public void RebuildChangedArea(Vector2 minWorld, Vector2 maxWorld)
    {
        if (World == null)
            return;

        World.RebuildSpriteChunksOverlappingWorldBounds(minWorld, maxWorld);

        if (ProcessColliderImmediately)
            TerrainColliderHelper.ProcessChunkColliders(World, minWorld, maxWorld);
    }
}