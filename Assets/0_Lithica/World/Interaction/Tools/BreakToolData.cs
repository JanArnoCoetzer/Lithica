using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Break Tool Data")]
public class BreakToolData : ScriptableObject
{
    [Header("Info")]
    public string toolName = "Basic Digger";

    [Header("Timing")]
    public float breakInterval = 0.05f;

    [Header("Brush")]
    public float breakRadius = 0.75f;
    public float breakRange = 6f;
    public int breakWidth = 1;

    [Header("Cone")]
    public float coneHalfAngle = 12f;
    public int coneRayCount = 6;
    public int forwardRemovalPixels = 4;

    [Header("Debug")]
    public Color gizmoColor = new Color(1f, 0.25f, 0.25f, 0.9f);
    public Color gizmoFillColor = new Color(1f, 0.25f, 0.25f, 0.15f);

    [Header("Behavior")]
    public BreakBehavior breakBehavior;

    private void OnValidate()
    {
        breakInterval = Mathf.Max(0.01f, breakInterval);
        breakRadius = Mathf.Max(0.01f, breakRadius);
        breakRange = Mathf.Max(0.01f, breakRange);
        breakWidth = Mathf.Max(1, breakWidth);

        coneHalfAngle = Mathf.Max(0f, coneHalfAngle);
        coneRayCount = Mathf.Max(1, coneRayCount);
        forwardRemovalPixels = Mathf.Max(1, forwardRemovalPixels);
    }
}