using System;
using UnityEngine;

[Serializable]
public class AddNoiseFilter : TerrainFilter
{
    public float noiseScale = 0.12f;
    public Vector2 offset = Vector2.zero;
    public float strength = 0.15f;

    public AddNoiseFilter()
    {
        displayName = "Add Noise";
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

        float value = (noise - 0.5f) * 2f * strength;

        float oldDensity = density;
        density += value;

        // Optional: if this filter is used as an overlay material painter, you can
        // assign its material when it increases density.
        if (material != null && density > oldDensity)
        {
            materialId = material.generation.materialId;
        }
    }
}