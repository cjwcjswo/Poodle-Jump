using UnityEngine;
using DG.Tweening;

/// <summary>
/// 발판 프리팹에 붙이는 컴포넌트. 타입은 프리팹별로 Inspector에서 지정하며, 상호작용(점프력 반환, 연출) 및 컬러 적용을 담당합니다. (SRP)
/// </summary>
public class Platform : MonoBehaviour
{
    public enum PlatformType
    {
        Normal,
        OneTime,
        Broken,
        Moving,
        HighJump
    }

    [Header("타입 (프리팹별로 고정)")]
    [SerializeField] private PlatformType platformType = PlatformType.Normal;

    [Header("Moving / HighJump 파라미터")]
    [SerializeField] private float movingSpeed = 2f;
    [SerializeField] private float movingRange = 30f;
    [SerializeField] private float highJumpMultiplier = 1.8f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private CylinderMovement _cylinderRef;
    private float _baseAngleRad;
    private float _baseY;
    private Collider _collider;

    /// <summary>풀에서 꺼낸 뒤 타입은 프리팹에 고정이므로 위치·참조만 초기화.</summary>
    public void Init(CylinderMovement movement, float angleRad, float y)
    {
        _cylinderRef = movement;
        _baseAngleRad = angleRad;
        _baseY = y;

        transform.localScale = Vector3.one;
        _collider = GetComponent<Collider>();
        if (_collider == null) _collider = GetComponentInChildren<Collider>();
        if (_collider != null) _collider.enabled = true;
    }

    /// <summary>풀 반환 시 타입 조회용.</summary>
    public PlatformType Type => platformType;

    private void Update()
    {
        if (platformType != PlatformType.Moving || _cylinderRef == null) return;

        float angleOffsetRad = Mathf.Sin(Time.time * movingSpeed) * movingRange * Mathf.Deg2Rad;
        float currentAngleRad = _baseAngleRad + angleOffsetRad;
        Vector3 position = _cylinderRef.GetPositionOnCylinder(currentAngleRad, _baseY);
        Quaternion rotation = _cylinderRef.GetRotationTowardOutward(currentAngleRad);
        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>플레이어가 밟았을 때 호출. 사용할 점프력 반환. 0이면 점프 불가.</summary>
    public float Interact(float defaultJumpForce)
    {
        switch (platformType)
        {
            case PlatformType.Normal:
                return defaultJumpForce;
            case PlatformType.HighJump:
                return defaultJumpForce * highJumpMultiplier;
            case PlatformType.Broken:
                PlayBrokenDisappear();
                return 0f;
            case PlatformType.OneTime:
                PlayOneTimeDisappear();
                return defaultJumpForce;
            case PlatformType.Moving:
                return defaultJumpForce;
            default:
                return defaultJumpForce;
        }
    }

    /// <summary>현재 오브젝트(또는 자식)의 MeshRenderer에 색상을 MaterialPropertyBlock으로 적용.</summary>
    public void ApplyColor(Color color)
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null) renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return;

        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        if (renderer.sharedMaterial.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, color);
        else if (renderer.sharedMaterial.HasProperty(ColorId))
            block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }

    private void PlayBrokenDisappear()
    {
        if (_collider != null) _collider.enabled = false;
        float duration = 0.3f;
        Sequence s = DOTween.Sequence();
        s.Join(transform.DOMove(transform.position + Vector3.down * 4f, duration).SetEase(Ease.InQuad));
        s.Join(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad));
        s.SetTarget(transform).OnKill(CleanupCollider);
    }

    private void PlayOneTimeDisappear()
    {
        if (_collider != null) _collider.enabled = false;
        transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad).SetTarget(transform);
    }

    private void CleanupCollider()
    {
        if (_collider != null) _collider.enabled = false;
    }
}
