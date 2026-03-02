using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// 타이틀 씬의 UI 및 데이터를 담당합니다.
/// 닉네임 표시·수정, 랭킹 보드(ScrollView + 프리팹), 내 랭킹 표시, 게임 시작 버튼 처리.
/// </summary>
public class TitleSceneController : MonoBehaviour
{
    private const string GuestPrefix = "TEST_";

    [Header("Nickname")]
    [SerializeField] private TextMeshProUGUI nicknameText;

    [Header("Nickname Edit Popup")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private GameObject nicknamePopup;

    [Header("Ranking Board (ScrollView)")]
    [Tooltip("ScrollView의 Content. 여기 아래에 랭킹 아이템이 생성됩니다. Content에는 Vertical Layout Group + Content Size Fitter를 붙여 두는 것을 권장합니다.")]
    [SerializeField] private Transform contentTransform;
    [SerializeField] private GameObject rankingItemPrefab;
    [Tooltip("서버 오류 또는 빈 리스트일 때 표시할 메시지용. ScrollView 위/아래 별도 텍스트로 두면 됩니다.")]
    [SerializeField] private TextMeshProUGUI rankingErrorMessageText;

    [Header("My Rank")]
    [Tooltip("내 순위 한 줄을 표시할 부모. 여기 아래에 rankingItemPrefab 인스턴스가 1개 생성됩니다.")]
    [SerializeField] private Transform myRankItemParent;

    [Header("Loading & Error")]
    [SerializeField] private GameObject loadingSpinner;
    [SerializeField] private string serverErrorMessage = "서버와 연결할 수 없습니다.";

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("API (선택)")]
    [Tooltip("비워두면 아래 apiBaseUrl 등 사용. 인게임과 동일 설정 시 여기서 참조 가능")]
    [SerializeField] private ScoreMarkerManager apiConfigSource;
    [SerializeField] private string apiBaseUrl = "http://localhost:5000/api/Ranking";
    [SerializeField] private string apiKeyHeader = "X-Api-Key";
    [SerializeField] private string apiKey = "";

    private string _apiBase;
    private string _apiKey;
    private string _apiKeyHeader;
    private string _myNickname;
    private List<RankingEntryDto> _lastTopList;
    private GameObject _myRankItemInstance;

    private void Awake()
    {
        if (apiConfigSource != null)
        {
            _apiBase = apiConfigSource.ApiBaseUrl?.TrimEnd('/') ?? "";
            _apiKey = apiConfigSource.ApiKey ?? "";
            _apiKeyHeader = apiConfigSource.ApiKeyHeader ?? "X-Api-Key";
        }
        else
        {
            _apiBase = apiBaseUrl?.TrimEnd('/') ?? "";
            _apiKey = apiKey ?? "";
            _apiKeyHeader = apiKeyHeader ?? "X-Api-Key";
        }
    }

    private void Start()
    {
        _myNickname = RankingSubmitter.GetOrCreateNickname();
        if (nicknameText != null)
            nicknameText.text = _myNickname;

        if (loadingSpinner != null)
            loadingSpinner.SetActive(true);
        if (rankingErrorMessageText != null)
            rankingErrorMessageText.text = "";
        if (rankingErrorMessageText != null)
            rankingErrorMessageText.gameObject.SetActive(false);
        if (nicknamePopup != null)
            nicknamePopup.SetActive(false);

        StartCoroutine(FetchRankingAndUpdateUI());
    }

    private IEnumerator FetchRankingAndUpdateUI()
    {
        List<RankingEntryDto> topList = null;
        bool serverError = false;

        yield return FetchTopRankings(list =>
        {
            topList = list;
        }, () =>
        {
            serverError = true;
        });

        if (loadingSpinner != null)
            loadingSpinner.SetActive(false);

        if (serverError)
        {
            if (rankingErrorMessageText != null)
            {
                rankingErrorMessageText.text = serverErrorMessage;
                rankingErrorMessageText.gameObject.SetActive(true);
            }
            if (contentTransform != null)
                ClearContentChildren();
            _lastTopList = null;
            RefreshMyRankItem(0, _myNickname, 0);
            yield break;
        }

        if (contentTransform != null)
            ClearContentChildren();

        if (rankingErrorMessageText != null)
            rankingErrorMessageText.gameObject.SetActive(false);

        _lastTopList = topList;

        if (topList != null && topList.Count > 0 && rankingItemPrefab != null)
        {
            foreach (var e in topList)
            {
                var go = Instantiate(rankingItemPrefab, contentTransform);
                var item = go.GetComponent<RankingItem>();
                if (item != null)
                    item.SetData(e.rank, e.nickname ?? "", e.score);
            }
        }
        else
        {
            if (rankingErrorMessageText != null)
            {
                rankingErrorMessageText.text = "아직 기록이 없습니다.";
                rankingErrorMessageText.gameObject.SetActive(true);
            }
        }

        int myRank = FindMyRankInList(topList, _myNickname);
        long myScore = GetMyScoreInList(topList, _myNickname);
        RefreshMyRankItem(myRank, _myNickname, myScore);
    }

