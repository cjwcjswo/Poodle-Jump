using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// 서버 GetMarkers API를 호출해 상위 기록을 가져와 원통 외곽에 마커를 배치하고,
/// 추월 시 연출·구간별 갱신·카메라 뒤 마커 비활성화를 처리합니다.
/// ScoreMarker 프리팹 구성: 빈 GameObject + ScoreMarker.cs, 자식에 Canvas(Render Mode: World Space) + 닉네임용 TextMeshProUGUI.
/// </summary>
public class ScoreMarkerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CylinderMovement cylinderMovement;
    [SerializeField] private GameManager gameManager;

    [Header("Marker")]
    [Tooltip("World Space Canvas + 닉네임 Text가 있는 마커 프리팹")]
    [SerializeField] private GameObject scoreMarkerPrefab;
    [Tooltip("원통 반지름보다 살짝 밖에 배치할 반지름 (시인성)")]
    [SerializeField] [Min(1f)] private float markerRadius = 6.5f;
    [Tooltip("마커 배치 시 사용할 기준 각도(라디안). 플레이어 반대편 등")]
    [SerializeField] private float markerBaseAngleRad = Mathf.PI;

    [Header("API")]
    [SerializeField] private string apiBaseUrl = "http://localhost:5000/api/Ranking";
    [SerializeField] private string apiKeyHeader = "X-Api-Key";
    [SerializeField] private string apiKey = "";

    /// <summary>랭킹 API 베이스 URL (RankingSubmitter 등에서 공유)</summary>
    public string ApiBaseUrl => apiBaseUrl;
    /// <summary>API Key 헤더 이름</summary>
    public string ApiKeyHeader => apiKeyHeader;
    /// <summary>API Key 값</summary>
    public string ApiKey => apiKey;

    [Header("Segment Refresh")]
    [Tooltip("이 높이(미터) 구간마다 다음 마커 세트를 서버에서 다시 받아옵니다.")]
    [SerializeField] [Min(10f)] private float segmentInterval = 100f;

    [Header("Optimization")]
    [Tooltip("플레이어 Y보다 이만큼 아래로 내려간 마커는 비활성화 후 풀에 반환")]
    [SerializeField] [Min(5f)] private float cullBelowPlayer = 25f;

    [Header("Overtake Feedback")]
    [SerializeField] private AudioClip overtakeSound;
    [SerializeField] private AudioSource audioSource;
    [Tooltip("추월 시 잠깐 보여줄 팝업 텍스트 (선택). 비우면 사운드만 재생")]
    [SerializeField] private TextMeshProUGUI overtakePopupText;
    [SerializeField] private float overtakePopupDuration = 1.2f;

    private readonly List<ScoreMarker> _activeMarkers = new List<ScoreMarker>();
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private float _nextSegmentBase;
    private bool _fetchInProgress;
    private Transform _poolContainer;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (cylinderMovement == null && player != null)
            cylinderMovement = player.GetComponent<CylinderMovement>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();

        _poolContainer = new GameObject("ScoreMarkerPool").transform;
        _poolContainer.SetParent(transform);
    }

    private void Start()
    {
        _nextSegmentBase = segmentInterval;
        StartCoroutine(FetchAndSpawnMarkers(0));
    }

    private void Update()
    {
        if (player == null || gameManager == null || gameManager.State != GameManager.GameState.Playing) return;

        float playerY = player.position.y;
        int currentScore = gameManager.Score;

        for (int i = _activeMarkers.Count - 1; i >= 0; i--)
        {
            var marker = _activeMarkers[i];
            if (marker == null || !marker.gameObject.activeInHierarchy) continue;

            if (!marker.IsOvertaken && playerY > marker.ScoreY)
            {
                TriggerOvertake(marker);
                marker.IsOvertaken = true;
                continue;
            }

            if (playerY - marker.ScoreY > cullBelowPlayer)
            {
                ReturnToPool(marker);
                _activeMarkers.RemoveAt(i);
            }
        }

        if (!_fetchInProgress && playerY >= _nextSegmentBase)
        {
            _nextSegmentBase += segmentInterval;
            StartCoroutine(FetchAndSpawnMarkers(currentScore));
        }
    }

    private void TriggerOvertake(ScoreMarker marker)
    {
        if (overtakeSound != null && audioSource != null && !audioSource.isPlaying)
            audioSource.PlayOneShot(overtakeSound);

        if (overtakePopupText != null)
        {
            overtakePopupText.text = "추월 성공!";
            overtakePopupText.gameObject.SetActive(true);
            StartCoroutine(HideOvertakePopupAfter(overtakePopupDuration));
        }
    }

    private IEnumerator HideOvertakePopupAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (overtakePopupText != null)
            overtakePopupText.gameObject.SetActive(false);
    }

    private IEnumerator FetchAndSpawnMarkers(int currentScore)
    {
        _fetchInProgress = true;

        string url = $"{apiBaseUrl.TrimEnd('/')}/GetMarkers?currentScore={currentScore}";
        using (var req = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(apiKey))
                req.SetRequestHeader(apiKeyHeader, apiKey);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ScoreMarkerManager] GetMarkers failed: {req.error}");
                _fetchInProgress = false;
                yield break;
            }

            string json = req.downloadHandler.text;
            var entries = ParseMarkersJson(json);
            if (entries != null && entries.Length > 0)
            {
                foreach (var m in _activeMarkers.ToArray())
                    ReturnToPool(m);
                _activeMarkers.Clear();

                float angleRad = markerBaseAngleRad;
                float angleStep = (2f * Mathf.PI) / Mathf.Max(1, entries.Length);
                foreach (var e in entries)
                {
                    float scoreY = e.score;
                    var marker = GetOrCreateMarker();
                    if (marker != null)
                    {
                        marker.SetData(e.rank, e.nickname ?? "", scoreY);
                        PlaceMarkerOnCylinder(marker, angleRad, scoreY);
                        _activeMarkers.Add(marker);
                        angleRad += angleStep;
                    }
                }
            }
        }

        _fetchInProgress = false;
    }

    private void PlaceMarkerOnCylinder(ScoreMarker marker, float angleRad, float heightY)
    {
        float x = markerRadius * Mathf.Cos(angleRad);
        float z = markerRadius * Mathf.Sin(angleRad);
        marker.transform.position = new Vector3(x, heightY, z);

        Vector3 outward = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
        if (outward.sqrMagnitude > 0.001f)
            marker.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
    }

    private ScoreMarker GetOrCreateMarker()
    {
        GameObject go;
        if (_pool.Count > 0)
        {
            go = _pool.Dequeue();
            go.SetActive(true);
        }
        else
        {
            if (scoreMarkerPrefab == null) return null;
            go = Instantiate(scoreMarkerPrefab, _poolContainer);
        }

        var marker = go.GetComponent<ScoreMarker>();
        if (marker == null)
            marker = go.AddComponent<ScoreMarker>();
        marker.IsOvertaken = false;
        return marker;
    }

    private void ReturnToPool(ScoreMarker marker)
    {
        if (marker == null) return;
        marker.IsOvertaken = false;
        marker.gameObject.SetActive(false);
        marker.transform.SetParent(_poolContainer);
        _pool.Enqueue(marker.gameObject);
    }

    private static RankingEntryDto[] ParseMarkersJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<RankingEntryDto>();

        string wrapped = "{\"items\":" + json + "}";
        try
        {
            var wrapper = JsonUtility.FromJson<RankingMarkersWrapper>(wrapped);
            return wrapper?.items ?? Array.Empty<RankingEntryDto>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ScoreMarkerManager] Parse error: {e.Message}");
            return Array.Empty<RankingEntryDto>();
        }
    }

    [Serializable]
    private class RankingMarkersWrapper
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
