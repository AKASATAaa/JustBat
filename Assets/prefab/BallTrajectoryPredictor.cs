using System.Collections.Generic;
using UnityEngine;

public class BallTrajectoryPredictor : MonoBehaviour
{
    [Header("궤적 예측 설정")]
    [Tooltip("궤적을 그릴지 여부")]
    public bool showTrajectory = true;

    [Tooltip("궤적 시뮬레이션 최대 시간 (초)")]
    [Range(1f, 10f)]
    public float maxSimulationTime = 5f;

    [Tooltip("시뮬레이션 시간 간격 (초) - 작을수록 정확하지만 성능 저하")]
    [Range(0.01f, 0.1f)]
    public float timeStep = 0.02f;

    [Tooltip("궤적 점들의 개수")]
    [Range(50, 500)]
    public int trajectoryPoints = 200;

    [Header("시각화 설정")]
    [Tooltip("궤적 라인 색상")]
    public Color trajectoryColor = Color.yellow;

    [Tooltip("궤적 라인 두께")]
    [Range(0.01f, 0.2f)]
    public float lineWidth = 0.05f;

    [Tooltip("궤적을 실시간으로 업데이트할지 여부")]
    public bool realTimeUpdate = true;

    [Header("물리 파라미터 (BallController와 동일하게 설정)")]
    [Tooltip("공기 저항 상수")]
    public float airResistanceConstant = 0.02f;

    [Tooltip("마그누스 효과 상수")]
    public float magnusEffectConstant = 0.007f;

    [Tooltip("최대 수직 속도")]
    public float maxVerticalVelocity = 20f;

    [Tooltip("공의 질량")]
    public float ballMass = 0.145f; // 야구공 표준 무게

    [Header("디버그")]
    [Tooltip("디버그 로그 출력")]
    public bool debugMode = false;

    // 내부 변수들
    private LineRenderer trajectoryLine;
    private List<Vector3> trajectoryPoints_List = new List<Vector3>();
    private BallController ballController;
    private Camera mainCamera;

    // 시뮬레이션 상태
    private Vector3 currentLaunchDirection;
    private TrajectoryBallSettings currentBallSettings;
    private bool trajectoryCalculated = false;

    void Start()
    {
        InitializeTrajectoryLine();
        ballController = GetComponent<BallController>();
        mainCamera = Camera.main;

        if (debugMode)
        {
            Debug.Log("BallTrajectoryPredictor 초기화 완료");
        }
    }

    void Update()
    {
        if (showTrajectory && realTimeUpdate)
        {
            UpdateTrajectoryPreview();
        }
    }

    /// <summary>
    /// 궤적 라인 렌더러를 초기화합니다
    /// </summary>
    private void InitializeTrajectoryLine()
    {
        // LineRenderer 컴포넌트 가져오기 또는 추가
        trajectoryLine = GetComponent<LineRenderer>();
        if (trajectoryLine == null)
        {
            trajectoryLine = gameObject.AddComponent<LineRenderer>();
        }

        // LineRenderer 설정
        trajectoryLine.material = CreateTrajectoryMaterial();

        // Unity 버전에 따른 색상 설정
        if (trajectoryLine.colorGradient != null)
        {
            // Unity 2017 이후 버전: Gradient 사용
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(trajectoryColor, 0.0f), new GradientColorKey(trajectoryColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(trajectoryColor.a, 0.0f), new GradientAlphaKey(trajectoryColor.a, 1.0f) }
            );
            trajectoryLine.colorGradient = gradient;
        }
        else
        {
            // 이전 버전: startColor, endColor 사용
            trajectoryLine.startColor = trajectoryColor;
            trajectoryLine.endColor = trajectoryColor;
        }

