using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 타입별 발판 프리팹을 관리하는 풀링 전담 클래스. 타입마다 별도 ObjectPool을 두어 Get(type) / Release(go)로 사용합니다.
/// </summary>
public class PlatformPool : MonoBehaviour
{
    [System.Serializable]
    public struct PrefabMapping
    {
        [Tooltip("이 타입에 쓸 프리팹")]
        public Platform.PlatformType type;
        [Tooltip("해당 타입 전용 발판 프리팹")]
        public GameObject prefab;
    }

    [SerializeField] private List<PrefabMapping> prefabMappings = new List<PrefabMapping>();
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int defaultCapacity = 32;
    [SerializeField] private int maxSize = 128;

    private Dictionary<Platform.PlatformType, ObjectPool<GameObject>> _pools = new Dictionary<Platform.PlatformType, ObjectPool<GameObject>>();

    private void Awake()
    {
        if (prefabMappings == null || prefabMappings.Count == 0)
        {
            Debug.LogError("[PlatformPool] Prefab Mappings이 비어 있습니다.", this);
            return;
        }

        Transform parent = poolContainer != null ? poolContainer : transform;
        foreach (var mapping in prefabMappings)
        {
            if (mapping.prefab == null) continue;
            GameObject prefab = mapping.prefab;
            var pool = new ObjectPool<GameObject>(
                createFunc: () => CreateFromPrefab(prefab, parent),
                actionOnGet: OnGet,
                actionOnRelease: go => OnRelease(go, parent),
                actionOnDestroy: OnDestroy,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
            _pools[mapping.type] = pool;
        }
    }

    private static GameObject CreateFromPrefab(GameObject prefab, Transform parent)
    {
        GameObject instance = Object.Instantiate(prefab, parent);
        instance.SetActive(false);
        return instance;
    }

    private static void OnGet(GameObject go)
    {
        go.SetActive(true);
    }

    private static void OnRelease(GameObject go, Transform poolContainer)
    {
        go.SetActive(false);
        if (poolContainer != null)
            go.transform.SetParent(poolContainer);
    }

    private static void OnDestroy(GameObject go)
    {
        if (go != null)
            Object.Destroy(go);
    }

    /// <summary>지정한 타입의 풀에서 발판 인스턴스 하나를 꺼냅니다.</summary>
    public GameObject Get(Platform.PlatformType type)
    {
        if (_pools == null || !_pools.TryGetValue(type, out var pool)) return null;
        return pool.Get();
    }

    /// <summary>발판 인스턴스를 해당 타입 풀에 반환합니다. Platform 컴포넌트의 타입을 읽어 사용합니다.</summary>
    public void Release(GameObject go)
    {
        if (go == null || _pools == null) return;
        var platform = go.GetComponent<Platform>();
        if (platform == null || !_pools.TryGetValue(platform.Type, out var pool)) return;
        pool.Release(go);
    }
}
