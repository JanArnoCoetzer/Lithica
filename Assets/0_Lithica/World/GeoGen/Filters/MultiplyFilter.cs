using System;
using UnityEngine;

[Serializable]
public class MultiplyFilter : TerrainFilter
{
    public float multiplier = 1f;

    public MultiplyFilter()
    {
        displayName = "Multiply";
    }

    public override void Apply(WorldManager world, Vector2 worldPos, ref float density, ref int materialId)
    {
        if (!enabled)
            return;

        density *= multiplier;
        // Material unchanged.
    }
}