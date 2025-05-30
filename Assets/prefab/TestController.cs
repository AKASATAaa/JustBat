using System.Collections;
using UnityEngine;

public class TestController : MonoBehaviour
{
    // ===== 기존 BallController의 모든 기능 유지 =====
    private Rigidbody ballRigidbody;

    [Header("구종정보")]
    public string pitchTypeName = "Fastball";

    [Header("방향 설정")]
    private Vector3 launchDirection;

    [Header("공 물리 설정")]
    [Tooltip("공의 속도 (km/h)")]
    public float initialForce;                     // 공의 속도 (km/h)
    public float verticalRotationSpeed;
    public float horizontalRotationSpeed;

    [Header("각도 설정")]
    [Range(-45f, 45f)] public float verticalAngleOffset;
    [Range(-45f, 45f)] public float horizontalAngleOffset;
    [Range(0f, 360f)] public float initialRotationAngleZ;
    public float initialRotationAngleX;

    [Header("타이밍 설정")]
    [Range(0f, 5f)] public float brakingDelay;

    // ===== 개선된 물리 시스템 추가 =====
    [Header("🔬 정확한 물리 설정")]
    [SerializeField] public bool useAccuratePhysics = true;
    [SerializeField] public float ballMass = 0.145f;              // kg
    [SerializeField] public float ballRadius = 0.0366f;          // m
    [SerializeField] public float airDensity = 1.225f;           // kg/m³
    [SerializeField] public float temperature = 21f;             // °C

    [Header("🌪️ 개선된 물리 효과")]
    [SerializeField] public float accurateDragMultiplier = 1.0f;    // 드래그 강도 조절
    [SerializeField] public float accurateMagnusMultiplier = 1.0f;  // 마그누스 강도 조절

    // 기존 물리 상수들 (호환성을 위해 유지)
    [Header("기존 물리 효과 (레거시)")]
    public float airResistanceConstant = 0.02f;
    public float magnusEffectConstant = 0.007f;
    public float horizontalRotationMultiplier = 3;

    // 드래그 계수 곡선 (개선된 물리용)
    private AnimationCurve dragCoefficientCurve;

    // ===== 기존 모든 변수들 유지 =====
    [Header("상태")]
    public bool hasCollided = false;
    private ABSScoreDetector scoreDetector;
    private bool isBallLaunched = false;

    [Header("오디오 설정")]
    public AudioSource whooshClip;
    [Header("볼륨 설정")]
    public float minVolume = 0.5f;
    public float maxVolume = 1.5f;
    public float volumeCurve = 2.0f;
    [Header("던지기 파워 설정")]
    [Range(70f, 170f)] public float minThrowPower = 70f;
    public float maxThrowPower = 170f;

    [Header("회전축 기즈모 설정")]
    public bool showRotationAxisGizmo = true;
    public float gizmoLength = 2.0f;
    public Color rotationAxisColor = Color.red;

    [Header("충돌 시 물리 처리")]
    [SerializeField] private float collisionDampingFactor = 0.5f;
    [SerializeField] private float minBallVelocityAfterHit = 2.0f;
    [SerializeField] private float gravityMultiplierAfterHit = 1.5f;
    [SerializeField] private float maxVerticalVelocity = 20f;

    [Header("홈런 판정")]
    public bool isHomeRun = false;
    public bool isHomeRunCounted = false;

    // 기존 모든 private 변수들
    private float lastCollisionTime = 0f;
    private float collisionCooldown = 0.1f;
    private float startTime;
    private const float TARGET_DISTANCE = 18.44f;

    [Header("Distance Calculation")]
    public bool hasLanded = false;
    public float actualDistance = 0f;
    public Vector3 launchPosition;
    public bool isCalculatingDistance = false;
    private Vector3 lastPosition;
    private float groundY = 0.05f;
    public float rotationSpeed = 50000;
    private bool isGround = false;

