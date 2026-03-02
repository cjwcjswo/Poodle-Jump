using UnityEngine;

/// <summary>
/// 원기둥(Cylinder) 표면을 따라 이동하는 수학적 로직만 담당합니다.
/// 위치/회전 적용은 PlayerController에서 수행합니다.
/// </summary>
public class CylinderMovement : MonoBehaviour
{
    [SerializeField] [Min(0.1f)] private float radius = 5f;

    /// <summary>원기둥 반지름. Inspector에서 설정 가능.</summary>
    public float Radius => radius;

    /// <summary>
    /// 주어진 각도(라디안)와 높이로 원기둥 표면 위의 월드 위치를 계산합니다.
    /// </summary>
    /// <param name="angleRad">현재 각도 (라디안)</param>
    /// <param name="heightY">Y축 높이 (월드 좌표)</param>
    /// <returns>원기둥 표면 위의 3D 위치 (중심은 XZ 평면 (0,0) 가정)</returns>
    public Vector3 GetPositionOnCylinder(float angleRad, float heightY)
    {
        float x = radius * Mathf.Cos(angleRad);
        float z = radius * Mathf.Sin(angleRad);
        return new Vector3(x, heightY, z);
    }

    /// <summary>
    /// 각도에 델타를 더한 새 각도를 반환합니다. (범위 제한 없음, 호출측에서 필요 시 래핑)
    /// </summary>
    public float AddAngleDelta(float currentAngleRad, float angleDelta)
    {
        return currentAngleRad + angleDelta;
    }

    /// <summary>
    /// 원기둥의 법선(바깥쪽) 방향을 향하는 회전을 반환합니다.
    /// 캐릭터가 원기둥 바깥을 보도록 할 때 사용합니다.
    /// </summary>
    /// <param name="angleRad">현재 각도 (라디안)</param>
    /// <returns>바깥 방향을 향하는 Quaternion (Up = Vector3.up)</returns>
    public Quaternion GetRotationTowardOutward(float angleRad)
    {
        Vector3 outward = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
        if (outward.sqrMagnitude < 0.001f)
            return Quaternion.identity;
        return Quaternion.LookRotation(outward, Vector3.up);
    }
}
