using UnityEngine;

public abstract class BreakBehavior : ScriptableObject
{
    public abstract void ExecuteBreak(
        TerrainInteractionManager terrainInteraction,
        Vector2 targetWorldPos,
        Vector3 playerPosition,
        BreakToolData toolData);
}