    [Tooltip("디버그 로그 출력 여부")]
    public bool debugMode = false;

    void Start()
    {
        InitializeImprovedPhysics();
    }

    /// <summary>
    /// 개선된 물리 시스템 초기화
    /// </summary>
    void InitializeImprovedPhysics()
    {
        if (!useAccuratePhysics) return;

        // 드래그 계수 곡선 초기화 (Reynolds 수 기반)
        dragCoefficientCurve = new AnimationCurve();
        dragCoefficientCurve.AddKey(50000f, 0.5f);
        dragCoefficientCurve.AddKey(100000f, 0.35f);
        dragCoefficientCurve.AddKey(150000f, 0.25f);
        dragCoefficientCurve.AddKey(200000f, 0.2f);
        dragCoefficientCurve.AddKey(300000f, 0.22f);

        for (int i = 0; i < dragCoefficientCurve.keys.Length; i++)
        {
            dragCoefficientCurve.SmoothTangents(i, 0f);
        }
    }

    // ===== 기존 모든 메서드들 유지 =====
    private void Update()
    {
        UpdateAudioVolume();
        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);

        if (hasCollided && ballRigidbody != null)
        {
            ballRigidbody.AddForce(Vector3.down * Physics.gravity.magnitude * gravityMultiplierAfterHit, ForceMode.Acceleration);
            if (ballRigidbody.velocity.magnitude < minBallVelocityAfterHit)
            {
                ballRigidbody.velocity = ballRigidbody.velocity.normalized * minBallVelocityAfterHit;
            }
        }

        if (hasCollided && !hasLanded && !isCalculatingDistance)
        {
            if (launchPosition == Vector3.zero)
            {
                launchPosition = Vector3.zero;
            }
            isCalculatingDistance = true;
            lastPosition = transform.position;
            StartCoroutine(CalculateActualDistance());
        }

