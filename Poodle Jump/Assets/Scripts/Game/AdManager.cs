using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;

/// <summary>
/// 구글 AdMob SDK로 전면 광고(Interstitial)와 리워드 광고를 제어합니다.
/// GameManager.OnGameOver를 구독해 adsInterval마다 전면 광고를 노출합니다.
/// 에디터에서는 테스트 ID, 모바일 빌드에서는 인스펙터 ID, WebGL에서는 비활성화됩니다.
/// </summary>
public class AdManager : MonoBehaviour
{
    // 구글 공식 테스트용 Ad Unit ID (에디터/테스트 시 사용)
    private const string AndroidInterstitialTestId = "ca-app-pub-3940256099942544/1033173712";
    private const string IosInterstitialTestId = "ca-app-pub-3940256099942544/4411468910";
    private const string AndroidRewardedTestId = "ca-app-pub-3940256099942544/5224354917";
    private const string IosRewardedTestId = "ca-app-pub-3940256099942544/1712485313";

    [Header("Ad Unit - 전면 (실기기 배포 시 인스펙터에 입력)")]
    [Tooltip("Android 전면 광고 ID. 에디터에서는 테스트 ID가 사용됩니다.")]
    [SerializeField] private string androidAdUnitId = "";
    [Tooltip("iOS 전면 광고 ID. 에디터에서는 테스트 ID가 사용됩니다.")]
    [SerializeField] private string iosAdUnitId = "";

    [Header("Ad Unit - 리워드 (부활용, 실기기 배포 시 인스펙터에 입력)")]
    [Tooltip("Android 리워드 광고 ID. 에디터에서는 테스트 ID가 사용됩니다.")]
    [SerializeField] private string androidRewardedAdUnitId = "";
    [Tooltip("iOS 리워드 광고 ID. 에디터에서는 테스트 ID가 사용됩니다.")]
    [SerializeField] private string iosRewardedAdUnitId = "";

    [Header("노출 주기")]
    [Tooltip("게임 오버 N회마다 1회 전면 광고 노출 (예: 3이면 3판당 1회)")]
    [SerializeField] private int adsInterval = 3;

    [SerializeField] private GameManager gameManager;

    private InterstitialAd _interstitialAd;
    private RewardedAd _rewardedAd;
    private static int _gameOverCount;
    private float _savedTimeScale = 1f;
    private float _savedVolume = 1f;
    private bool _isInitialized;
    private Action _pendingRewardCallback;
    private bool _rewardEarnedThisShow;
    private bool _rewardedAdDidOpen;

    private string AdUnitId
    {
        get
        {
#if UNITY_EDITOR
            return AndroidInterstitialTestId;
#elif UNITY_ANDROID
            return string.IsNullOrEmpty(androidAdUnitId) ? AndroidInterstitialTestId : androidAdUnitId;
#elif UNITY_IPHONE
            return string.IsNullOrEmpty(iosAdUnitId) ? IosInterstitialTestId : iosAdUnitId;
#else
            return androidAdUnitId;
#endif
        }
    }

    private string RewardedAdUnitId
    {
        get
        {
#if UNITY_EDITOR
            return AndroidRewardedTestId;
#elif UNITY_ANDROID
            return string.IsNullOrEmpty(androidRewardedAdUnitId) ? AndroidRewardedTestId : androidRewardedAdUnitId;
#elif UNITY_IPHONE
            return string.IsNullOrEmpty(iosRewardedAdUnitId) ? IosRewardedTestId : iosRewardedAdUnitId;
#else
            return androidRewardedAdUnitId;
#endif
        }
    }

    private void Awake()
    {
#if UNITY_WEBGL
        enabled = false;
        return;
#endif
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
#if UNITY_WEBGL
        return;
#endif
        MobileAds.Initialize(OnMobileAdsInitialized);
    }

    private void OnEnable()
    {
#if UNITY_WEBGL
        return;
#endif
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

    /// <summary>광고 열릴 때 게임 일시정지(Time.timeScale, 볼륨) 저장 후 0으로 설정.</summary>
    private void PauseGameForAd()
    {
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (AudioListener.volume > 0f)
        {
            _savedVolume = AudioListener.volume;
            AudioListener.volume = 0f;
        }
    }

    /// <summary>광고 닫힐 때 저장해 둔 Time.timeScale, 볼륨 복구.</summary>
    private void UnpauseGameAfterAd()
    {
        Time.timeScale = _savedTimeScale;
        AudioListener.volume = _savedVolume;
    }

    private void DestroyCurrentInterstitial()
    {
        if (_interstitialAd == null) return;
        _interstitialAd.OnAdFullScreenContentOpened -= OnAdOpened;
        _interstitialAd.OnAdFullScreenContentClosed -= OnAdClosed;
        _interstitialAd.OnAdFullScreenContentFailed -= OnAdFailedToShow;
        _interstitialAd.Destroy();
        _interstitialAd = null;
    }

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

    public void LoadInterstitialAd()
    {
        if (!_isInitialized) return;

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
            _interstitialAd.OnAdFullScreenContentOpened += OnAdOpened;
            _interstitialAd.OnAdFullScreenContentClosed += OnAdClosed;
            _interstitialAd.OnAdFullScreenContentFailed += OnAdFailedToShow;
        });
    }

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

    public bool IsRewardedAdReady => _rewardedAd != null && _rewardedAd.CanShowAd();

    public event Action OnRewardedAdFlowEndedWithoutReward;

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
            _rewardEarnedThisShow = true;
        });
    }

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
        PauseGameForAd();
    }

    private void OnAdClosed()
    {
        UnpauseGameAfterAd();
        LoadInterstitialAd();
    }

    private void OnAdFailedToShow(AdError error)
    {
        UnpauseGameAfterAd();
        Debug.LogWarning($"[AdManager] Interstitial show failed: {error?.GetMessage()}");
        LoadInterstitialAd();
    }

    private void OnRewardedAdOpened()
    {
        _rewardedAdDidOpen = true;
        PauseGameForAd();
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
        UnpauseGameAfterAd();
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
        UnpauseGameAfterAd();
        Debug.LogWarning($"[AdManager] Rewarded show failed: {error?.GetMessage()}");
        LoadRewardedAd();
    }
}
