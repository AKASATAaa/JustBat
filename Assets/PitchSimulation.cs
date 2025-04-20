using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현실적인 야구 피칭 시뮬레이션 구현 클래스
/// - 속도, 회전축, RPM을 기반으로 물리적으로 정확한 공의 움직임 구현
/// - 마그누스 효과를 적용하여 회전에 따른 궤적 변화 계산
/// </summary>
public class PitchSimulation : MonoBehaviour
{
    [Header("필수 컴포넌트")]
    [Tooltip("투수가 공을 릴리스하는 지점")]
    public Transform releasePoint;

    [Tooltip("투구 목표 지점")]
    public Transform targetPoint;

    [Tooltip("공 오브젝트")]
    public GameObject ballPrefab;

    [Header("투구 설정")]
    [Tooltip("구속 (km/h)")]
    [Range(70, 170)]
    public float velocity = 145.0f;

    [Tooltip("회전수 (RPM)")]
    [Range(1000, 3000)]
    public float spinRate = 2500.0f;

    [Tooltip("회전축 (x,y,z)")]
    public Vector3 spinAxis = new Vector3(1, 0, 0); // 기본값: 패스트볼(x축 회전)

    [Tooltip("랜덤 오차 사용 여부")]
    public bool useRandomError = false;

    [Tooltip("커브볼 추가 발사각(도)")]
    [Range(0f, 15f)]
    public float curveballExtraAngle = 8f;

    [Header("물리 설정")]
    [Tooltip("공기 저항 계수")]
    public float dragCoefficient = 0.3f;

    [Tooltip("기본 마그누스 효과 강도")]
    public float baseMagnusCoefficient = 2.0f;

    [Tooltip("구종별 마그누스 계수 조정 사용")]
    public bool usePitchSpecificMagnus = true;

    [Header("프리셋 설정")]
    [Tooltip("투구 프리셋 목록")]
    public List<PitchPreset> presets = new List<PitchPreset>();

    // 내부 변수
    private List<GameObject> activeBalls = new List<GameObject>();
    private List<Rigidbody> activeRigidbodies = new List<Rigidbody>();
    private Vector3 initialVelocity;
    private bool canPitch = true;
    public float currentMagnusMultiplier = 1.0f;

    // 시뮬레이션 정밀도 설정
    private const float FIXED_TIME_STEP = 0.01f; // 물리 시뮬레이션 타임스텝

    // 실제 공의 특성 (야구공 기준)
    private const float BALL_MASS = 0.145f; // kg
    private const float BALL_RADIUS = 0.037f; // m
    private const float AIR_DENSITY = 1.225f; // kg/m³ (표준 대기압 기준)
    private readonly Vector3 GRAVITY = new Vector3(0, -9.8f, 0); // 중력 가속도 고정 (m/s²)

    void Start()
    {
        // 시작 시 회전축 정규화
        spinAxis.Normalize();

        // 컴포넌트 확인
        if (releasePoint == null || targetPoint == null || ballPrefab == null)
        {
            Debug.LogError("PitchSimulation: 필수 컴포넌트가 할당되지 않았습니다. releasePoint, targetPoint, ballPrefab을 설정해주세요.");
            enabled = false;
            return;
        }

        // 프리셋이 없으면 기본 프리셋 초기화
        if (presets.Count == 0)
        {
            InitializeDefaultPresets();
        }
    }

    void Update()
    {
        // 스페이스바 입력 시 새로운 공 생성 및 투구 시작
        if (Input.GetKeyDown(KeyCode.Space) && canPitch)
        {
            CreateBallAndPitch();
        }

        // ESC 키로 모든 공 제거
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearAllBalls();
        }

