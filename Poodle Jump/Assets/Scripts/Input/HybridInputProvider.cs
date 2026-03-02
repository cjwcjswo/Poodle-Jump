using UnityEngine;

/// <summary>
/// 키보드(Horizontal)와 기울기(Input.acceleration.x)를 합산하여 좌우 입력을 제공합니다.
/// PlayerController의 inputProvider 슬롯에 연결하면 데스크톱/모바일 모두 대응합니다.
/// </summary>
public class HybridInputProvider : MonoBehaviour, IPlayerInput
{
    [SerializeField] [Min(0.1f)] private float tiltSensitivity = 2f;

    public float MoveInput
    {
        get
        {
            float keyboard = Input.GetAxis("Horizontal");
            float tilt = Input.acceleration.x * tiltSensitivity;
            return Mathf.Clamp(keyboard + tilt, -1f, 1f);
        }
    }
}
