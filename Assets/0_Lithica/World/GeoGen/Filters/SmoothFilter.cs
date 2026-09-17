using System;
using UnityEngine;

[Serializable]
public class SmoothFilter : TerrainFilter
{
    [Range(1, 3)]
    public int radius = 1;

    [Range(0f, 1f)]
    public float blend = 0.5f;

    public float sampleSpacing = 1f;
    public bool includeDiagonals = true;

    public SmoothFilter()
    {
        displayName = "Smooth";
    }

    public override void Apply(WorldManager world, Vector2 worldPos, ref float density, ref int materialId)
    {
        if (!enabled)
            return;

        TerrainGenerationManager generator = world.GetComponent<TerrainGenerationManager>();
        if (generator == null)
            generator = world.GetComponentInChildren<TerrainGenerationManager>();

        if (generator == null)
            return;

        int myIndex = generator.GetFilterIndex(this);
        if (myIndex < 0)
            return;

        float spacing = Mathf.Max(0.001f, sampleSpacing);

        float currentDensity = density;
        float total = currentDensity;
        float weightSum = 1f;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                if (!includeDiagonals && Mathf.Abs(x) + Mathf.Abs(y) > 1)
                    continue;

                Vector2 samplePos = worldPos + new Vector2(x, y) * spacing;

                float sampleDensity = generator.EvaluateDensityUpTo(myIndex, samplePos, world);

                float weight = 1f / (1f + x * x + y * y);

                total += sampleDensity * weight;
                weightSum += weight;
            }
        }

        float smoothed = total / weightSum;
        density = Mathf.Lerp(currentDensity, smoothed, blend);
        // Material unchanged.
    }
}