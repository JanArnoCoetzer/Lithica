using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerController playerController;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float maxSpeed = 100f;

    [Header("Look Ahead")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSmooth = 8f;

    [Header("Stability")]
    [SerializeField] private float deadZoneX = 0.05f;
    [SerializeField] private float deadZoneY = 0.02f;
    [SerializeField] private bool followY = true;

    private Vector3 velocity;
    private float currentLookAhead;

    private void Awake()
    {
        if (player != null && playerController == null)
            playerController = player.GetComponent<PlayerController>();
    }

    private void LateUpdate()
    {
        if (player == null || playerController == null)
            return;

        float targetLookAhead = playerController.FacingDirection * lookAheadDistance;
        currentLookAhead = Mathf.Lerp(
            currentLookAhead,
            targetLookAhead,
            Time.deltaTime * lookAheadSmooth
        );

        Vector3 targetPos = player.position + offset + new Vector3(currentLookAhead, 0f, 0f);
        Vector3 currentPos = transform.position;

        float x = currentPos.x;
        float y = currentPos.y;

        if (Mathf.Abs(targetPos.x - currentPos.x) > deadZoneX)
            x = Mathf.SmoothDamp(currentPos.x, targetPos.x, ref velocity.x, smoothTime, maxSpeed);

        if (followY && Mathf.Abs(targetPos.y - currentPos.y) > deadZoneY)
            y = Mathf.SmoothDamp(currentPos.y, targetPos.y, ref velocity.y, smoothTime, maxSpeed);

        transform.position = new Vector3(x, followY ? y : currentPos.y, offset.z);
    }
}