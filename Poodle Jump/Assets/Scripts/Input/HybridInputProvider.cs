using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// New Input System으로 플랫폼별 키보드(에디터/스탠드얼론) 또는 가속도계(모바일/WebGL) 입력을 제공합니다.
/// IPlayerInput을 구현하여 PlayerController의 inputProvider 슬롯에 연결해 사용합니다.
/// </summary>
public class HybridInputProvider : MonoBehaviour, IPlayerInput
{
    [Tooltip("입력값에 곱하는 민감도. 결과는 -1~1로 Clamp됩니다.")]
    [SerializeField] [Min(0.1f)] private float tiltSensitivity = 2f;

    private void Start()
    {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_WEBGL
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
#endif
    }

    public float MoveInput
    {
        get
        {
            float raw = 0f;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Keyboard.current != null)
            {
                bool left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
                bool right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
                if (left && !right) raw = -1f;
                else if (right && !left) raw = 1f;
            }
#elif UNITY_ANDROID || UNITY_IPHONE || UNITY_WEBGL
            if (Accelerometer.current != null)
                raw = Accelerometer.current.acceleration.ReadValue().x;
#else
            if (Keyboard.current != null)
            {
                bool left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
                bool right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
                if (left && !right) raw = -1f;
                else if (right && !left) raw = 1f;
            }
#endif

            return Mathf.Clamp(raw * tiltSensitivity, -1f, 1f);
        }
    }
}
