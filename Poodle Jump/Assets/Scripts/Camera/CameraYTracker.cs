using UnityEngine;

/// <summary>
/// 플레이어의 Y축(최고 도달 높이 유지)과 원기둥 주위 궤도(Orbit)를 함께 추적합니다.
/// X, Z는 플레이어와 동일한 각도에서 cameraDistance만큼 떨어진 위치로 배치되며,
/// LookAt으로 원기둥 축을 바라보아 좌우 조작 반전 문제를 해소합니다.
/// GameManager.OnPlayerTeleported 구독 시 부활 등 순간이동 시 카메라 Y를 즉시 스냅합니다.
/// </summary>
public class CameraYTracker : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Y Tracking (기존 로직)")]
    [SerializeField] [Min(0.01f)] private float lerpSpeed = 5f;

    [Header("Orbit")]
    [SerializeField] [Min(0.1f)] private float cameraDistance = 12f;

    [Header("LookAt")]
    [Tooltip("LookAt 대상의 Y 높이 = 카메라 Y - 이 값. 시선이 너무 아래를 향하지 않도록 조절합니다.")]
    [SerializeField] private float lookAtHeightOffset = 3f;

    private float _maxReachedY;
    private GameManager _gameManager;

    private void Start()
    {
        if (target != null)
            _maxReachedY = target.position.y;
        _gameManager = FindFirstObjectByType<GameManager>();
        if (_gameManager != null)
            _gameManager.OnPlayerTeleported += HandlePlayerTeleported;
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
            _gameManager.OnPlayerTeleported -= HandlePlayerTeleported;
    }

    private void HandlePlayerTeleported(Vector3 position)
    {
        _maxReachedY = position.y;
        float angleRad = Mathf.Atan2(position.z, position.x);
        float camX = cameraDistance * Mathf.Cos(angleRad);
        float camZ = cameraDistance * Mathf.Sin(angleRad);
        transform.position = new Vector3(camX, position.y, camZ);
        float lookAtY = position.y - lookAtHeightOffset;
        transform.LookAt(new Vector3(0f, lookAtY, 0f));
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. 기존 로직: 최고 도달 높이 유지 (추락 시 카메라 내려가지 않음)
        _maxReachedY = Mathf.Max(_maxReachedY, target.position.y);
        float currentY = transform.position.y;
        float newY = Mathf.Lerp(currentY, _maxReachedY, lerpSpeed * Time.deltaTime);

        // 2. 궤도 추적: 원통 중심(0,0,0) 기준 플레이어 각도 → 동일 각도에 cameraDistance만큼 떨어진 X,Z
        float angleRad = Mathf.Atan2(target.position.z, target.position.x);
        float camX = cameraDistance * Mathf.Cos(angleRad);
        float camZ = cameraDistance * Mathf.Sin(angleRad);
        transform.position = new Vector3(camX, newY, camZ);

        // 3. 카메라 회전: 축(또는 보정된 높이)을 바라보기 (시선이 너무 아래를 향하지 않도록)
        float lookAtY = newY - lookAtHeightOffset;
        Vector3 lookAtPoint = new Vector3(0f, lookAtY, 0f);
        transform.LookAt(lookAtPoint);
    }
}