    private void RefreshMyRankItem(int myRank, string nickname, long myScore)
    {
        if (myRankItemParent == null || rankingItemPrefab == null) return;

        if (_myRankItemInstance != null)
        {
            Destroy(_myRankItemInstance);
            _myRankItemInstance = null;
        }

        _myRankItemInstance = Instantiate(rankingItemPrefab, myRankItemParent);
        var item = _myRankItemInstance.GetComponent<RankingItem>();
        if (item != null)
            item.SetData(myRank, nickname ?? "", myScore);
    }

    private IEnumerator FetchTopRankings(Action<List<RankingEntryDto>> onSuccess, Action onFailure)
    {
        if (string.IsNullOrEmpty(_apiBase))
        {
            onFailure?.Invoke();
            yield break;
        }

        string url = _apiBase + "/GetTopRankings";
        using (var req = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(_apiKey))
                req.SetRequestHeader(_apiKeyHeader, _apiKey);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[TitleSceneController] GetTopRankings failed: {req.error}");
                onFailure?.Invoke();
                yield break;
            }

            string json = req.downloadHandler?.text ?? "";
            var list = ParseTopRankingsJson(json);
            onSuccess?.Invoke(list);
        }
    }

    private static List<RankingEntryDto> ParseTopRankingsJson(string json)
    {
        var list = new List<RankingEntryDto>();
        if (string.IsNullOrWhiteSpace(json)) return list;

        string wrapped = "{\"items\":" + json + "}";
        try
        {
            var wrapper = JsonUtility.FromJson<RankingTopWrapper>(wrapped);
            if (wrapper?.items != null)
                list.AddRange(wrapper.items);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TitleSceneController] Parse error: {e.Message}");
        }

        return list;
    }

    private static int FindMyRankInList(List<RankingEntryDto> list, string nickname)
    {
        if (list == null || string.IsNullOrEmpty(nickname)) return 0;
        var n = nickname.Trim();
        foreach (var e in list)
        {
            if (string.Equals(e.nickname?.Trim(), n, StringComparison.OrdinalIgnoreCase))
                return e.rank;
        }
        return 0;
    }

    private static long GetMyScoreInList(List<RankingEntryDto> list, string nickname)
    {
        if (list == null || string.IsNullOrEmpty(nickname)) return 0;
        var n = nickname.Trim();
        foreach (var e in list)
        {
            if (string.Equals(e.nickname?.Trim(), n, StringComparison.OrdinalIgnoreCase))
                return e.score;
        }
        return 0;
    }

    private void ClearContentChildren()
    {
        if (contentTransform == null) return;
        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            var child = contentTransform.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    /// <summary>닉네임 수정 팝업을 열고, 현재 닉네임을 InputField에 표시합니다. DOTween으로 0→1 스케일 애니메이션.</summary>
    public void OpenNicknamePopup()
    {
        if (nicknamePopup == null) return;
        nicknamePopup.SetActive(true);
        if (nicknameInputField != null)
            nicknameInputField.text = _myNickname;

        nicknamePopup.transform.localScale = Vector3.zero;
        nicknamePopup.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    /// <summary>한글·영문·숫자만 남기고 10자 이하로 정제 후 저장하고, 팝업을 닫은 뒤 닉네임·내 랭킹을 갱신합니다.</summary>
    public void SaveNickname()
    {
        if (nicknameInputField == null) return;
        string raw = nicknameInputField.text ?? "";

        string refined = RefineNickname(raw);
        if (string.IsNullOrEmpty(refined)) return;

        if (refined.Length > 10)
            refined = refined.Substring(0, 10);

        RankingSubmitter.SaveNickname(refined);
        _myNickname = refined;
        if (nicknameInputField != null)
            nicknameInputField.text = refined;
        if (nicknameText != null)
            nicknameText.text = refined;

        if (nicknamePopup != null)
        {
            nicknamePopup.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                if (nicknamePopup != null)
                    nicknamePopup.SetActive(false);
            });
        }

        int myRank = FindMyRankInList(_lastTopList, _myNickname);
        long myScore = GetMyScoreInList(_lastTopList, _myNickname);
        RefreshMyRankItem(myRank, _myNickname, myScore);
    }

    /// <summary>특수문자 제거, 한글·영문·숫자만 남깁니다.</summary>
    private static string RefineNickname(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var onlyAllowed = new Regex(@"[^\uAC00-\uD7A3a-zA-Z0-9]");
        return onlyAllowed.Replace(input, "").Trim();
    }

    /// <summary>게임 시작 버튼 OnClick에서 호출. TEST_ 기본 닉네임이면 닉네임 팝업을 먼저 띄우고, 이미 변경했으면 인게임 씬으로 전환합니다.</summary>
    public void OnStartGameClicked()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[TitleSceneController] gameSceneName is empty.");
            return;
        }

        string current = RankingSubmitter.GetOrCreateNickname();
        if (current.StartsWith(GuestPrefix, StringComparison.OrdinalIgnoreCase))
        {
            OpenNicknamePopup();
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    [Serializable]
    private class RankingTopWrapper
    {
        public RankingEntryDto[] items;
    }

    [Serializable]
    private class RankingEntryDto
    {
        public int rank;
        public string nickname;
        public long score;
    }
}
