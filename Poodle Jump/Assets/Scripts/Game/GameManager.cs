using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 상태·점수·추락 판정·재시작을 담당하는 매니저. UI는 이벤트로만 전달하여 느슨하게 결합합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        GameOver
    }

    [SerializeField] private Transform player;
    [SerializeField] [Min(1f)] private float fallDeathDistance = 15f;
    [Tooltip("부활 시 플레이어에게 주는 위쪽 속도 (슈퍼 점프). 인스펙터에서 30~40 등 더 키우면 더 높이 뜁니다.")]
    [SerializeField] [Min(1f)] private float reviveJumpForce = 25f;
    [Tooltip("부활 후 이 시간(초) 동안은 추락 판정만 무시. 최고 높이(_maxReachedY) 갱신은 항상 수행됩니다.")]
    [SerializeField] [Min(0.5f)] private float reviveGraceDuration = 2f;
    [Tooltip("부활 시 _maxReachedY를 (현재 Y - 이 값)으로 설정해 하강 시 여유 공간 확보")]
    [SerializeField] [Min(0f)] private float reviveMaxHeightMargin = 5f;

    private float _maxReachedY;
    private GameState _state = GameState.Playing;
    private bool _reviveUsedThisRun;
    private float _reviveGraceEndTime = -1f;

    /// <summary>현재 점수 (최고 도달 높이를 정수로 변환한 값)</summary>
    public int Score => Mathf.FloorToInt(_maxReachedY);

    /// <summary>현재 게임 상태</summary>
    public GameState State => _state;

    /// <summary>이번 판에서 아직 부활을 사용하지 않았으면 true. UI에서 부활 버튼 표시 여부에 사용.</summary>
    public bool CanRevive => !_reviveUsedThisRun;

    /// <summary>게임 오버가 될 때 발생 (부활 가능 여부와 무관)</summary>
    public event Action OnGameOver;

    /// <summary>최종 게임 오버 시 발생. 부활하지 않거나 이미 부활한 뒤 다시 죽었을 때만. 랭킹 제출 등은 이 이벤트로 처리.</summary>
    public event Action OnFinalGameOver;

    /// <summary>부활 직후 한 번 발생. 반짝임·무적 연출 등 시각적 피드백 구독용.</summary>
    public event Action OnRevived;

    /// <summary>플레이어가 순간이동(부활 등)했을 때 새 위치. 카메라가 Y를 즉시 스냅할 때 구독.</summary>
    public event Action<Vector3> OnPlayerTeleported;

    private void Awake()
    {
        var managers = FindObjectsByType<GameManager>(FindObjectsSortMode.None);
        if (managers.Length > 1)
            Debug.LogWarning($"[GameManager] 씬에 GameManager가 {managers.Length}개 있습니다. 1개만 두어야 합니다.");
    }

    private void Update()
    {
        if (player == null || _state != GameState.Playing) return;

        float y = player.position.y;
        if (y > _maxReachedY)
            _maxReachedY = y;

        if (Time.time < _reviveGraceEndTime)
            return;

        if (y < _maxReachedY - fallDeathDistance)
        {
            _state = GameState.GameOver;
            OnGameOver?.Invoke();
            if (_reviveUsedThisRun)
                OnFinalGameOver?.Invoke();
        }
    }

    /// <summary>
    /// 리워드 광고 시청 완료 시 호출. 죽기 전 최고 높이(_maxReachedY)로 복귀 후 슈퍼 점프. 판당 1회만 가능.
    /// </summary>
    public void Revive()
    {
        Debug.Log($"[GameManager] Revive Attempt. Used: {_reviveUsedThisRun}, State: {_state}");
        if (_reviveUsedThisRun)
            return;
        if (player == null || _state != GameState.GameOver)
            return;

        var cylinder = player.GetComponent<CylinderMovement>();
        if (cylinder == null) return;

        float angleRad = Mathf.Atan2(player.position.z, player.position.x);
        Vector3 atMaxHeight = cylinder.GetPositionOnCylinder(angleRad, _maxReachedY);

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = atMaxHeight;
            rb.linearVelocity = Vector3.zero;
            rb.linearVelocity = Vector3.up * reviveJumpForce;
        }
        else
        {
            player.position = atMaxHeight;
        }

        _state = GameState.Playing;
        _reviveUsedThisRun = true;
        _reviveGraceEndTime = Time.time + reviveGraceDuration;

        OnPlayerTeleported?.Invoke(atMaxHeight);
        OnRevived?.Invoke();
    }

    /// <summary>유저가 '포기' 버튼을 눌렀을 때 UI에서 호출. 최종 게임 오버로 간주해 OnFinalGameOver를 발생시킵니다.</summary>
    public void NotifyGiveUp()
    {
        if (_state != GameState.GameOver) return;
        OnFinalGameOver?.Invoke();
    }

    /// <summary>현재 씬을 다시 로드하여 재시작합니다. 씬 로드 시 이 오브젝트가 파괴되며 새 인스턴스가 생성되므로 _maxReachedY, _reviveUsedThisRun 등은 자동으로 초기화됩니다.</summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
