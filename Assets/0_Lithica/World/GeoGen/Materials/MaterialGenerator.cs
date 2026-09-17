using System.Collections.Generic;
using UnityEngine;

public class MaterialGenerator : MonoBehaviour
{
    [Header("Fallback")]
    public TerrainPixelMaterial fallbackMaterial;

    // Internal cache built from TerrainGenerationManager.filters.material
    private TerrainPixelMaterial[] cachedMaterials;

    private TerrainGenerationManager generationManager;

    private void Awake()
    {
        ResolveGenerationManager();
        RebuildMaterialCache();
    }

    private void OnValidate()
    {
        ResolveGenerationManager();
        RebuildMaterialCache();
    }

    private void ResolveGenerationManager()
    {
        if (generationManager != null)
            return;

        generationManager = GetComponent<TerrainGenerationManager>();
        if (generationManager == null)
            generationManager = GetComponentInParent<TerrainGenerationManager>();
    }

    private void RebuildMaterialCache()
    {
        if (generationManager == null || generationManager.filters == null)
        {
            cachedMaterials = null;
            return;
        }

        var list = new List<TerrainPixelMaterial>();

        foreach (var filter in generationManager.filters)
        {
            if (filter == null)
                continue;

            var mat = filter.material;
            if (mat == null)
                continue;

            // Avoid duplicates
            if (!list.Contains(mat))
                list.Add(mat);
        }

        cachedMaterials = list.Count > 0 ? list.ToArray() : null;
    }

    private TerrainPixelMaterial[] Materials => cachedMaterials;

    public TerrainPixelMaterial GetById(int id)
    {
        var mats = Materials;
        if (mats == null || mats.Length == 0)
            return fallbackMaterial;

        for (int i = 0; i < mats.Length; i++)
        {
            var mat = mats[i];
            if (mat == null)
                continue;

            if (mat.generation.materialId == id)
                return mat;
        }

        return fallbackMaterial;
    }

    /// <summary>
    /// Resolve material for a cell+UV, using world materialIds if set,
    /// and falling back to filter materials.
    /// </summary>
    public TerrainPixelMaterial ResolveMaterial(
        WorldManager world,
        int globalCellX,
        int globalCellY,
        float u,
        float v
    )
    {
        // 1) Prefer explicit materialId written by filters / world.
        int id = world.GetCellMaterialId(globalCellX, globalCellY);
        if (id != 0)
        {
            var matById = GetById(id);
            if (matById != null)
                return matById;
        }

        // 2) Fallback: choose material based on density + suitability
        //    from the materials used by filters.
        var mats = Materials;
        if (mats == null || mats.Length == 0)
            return fallbackMaterial;

        Vector2 worldPos = new Vector2(
            world.transform.position.x + (globalCellX + u) * world.cellSize,
            world.transform.position.y + (globalCellY + v) * world.cellSize
        );

        float density = world.generationManager.EvaluateDensity(worldPos, world);

        TerrainPixelMaterial best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < mats.Length; i++)
        {
            var mat = mats[i];
            if (mat == null)
                continue;

            float score = mat.EvaluateSuitability(worldPos, density);
            if (score > bestScore)
            {
                bestScore = score;
                best = mat;
            }
        }

        return best != null ? best : fallbackMaterial;
    }
}