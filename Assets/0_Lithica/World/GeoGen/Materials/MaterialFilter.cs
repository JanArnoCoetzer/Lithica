using UnityEngine;

[System.Serializable]
public class MaterialFilter
{
    public string name;
    public bool enabled = true;
    public TerrainPixelMaterial material;

    public float minDensity = 0f;
    public float maxDensity = 1f;
    public float minWorldY = -99999f;
    public float maxWorldY = 99999f;

    public bool useSelectionNoise = false;
    public float selectionNoiseScale = 0.2f;
    [Range(0f, 1f)] public float selectionMin = 0f;
    [Range(0f, 1f)] public float selectionMax = 1f;

    public bool Matches(float density, Vector2 worldPos)
    {
        if (!enabled || material == null)
            return false;

        if (density < minDensity || density > maxDensity)
            return false;

        if (worldPos.y < minWorldY || worldPos.y > maxWorldY)
            return false;

        if (useSelectionNoise)
        {
            float n = Mathf.PerlinNoise(worldPos.x * selectionNoiseScale, worldPos.y * selectionNoiseScale);
            if (n < selectionMin || n > selectionMax)
                return false;
        }

        return true;
    }
}