        trajectoryLine.startWidth = lineWidth;
        trajectoryLine.endWidth = lineWidth;
        trajectoryLine.positionCount = 0;
        trajectoryLine.useWorldSpace = true;
        trajectoryLine.sortingOrder = 10; // 다른 오브젝트보다 앞에 표시
    }

    /// <summary>
    /// 궤적용 머티리얼을 생성합니다
    /// </summary>
    private Material CreateTrajectoryMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = trajectoryColor;
        return mat;
    }

    /// <summary>
    /// 공의 설정값을 기반으로 궤적을 계산합니다
    /// </summary>
    /// <param name="launchDirection">발사 방향</param>
    /// <param name="ballSettings">공 설정값</param>
    public void CalculateTrajectory(Vector3 launchDirection, TrajectoryBallSettings ballSettings)
    {
        if (!showTrajectory) return;

        currentLaunchDirection = launchDirection;
        currentBallSettings = ballSettings;

        // 궤적 시뮬레이션 수행
        SimulateTrajectory();

        // 라인 렌더러에 궤적 적용
        UpdateLineRenderer();

        trajectoryCalculated = true;

        if (debugMode)
        {
            Debug.Log($"궤적 계산 완료: {trajectoryPoints_List.Count}개 점");
        }
    }

    /// <summary>
    /// BallController의 설정값을 사용하여 궤적을 계산합니다
    /// </summary>
    /// <param name="launchDirection">발사 방향</param>
    /// <param name="ballController">BallController 참조</param>
    public void CalculateTrajectoryFromBallController(Vector3 launchDirection, BallController ballController)
    {
        if (!showTrajectory || ballController == null) return;

        // BallController의 설정을 TrajectoryBallSettings로 변환
        TrajectoryBallSettings settings = new TrajectoryBallSettings
        {
            throwPower = ballController.initialForce,
            vtorqueSpeed = ballController.verticalRotationSpeed,
            ztorqueSpeed = ballController.horizontalRotationSpeed,
            voffset = ballController.verticalAngleOffset,
            hoffset = ballController.horizontalAngleOffset,
            ballAngleZ = ballController.initialRotationAngleZ,
            ballAngleX = ballController.initialRotationAngleX,
            brakingStartTime = ballController.brakingDelay
        };

        CalculateTrajectory(launchDirection, settings);
    }

    /// <summary>
    /// 실시간으로 궤적 미리보기를 업데이트합니다
    /// </summary>
    private void UpdateTrajectoryPreview()
    {
        if (ballController == null) return;

        // BallController가 아직 발사되지 않은 경우에만 미리보기 표시
        if (!ballController.IsBallLaunched() && trajectoryCalculated)
        {
            trajectoryLine.enabled = true;
        }
        else
        {
            trajectoryLine.enabled = false;
        }
    }

    /// <summary>
    /// 물리 시뮬레이션을 통해 궤적을 계산합니다
    /// </summary>
    private void SimulateTrajectory()
    {
        trajectoryPoints_List.Clear();

        // 시뮬레이션 초기 설정
        Vector3 position = transform.position;
        Vector3 velocity = CalculateInitialVelocity();
        Vector3 angularVelocity = CalculateInitialAngularVelocity();

        float currentTime = 0f;
        bool brakingApplied = false;

        // 시뮬레이션 루프
        while (currentTime < maxSimulationTime && position.y >= 0f)
        {
            // 현재 위치를 궤적에 추가
            trajectoryPoints_List.Add(position);

            // 감속(토크) 적용 시점 확인
            if (!brakingApplied && currentTime >= currentBallSettings.brakingStartTime)
            {
                angularVelocity = ApplyBrakingTorque(angularVelocity);
                brakingApplied = true;
            }

            // 물리력 계산
            Vector3 airResistance = CalculateAirResistance(velocity);
            Vector3 magnusForce = CalculateMagnusForce(velocity, angularVelocity);
            Vector3 gravity = Physics.gravity;

            // 총 가속도 계산 (F = ma에서 a = F/m)
            Vector3 totalForce = airResistance + magnusForce + (gravity * ballMass);
            Vector3 acceleration = totalForce / ballMass;

            // 수치 적분으로 속도와 위치 업데이트
            velocity += acceleration * timeStep;

            // 수직 속도 제한
            if (velocity.y > maxVerticalVelocity)
            {
                velocity.y = maxVerticalVelocity;
            }

            position += velocity * timeStep;

            currentTime += timeStep;

            // 너무 많은 점이 생성되는 것 방지
            if (trajectoryPoints_List.Count >= trajectoryPoints)
            {
                break;
            }
        }

        if (debugMode)
        {
            Debug.Log($"시뮬레이션 완료: 시간={currentTime:F2}s, 최종위치={position}, 점개수={trajectoryPoints_List.Count}");
        }
    }

    /// <summary>
    /// 초기 속도를 계산합니다
    /// </summary>
    private Vector3 CalculateInitialVelocity()
    {
        // BallController와 동일한 계산 방식 사용
        Quaternion angleAdjustment = Quaternion.Euler(
            currentBallSettings.voffset, 0, currentBallSettings.hoffset);
        Vector3 adjustedDirection = angleAdjustment * currentLaunchDirection;

        float normalizedForce = 0.043306f * currentBallSettings.throwPower - 0.102461f;

        return adjustedDirection * normalizedForce;
    }

    /// <summary>
    /// 초기 각속도를 계산합니다
    /// </summary>
    private Vector3 CalculateInitialAngularVelocity()
    {
        // 초기에는 각속도가 0이고, 나중에 토크로 추가됨
        return Vector3.zero;
    }

    /// <summary>
    /// 감속 토크를 적용합니다
    /// </summary>
    private Vector3 ApplyBrakingTorque(Vector3 currentAngularVelocity)
    {
        Vector3 torque = new Vector3(
            currentBallSettings.vtorqueSpeed,
            0,
            currentBallSettings.ztorqueSpeed * 3 // horizontalRotationMultiplier
        );

        // 토크를 각가속도로 변환 (간단화: I = 1로 가정)
        Vector3 angularAcceleration = torque;

        return currentAngularVelocity + angularAcceleration * timeStep;
    }

    /// <summary>
    /// 공기 저항을 계산합니다
    /// </summary>
    private Vector3 CalculateAirResistance(Vector3 velocity)
    {
        float speed = velocity.magnitude;
        Vector3 direction = velocity.normalized;
        float resistanceMagnitude = airResistanceConstant * Mathf.Pow(speed, 2f);
        return resistanceMagnitude * -direction;
    }

    /// <summary>
    /// 마그누스 효과를 계산합니다
    /// </summary>
    private Vector3 CalculateMagnusForce(Vector3 velocity, Vector3 angularVelocity)
    {
        return magnusEffectConstant * Vector3.Cross(velocity, angularVelocity);
    }

    /// <summary>
    /// LineRenderer를 업데이트합니다
    /// </summary>
    private void UpdateLineRenderer()
    {
        if (trajectoryLine == null || trajectoryPoints_List.Count == 0)
            return;

        trajectoryLine.positionCount = trajectoryPoints_List.Count;
        trajectoryLine.SetPositions(trajectoryPoints_List.ToArray());

        // Unity 버전에 따른 색상 설정
        UpdateLineRendererColor();

        trajectoryLine.startWidth = lineWidth;
        trajectoryLine.endWidth = lineWidth;
    }

    /// <summary>
    /// LineRenderer의 색상을 업데이트합니다
    /// </summary>
    private void UpdateLineRendererColor()
    {
        if (trajectoryLine.colorGradient != null)
        {
            // Unity 2017 이후 버전: Gradient 사용
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(trajectoryColor, 0.0f), new GradientColorKey(trajectoryColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(trajectoryColor.a, 0.0f), new GradientAlphaKey(trajectoryColor.a, 1.0f) }
            );
            trajectoryLine.colorGradient = gradient;
        }
        else
        {
            // 이전 버전 또는 fallback: startColor, endColor 사용
            trajectoryLine.startColor = trajectoryColor;
            trajectoryLine.endColor = trajectoryColor;
        }
    }

    /// <summary>
    /// 궤적 표시를 켜거나 끕니다
    /// </summary>
    public void SetTrajectoryVisible(bool visible)
    {
        showTrajectory = visible;
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = visible && trajectoryCalculated;
        }
    }

    /// <summary>
    /// 궤적을 지웁니다
    /// </summary>
    public void ClearTrajectory()
    {
        trajectoryPoints_List.Clear();
        if (trajectoryLine != null)
        {
            trajectoryLine.positionCount = 0;
        }
        trajectoryCalculated = false;
    }

    /// <summary>
    /// 궤적 색상을 변경합니다
    /// </summary>
    public void SetTrajectoryColor(Color color)
    {
        trajectoryColor = color;
        if (trajectoryLine != null)
        {
            UpdateLineRendererColor();
            if (trajectoryLine.material != null)
            {
                trajectoryLine.material.color = color;
            }
        }
    }

    /// <summary>
    /// 예측된 착지 지점을 반환합니다
    /// </summary>
    public Vector3 GetPredictedLandingPoint()
    {
        if (trajectoryPoints_List.Count > 0)
        {
            return trajectoryPoints_List[trajectoryPoints_List.Count - 1];
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 예측된 최대 높이를 반환합니다
    /// </summary>
    public float GetPredictedMaxHeight()
    {
        float maxHeight = 0f;
        foreach (Vector3 point in trajectoryPoints_List)
        {
            if (point.y > maxHeight)
            {
                maxHeight = point.y;
            }
        }
        return maxHeight;
    }

    /// <summary>
    /// 예측된 비행 시간을 반환합니다
    /// </summary>
    public float GetPredictedFlightTime()
    {
        return trajectoryPoints_List.Count * timeStep;
    }

    // Unity 에디터에서 기즈모로 궤적 표시
    private void OnDrawGizmos()
    {
        if (!showTrajectory || trajectoryPoints_List.Count < 2)
            return;

        Gizmos.color = trajectoryColor;

        for (int i = 0; i < trajectoryPoints_List.Count - 1; i++)
        {
            Gizmos.DrawLine(trajectoryPoints_List[i], trajectoryPoints_List[i + 1]);
        }

        // 착지 예상 지점 표시
        if (trajectoryPoints_List.Count > 0)
        {
            Vector3 landingPoint = trajectoryPoints_List[trajectoryPoints_List.Count - 1];
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(landingPoint, 0.1f);
        }
    }
}

/// <summary>
/// TrajectoryBallSettings 클래스 - 궤적 예측용 공 설정
/// 기존 BallSettings와의 모호성을 피하기 위해 별도로 정의
/// </summary>
[System.Serializable]
public class TrajectoryBallSettings
{
    public float throwPower = 100f;
    public float vtorqueSpeed = 0f;
    public float ztorqueSpeed = 0f;
    public float voffset = 0f;
    public float hoffset = 0f;
    public float ballAngleZ = 0f;
    public float ballAngleX = 0f;
    public float brakingStartTime = 1f;
}