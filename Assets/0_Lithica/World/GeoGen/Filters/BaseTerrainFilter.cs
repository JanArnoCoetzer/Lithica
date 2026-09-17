using UnityEngine;

[System.Serializable]
public class BaseTerrainFilter : TerrainFilter
{
    public float terrainHeightPercent = 0.5f;
    public float heightVariationPercent = 0.2f;

    public float noiseScale = 0.03f;
    public int octaves = 4;
    public float lacunarity = 2.11f;
    public float persistence = 0.5f;
    public Vector2 offset;

    public BaseTerrainFilter()
    {
        displayName = "Base Terrain";
    }

    public override void Apply(
     WorldManager world,
     Vector2 worldPos,
     ref float density,
     ref int materialId
 )
    {
        if (!enabled)
            return;

        float safeScale = Mathf.Max(0.0001f, noiseScale);
        float terrainHeightPercentNormalised = terrainHeightPercent * 2f;

        float heightNoise = FBm(
            worldPos * safeScale + offset,
            safeScale,
            octaves,
            lacunarity,
            persistence
        );

        // --- BASE HEIGHT AS PERCENT OF TOTAL WORLD HEIGHT ---

        // Normalized world height [0..1] from bottom to top of world (all chunks)
        float worldBottom = world.transform.position.y;
        float localY = worldPos.y - worldBottom;
        float worldHeight = world.TotalMapHeight; // chunkCountY * cellsPerChunk * cellSize
        float yNorm = Mathf.InverseLerp(0f, worldHeight, localY); // 0=bottom, 1=top

        // Base surface height as percent of total world height
        float baseHeight = terrainHeightPercentNormalised; // 0..1 over full world

        // Add noise variation around that base height
        float targetHeight = baseHeight +
                             (heightNoise - 0.5f) * heightVariationPercent;

        // How much to fill: positive when we're below the surface we want
        float add = Mathf.Clamp01(targetHeight - yNorm);

        float oldDensity = density;
        density = Mathf.Max(density, add);

        // Assign material when this filter actually adds terrain
        if (material != null && density > 0f && density > oldDensity + 0.001f)
        {
            materialId = material.generation.materialId;
        }
    }

    private float FBm(Vector2 p, float scale, int oct, float lac, float pers)
    {
        float a = 1f;
        float f = scale;
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < oct; i++)
        {
            sum += Mathf.PerlinNoise(p.x * f, p.y * f) * a;
            norm += a;
            a *= pers;
            f *= lac;
        }

        return norm > 0f ? sum / norm : 0f;
    }
}