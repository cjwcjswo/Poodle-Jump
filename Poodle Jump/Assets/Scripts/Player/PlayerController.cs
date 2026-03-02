using System;
using UnityEngine;

/// <summary>
/// 플레이어 오브젝트를 제어하는 파사드. IPlayerInput으로 입력을 받고,
/// CylinderMovement로 위치/회전을 계산하며, Rigidbody로 중력 및 Platform 충돌 시 점프를 처리합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CylinderMovement))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float turnSpeed = 2f;
    [SerializeField] private float jumpForce = 12f;

    /// <summary>플랫폼을 밟고 점프가 발생한 순간 한 번 발생합니다. 시각 연출(스쿼시 앤 스트레치 등) 구독용.</summary>
    public event Action OnJump;

    [Tooltip("IPlayerInput을 구현한 컴포넌트. HybridInputProvider(키보드+기울기) 또는 키보드 전용 등을 연결할 수 있습니다. 비워두면 같은 GameObject에서 자동 탐색합니다.")]
    [SerializeField] private MonoBehaviour inputProvider;
    private IPlayerInput _input;
    private CylinderMovement _cylinderMovement;
    private Rigidbody _rb;
    private float _currentAngleRad;

    private void Awake()
    {
        _cylinderMovement = GetComponent<CylinderMovement>();
        _rb = GetComponent<Rigidbody>();
        _input = inputProvider != null && inputProvider is IPlayerInput ip ? ip : GetComponent<IPlayerInput>();
        _currentAngleRad = Mathf.Atan2(transform.position.z, transform.position.x);
    }

    private void FixedUpdate()
    {
        if (_input == null || _cylinderMovement == null || _rb == null) return;

        float moveInput = _input.MoveInput;
        float angleDelta = moveInput * turnSpeed * Time.fixedDeltaTime;
        _currentAngleRad = _cylinderMovement.AddAngleDelta(_currentAngleRad, angleDelta);

        float currentY = _rb.position.y;
        Vector3 targetPosition = _cylinderMovement.GetPositionOnCylinder(_currentAngleRad, currentY);
        _rb.MovePosition(targetPosition);

        Quaternion targetRotation = _cylinderMovement.GetRotationTowardOutward(_currentAngleRad);
        transform.rotation = targetRotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Platform")) return;
        if (_rb.linearVelocity.y > 0f) return; // 하강 중일 때만 점프 처리

        var platform = other.GetComponent<Platform>() ?? other.GetComponentInParent<Platform>();
        if (platform == null) return;

        float force = platform.Interact(jumpForce);
        if (force <= 0f) return;

        Vector3 v = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(v.x, force, v.z);
        OnJump?.Invoke();
    }
}
