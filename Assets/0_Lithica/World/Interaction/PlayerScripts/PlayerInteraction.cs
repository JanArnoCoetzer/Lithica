using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TerrainInteractionManager terrainInteraction;
    [SerializeField] private BreakToolData equippedTool;

    [Header("Break Gizmos")]
    [SerializeField] private bool showBreakRadiusGizmo = true;
    [SerializeField] private bool showBreakAimWhenSelected = true;
    [SerializeField] private bool showConePreview = true;
    [SerializeField] private int conePreviewSegments = 18;
    private bool ToolGizmosEnabled =>
    debugManager != null && debugManager.PlayerInteractionToolGizmos;

    private DebugManager debugManager;

    private bool breakHeld;
    private bool isBreaking;
    private float breakTimer;

    private Vector2 lastBreakStart;
    private Vector2 lastBreakEnd;
    private bool hasBreakPreview;

    public float BreakRadius => equippedTool != null ? equippedTool.breakRadius : 0.75f;

    private bool DebugEnabled =>
        debugManager != null && debugManager.PlayerInteractionDebug;

    public void SetDebugManager(DebugManager manager)
    {
        debugManager = manager;
    }

    public void SetTerrainInteraction(TerrainInteractionManager manager)
    {
        terrainInteraction = manager;
    }

    public void SetEquippedTool(BreakToolData tool)
    {
        equippedTool = tool;
    }

    private void Awake()
    {
        ResolveMainCamera();

        BreakDebugLog($"Awake | mainCamera={(mainCamera != null ? mainCamera.name : "null")}");
        BreakDebugLog($"Awake | terrainInteraction={(terrainInteraction != null ? terrainInteraction.name : "null")}");
        BreakDebugLog($"Awake | equippedTool={(equippedTool != null ? equippedTool.toolName : "null")}");
    }

    private void Update()
    {
        ForceStopBreakOnRelease();
        HandleBreaking();
    }

    public void OnBreak(InputValue value)
    {
        breakHeld = value.isPressed;

        if (breakHeld)
        {
            breakTimer = 0f;
            isBreaking = true;
            BreakDebugLog("Break pressed");
        }
        else
        {
            breakTimer = 0f;
            isBreaking = false;
            BreakDebugLog("Break released");
        }
    }

    public bool TryGetBreakWorldPosition(out Vector3 world3, out Vector2 world2)
    {
        world3 = default;
        world2 = default;

        if (mainCamera == null || !mainCamera.isActiveAndEnabled)
            ResolveMainCamera();

        if (mainCamera == null || !mainCamera.isActiveAndEnabled)
            return false;

        Vector2 mouseScreenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        float zDepth = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);

        Vector3 screenPoint = new Vector3(mouseScreenPos.x, mouseScreenPos.y, zDepth);
        world3 = mainCamera.ScreenToWorldPoint(screenPoint);
        world2 = new Vector2(world3.x, world3.y);

        return true;
    }

    private void HandleBreaking()
    {
        if (!breakHeld)
        {
            hasBreakPreview = false;
            return;
        }

        if (terrainInteraction == null)
        {
            BreakDebugLog("HandleBreaking | terrainInteraction null");
            hasBreakPreview = false;
            return;
        }

        if (equippedTool == null)
        {
            BreakDebugLog("HandleBreaking | equippedTool null");
            hasBreakPreview = false;
            return;
        }

        if (equippedTool.breakBehavior == null)
        {
            BreakDebugLog("HandleBreaking | breakBehavior null");
            hasBreakPreview = false;
            return;
        }

        if (!TryGetBreakWorldPosition(out _, out Vector2 worldPos))
        {
            BreakDebugLog("HandleBreaking | failed world position");
            hasBreakPreview = false;
            return;
        }

        UpdateBreakPreview(worldPos);

        breakTimer -= Time.deltaTime;
        if (breakTimer > 0f)
            return;

        breakTimer += equippedTool.breakInterval;

        equippedTool.breakBehavior.ExecuteBreak(
            terrainInteraction,
            worldPos,
            transform.position,
            equippedTool);
    }

    private void UpdateBreakPreview(Vector2 targetWorldPos)
    {
        Vector2 start = transform.position;

        if (equippedTool == null || equippedTool.breakBehavior == null)
        {
            hasBreakPreview = false;
            return;
        }

        lastBreakStart = start;
        lastBreakEnd = targetWorldPos;
        hasBreakPreview = true;
    }

    private void ForceStopBreakOnRelease()
    {
        if (Mouse.current == null)
            return;

        bool held = Mouse.current.leftButton.isPressed;

        if (!held)
        {
            if (breakHeld || isBreaking)
                BreakDebugLog("Force stopped breaking because mouse button was released");

            breakHeld = false;
            isBreaking = false;
            breakTimer = 0f;
        }
    }

    private void ResolveMainCamera()
    {
        if (mainCamera != null && mainCamera.isActiveAndEnabled)
            return;

        mainCamera = Camera.main;

        if (mainCamera != null && mainCamera.isActiveAndEnabled)
            return;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].isActiveAndEnabled)
            {
                mainCamera = cameras[i];
                Debug.LogWarning(
                    $"[PlayerInteraction] Camera.main was null, falling back to enabled camera '{mainCamera.name}'. Tag your gameplay camera as MainCamera.",
                    this
                );
                return;
            }
        }

        Debug.LogError("[PlayerInteraction] No enabled camera found in scene.", this);
    }

    private void BreakDebugLog(string message)
    {
        if (!DebugEnabled)
            return;

        Debug.Log($"[PlayerInteraction] {message}", this);
    }

    private bool IsCircleBreak()
    {
        return equippedTool != null &&
               equippedTool.breakBehavior != null &&
               equippedTool.breakBehavior is CircleBreakBehavior;
    }

    private bool IsConeBreak()
    {
        return equippedTool != null &&
               equippedTool.breakBehavior != null &&
               equippedTool.breakBehavior is ConeBreakBehavior;
    }

    private void DrawConePreview(Vector3 playerPos, Vector2 world2, Color wireColor, Color fillColor)
    {
        if (!showConePreview || equippedTool == null)
            return;

        float range = equippedTool.breakRange;
        float halfAngle = equippedTool.coneHalfAngle;
        int segments = Mathf.Max(3, conePreviewSegments);

        Vector2 start = new Vector2(playerPos.x, playerPos.y);
        Vector2 dir = world2 - start;

        if (dir.sqrMagnitude <= 0.0001f)
            return;

        dir.Normalize();

        Vector2 leftDir = RotateVector(dir, -halfAngle);
        Vector2 rightDir = RotateVector(dir, halfAngle);

        Vector3 origin = new Vector3(start.x, start.y, 0f);
        Vector3 leftEnd = new Vector3(start.x + leftDir.x * range, start.y + leftDir.y * range, 0f);
        Vector3 rightEnd = new Vector3(start.x + rightDir.x * range, start.y + rightDir.y * range, 0f);

        Gizmos.color = wireColor;
        Gizmos.DrawLine(origin, leftEnd);
        Gizmos.DrawLine(origin, rightEnd);

        Vector3 previous = leftEnd;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector2 arcDir = RotateVector(dir, angle);
            Vector3 next = new Vector3(start.x + arcDir.x * range, start.y + arcDir.y * range, 0f);

            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        Gizmos.color = fillColor;
        Vector3 centerEnd = new Vector3(start.x + dir.x * range, start.y + dir.y * range, 0f);
        Gizmos.DrawSphere(centerEnd, 0.08f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(playerPos, new Vector3(world2.x, world2.y, playerPos.z));
    }

    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        Vector2 rotated = new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );

        return rotated.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (!ToolGizmosEnabled || terrainInteraction == null || equippedTool == null)
            return;

        if (!TryGetBreakWorldPosition(out _, out Vector2 world2))
            return;

        Color fillColor = new Color(1f, 0.25f, 0.25f, 0.15f);
        Color wireColor = new Color(1f, 0.25f, 0.25f, 0.9f);

        if (equippedTool != null)
        {
            fillColor = equippedTool.gizmoFillColor;
            wireColor = equippedTool.gizmoColor;
        }

        Vector3 playerPos = transform.position;

        if (IsConeBreak())
        {
            DrawConePreview(playerPos, world2, wireColor, fillColor);
        }
        else if (IsCircleBreak())
        {
            if (!showBreakRadiusGizmo)
                return;

            float radius = equippedTool.breakRadius;

            Gizmos.color = fillColor;
            Gizmos.DrawSphere(new Vector3(world2.x, world2.y, 0f), radius);

            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(new Vector3(world2.x, world2.y, 0f), radius);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(playerPos, new Vector3(world2.x, world2.y, playerPos.z));
        }
        else
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(playerPos, new Vector3(world2.x, world2.y, playerPos.z));
        }
    }
}