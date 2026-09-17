using UnityEngine;

public static class TerrainInventoryHelper
{
    /// <summary>
    /// Resolve the material ID (string) for a damage pixel.
    /// </summary>
    public static string ResolveMaterialIdForDamagePixel(
        WorldManager world,
        MaterialGenerator materialGenerator,
        int px,
        int py)
    {
        var material = ResolveMaterialAssetForDamagePixel(world, materialGenerator, px, py);

        if (material == null)
            return "Unknown";

        return string.IsNullOrWhiteSpace(material.name) ? "Unknown" : material.name;
    }

    /// <summary>
    /// Resolve the TerrainPixelMaterial asset for a damage pixel.
    /// This is used so inventory can read the material's icon sprite.
    /// </summary>
    public static TerrainPixelMaterial ResolveMaterialAssetForDamagePixel(
        WorldManager world,
        MaterialGenerator materialGenerator,
        int px,
        int py)
    {
        if (world == null || materialGenerator == null)
            return null;

        Vector2 worldPos = TerrainSamplingHelper.DamagePixelToWorldCenter(world, px, py);

        float localX = (worldPos.x - world.transform.position.x) / world.cellSize;
        float localY = (worldPos.y - world.transform.position.y) / world.cellSize;

        int globalCellX = Mathf.Clamp(Mathf.FloorToInt(localX), 0, world.TotalCellsX - 1);
        int globalCellY = Mathf.Clamp(Mathf.FloorToInt(localY), 0, world.TotalCellsY - 1);

        float u = Mathf.Clamp01(localX - globalCellX);
        float v = Mathf.Clamp01(localY - globalCellY);

        TerrainPixelMaterial material =
            materialGenerator.ResolveMaterial(world, globalCellX, globalCellY, u, v);

        return material;
    }
}