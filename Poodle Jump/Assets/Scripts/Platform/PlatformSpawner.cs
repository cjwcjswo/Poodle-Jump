using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlatformProbability
{
    public Platform.PlatformType type;
    [Tooltip("시작 높이(y≈0)에서의 가중치")]
    public float startWeight;
    [Tooltip("difficultyMaxHeight 도달 시 가중치")]
    public float maxWeight;
}

/// <summary>
/// PlatformPool을 참조하여 발판을 무한히 깔아주는 매니저. CylinderMovement 방식으로 나선형 계단 배치 및 회수 로직을 담당합니다.
/// </summary>
public class PlatformSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlatformPool platformPool;
    [SerializeField] private CylinderMovement cylinderMovement;
    [SerializeField] private Transform player;

    [Header("Spiral Layout (나선형 계단)")]
    [Tooltip("Y축 발판 간격 최소값. 난이도가 올라가면 min~max 사이로 보간됨.")]
    [SerializeField] [Min(0.5f)] private float minSpawnHeightStep = 2f;
    [Tooltip("Y축 발판 간격 최대값. 플레이어 jumpForce 한계를 넘지 않도록 제한.")]
    [SerializeField] [Min(0.5f)] private float maxSpawnHeightStep = 4f;
    [Tooltip("발판 사이 회전 간격 최소값 (도). 매 스폰마다 난이도에 따라 min~범위 최대 사이 랜덤 적용.")]
    [SerializeField] [Min(1f)] private float minSpawnAngleStep = 30f;
    [Tooltip("발판 사이 회전 간격 최대값 (도)")]
    [SerializeField] [Min(1f)] private float maxSpawnAngleStep = 100f;

    [Header("Difficulty")]
    [Tooltip("이 높이에서 난이도가 최고조(간격·각도 최대)에 달함. 그 이상은 max 값 유지.")]
    [SerializeField] private float difficultyMaxHeight = 500f;

    [Header("Spawn / Despawn")]
    [Tooltip("플레이어 Y보다 이만큼 위까지 미리 생성")]
    [SerializeField] [Min(1f)] private float spawnAheadDistance = 15f;
    [Tooltip("플레이어 Y보다 이만큼 아래에 있으면 풀 반환")]
    [SerializeField] [Min(1f)] private float despawnDistance = 10f;

    [Header("Color Variations")]
    [Tooltip("높이에 따른 발판 색상. MaterialPropertyBlock으로 적용하므로 머티리얼 인스턴스 생성 없음.")]
    [SerializeField] private Gradient platformColorGradient;
    [Tooltip("이 높이를 1로 간주하여 Gradient를 평가합니다.")]
    [SerializeField] private float maxHeightForColor = 500f;

    [Header("Branching Paths")]
    [Tooltip("메인 발판 생성 시 반대편에 서브 발판을 함께 스폰할 확률 (0~1).")]
    [SerializeField] [Range(0f, 1f)] private float doubleSpawnChance = 0.3f;
    [Tooltip("서브 발판의 Y 높이 오프셋 범위. 메인 spawnY ± 이 값 안에서 랜덤.")]
    [SerializeField] private float subPlatformHeightOffset = 1f;

    [Header("Dynamic Type Probability")]
    [Tooltip("높이에 따라 startWeight → maxWeight로 보간. 가중치 합으로 비율 결정.")]
    [SerializeField] private List<PlatformProbability> typeProbabilities;

    private float _lastSpawnY;
    private float _lastSpawnAngleRad;
    private readonly List<GameObject> _activePlatforms = new List<GameObject>();

    private void Start()
    {
        if (player == null || platformPool == null || cylinderMovement == null) return;

        _lastSpawnAngleRad = Mathf.Atan2(player.position.z, player.position.x);
        _lastSpawnY = player.position.y - 0.5f;

        GameObject startPlatform = platformPool.Get(Platform.PlatformType.Normal);
        if (startPlatform != null)
        {
            Vector3 position = cylinderMovement.GetPositionOnCylinder(_lastSpawnAngleRad, _lastSpawnY);
            Quaternion rotation = cylinderMovement.GetRotationTowardOutward(_lastSpawnAngleRad);
            startPlatform.transform.SetPositionAndRotation(position, rotation);
            var startPlatformComp = startPlatform.GetComponent<Platform>();
            if (startPlatformComp != null) startPlatformComp.Init(cylinderMovement, _lastSpawnAngleRad, _lastSpawnY);
            ApplyPlatformColor(startPlatform, _lastSpawnY);
            _activePlatforms.Add(startPlatform);
        }

        PrewarmPlatforms();
    }

    private void PrewarmPlatforms()
    {
        if (player == null || platformPool == null || cylinderMovement == null) return;
        float targetY = player.position.y + spawnAheadDistance;
        while (_lastSpawnY < targetY)
            SpawnOne();
    }

    private void Update()
    {
        if (player == null || platformPool == null || cylinderMovement == null) return;

        float playerY = player.position.y;

        for (int i = _activePlatforms.Count - 1; i >= 0; i--)
        {
            GameObject go = _activePlatforms[i];
            if (go == null) continue;
            if (go.transform.position.y < playerY - despawnDistance)
            {
                platformPool.Release(go);
                _activePlatforms.RemoveAt(i);
            }
        }

        while (playerY + spawnAheadDistance > _lastSpawnY)
            SpawnOne();
    }

    private void SpawnOne()
    {
        Platform.PlatformType platformType = RollPlatformType();
        GameObject go = platformPool.Get(platformType);
        if (go == null) return;

        float difficultyT = Mathf.Clamp01(_lastSpawnY / difficultyMaxHeight);
        float currentHeightStep = Mathf.Lerp(minSpawnHeightStep, maxSpawnHeightStep, difficultyT);
        float currentMinAngle = Mathf.Lerp(minSpawnAngleStep, maxSpawnAngleStep * 0.7f, difficultyT);

        float spawnY = _lastSpawnY + currentHeightStep;
        float angleStep = Random.Range(currentMinAngle, maxSpawnAngleStep);
        float angleSign = Random.value < 0.5f ? 1f : -1f;
        float angleRad = _lastSpawnAngleRad + angleSign * angleStep * Mathf.Deg2Rad;

        Vector3 position = cylinderMovement.GetPositionOnCylinder(angleRad, spawnY);
        Quaternion rotation = cylinderMovement.GetRotationTowardOutward(angleRad);

        var platformComp = go.GetComponent<Platform>();
        if (platformComp != null) platformComp.Init(cylinderMovement, angleRad, spawnY);

        go.transform.SetPositionAndRotation(position, rotation);
        ApplyPlatformColor(go, spawnY);
        _activePlatforms.Add(go);

        _lastSpawnY = spawnY;
        _lastSpawnAngleRad = angleRad;

        if (Random.value < doubleSpawnChance)
        {
            float branchPointAngleRad = angleRad - angleSign * angleStep * Mathf.Deg2Rad;
            float subAngleRad = branchPointAngleRad + (-angleSign) * angleStep * Mathf.Deg2Rad;
            float subSpawnY = spawnY + Random.Range(-subPlatformHeightOffset, subPlatformHeightOffset);
            Platform.PlatformType subType = RollPlatformType();
            GameObject sub = platformPool.Get(subType);
            if (sub != null)
            {
                var subPlatformComp = sub.GetComponent<Platform>();
                if (subPlatformComp != null) subPlatformComp.Init(cylinderMovement, subAngleRad, subSpawnY);
                Vector3 subPosition = cylinderMovement.GetPositionOnCylinder(subAngleRad, subSpawnY);
                Quaternion subRotation = cylinderMovement.GetRotationTowardOutward(subAngleRad);
                sub.transform.SetPositionAndRotation(subPosition, subRotation);
                ApplyPlatformColor(sub, subSpawnY);
                _activePlatforms.Add(sub);
            }
        }
    }

    private Platform.PlatformType RollPlatformType()
    {
        if (typeProbabilities == null || typeProbabilities.Count == 0)
            return Platform.PlatformType.Normal;

        float difficultyT = Mathf.Clamp01(_lastSpawnY / difficultyMaxHeight);

        float totalWeight = 0f;
        for (int i = 0; i < typeProbabilities.Count; i++)
        {
            var p = typeProbabilities[i];
            float currentWeight = Mathf.Lerp(p.startWeight, p.maxWeight, difficultyT);
            totalWeight += Mathf.Max(0f, currentWeight);
        }

        if (totalWeight <= 0f)
            return Platform.PlatformType.Normal;

        float roll = Random.Range(0f, totalWeight);
        float acc = 0f;
        for (int i = 0; i < typeProbabilities.Count; i++)
        {
            var p = typeProbabilities[i];
            float currentWeight = Mathf.Lerp(p.startWeight, p.maxWeight, difficultyT);
            currentWeight = Mathf.Max(0f, currentWeight);
            acc += currentWeight;
            if (roll < acc)
                return p.type;
        }

        return Platform.PlatformType.Normal;
    }

    private void ApplyPlatformColor(GameObject platform, float worldY)
    {
        if (platformColorGradient == null) return;

        float t = Mathf.Clamp01(worldY / maxHeightForColor);
        Color color = platformColorGradient.Evaluate(t);

        var platformComp = platform.GetComponent<Platform>();
        if (platformComp != null)
            platformComp.ApplyColor(color);
    }
}
