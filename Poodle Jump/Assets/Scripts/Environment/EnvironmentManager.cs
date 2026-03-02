using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ThemeSettings
{
    [Tooltip("이 테마가 시작되는 월드 Y 높이 (예: 개미 왕국 0, 달나라 400)")]
    public float startY;
    [Tooltip("이 테마의 하늘/앰비언트 색상")]
    public Color skyColor;
    [Tooltip("이 테마의 안개 색상")]
    public Color fogColor;
    [Tooltip("이 테마 전용 Skybox 머티리얼 (비워두면 기존 스카이박스 유지)")]
    public Material skyboxMaterial;
    [Tooltip("이 테마 구역에서 스폰할 배경 데코 프리팹들 (비어 있으면 스폰 안 함)")]
    public GameObject[] decoPrefabs;
}

/// <summary>
/// 고도에 따라 배경색(Skybox/Ambient), 안개색, 배경 데코 스폰을 통합 관리합니다.
/// 테마는 startY 오름차순으로 정렬되어 사용됩니다.
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Themes (startY 오름차순 권장)")]
    [SerializeField] private List<ThemeSettings> themes = new List<ThemeSettings>();

    [Header("Deco Spawn (Parallax)")]
    [Tooltip("데코가 스폰되는 원기둥 반지름 (플레이어보다 멀리)")]
    [SerializeField] [Min(10f)] private float decoMinRadius = 15f;
    [SerializeField] [Min(10f)] private float decoMaxRadius = 20f;
    [Tooltip("플레이어 Y보다 이만큼 아래로 내려가면 데코 제거")]
    [SerializeField] [Min(5f)] private float decoDespawnBelow = 25f;
    [Tooltip("0~1. 플레이어 상승량의 이 비율만큼 데코가 따라옴 (낮을수록 입체감 강함)")]
    [SerializeField] [Range(0f, 1f)] private float parallaxFactor = 0.25f;

    [Header("Theme Transition (Fog Curtain)")]
    [Tooltip("배경을 완전히 가릴 정도의 짙은 안개 밀도")]
    [SerializeField] [Min(0.01f)] private float transitionFogDensity = 0.08f;
    [Tooltip("안개가 짙어지고 옅어지는 속도 (밀도/초)")]
    [SerializeField] [Min(0.1f)] private float fadeSpeed = 2f;

    private List<ThemeSettings> _sortedThemes = new List<ThemeSettings>();
    private readonly List<GameObject> _activeDecos = new List<GameObject>();
    private int _maxReachedThemeIndex = -1;
    private int _currentThemeIndex = -1;
    private int _lastThemeIndex = -1;
    private float _lastPlayerY;
    private float _baseFogDensity = 0.01f;
    private Coroutine _transitionRoutine;
    private Material _runtimeSkyboxInstance;


    private void Awake()
    {
        if (themes == null || themes.Count == 0) return;

        _sortedThemes.AddRange(themes);
        _sortedThemes.Sort((a, b) => a.startY.CompareTo(b.startY));
    }

    private void Start()
    {
        if (player != null)
            _lastPlayerY = player.position.y;
        if (_sortedThemes != null && _sortedThemes.Count > 0)
            RenderSettings.fog = true;
        _baseFogDensity = RenderSettings.fogDensity;
    }

    private void Update()
    {
        if (player == null) return;

        float playerY = player.position.y;

        int newThemeIndex = _sortedThemes != null && _sortedThemes.Count > 0 ? GetThemeIndexForHeight(playerY) : -1;
        if (newThemeIndex >= 0)
            _maxReachedThemeIndex = Mathf.Max(_maxReachedThemeIndex, newThemeIndex);

        if (_maxReachedThemeIndex != _currentThemeIndex && _maxReachedThemeIndex >= 0 && _maxReachedThemeIndex < _sortedThemes.Count)
        {
            ThemeSettings theme = _sortedThemes[_maxReachedThemeIndex];
            if (theme.skyboxMaterial != null)
            {
                if (_transitionRoutine != null)
                    StopCoroutine(_transitionRoutine);
                _transitionRoutine = StartCoroutine(TransitionThemeRoutine(_maxReachedThemeIndex));
            }
            _currentThemeIndex = _maxReachedThemeIndex;
        }

        ApplyThemeColors(playerY);
        UpdateThemeDecoSpawning(playerY);
        UpdateDecosParallaxAndDespawn(playerY);

        _lastPlayerY = playerY;
    }

    private void ApplyThemeColors(float playerY)
    {
        if (_sortedThemes == null || _sortedThemes.Count == 0) return;

        int currentIndex = _maxReachedThemeIndex >= 0 ? Mathf.Min(_maxReachedThemeIndex, _sortedThemes.Count - 1) : 0;
        ThemeSettings current = _sortedThemes[currentIndex];
        float t = 0f;
        ThemeSettings? next = null;

        if (currentIndex + 1 < _sortedThemes.Count)
        {
            next = _sortedThemes[currentIndex + 1];
            float range = next.Value.startY - current.startY;
            if (range > 0.0001f && playerY >= current.startY)
                t = Mathf.Clamp01((playerY - current.startY) / range);
            else
                t = 0f;
        }
        else
        {
            t = 1f;
        }

        Color skyColor = next.HasValue ? Color.Lerp(current.skyColor, next.Value.skyColor, t) : current.skyColor;
        Color fogColor = next.HasValue ? Color.Lerp(current.fogColor, next.Value.fogColor, t) : current.fogColor;

        RenderSettings.ambientLight = skyColor;
        RenderSettings.fogColor = fogColor;
    }

    private IEnumerator TransitionThemeRoutine(int newIndex)
    {
        if (newIndex < 0 || newIndex >= _sortedThemes.Count)
        {
            _transitionRoutine = null;
            yield break;
        }

        ThemeSettings theme = _sortedThemes[newIndex];
        if (theme.skyboxMaterial == null)
        {
            _transitionRoutine = null;
            yield break;
        }

        const float densityEpsilon = 0.0005f;

        // 1단계: 안개 커튼 치기
        while (RenderSettings.fogDensity < transitionFogDensity - densityEpsilon)
        {
            RenderSettings.fogDensity = Mathf.MoveTowards(
                RenderSettings.fogDensity,
                transitionFogDensity,
                fadeSpeed * Time.deltaTime
            );
            yield return null;
        }
        RenderSettings.fogDensity = transitionFogDensity;

        // 2단계: 몰래 스카이박스 교체 (런타임 인스턴스 사용으로 원본 자산 수정 방지)
        if (_runtimeSkyboxInstance != null)
        {
            Destroy(_runtimeSkyboxInstance);
            _runtimeSkyboxInstance = null;
        }
        _runtimeSkyboxInstance = new Material(theme.skyboxMaterial);
        float targetExposure = (theme.skyboxMaterial.HasProperty("_Exposure"))
            ? theme.skyboxMaterial.GetFloat("_Exposure")
            : 1f;
        RenderSettings.skybox = _runtimeSkyboxInstance;
        if (_runtimeSkyboxInstance.HasProperty("_Exposure"))
            _runtimeSkyboxInstance.SetFloat("_Exposure", targetExposure * 0.5f);
        DynamicGI.UpdateEnvironment();

        // 3단계: 커튼 걷기 + 노출도 복원 (안개와 어우러지는 전환)
        float exposure = _runtimeSkyboxInstance.HasProperty("_Exposure") ? _runtimeSkyboxInstance.GetFloat("_Exposure") : targetExposure;
        while (RenderSettings.fogDensity > _baseFogDensity + densityEpsilon)
        {
            RenderSettings.fogDensity = Mathf.MoveTowards(
                RenderSettings.fogDensity,
                _baseFogDensity,
                fadeSpeed * Time.deltaTime
            );
            if (_runtimeSkyboxInstance != null && _runtimeSkyboxInstance.HasProperty("_Exposure"))
            {
                exposure = Mathf.MoveTowards(exposure, targetExposure, Time.deltaTime * 1.5f);
                _runtimeSkyboxInstance.SetFloat("_Exposure", exposure);
            }
            yield return null;
        }
        RenderSettings.fogDensity = _baseFogDensity;
        if (_runtimeSkyboxInstance != null && _runtimeSkyboxInstance.HasProperty("_Exposure"))
            _runtimeSkyboxInstance.SetFloat("_Exposure", targetExposure);

        _transitionRoutine = null;
    }

    private int GetThemeIndexForHeight(float y)
    {
        int index = 0;
        for (int i = 0; i < _sortedThemes.Count; i++)
        {
            if (y >= _sortedThemes[i].startY)
                index = i;
        }
        return index;
    }

    private void UpdateThemeDecoSpawning(float playerY)
    {
        if (_sortedThemes == null || _sortedThemes.Count == 0) return;
        if (_maxReachedThemeIndex < 0) return;

        if (_maxReachedThemeIndex == _lastThemeIndex) return;
        _lastThemeIndex = _maxReachedThemeIndex;

        ThemeSettings theme = _sortedThemes[_maxReachedThemeIndex];
        if (theme.decoPrefabs == null || theme.decoPrefabs.Length == 0) return;

        GameObject prefab = theme.decoPrefabs[Random.Range(0, theme.decoPrefabs.Length)];
        if (prefab == null) return;

        float angleRad = Random.Range(0f, 2f * Mathf.PI);
        float radius = Random.Range(decoMinRadius, decoMaxRadius);
        float x = radius * Mathf.Cos(angleRad);
        float z = radius * Mathf.Sin(angleRad);
        Vector3 position = new Vector3(x, playerY + Random.Range(-1f, 3f), z);

        GameObject deco = Instantiate(prefab, position, Quaternion.identity, transform);
        _activeDecos.Add(deco);
    }

    private void UpdateDecosParallaxAndDespawn(float playerY)
    {
        float deltaY = playerY - _lastPlayerY;

        for (int i = _activeDecos.Count - 1; i >= 0; i--)
        {
            GameObject deco = _activeDecos[i];
            if (deco == null)
            {
                _activeDecos.RemoveAt(i);
                continue;
            }

            if (deco.transform.position.y < playerY - decoDespawnBelow)
            {
                Destroy(deco);
                _activeDecos.RemoveAt(i);
                continue;
            }

            Vector3 pos = deco.transform.position;
            pos.y += deltaY * parallaxFactor;
            deco.transform.position = pos;
        }
    }

    private void OnDestroy()
    {
        foreach (GameObject deco in _activeDecos)
        {
            if (deco != null)
                Destroy(deco);
        }
        _activeDecos.Clear();
        if (_runtimeSkyboxInstance != null)
        {
            Destroy(_runtimeSkyboxInstance);
            _runtimeSkyboxInstance = null;
        }
    }
}
