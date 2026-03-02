using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// 인게임 점수·게임 오버 패널·재시작 버튼 등 UI 전담. GameManager 이벤트에 구독하여 느슨하게 결합합니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("In-Game")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup gameOverPanelCanvasGroup;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton;
    [Tooltip("게임 오버 시 '타이틀로' 버튼. OnClick에 OnReturnToTitleClicked 연결")]
    [SerializeField] private Button returnToTitleButton;
    [Tooltip("CanRevive일 때만 표시. 이미 부활 사용 시 숨김")]
    [SerializeField] private GameObject reviveButton;
    [SerializeField] private float gameOverFadeDuration = 0.4f;

    private GameManager _gameManager;
    private AdManager _adManager;

    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        _adManager = FindFirstObjectByType<AdManager>();
        if (_gameManager != null)
        {
            _gameManager.OnGameOver += HandleGameOver;
            _gameManager.OnRevived += HandleRevived;
        }
        if (_adManager != null)
            _adManager.OnRewardedAdFlowEndedWithoutReward += RestoreReviveButtonInteractable;

        if (restartButton != null && _gameManager != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (returnToTitleButton != null)
            returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (gameOverPanelCanvasGroup != null)
            gameOverPanelCanvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
        {
            _gameManager.OnGameOver -= HandleGameOver;
            _gameManager.OnRevived -= HandleRevived;
        }
        if (_adManager != null)
            _adManager.OnRewardedAdFlowEndedWithoutReward -= RestoreReviveButtonInteractable;
        if (restartButton != null && _gameManager != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
        if (returnToTitleButton != null)
            returnToTitleButton.onClick.RemoveListener(OnReturnToTitleClicked);
    }

    private void RestoreReviveButtonInteractable()
    {
        if (reviveButton == null) return;
        if (_gameManager != null && !_gameManager.CanRevive) return;
        var btn = reviveButton.GetComponent<Button>();
        if (btn != null)
            btn.interactable = true;
    }

    private void OnRestartClicked()
    {
        if (_gameManager == null) return;
        _gameManager.NotifyGiveUp();
        _gameManager.RestartGame();
    }

    /// <summary>게임 오버 패널의 '타이틀로' 버튼 OnClick에서 호출. 포기 처리 후 TitleScene을 로드합니다.</summary>
    public void OnReturnToTitleClicked()
    {
		Time.timeScale = 1f;

		if (_gameManager != null)
            _gameManager.NotifyGiveUp();
        SceneManager.LoadScene("TitleScene");
    }

    private void Update()
    {
        if (_gameManager == null || _gameManager.State != GameManager.GameState.Playing) return;
        if (scoreText != null)
            scoreText.text = _gameManager.Score.ToString();
    }

    private void HandleGameOver()
    {
        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        if (finalScoreText != null && _gameManager != null)
            finalScoreText.text = _gameManager.Score.ToString();

        if (reviveButton != null)
        {
            reviveButton.SetActive(_gameManager != null && _gameManager.CanRevive); // _reviveUsedThisRun이 true면 CanRevive=false로 버튼 숨김
            var reviveBtn = reviveButton.GetComponent<Button>();
            if (reviveBtn != null)
                reviveBtn.interactable = true;
        }

        if (gameOverPanelCanvasGroup != null)
        {
            gameOverPanelCanvasGroup.alpha = 0f;
            gameOverPanelCanvasGroup.DOFade(1f, gameOverFadeDuration).SetUpdate(true);
        }
    }

    private void HandleRevived()
    {
        if (reviveButton != null)
            reviveButton.SetActive(false);

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            if (_gameManager != null)
                scoreText.text = _gameManager.Score.ToString();
        }
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (gameOverPanelCanvasGroup != null)
            gameOverPanelCanvasGroup.alpha = 0f;
    }

    public void OnReviveButtonClicked()
    {
        if (reviveButton != null)
        {
            var btn = reviveButton.GetComponent<Button>();
            if (btn != null)
                btn.interactable = false;
        }

        var adManager = _adManager != null ? _adManager : FindFirstObjectByType<AdManager>();
        var gameManager = _gameManager != null ? _gameManager : FindFirstObjectByType<GameManager>();
        if (adManager == null || gameManager == null)
        {
            RestoreReviveButtonInteractable();
            return;
        }

        adManager.ShowRewardedAd(() =>
        {
            gameManager.Revive();
            if (reviveButton != null)
                reviveButton.SetActive(false);
        });
    }
}
