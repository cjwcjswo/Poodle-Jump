using UnityEngine;

/// <summary>
/// 타이틀 씬 하단에서 강아지 모델이 화면 좌측~우측 끝 사이를 왕복하도록 합니다.
/// 강아지마다 이동 속도와 끝에서의 대기 시간을 랜덤하게 부여합니다.
/// </summary>
[RequireComponent(typeof(Transform))]
public class RoamingDog : MonoBehaviour
{
    [Header("Bounds (World X)")]
    [Tooltip("비워두면 메인 카메라 뷰포트 기준으로 자동 계산")]
    [SerializeField] private float leftBoundX;
    [SerializeField] private float rightBoundX;
    [Tooltip("뷰포트 기준으로 자동 계산할 때 사용할 카메라와의 거리(깊이)")]
    [SerializeField] private float viewportDepth = 10f;

    [Header("Random Range")]
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float minWaitAtEdge = 0.2f;
    [SerializeField] private float maxWaitAtEdge = 1.2f;

    private float _speed;
    private float _waitAtEdge;
    private float _direction = 1f; // 1 = right, -1 = left
    private float _waitTimer;
    private bool _useAutoBounds = true;
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
        if (_camera == null)
            _camera = FindFirstObjectByType<Camera>();

        if (leftBoundX != 0 || rightBoundX != 0)
            _useAutoBounds = false;
        else if (_camera != null)
            RecomputeBounds();

        _speed = Random.Range(minSpeed, maxSpeed);
        _waitAtEdge = Random.Range(minWaitAtEdge, maxWaitAtEdge);
        _waitTimer = 0f;
        _direction = Random.value > 0.5f ? 1f : -1f;
        ApplyFacing();
    }

    private void RecomputeBounds()
    {
        if (_camera == null) return;
        var left = _camera.ViewportToWorldPoint(new Vector3(0f, 0.5f, viewportDepth));
        var right = _camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, viewportDepth));
        leftBoundX = left.x;
        rightBoundX = right.x;
    }

    private void Update()
    {
        if (_useAutoBounds && _camera != null)
            RecomputeBounds();

        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.deltaTime;
            return;
        }

        float x = transform.position.x;
        float move = _direction * _speed * Time.deltaTime;
        x += move;

        if (_direction > 0 && x >= rightBoundX)
        {
            x = rightBoundX;
            _direction = -1f;
            _waitTimer = _waitAtEdge;
            ApplyFacing();
        }
        else if (_direction < 0 && x <= leftBoundX)
        {
            x = leftBoundX;
            _direction = 1f;
            _waitTimer = _waitAtEdge;
            ApplyFacing();
        }

        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    private void ApplyFacing()
    {
        transform.rotation = Quaternion.Euler(0f, _direction > 0 ? 0f : 180f, 0f);
    }
}
