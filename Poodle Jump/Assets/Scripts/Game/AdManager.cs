using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;

/// <summary>
/// 구글 AdMob SDK로 전면 광고(Interstitial)를 제어합니다.
/// GameManager.OnGameOver를 구독해 adsInterval마다 광고를 노출하고, 노출 후 다음 광고를 미리 로드합니다.
/// </summary>
public class AdManager : MonoBehaviour
{
    [Header("Ad Unit - 전면 (테스트 ID 기본값)")]
    [Tooltip("Android 테스트: ca-app-pub-3940256099942544/1033173712")]
    [SerializeField] private string androidAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    [Tooltip("iOS 테스트: ca-app-pub-3940256099942544/4411468910")]
    [SerializeField] private string iosAdUnitId = "ca-app-pub-3940256099942544/4411468910";

    [Header("Ad Unit - 리워드 (부활용)")]
    [Tooltip("Android 테스트: ca-app-pub-3940256099942544/5224354917")]
    [SerializeField] private string androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    [Tooltip("iOS 테스트: ca-app-pub-3940256099942544/1712485313")]
    [SerializeField] private string iosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

    [Header("노출 주기")]
    [Tooltip("게임 오버 N회마다 1회 전면 광고 노출 (예: 3이면 3판당 1회)")]
    [SerializeField] private int adsInterval = 3;

    [SerializeField] private GameManager gameManager;

    private InterstitialAd _interstitialAd;
    private RewardedAd _rewardedAd;
    /// <summary>씬 재로드 후에도 유지되어 N판당 1회 광고 노출을 맞춥니다.</summary>
    private static int _gameOverCount;
    private float _savedTimeScale = 1f;
    private float _savedVolume = 1f;
    private bool _isInitialized;
    /// <summary>보상 획득 시 콜백. 광고가 완전히 닫힌 후(OnRewardedAdClosed)에 실행해 게임이 광고 도중 풀리는 현상 방지.</summary>
    private Action _pendingRewardCallback;
    /// <summary>이번 광고 시청에서 보상을 실제로 획득했는지. 닫을 때 보상이 있을 때만 콜백 실행.</summary>
    private bool _rewardEarnedThisShow;
    /// <summary>이번 리워드 광고가 실제로 열렸는지(OnRewardedAdOpened 호출됨). 열리지 않고 닫히는 경우 보상 미지급.</summary>
    private bool _rewardedAdDidOpen;

    private string AdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidAdUnitId;
#elif UNITY_IPHONE
            return iosAdUnitId;
#else
            return androidAdUnitId;
#endif
        }
    }

    private string RewardedAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidRewardedAdUnitId;
#elif UNITY_IPHONE
            return iosRewardedAdUnitId;
#else
            return androidRewardedAdUnitId;
