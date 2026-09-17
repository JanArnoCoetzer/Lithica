using UnityEngine;


[System.Serializable]
public class TerrainGenerationSettings
{
    [Header("Basic type")]
    public TerrainMaterialType type = TerrainMaterialType.Custom;

    [Header("Vertical placement")]
    public float minWorldY = -Mathf.Infinity;
    public float maxWorldY = Mathf.Infinity;

    [Header("Base density / solidity")]
    [Tooltip("Target density this material prefers (used as a heuristic).")]
    [Range(-1f, 1f)] public float preferredDensity = 0.4f;

    [Tooltip("Minimum density where this material is allowed.")]
    [Range(-1f, 1f)] public float minAllowedDensity = -0.2f;

    [Header("Material noise mask")]
    [Min(0f)] public float materialNoiseScale = 0.2f;
    [Range(0f, 1f)] public float materialThreshold = 0.5f;

    [Header("Veins / pockets")]
    public bool enableVeins = false;
    [Min(0f)] public float veinNoiseScale = 0.5f;
    [Range(0f, 1f)] public float veinThreshold = 0.7f;

    [Header("Gameplay")]
    [Tooltip("Higher means harder to mine.")]
    public float miningHardness = 1.0f;

    [Tooltip("Optional drop prefab when this material is destroyed.")]
    public GameObject dropPrefab;

    [Min(0)] public int minDropCount = 1;
    [Min(0)] public int maxDropCount = 3;

    [Header("ID / index")]
    [Tooltip("Unique ID for storing this material in world logical data.")]
    public int materialId = 0;
}