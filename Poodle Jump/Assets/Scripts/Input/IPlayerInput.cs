/// <summary>
/// 플레이어 좌우 이동 입력을 제공하는 인터페이스.
/// DIP에 따라 PlayerController는 이 인터페이스에만 의존합니다.
/// </summary>
public interface IPlayerInput
{
    /// <summary>
    /// 좌우 이동 입력값. -1 ~ 1 (왼쪽 음수, 오른쪽 양수).
    /// 원기둥 회전 각도 변화량 계산에 사용됩니다.
    /// </summary>
    float MoveInput { get; }
}
