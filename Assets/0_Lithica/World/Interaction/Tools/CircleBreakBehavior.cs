using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Break Behaviors/Circle Break")]
public class CircleBreakBehavior : BreakBehavior
{
    public override void ExecuteBreak(
        TerrainInteractionManager terrainInteraction,
        Vector2 targetWorldPos,
        Vector3 playerPosition,
        BreakToolData toolData)
    {
        if (terrainInteraction == null || toolData == null)
            return;

        terrainInteraction.BreakBrushAtWorld(
            targetWorldPos,
            toolData.breakRadius,
            playerPosition);
    }
}