        // 숫자 키로 프리셋 선택 (1-8키)
        for (int i = 0; i < 8; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                ApplyPreset(i);
            }
        }
    }

    void FixedUpdate()
    {
        // 활성화된 모든 공에 공기역학 적용
        for (int i = activeRigidbodies.Count - 1; i >= 0; i--)
        {
            if (activeRigidbodies[i] != null)
            {
                ApplyAerodynamics(activeRigidbodies[i]);
            }
            else
            {
                activeRigidbodies.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 새 공을 생성하고 투구 시작
    /// </summary>
    public void CreateBallAndPitch()
    {
        // 새 공 생성
        GameObject newBall = Instantiate(ballPrefab, releasePoint.position, Quaternion.identity);

        // 공에 Rigidbody 추가 및 설정
        Rigidbody rb = newBall.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = newBall.AddComponent<Rigidbody>();
        }

        // 물리 설정
        rb.mass = BALL_MASS;
        rb.drag = 0; // 공기저항은 수동으로 계산
        rb.angularDrag = 0; // 회전저항도 수동으로 계산
        rb.useGravity = false; // 중력도 수동으로 계산
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 초기 속도와 회전 적용
        ApplyInitialVelocityAndSpin(rb);

        // 리스트에 추가
        activeBalls.Add(newBall);
        activeRigidbodies.Add(rb);

        // 3초 후 파괴 예약
        StartCoroutine(DestroyBallAfterDelay(newBall, 3.0f));

        // 투구 정보 출력
        float currentRPM = (rb.angularVelocity.magnitude * 60f) / (2f * Mathf.PI);
        Debug.Log($"투구 시작: 속도={velocity}km/h, 회전수={currentRPM:F0}RPM, 회전축={spinAxis.normalized}");
    }

    /// <summary>
    /// 공에 초기 속도와 회전을 적용
    /// </summary>
    private void ApplyInitialVelocityAndSpin(Rigidbody rb)
    {
        // 초기 속도 계산 (km/h에서 m/s로 변환)
        float velocityInMS = velocity * 1000f / 3600f;

        // 목표 방향으로 초기 벡터 설정
        Vector3 directionToTarget = (targetPoint.position - releasePoint.position).normalized;

        // 중력을 보정하여 더 직선적인 초기 경로 계산 (약간 위쪽으로 투구)
        // 중력 보정 요소
        float distanceToTarget = Vector3.Distance(releasePoint.position, targetPoint.position);
        float flightTime = distanceToTarget / velocityInMS; // 예상 비행 시간

        // 중력을 보정하기 위한 각도 계산
        float gravityCompensation = 0.5f * 9.8f * flightTime * flightTime;
        float upwardAngle = Mathf.Atan2(gravityCompensation, distanceToTarget);

        // 커브볼(x축이 -1)일 경우 발사각 추가
        if (spinAxis.normalized.x < -0.8f)
        {
            // 커브볼의 경우 추가 발사각 (인스펙터에서 설정 가능)
            upwardAngle += Mathf.Deg2Rad * curveballExtraAngle;
            Debug.Log($"커브볼 감지: 발사각 {curveballExtraAngle}도 증가 적용");
        }

        // 원래 방향에서 약간 위쪽으로 조정
        Vector3 adjustedDirection = Quaternion.AngleAxis(upwardAngle * Mathf.Rad2Deg, Vector3.Cross(directionToTarget, Vector3.up).normalized) * directionToTarget;

        // 최종 초기 속도 벡터 계산
        initialVelocity = adjustedDirection.normalized * velocityInMS;

        if (useRandomError)
        {
            // 랜덤한 작은 오차 추가 (매우 작게 설정)
            Vector3 randomError = new Vector3(
                Random.Range(-0.005f, 0.005f),
                Random.Range(-0.005f, 0.005f),
                Random.Range(-0.001f, 0.001f)
            );

            // 공에 초기 속도 적용 (랜덤 오차 포함)
            rb.velocity = initialVelocity + randomError * velocityInMS;

            // 스핀 적용 (회전축과 RPM 기반)
            // 각속도(rad/s) = RPM * 2π / 60
            Vector3 angularVelocity = spinAxis.normalized * (spinRate * 2 * Mathf.PI / 60f);

            // 랜덤한 작은 회전 오차 추가 (매우 작게 설정)
            Vector3 randomSpinError = new Vector3(
                Random.Range(-0.005f, 0.005f),
                Random.Range(-0.005f, 0.005f),
                Random.Range(-0.005f, 0.005f)
            );

            rb.angularVelocity = angularVelocity + randomSpinError * angularVelocity.magnitude;
        }
        else
        {
            // 오차 없이 정확한 방향으로 투구
            rb.velocity = initialVelocity;

            // 회전 적용 (회전축과 RPM 기반)
            Vector3 angularVelocity = spinAxis.normalized * (spinRate * 2 * Mathf.PI / 60f);
            rb.angularVelocity = angularVelocity;
        }
    }

    /// <summary>
    /// 공기역학 효과(항력과 마그누스 효과) 적용
    /// </summary>
    private void ApplyAerodynamics(Rigidbody rb)
    {
        if (rb == null) return;

        Vector3 velocity = rb.velocity;
        float speed = velocity.magnitude;

        // 1. 항력(공기저항) 계산
        Vector3 dragForce = Vector3.zero;
        if (speed > 0)
        {
            // 항력 공식: 0.5 * ρ * v² * A * Cd
            // ρ: 공기밀도, v: 속도, A: 단면적, Cd: 항력계수
            float crossSectionalArea = Mathf.PI * BALL_RADIUS * BALL_RADIUS;
            float dragMagnitude = 0.5f * AIR_DENSITY * speed * speed * crossSectionalArea * dragCoefficient;
            dragForce = -velocity.normalized * dragMagnitude;
        }

        // 2. 마그누스 효과 계산 (회전에 의한 양력)
        Vector3 magnusForce = Vector3.zero;
        if (speed > 0)
        {
            // 마그누스 힘 = Cm * ρ * R³ * ω × v
            // Cm: 마그누스 계수, ρ: 공기밀도, R: 반지름, ω: 각속도 벡터, v: 속도 벡터
            Vector3 spinAngularVelocity = rb.angularVelocity;
            float spinSpeed = spinAngularVelocity.magnitude;

            if (spinSpeed > 0)
            {
                // 회전축과 속도 벡터의 외적으로 마그누스 방향 계산
                Vector3 magnusDirection = Vector3.Cross(spinAngularVelocity, velocity).normalized;

                // 마그누스 효과 강도 계산 - 개선된 공식
                // 실제 야구에서는 회전수가 높을수록 더 큰 영향을 미침
                // RPM을 고려한 스케일링 적용
                float rpmScaleFactor = spinSpeed * 60f / (2f * Mathf.PI) / 1000f; // RPM을 1000으로 나눠 정규화
                float velocityFactor = speed / 30f; // 속도 요소 (일반적인 투구 속도 기준 정규화)

                // 스핀 효율성 계수 - 회전이 효과적으로 마그누스 힘으로 변환되는 정도
                float spinEfficiency = 2.0f;

                float magnusMagnitude = 0;

                if (usePitchSpecificMagnus)
                {
                    // 구종별 마그누스 효과 적용
                    magnusMagnitude = baseMagnusCoefficient * currentMagnusMultiplier * AIR_DENSITY * Mathf.Pow(BALL_RADIUS, 3) *
                                       spinSpeed * speed * (1f + rpmScaleFactor * velocityFactor) * spinEfficiency;
                }
                else
                {
                    // 모든 구종에 동일한 마그누스 효과 적용
                    magnusMagnitude = baseMagnusCoefficient * AIR_DENSITY * Mathf.Pow(BALL_RADIUS, 3) *
                                       spinSpeed * speed * (1f + rpmScaleFactor * velocityFactor) * spinEfficiency;
                }

                magnusForce = magnusDirection * magnusMagnitude;
            }
        }

        // 3. 중력 설정 (고정된 중력 값 사용)
        Vector3 gravityForce = GRAVITY * BALL_MASS;

        // 4. 모든 힘 적용
        Vector3 totalForce = dragForce + magnusForce + gravityForce;
        rb.AddForce(totalForce);
    }

    /// <summary>
    /// 모든 활성화된 공 제거
    /// </summary>
    public void ClearAllBalls()
    {
        // 현재 활성화된 모든 공 파괴
        foreach (GameObject ball in activeBalls)
        {
            if (ball != null)
            {
                Destroy(ball);
            }
        }

        // 리스트 초기화
        activeBalls.Clear();
        activeRigidbodies.Clear();

        Debug.Log("모든 공이 제거되었습니다.");
    }

    /// <summary>
    /// 지정된 시간 후 공을 파괴하는 코루틴
    /// </summary>
    private IEnumerator DestroyBallAfterDelay(GameObject ball, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ball != null)
        {
            // 활성 리스트에서 제거
            int index = activeBalls.IndexOf(ball);
            if (index >= 0)
            {
                activeBalls.RemoveAt(index);
                if (index < activeRigidbodies.Count)
                {
                    activeRigidbodies.RemoveAt(index);
                }
            }

            // 파괴
            Destroy(ball);
        }
    }

    /// <summary>
    /// 투구 유형 설정 메서드
    /// </summary>
    public void SetPitchType(float newVelocity, float newSpinRate, Vector3 newSpinAxis, float magnusMultiplier = 1.0f)
    {
        velocity = newVelocity;
        spinRate = newSpinRate;
        spinAxis = newSpinAxis.normalized;
        currentMagnusMultiplier = magnusMultiplier;
    }

    /// <summary>
    /// 특정 프리셋 적용
    /// </summary>
    public void ApplyPreset(int index)
    {
        if (index >= 0 && index < presets.Count)
        {
            PitchPreset preset = presets[index];
            velocity = preset.velocity;
            spinRate = preset.spinRate;
            spinAxis = preset.spinAxis;
            currentMagnusMultiplier = preset.magnusMultiplier;

            Debug.Log($"프리셋 적용: {preset.name} (구속: {preset.velocity}km/h, 회전수: {preset.spinRate}RPM, 마그누스 배율: {preset.magnusMultiplier})");
        }
        else
        {
            Debug.LogWarning($"프리셋 인덱스 {index}가 유효하지 않습니다. 프리셋 수: {presets.Count}");
        }
    }

    /// <summary>
    /// 기본 투구 프리셋 초기화
    /// </summary>
    private void InitializeDefaultPresets()
    {
        // 패스트볼 - x축 방향 회전 (백스핀) - 마그누스 효과 감소
        presets.Add(new PitchPreset("4-Seam Fastball", 150, 2800, new Vector3(1, 0, 0), 0.5f));

        // 커브볼 - 탑스핀 - 마그누스 효과 증가
        presets.Add(new PitchPreset("Curveball", 125, 3200, new Vector3(-1, 0, 0), 3.5f));

        // 슬라이더 - 측면 회전 (오른손 투수 기준) - 기본 마그누스
        presets.Add(new PitchPreset("Slider", 140, 3000, new Vector3(0.3f, 0, 1), 2.0f));

        // 체인지업 - 느린 속도의 패스트볼과 유사한 회전 - 기본 마그누스
        presets.Add(new PitchPreset("Changeup", 130, 2200, new Vector3(0.8f, 0, 0), 2.0f));

        // 싱커 - 하단 방향 회전 - 기본 마그누스
        presets.Add(new PitchPreset("Sinker", 145, 2400, new Vector3(0.7f, -0.4f, 0.3f), 2.0f));

        // 컷패스트볼 - 약간의 측면 회전을 가진 빠른 공 - 기본 마그누스
        presets.Add(new PitchPreset("Cutter", 148, 2800, new Vector3(1, 0, -0.3f), 2.0f));

        // 느린 커브 - 매우 느린 속도와 큰 회전 - 마그누스 효과 증가
        presets.Add(new PitchPreset("Slow Curve", 75, 2800, new Vector3(-1, 0.1f, 0.1f), 3.5f));

        // 너클볼 - 매우 낮은 회전수 - 기본 마그누스
        presets.Add(new PitchPreset("Knuckleball", 85, 1200, new Vector3(0.3f, 0.3f, 0.3f), 2.0f));

        Debug.Log($"기본 프리셋 {presets.Count}개가 초기화되었습니다.");
    }

    /// <summary>
    /// 디버그 정보 표시
    /// </summary>
    void OnGUI()
    {
        // 활성화된 공의 수 표시
        GUI.Label(new Rect(10, 10, 300, 20), $"활성화된 공: {activeBalls.Count}개");

        // 각 공의 정보 표시 (최대 3개만)
        int displayCount = Mathf.Min(activeBalls.Count, 3);
        for (int i = 0; i < displayCount; i++)
        {
            if (i < activeRigidbodies.Count && activeRigidbodies[i] != null)
            {
                float speed = activeRigidbodies[i].velocity.magnitude * 3.6f;
                float rpm = (activeRigidbodies[i].angularVelocity.magnitude * 60f) / (2f * Mathf.PI);

                GUI.Label(new Rect(10, 35 + i * 40, 300, 20), $"공 #{i + 1} 속도: {speed:F1} km/h");
                GUI.Label(new Rect(10, 55 + i * 40, 300, 20), $"공 #{i + 1} 회전수: {rpm:F0} RPM");
            }
        }

        // 현재 투구 설정 표시
        GUI.Label(new Rect(10, Screen.height - 120, 400, 20), $"현재 설정: 속도={velocity}km/h, 회전수={spinRate}RPM");
        GUI.Label(new Rect(10, Screen.height - 100, 400, 20), $"회전축={spinAxis.normalized}, 마그누스 배율={currentMagnusMultiplier:F1}");

        // 조작법 안내
        GUI.Box(new Rect(10, Screen.height - 80, 400, 80), "조작 방법");
        GUI.Label(new Rect(20, Screen.height - 60, 390, 20), "스페이스바: 새 공 투구 / ESC: 모든 공 제거");
        GUI.Label(new Rect(20, Screen.height - 40, 390, 20), "숫자 키 1-8: 투구 타입 선택");
        GUI.Label(new Rect(20, Screen.height - 20, 390, 20), "예) 1:패스트볼, 2:커브볼, 3:슬라이더, 4:체인지업 등");

        // 프리셋 버튼 표시
        GUI.Box(new Rect(Screen.width - 210, 10, 200, 40 + presets.Count * 25), "투구 타입 선택 (1-8키)");

        for (int i = 0; i < presets.Count; i++)
        {
            if (i < 8) // 최대 8개 프리셋만 표시
            {
                string buttonText = $"{i + 1}: {presets[i].name}";
                if (GUI.Button(new Rect(Screen.width - 200, 45 + i * 25, 180, 20), buttonText))
                {
                    ApplyPreset(i);
                }
            }
        }
    }

    /// <summary>
    /// 사용자 인터페이스를 위한 디버그 시각화
    /// </summary>
    void OnDrawGizmos()
    {
        if (releasePoint != null && targetPoint != null)
        {
            // 투구 방향 시각화
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(releasePoint.position, targetPoint.position);

            // 타겟 위치 시각화
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPoint.position, 0.1f);

            // 릴리스 포인트 시각화
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(releasePoint.position, 0.05f);
        }
    }
}

/// <summary>
/// 투구 유형 프리셋을 저장하는 클래스
/// </summary>
[System.Serializable]
public class PitchPreset
{
    public string name;
    public float velocity;
    public float spinRate;
    public Vector3 spinAxis;
    public float magnusMultiplier = 1.0f; // 구종별 마그누스 효과 배율

    public PitchPreset(string name, float velocity, float spinRate, Vector3 spinAxis, float magnusMultiplier = 1.0f)
    {
        this.name = name;
        this.velocity = velocity;
        this.spinRate = spinRate;
        this.spinAxis = spinAxis.normalized;
        this.magnusMultiplier = magnusMultiplier;
    }
}