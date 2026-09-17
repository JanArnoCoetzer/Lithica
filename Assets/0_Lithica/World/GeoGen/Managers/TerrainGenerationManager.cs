using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TerrainGenerationManager : MonoBehaviour
{
    [Range(0f, 1f)] public float isoLevel = 0.5f;
    public bool clampFinal01 = true;

    [SerializeReference]
    public List<TerrainFilter> filters = new List<TerrainFilter>();

    private void OnValidate()
    {
        WorldManager world = GetComponent<WorldManager>();
        if (world == null)
            world = GetComponentInParent<WorldManager>();

        if (!Application.isPlaying && world != null)
            world.NotifyGenerationSettingsChanged();
    }

    public void NotifyChangedFromEditor()
    {
        OnValidate();
    }

    public float EvaluateDensity(Vector2 worldPos, WorldManager world)
    {
        float density = 0f;
        int materialId = 0;

        if (filters != null)
        {
            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                if (filter == null)
                    continue;

                filter.Apply(world, worldPos, ref density, ref materialId);
            }
        }

        if (clampFinal01)
            density = Mathf.Clamp01(density);

        return density;
    }

    public float EvaluateCellDensity(int cellX, int cellY, WorldManager world, out int materialId)
    {
        // Center of the cell in world space
        Vector2 worldPos = (Vector2)world.transform.position +
                           new Vector2((cellX + 0.5f) * world.cellSize,
                                       (cellY + 0.5f) * world.cellSize);

        float density = 0f;
        materialId = 0;

        if (filters != null)
        {
            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                if (filter == null)
                    continue;

                filter.Apply(world, worldPos, ref density, ref materialId);
            }
        }

        if (clampFinal01)
            density = Mathf.Clamp01(density);

        return density;
    }

    public float EvaluateDensityUpTo(int lastFilterIndex, Vector2 worldPos, WorldManager world)
    {
        float density = 0f;
        int materialId = 0;

        if (filters == null || filters.Count == 0)
            return density;

        int maxIndex = Mathf.Clamp(lastFilterIndex, 0, filters.Count - 1);

        for (int i = 0; i <= maxIndex; i++)
        {
            var filter = filters[i];
            if (filter == null)
                continue;

            filter.Apply(world, worldPos, ref density, ref materialId);
        }

        if (clampFinal01)
            density = Mathf.Clamp01(density);

        return density;
    }


    public int GetFilterIndex(TerrainFilter target)
    {
        if (filters == null)
            return -1;

        for (int i = 0; i < filters.Count; i++)
        {
            if (ReferenceEquals(filters[i], target))
                return i;
        }

        return -1;
    }
}