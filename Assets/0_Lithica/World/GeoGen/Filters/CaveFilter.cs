using System;
using UnityEngine;

[Serializable]
public class CaveFilter : TerrainFilter
{
    public float noiseScale = 0.08f;
    public Vector2 offset = new Vector2(1000f, 1000f);
    public float strength = 0.3f;
    public float terrainMaskStart = 0.45f;

    public CaveFilter()
    {
        displayName = "Cave";
    }

    public override void Apply(WorldManager world, Vector2 worldPos, ref float density, ref int materialId)
    {
        if (!enabled)
            return;

        float safeNoiseScale = Mathf.Max(0.0001f, noiseScale);

        float noise = Mathf.PerlinNoise(
            (worldPos.x + offset.x) * safeNoiseScale,
            (worldPos.y + offset.y) * safeNoiseScale
        );

        float carve = Mathf.Max(0f, (noise - 0.5f) * 2f * strength);

        // How strongly this cave can affect the current density.
        float terrainMask = Mathf.InverseLerp(terrainMaskStart, 1f, density);

        density -= carve * terrainMask;

        // If density goes to (or below) zero, clear material → air.
        if (density <= 0f)
            materialId = 0;
    }
}