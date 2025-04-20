using UnityEngine;

/// <summary>
/// 타구 정보를 저장하는 클래스
/// </summary>
[System.Serializable]
public class HitInfo
{
    // 타구 속도 (km/h)
    public float ExitVelocity;

    // 타구 각도 (도) - 수평 기준 (위쪽+, 아래쪽-)
    public float LaunchAngle;

    // 예상 비거리 (m)
    public float EstimatedDistance;

    // 타구 방향 벡터 (정규화된 Vector3)
    public Vector3 Direction;

    // 타구 시간
    public float HitTime;

    // 스핀 관련 정보 추가
    public float SpinRate; // 회전 속도 (rad/s)
    public Vector3 SpinAxis; // 회전축 (정규화된 Vector3)

    public string QualityText;
    public float QualityFactor;

    // 스핀 타입 열거형
    public enum SpinType
    {
        Backspin,   // 역회전 (타구가 위로 뜸)
        Topspin,    // 순회전 (타구가 아래로 내려감)
        Sidespin,   // 옆 회전 (타구가 좌우로 휨)
        Gyro        // 자이로 (여러 축 혼합)
    }

    // 계산된 스핀 타입
    public SpinType CalculatedSpinType
    {
        get
        {
            if (SpinAxis.magnitude < 0.001f) return SpinType.Gyro;

            // 수직 성분의 비율로 역회전/순회전 판단
            float verticalComponent = Mathf.Abs(SpinAxis.y);
            float horizontalComponent = Mathf.Sqrt(SpinAxis.x * SpinAxis.x + SpinAxis.z * SpinAxis.z);

            if (verticalComponent > 0.7f)
            {
                // 위쪽을 향하는 축이면 역회전, 아래쪽이면 순회전
                return SpinAxis.y > 0 ? SpinType.Backspin : SpinType.Topspin;
            }
            else if (horizontalComponent > 0.7f)
            {
                return SpinType.Sidespin;
            }
            else
            {
                return SpinType.Gyro;
            }
        }
    }

    // 스핀 타입을 텍스트로 반환
    public string SpinTypeText
    {
        get
        {
            switch (CalculatedSpinType)
            {
                case SpinType.Backspin: return "역회전";
                case SpinType.Topspin: return "순회전";
                case SpinType.Sidespin: return "옆회전";
                case SpinType.Gyro: return "자이로";
                default: return "일반";
            }
        }
    }

    public HitInfo()
    {
        HitTime = Time.time;
        SpinRate = 0f;
        SpinAxis = Vector3.up;
    }

    public override string ToString()
    {
        return $"속도: {ExitVelocity:F1} km/h\n각도: {LaunchAngle:F1}°\n거리: {EstimatedDistance:F1} m\n회전: {SpinTypeText} ({SpinRate:F0} rad/s)";
    }
}