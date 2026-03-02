using UnityEngine;

/// <summary>
/// 플레이어 도달 높이에 따라 카메라 배경색만 변경합니다. (SRP: 배경색 전담)
/// 카메라의 Clear Flags가 Solid Color로 설정되어 있어야 배경색이 보입니다.
/// </summary>
public class CameraColorManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Gradient backgroundColorGradient;
    [SerializeField] private float maxHeight = 500f;

    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (player == null || _camera == null || backgroundColorGradient == null) return;

        float t = Mathf.Clamp01(player.position.y / maxHeight);
        _camera.backgroundColor = backgroundColorGradient.Evaluate(t);
    }
}
