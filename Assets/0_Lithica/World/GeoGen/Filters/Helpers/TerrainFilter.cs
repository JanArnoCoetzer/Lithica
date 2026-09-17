using UnityEngine;

[System.Serializable]
public abstract class TerrainFilter
{
    [Header("Common")]
    public bool enabled = true;
    public string displayName = "Filter";

    [Header("Material override for this filter")]
    public TerrainPixelMaterial material;   // Dirt, Stone, etc.

    /// <summary>
    /// Apply this filter to the density and material fields.
    /// </summary>
    public abstract void Apply(
        WorldManager world,
        Vector2 worldPos,
        ref float density,
        ref int materialId
    );
}