        if (hasLanded)
        {
            Debug.DrawLine(launchPosition, new Vector3(transform.position.x, launchPosition.y, transform.position.z), Color.red);
        }
    }

    public void SetBallSettings(BallSettings settings)
    {
        initialForce = settings.throwPower;
        verticalRotationSpeed = settings.vtorqueSpeed;
        horizontalRotationSpeed = settings.ztorqueSpeed;
        verticalAngleOffset = settings.voffset;
        horizontalAngleOffset = settings.hoffset;
        initialRotationAngleZ = settings.ballAngleZ;
        initialRotationAngleX = settings.ballAngleX;
        brakingDelay = settings.brakingStartTime;
    }

    public void SetPitchType(string type)
    {
        pitchTypeName = type;
        if (debugMode)
        {
            Debug.Log($"공 구종 설정: {pitchTypeName}");
        }
    }

    private void UpdateAudioVolume()
    {
        if (whooshClip == null) return;
        float powerFactor = Mathf.Clamp01((initialForce - minThrowPower) / (maxThrowPower - minThrowPower));
        float volume = Mathf.Lerp(minVolume, maxVolume, Mathf.Pow(powerFactor, volumeCurve));
        whooshClip.volume = volume;
    }

    public void CalculateDirection(Vector3 startPosition, Vector3 targetPosition)
    {
        launchDirection = (targetPosition - startPosition).normalized;
        if (debugMode)
        {
            float distance = Vector3.Distance(startPosition, targetPosition);
            Debug.Log($"발사 방향 계산: 시작={startPosition}, 목표={targetPosition}, 방향={launchDirection}, 거리={distance:F2}m");
        }
    }

    public void LaunchBall()
    {
        if (ballRigidbody == null)
        {
            ballRigidbody = GetComponent<Rigidbody>();
            ballRigidbody.maxAngularVelocity = 500f; // 높은 스핀율을 위해 증가
        }

        // 각도 조정
        Quaternion angleAdjustment = Quaternion.Euler(verticalAngleOffset, 0, horizontalAngleOffset);
        Vector3 adjustedDirection = angleAdjustment * launchDirection;

        // throwPower를 직접 km/h 속도로 사용
        float targetSpeedKmH = initialForce; // throwPower = km/h
        float targetSpeedMS = targetSpeedKmH / 3.6f; // m/s로 변환

        // Rigidbody 질량을 고려한 힘 계산 (F = m * v, Impulse 모드 사용)
        float forceMultiplier = ballRigidbody.mass * targetSpeedMS;

        ballRigidbody.AddForce(adjustedDirection * forceMultiplier, ForceMode.Impulse);
        ballRigidbody.rotation = Quaternion.Euler(initialRotationAngleX, 0, initialRotationAngleZ);

        // 초기 스핀 설정 (발사와 동시에 적용)
        SetInitialSpin();

        isBallLaunched = true;
        startTime = Time.time;
        StartCoroutine(ApplyBrakingAfterDelay(brakingDelay));

        if (debugMode)
        {
            Debug.Log($"공 발사: 목표속도={targetSpeedKmH}km/h ({targetSpeedMS:F1}m/s), 방향={adjustedDirection}, " +
                     $"힘={forceMultiplier:F2}N, 각도=({verticalAngleOffset}, {horizontalAngleOffset}), " +
                     $"스핀=({verticalRotationSpeed}RPM, {horizontalRotationSpeed}RPM)");
        }

        if (whooshClip != null) whooshClip.Play();
    }

    /// <summary>
    /// 초기 스핀 설정 (현실적인 RPM 값 적용)
    /// </summary>
    private void SetInitialSpin()
    {
        // RPM을 rad/s로 변환
        float verticalSpinRadS = ConvertRPMToRadS(verticalRotationSpeed);
        float horizontalSpinRadS = ConvertRPMToRadS(horizontalRotationSpeed);

        // 초기 각속도 설정 (월드 좌표계)
        Vector3 initialAngularVelocity = new Vector3(
            0f,                    // X축 회전 (없음)
            verticalSpinRadS,      // Y축 회전 (백스핀/톱스핀)
            horizontalSpinRadS     // Z축 회전 (사이드스핀)
        );

        ballRigidbody.angularVelocity = initialAngularVelocity;

        if (debugMode)
        {
            Debug.Log($"초기 스핀 설정: 수직={verticalRotationSpeed}RPM({verticalSpinRadS:F1}rad/s), " +
                     $"수평={horizontalRotationSpeed}RPM({horizontalSpinRadS:F1}rad/s)");
        }
    }

    private void Awake()
    {
        hasCollided = false;
        startTime = Time.time;
        if (whooshClip != null)
        {
            whooshClip.Play();
        }
        if (debugMode)
        {
            Debug.Log("Ball 오브젝트 초기화 완료");
        }
    }

    IEnumerator ApplyBrakingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ApplyBraking();
    }

    void ApplyBraking()
    {
        Vector3 torque = new Vector3(verticalRotationSpeed, 0, horizontalRotationSpeed * horizontalRotationMultiplier);
        ballRigidbody.AddRelativeTorque(torque, ForceMode.Acceleration);

        if (debugMode)
        {
            Debug.Log($"감속 회전력 적용: 회전력={torque}, 현재속도={ballRigidbody.velocity.magnitude:F2}m/s");
        }
    }

    // ===== 🔬 개선된 물리 계산 메서드들 =====
    void FixedUpdate()
    {
        if (!hasCollided)
        {
            if (useAccuratePhysics)
            {
                // 개선된 물리 시스템 사용
                ballRigidbody.AddForce(CalculateAccurateMagnusForce());
                ballRigidbody.AddForce(CalculateAccurateDragForce());
            }
            else
            {
                // 기존 물리 시스템 사용 (호환성)
                ballRigidbody.AddForce(CalculateMagnusForce());
                ballRigidbody.AddForce(CalculateAirResistance());
            }

            // 수직 속도 제한
            if (ballRigidbody.velocity.y > maxVerticalVelocity)
            {
                Vector3 newVelocity = ballRigidbody.velocity;
                newVelocity.y = maxVerticalVelocity;
                ballRigidbody.velocity = newVelocity;
            }

            // 스핀 감쇠 (공기 저항으로 인한 자연스러운 감소)
            ApplySpinDecay();
        }
        else
        {
            // 충돌 후에는 회전력 중단
            verticalRotationSpeed = 0;
            horizontalRotationSpeed = 0;
        }
    }

    /// <summary>
    /// 스핀 감쇠 적용 (현실적인 스핀 감소)
    /// </summary>
    private void ApplySpinDecay()
    {
        // 스핀은 공기 저항과 속도에 따라 점진적으로 감소
        float decayFactor = 0.998f; // 매 FixedUpdate마다 0.2% 감소
        ballRigidbody.angularVelocity *= decayFactor;

        // 디버그 모드에서 스핀 상태 모니터링
        if (debugMode && Time.frameCount % 60 == 0) // 1초마다
        {
            float currentVerticalSpinRPM = ConvertRadSToRPM(Mathf.Abs(ballRigidbody.angularVelocity.y));
            float currentHorizontalSpinRPM = ConvertRadSToRPM(Mathf.Abs(ballRigidbody.angularVelocity.z));
            Debug.Log($"현재 스핀: 수직={currentVerticalSpinRPM:F0}RPM, 수평={currentHorizontalSpinRPM:F0}RPM");
        }
    }

    /// <summary>
    /// 🔬 개선된 드래그 계산 (Reynolds 수 기반)
    /// </summary>
    private Vector3 CalculateAccurateDragForce()
    {
        float speed = ballRigidbody.velocity.magnitude;
        if (speed < 0.1f) return Vector3.zero;

        // Reynolds 수 계산
        float viscosity = GetAirViscosity(temperature);
        float reynoldsNumber = (airDensity * speed * ballRadius * 2f) / viscosity;

        // 실험 데이터 기반 드래그 계수
        float dragCoefficient = dragCoefficientCurve.Evaluate(reynoldsNumber);
        dragCoefficient *= 1.1f; // 야구공 솔기 효과

        // 드래그 힘 계산
        float crossArea = Mathf.PI * ballRadius * ballRadius;
        float dragMagnitude = 0.5f * airDensity * speed * speed * dragCoefficient * crossArea;

        Vector3 dragForce = -ballRigidbody.velocity.normalized * dragMagnitude * accurateDragMultiplier;

        if (debugMode && Time.frameCount % 30 == 0)
        {
            Debug.Log($"정확한 드래그: Re={reynoldsNumber:F0}, Cd={dragCoefficient:F3}, F={dragMagnitude:F3}N");
        }

        return dragForce;
    }

    /// <summary>
    /// 🔬 개선된 마그누스 효과 (Nathan 2008 모델) - 현실적인 스핀율 적용
    /// </summary>
    private Vector3 CalculateAccurateMagnusForce()
    {
        Vector3 velocity = ballRigidbody.velocity;
        Vector3 angularVelocity = ballRigidbody.angularVelocity;

        float speed = velocity.magnitude;
        float spinRate = angularVelocity.magnitude;

        if (speed < 0.1f || spinRate < 0.1f) return Vector3.zero;

        // 스핀 파라미터 계산 (S = rω/v)
        float spinParameter = (ballRadius * spinRate) / speed;

        // 마그누스 계수 (Nathan 2008 실험식)
        // Cm = 0.5 * S / (2.32 + S) for smooth sphere
        // 야구공의 경우 솔기 효과로 약 1.4배 증가
        float magnusCoefficient = 0.5f * spinParameter / (2.32f + spinParameter);
        magnusCoefficient *= 1.4f; // 야구공 솔기 효과

        // 마그누스 힘 계산: F = 0.5 * ρ * v² * Cm * A
        float crossArea = Mathf.PI * ballRadius * ballRadius;
        float magnusMagnitude = 0.5f * airDensity * speed * speed * magnusCoefficient * crossArea;

        // 힘의 방향: v × ω (속도와 각속도의 외적)
        Vector3 magnusDirection = Vector3.Cross(velocity.normalized, angularVelocity.normalized);
        Vector3 magnusForce = magnusDirection * magnusMagnitude * accurateMagnusMultiplier;

        if (debugMode && Time.frameCount % 30 == 0)
        {
            float currentSpinRPM = ConvertRadSToRPM(spinRate);
            Debug.Log($"정확한 마그누스: 스핀={currentSpinRPM:F0}RPM, S={spinParameter:F3}, " +
                     $"Cm={magnusCoefficient:F3}, F={magnusMagnitude:F3}N");
        }

        return magnusForce;
    }

    /// <summary>
    /// RPM을 rad/s로 변환
    /// </summary>
    private float ConvertRPMToRadS(float rpm)
    {
        return rpm * 2f * Mathf.PI / 60f;
    }

    /// <summary>
    /// rad/s를 RPM으로 변환
    /// </summary>
    private float ConvertRadSToRPM(float radPerSec)
    {
        return radPerSec * 60f / (2f * Mathf.PI);
    }

    /// <summary>
    /// 온도별 공기 점성계수
    /// </summary>
    private float GetAirViscosity(float tempCelsius)
    {
        float tempKelvin = tempCelsius + 273.15f;
        float mu0 = 1.716e-5f;
        float T0 = 273.15f;
        float S = 110.4f;
        return mu0 * Mathf.Pow(tempKelvin / T0, 1.5f) * (T0 + S) / (tempKelvin + S);
    }

    // ===== 기존 레거시 물리 메서드들 (호환성을 위해 유지) =====
    private Vector3 CalculateAirResistance()
    {
        float speed = ballRigidbody.velocity.magnitude;
        Vector3 direction = ballRigidbody.velocity.normalized;
        float resistanceMagnitude = airResistanceConstant * Mathf.Pow(speed, 2f);
        return resistanceMagnitude * -direction;
    }

    private Vector3 CalculateMagnusForce()
    {
        Vector3 velocity = ballRigidbody.velocity;
        Vector3 angularVelocity = ballRigidbody.angularVelocity;
        return magnusEffectConstant * Vector3.Cross(velocity, angularVelocity);
    }

    // ===== 기존 모든 충돌/거리 계산 메서드들 유지 =====
    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided || !isBallLaunched || (Time.time - lastCollisionTime < collisionCooldown))
            return;

        lastCollisionTime = Time.time;
        hasCollided = true;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true;
        }

        float finalSpeed = CalculateSpeed();

        if (debugMode)
        {
            Debug.Log($"공 충돌 발생: 충돌체={collision.gameObject.name}(태그:{collision.gameObject.tag}), 최종속도={finalSpeed:F2}km/h");
        }

        ABSScoreDetector scoreDetector = FindObjectOfType<ABSScoreDetector>();
        if (scoreDetector != null)
        {
            if (collision.gameObject.CompareTag("Bat"))
            {
                Vector3 reflectedVelocity = Vector3.Reflect(ballRigidbody.velocity, collision.contacts[0].normal);
                reflectedVelocity = reflectedVelocity * collisionDampingFactor;

                if (reflectedVelocity.magnitude < minBallVelocityAfterHit)
                    reflectedVelocity = reflectedVelocity.normalized * minBallVelocityAfterHit;
                if (reflectedVelocity.y > maxVerticalVelocity)
                    reflectedVelocity.y = maxVerticalVelocity;

                ballRigidbody.velocity = reflectedVelocity;
                scoreDetector.OnBallHit(gameObject);
            }
            else
            {
                scoreDetector.CheckFinalScore(gameObject);
            }
        }

        if (whooshClip != null)
            whooshClip.Stop();
    }

    public float CalculateSpeed()
    {
        // 현재 실제 속도를 km/h로 반환
        float currentSpeedMS = ballRigidbody.velocity.magnitude;
        float currentSpeedKmH = currentSpeedMS * 3.6f;

        if (debugMode)
        {
            float travelTime = Time.time - startTime;
            Debug.Log($"공 속도 정보: 현재속도={currentSpeedKmH:F1}km/h ({currentSpeedMS:F1}m/s), 경과시간={travelTime:F2}초");
        }

        return currentSpeedKmH;
    }

    private IEnumerator CalculateActualDistance()
    {
        bool reachedPeak = false;
        while (!hasLanded)
        {
            if (transform.position.y < lastPosition.y)
                reachedPeak = true;

            if ((reachedPeak && transform.position.y <= groundY) || (reachedPeak && isGround == true))
            {
                Vector3 landingPosition = transform.position;
                float xDistance = landingPosition.x - launchPosition.x;
                float zDistance = landingPosition.z - launchPosition.z;
                actualDistance = Mathf.Sqrt(xDistance * xDistance + zDistance * zDistance);
                hasLanded = true;
                UpdateHitInfoDisplay();

                if (debugMode)
                    Debug.Log($"공이 착지했습니다! 실제 비거리: {actualDistance:F1}m");
                break;
            }
            lastPosition = transform.position;
            yield return null;
        }
    }

    private void UpdateHitInfoDisplay()
    {
        HitInfoDisplay hitInfoDisplay = FindObjectOfType<HitInfoDisplay>();
        if (hitInfoDisplay != null)
        {
            HitInfo newHitInfo = new HitInfo();
            HitInfo currentInfo = hitInfoDisplay.GetCurrentHitInfo();

            if (currentInfo != null)
            {
                newHitInfo.ExitVelocity = currentInfo.ExitVelocity;
                newHitInfo.LaunchAngle = currentInfo.LaunchAngle;
                newHitInfo.Direction = currentInfo.Direction;
                newHitInfo.SpinRate = currentInfo.SpinRate;
                newHitInfo.SpinAxis = currentInfo.SpinAxis;
            }

            newHitInfo.EstimatedDistance = actualDistance;
            hitInfoDisplay.DisplayHitInfo(newHitInfo);
        }
    }

    public float GetStartTime() => startTime;
    public bool IsBallLaunched() => isBallLaunched;

    private void OnDrawGizmos()
    {
        if (!showRotationAxisGizmo || !Application.isPlaying || ballRigidbody == null || !isBallLaunched)
            return;

        Vector3 position = transform.position;
        Vector3 angularVelocity = ballRigidbody.angularVelocity;

        if (angularVelocity.magnitude < 0.1f) return;

        Vector3 rotationAxis = angularVelocity.normalized;
        Gizmos.color = rotationAxisColor;
        Gizmos.DrawLine(position, position + rotationAxis * gizmoLength);
        Gizmos.DrawSphere(position + rotationAxis * gizmoLength, 0.05f);

        if (debugMode)
        {
            Gizmos.color = Color.blue;
            Vector3 velocityDirection = ballRigidbody.velocity.normalized;
            float velocityLength = Mathf.Min(ballRigidbody.velocity.magnitude * 0.1f, gizmoLength);
            Gizmos.DrawLine(position, position + velocityDirection * velocityLength);
            Gizmos.DrawSphere(position + velocityDirection * velocityLength, 0.03f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHomeRun) return;

        if (other.CompareTag("HomeRunZone"))
        {
            isHomeRun = true;
            if (debugMode)
                Debug.Log($"홈런! 볼이 홈런 존을 통과했습니다. 위치: {transform.position}");
        }
    }

    public bool IsHomeRun() => isHomeRun;
}