#endif
        }
    }

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        MobileAds.Initialize(OnMobileAdsInitialized);
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnGameOver -= HandleGameOver;
        DestroyCurrentInterstitial();
        DestroyCurrentRewardedAd();
    }

    private void OnDestroy()
    {
        DestroyCurrentInterstitial();
        DestroyCurrentRewardedAd();
    }

    /// <summary>
    /// 현재 전면 광고 인스턴스의 이벤트를 해제하고 Destroy한 뒤 null로 둡니다.
    /// </summary>
    private void DestroyCurrentInterstitial()
    {
        if (_interstitialAd == null) return;
        _interstitialAd.OnAdFullScreenContentOpened -= OnAdOpened;
        _interstitialAd.OnAdFullScreenContentClosed -= OnAdClosed;
        _interstitialAd.OnAdFullScreenContentFailed -= OnAdFailedToShow;
        _interstitialAd.Destroy();
        _interstitialAd = null;
    }

    /// <summary>
    /// 현재 리워드 광고 인스턴스의 이벤트를 해제하고 Destroy한 뒤 null로 둡니다.
    /// </summary>
    private void DestroyCurrentRewardedAd()
    {
        if (_rewardedAd == null) return;
        _rewardedAd.OnAdFullScreenContentOpened -= OnRewardedAdOpened;
        _rewardedAd.OnAdFullScreenContentClosed -= OnRewardedAdClosed;
        _rewardedAd.OnAdFullScreenContentFailed -= OnRewardedAdFailedToShow;
        _rewardedAd.Destroy();
        _rewardedAd = null;
        _pendingRewardCallback = null;
        _rewardEarnedThisShow = false;
        _rewardedAdDidOpen = false;
    }

    private void OnMobileAdsInitialized(InitializationStatus status)
    {
        _isInitialized = true;
        LoadInterstitialAd();
        LoadRewardedAd();
    }

    /// <summary>
    /// 전면 광고를 미리 로드합니다. 실패 시 게임 흐름에 영향 없이 무시합니다.
    /// </summary>
    public void LoadInterstitialAd()
    {
        if (!_isInitialized)
            return;

        DestroyCurrentInterstitial();

        string adUnitId = AdUnitId;
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[AdManager] Ad Unit ID is empty. Skip load.");
            return;
        }

        var request = new AdRequest();
        InterstitialAd.Load(adUnitId, request, (InterstitialAd ad, LoadAdError loadError) =>
        {
            if (loadError != null)
            {
                Debug.LogWarning($"[AdManager] Interstitial load failed: {loadError.GetMessage()}");
                return;
            }
            if (ad == null)
            {
                Debug.LogWarning("[AdManager] Interstitial ad is null.");
                return;
            }

            _interstitialAd = ad;
            // 최신 AdMob Unity SDK: OnAdFullScreenContentOpened / Closed / Failed (인자 없는 버전)
            _interstitialAd.OnAdFullScreenContentOpened += OnAdOpened;
            _interstitialAd.OnAdFullScreenContentClosed += OnAdClosed;
            _interstitialAd.OnAdFullScreenContentFailed += OnAdFailedToShow;
        });
    }

    /// <summary>
    /// 리워드 광고를 미리 로드합니다. 게임 시작 시 및 시청 후 자동 호출됩니다.
    /// </summary>
    public void LoadRewardedAd()
    {
        if (!_isInitialized) return;

        DestroyCurrentRewardedAd();

        string adUnitId = RewardedAdUnitId;
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[AdManager] Rewarded Ad Unit ID is empty. Skip load.");
            return;
        }

        var request = new AdRequest();
        RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError loadError) =>
        {
            if (loadError != null)
            {
                Debug.LogWarning($"[AdManager] Rewarded load failed: {loadError.GetMessage()}");
                return;
            }
            if (ad == null)
            {
                Debug.LogWarning("[AdManager] Rewarded ad is null.");
                return;
            }

            _rewardedAd = ad;
            _rewardedAd.OnAdFullScreenContentOpened += OnRewardedAdOpened;
            _rewardedAd.OnAdFullScreenContentClosed += OnRewardedAdClosed;
            _rewardedAd.OnAdFullScreenContentFailed += OnRewardedAdFailedToShow;
        });
    }

    /// <summary>리워드 광고가 로드되어 표시 가능한지 여부. UI 버튼 비활성화 등에 사용.</summary>
    public bool IsRewardedAdReady => _rewardedAd != null && _rewardedAd.CanShowAd();

    /// <summary>리워드 광고가 보상 없이 종료되었을 때(취소·실패·미시청). 부활 버튼 interactable 복구 등에 사용.</summary>
    public event Action OnRewardedAdFlowEndedWithoutReward;

    /// <summary>
    /// 리워드 광고를 표시합니다. 시청 완료 시 보상은 광고가 완전히 닫힌 후(OnRewardedAdClosed)에 실행됩니다.
    /// </summary>
    public void ShowRewardedAd(Action onRewardSuccess)
    {
        if (_rewardedAd == null || !_rewardedAd.CanShowAd())
        {
            Debug.Log("[AdManager] Rewarded ad not ready");
            OnRewardedAdFlowEndedWithoutReward?.Invoke();
            LoadRewardedAd();
            return;
        }

        _pendingRewardCallback = onRewardSuccess;
        _rewardEarnedThisShow = false;
        _rewardedAdDidOpen = false;
        _rewardedAd.Show((Reward reward) =>
        {
            _rewardEarnedThisShow = true; // 실제 지급은 OnRewardedAdClosed에서 검증 후 짧은 지연으로 수행
        });
    }

    /// <summary>
    /// 로드된 전면 광고를 표시합니다. 준비되지 않았으면 무시합니다.
    /// </summary>
    public void ShowInterstitialAd()
    {
        if (_interstitialAd == null)
        {
            Debug.Log("[AdManager] Ad not ready");
            LoadInterstitialAd();
            return;
        }
        if (!_interstitialAd.CanShowAd())
        {
            LoadInterstitialAd();
            return;
        }

        _interstitialAd.Show();
    }

    private void HandleGameOver()
    {
        _gameOverCount++;
        Debug.Log($"[AdManager] Game over count: {_gameOverCount}/{adsInterval}");
        if (_gameOverCount >= adsInterval)
        {
            _gameOverCount = 0;
            ShowInterstitialAd();
        }
    }

    private void OnAdOpened()
    {
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (AudioListener.volume > 0f)
        {
            _savedVolume = AudioListener.volume;
            AudioListener.volume = 0f;
        }
    }

    private void OnAdClosed()
    {
        Time.timeScale = _savedTimeScale;
        AudioListener.volume = _savedVolume;

        LoadInterstitialAd();
    }

    private void OnAdFailedToShow(AdError error)
    {
        Time.timeScale = _savedTimeScale;
        AudioListener.volume = _savedVolume;
        Debug.LogWarning($"[AdManager] Interstitial show failed: {error?.GetMessage()}");
        LoadInterstitialAd();
    }

    private void OnRewardedAdOpened()
    {
        _rewardedAdDidOpen = true;
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (AudioListener.volume > 0f)
        {
            _savedVolume = AudioListener.volume;
            AudioListener.volume = 0f;
        }
    }

    private void OnRewardedAdClosed()
    {
        bool validShow = _rewardedAdDidOpen;
        bool earned = _rewardEarnedThisShow;
        Action toInvoke = null;
        if (validShow && earned && _pendingRewardCallback != null)
            toInvoke = _pendingRewardCallback;

        if (_pendingRewardCallback != null && toInvoke == null)
            OnRewardedAdFlowEndedWithoutReward?.Invoke();

        _pendingRewardCallback = null;
        _rewardEarnedThisShow = false;
        _rewardedAdDidOpen = false;
        Time.timeScale = _savedTimeScale;
        AudioListener.volume = _savedVolume;
        LoadRewardedAd();

        if (toInvoke != null)
            StartCoroutine(InvokeRewardAfterShortDelay(toInvoke));
    }

    private IEnumerator InvokeRewardAfterShortDelay(Action callback)
    {
        yield return new WaitForSecondsRealtime(0.05f);
        callback?.Invoke();
    }

    private void OnRewardedAdFailedToShow(AdError error)
    {
        if (_pendingRewardCallback != null)
            OnRewardedAdFlowEndedWithoutReward?.Invoke();
        _pendingRewardCallback = null;
        _rewardEarnedThisShow = false;
        _rewardedAdDidOpen = false;
        Time.timeScale = _savedTimeScale;
        AudioListener.volume = _savedVolume;
        Debug.LogWarning($"[AdManager] Rewarded show failed: {error?.GetMessage()}");
        LoadRewardedAd();
    }
}
