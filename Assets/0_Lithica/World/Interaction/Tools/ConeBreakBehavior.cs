using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Break Behaviors/Cone Break")]
public class ConeBreakBehavior : BreakBehavior
{
    public override void ExecuteBreak(
        TerrainInteractionManager terrainInteraction,
        Vector2 targetWorldPos,
        Vector3 playerPosition,
        BreakToolData toolData)
    {
        if (terrainInteraction == null || toolData == null)
            return;

        terrainInteraction.BreakInCone(
            targetWorldPos,
            playerPosition,
            toolData.breakRange,
            toolData.breakWidth,
            toolData.coneHalfAngle,
            toolData.coneRayCount,
            toolData.forwardRemovalPixels);
    }
}