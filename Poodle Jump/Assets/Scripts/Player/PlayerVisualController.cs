using UnityEngine;
using DG.Tweening;

/// <summary>
/// 점프 시 스쿼시 앤 스트레치(Squash & Stretch) 시각 연출만 담당합니다.
/// 물리/입력은 건드리지 않으며, visualModel Transform만 스케일 트윈합니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerVisualController : MonoBehaviour
{
    [SerializeField] private Transform visualModel;
    [SerializeField] private Animator animator;
    [SerializeField] private string jumpTriggerName = "Jump";

    [Header("Squash & Stretch")]
    [SerializeField] private float squashDuration = 0.05f;
    [SerializeField] private Vector3 squashScale = new Vector3(1.15f, 0.5f, 1.15f);
    [SerializeField] private float stretchDuration = 0.1f;
    [SerializeField] private Vector3 stretchScale = new Vector3(0.85f, 1.35f, 0.85f);
    [SerializeField] private float recoverDuration = 0.15f;

    private PlayerController _playerController;
    private Sequence _runningSequence;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (_playerController != null)
            _playerController.OnJump += PlayJumpVisual;
    }

    private void OnDisable()
    {
        if (_playerController != null)
            _playerController.OnJump -= PlayJumpVisual;
        _runningSequence?.Kill();
        _runningSequence = null;
    }

    private void PlayJumpVisual()
    {
        if (visualModel == null) return;

        if (animator != null)
            animator.SetTrigger(jumpTriggerName);

        _runningSequence?.Kill(true);
        _runningSequence = null;

        visualModel.localScale = Vector3.one;

        _runningSequence = DOTween.Sequence()
            .Append(visualModel.DOScale(squashScale, squashDuration).SetEase(Ease.OutQuad))
            .Append(visualModel.DOScale(stretchScale, stretchDuration).SetEase(Ease.OutQuad))
            .Append(visualModel.DOScale(Vector3.one, recoverDuration).SetEase(Ease.OutQuad))
            .SetTarget(visualModel)
            .OnKill(() => { if (visualModel != null) visualModel.localScale = Vector3.one; });
    }
}
