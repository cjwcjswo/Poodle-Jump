using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Unity New Input System을 사용해 모바일 터치 및 PC 키보드/게임패드 입력을 받아 IPlayerInput을 구현합니다.
/// </summary>
public class PlayerInputHandler : MonoBehaviour, IPlayerInput
{
    [SerializeField] private InputActionReference moveActionRef;
    [SerializeField] [Range(0.01f, 1f)] private float touchSensitivity = 0.2f;

    private InputAction _moveAction;
    private Vector2 _touchDelta;

    public float MoveInput { get; private set; }

    private void Awake()
    {
        if (moveActionRef != null)
            _moveAction = moveActionRef.action;
        else
            TryGetMoveActionFromPlayerInput();
    }

    private void TryGetMoveActionFromPlayerInput()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput?.actions != null)
            _moveAction = playerInput.actions.FindActionMap("Player")?.FindAction("Move");
    }

    private void OnEnable()
    {
        _moveAction?.Enable();
    }

    private void OnDisable()
    {
        _moveAction?.Disable();
    }

    private void Update()
    {
        float value = 0f;

        if (_moveAction != null)
            value = _moveAction.ReadValue<Vector2>().x;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 delta = Touchscreen.current.primaryTouch.delta.ReadValue();
            _touchDelta.x = delta.x * touchSensitivity;
        }
        else
        {
            _touchDelta.x = Mathf.MoveTowards(_touchDelta.x, 0f, Time.deltaTime * 5f);
        }

        if (Mathf.Abs(_touchDelta.x) > 0.01f)
            value = Mathf.Clamp(value + _touchDelta.x, -1f, 1f);

        MoveInput = Mathf.Clamp(value, -1f, 1f);
    }
}
