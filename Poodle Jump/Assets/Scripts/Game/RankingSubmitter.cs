using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 게임 오버 시 서버 SubmitScore API에 점수를 자동 제출합니다.
/// ScoreMarkerManager의 API 설정을 참조하며, 닉네임은 PlayerPrefs(SavedNickname)를 사용합니다.
/// </summary>
public class RankingSubmitter : MonoBehaviour
{
    private const string NicknameKey = "SavedNickname";
    private const string GuestPrefix = "GUEST_";

    [SerializeField] private GameManager gameManager;
    [Tooltip("비워두면 씬에서 ScoreMarkerManager를 찾아 API 설정을 사용합니다.")]
    [SerializeField] private ScoreMarkerManager apiConfigSource;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (apiConfigSource == null)
            apiConfigSource = FindFirstObjectByType<ScoreMarkerManager>();
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.OnFinalGameOver += HandleFinalGameOver;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnFinalGameOver -= HandleFinalGameOver;
    }

    /// <summary>최종 게임 오버(부활하지 않음 또는 부활 후 재사망) 시에만 점수 제출.</summary>
    private void HandleFinalGameOver()
    {
        int score = gameManager != null ? gameManager.Score : 0;
        if (score <= 0)
            return;

        string nickname = GetOrCreateNickname();
        StartCoroutine(SubmitScoreCoroutine(nickname, score));
    }

    /// <summary>
    /// PlayerPrefs에서 닉네임을 읽거나, 없으면 Guest_XXXX 생성 후 저장합니다.
    /// </summary>
    public static string GetOrCreateNickname()
    {
        string nickname = PlayerPrefs.GetString(NicknameKey, "");
        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = GuestPrefix + UnityEngine.Random.Range(1000, 10000).ToString();
            PlayerPrefs.SetString(NicknameKey, nickname);
            PlayerPrefs.Save();
        }
        return nickname.Trim();
    }

    /// <summary>
    /// 유저가 닉네임을 변경할 때 호출합니다. 다음 게임 오버부터 새 닉네임으로 제출됩니다.
    /// </summary>
    public static void SaveNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname)) return;
        PlayerPrefs.SetString(NicknameKey, nickname.Trim());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 현재 저장된 닉네임을 반환합니다. 없으면 빈 문자열.
    /// </summary>
    public static string GetSavedNickname()
    {
        return PlayerPrefs.GetString(NicknameKey, "");
    }

    private IEnumerator SubmitScoreCoroutine(string nickname, int score)
    {
        string url = (apiConfigSource != null ? apiConfigSource.ApiBaseUrl : "http://localhost:5000/api/Ranking").TrimEnd('/') + "/SubmitScore";
        string apiKey = apiConfigSource != null ? apiConfigSource.ApiKey : "";
        string apiKeyHeader = apiConfigSource != null ? apiConfigSource.ApiKeyHeader : "X-Api-Key";

        var body = new SubmitScoreBody { Nickname = nickname, Score = score };
        string json = JsonUtility.ToJson(body);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(apiKey))
                req.SetRequestHeader(apiKeyHeader, apiKey);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[RankingSubmitter] SubmitScore failed: {req.error} (URL: {url})");
                yield break;
            }

            string responseText = req.downloadHandler?.text ?? "";
            try
            {
                var response = JsonUtility.FromJson<SubmitScoreResponse>(responseText);
                if (!string.IsNullOrEmpty(response.message))
                    Debug.Log($"[RankingSubmitter] {response.message} (nickname: {response.nickname}, score: {response.score})");
                else
                    Debug.Log($"[RankingSubmitter] Score submitted. nickname: {response.nickname}, score: {response.score}");
            }
            catch
            {
                Debug.Log($"[RankingSubmitter] Score submitted. Response: {responseText}");
            }
        }
    }

    [Serializable]
    private class SubmitScoreBody
    {
        public string Nickname;
        public long Score;
    }

    [Serializable]
    private class SubmitScoreResponse
    {
        public string nickname;
        public long score;
        public string message;
    }
}
