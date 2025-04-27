using System.Collections;
using UnityEngine;

public class BallController : MonoBehaviour
{
    // 컴포넌트 참조
    private Rigidbody ballRigidbody;               // 물리 시뮬레이션을 위한 Rigidbody

    [Header("구종정보")]
    public string pitchTypeName = "Fastball";      // 투구 종류 이름

    [Header("방향 설정")]
    [Tooltip("발사 방향 벡터 (외부에서 설정됨)")]
    private Vector3 launchDirection;               // 발사 방향 (외부에서 설정)

    [Header("공 물리 설정")]
    [Tooltip("공을 발사하는 힘의 크기")]
    public float initialForce;                     // 초기 발사 힘
    [Tooltip("수직 방향 회전 속도")]
    public float verticalRotationSpeed;            // 수직 회전 속도
    [Tooltip("수평 방향 회전 속도")]
    public float horizontalRotationSpeed;          // 수평 회전 속도

    [Header("각도 설정")]
    [Tooltip("수직 방향 발사각 조정 (도)")]
    [Range(-45f, 45f)]
    public float verticalAngleOffset;              // 수직 발사각 조정값
    [Tooltip("수평 방향 발사각 조정 (도)")]
    [Range(-45f, 45f)]
    public float horizontalAngleOffset;            // 수평 발사각 조정값
    [Tooltip("공의 초기 Z축 회전 각도 (도)")]
    [Range(0f, 360f)]
    public float initialRotationAngleZ;            // 공의 초기 Z축 회전 각도
    [Tooltip("공의 초기 X축 회전 각도 (도)")]
    public float initialRotationAngleX;            // 공의 초기 X축 회전 각도

    [Header("타이밍 설정")]
    [Tooltip("발사 후 감속 시작까지의 대기 시간 (초)")]
    [Range(0f, 5f)]
    public float brakingDelay;                     // 감속 시작 전 대기 시간
      //
    [Header("물리 효과")]
    [Tooltip("공기 저항 상수")]
    public float airResistanceConstant = 0.02f;    // 공기 저항 계수
    [Tooltip("마그누스 효과 상수")]
    public float magnusEffectConstant = 0.007f;    // 마그누스 효과 계수
    [Tooltip("수평 회전 배율")]
    public float horizontalRotationMultiplier = 3; // 수평 회전 배율

    [Header("상태")]
    [Tooltip("공이 물체와 충돌했는지 여부")]
    public bool hasCollided = false;               // 충돌 발생 여부

    [Header("스트라이크/볼 판정")]
    private ABSScoreDetector scoreDetector;        // 점수 판정기 참조
    private bool isBallLaunched = false;           // 공 발사 여부

    [Header("오디오 설정")]
    public AudioSource whooshClip;                 // 휙 소리 오디오 소스

    [Header("볼륨 설정")]
    [Tooltip("최소 볼륨")]
    public float minVolume = 0.5f;                 // 최소 볼륨
    [Tooltip("최대 볼륨")]
    public float maxVolume = 1.5f;                 // 최대 볼륨
    [Tooltip("볼륨 곡선 강도")]
    public float volumeCurve = 2.0f;               // 곡선 강도 (1.0 = 선형, >1.0 = 지수적 증가)

    [Header("던지기 파워 설정")]
    [Range(70f, 170f)]
    [Tooltip("최소 던지기 파워")]
    public float minThrowPower = 70f;              // 최소 throwPower
    [Tooltip("최대 던지기 파워")]
    public float maxThrowPower = 170f;             // 최대 throwPower

    [Header("회전축 기즈모 설정")]
    [Tooltip("회전축 기즈모 활성화 여부")]
    public bool showRotationAxisGizmo = true;      // 회전축 기즈모 표시 여부
    [Tooltip("회전축 기즈모 길이")]
    public float gizmoLength = 2.0f;               // 기즈모 라인 길이
    [Tooltip("회전축 기즈모 색상")]
    public Color rotationAxisColor = Color.red;    // 회전축 색상

    [Header("충돌 시 물리 처리")]
    [Tooltip("충돌 시 감쇠 계수")]
    [SerializeField] private float collisionDampingFactor = 0.5f; // 충돌 시 감쇠 계수
    [Tooltip("타격 후 최소 속도")]
    [SerializeField] private float minBallVelocityAfterHit = 2.0f; // 최소 속도 유지 (너무 느리게 떨어지지 않도록)
    [Tooltip("타격 후 중력 배율")]
    [SerializeField] private float gravityMultiplierAfterHit = 1.5f; // 타격 후 중력 배율

    [Tooltip("최대 수직 속도")]
    [SerializeField] private float maxVerticalVelocity = 20f; // 최대 수직 속도

    // 충돌 이벤트가 여러번 호출되는 것을 방지하기 위한 필드
    private float lastCollisionTime = 0f;
    [Tooltip("충돌 쿨다운 시간")]
    private float collisionCooldown = 0.1f; // 충돌 쿨다운 시간 (초)

    // 속도 계산을 위한 변수
    private float startTime;
    private const float TARGET_DISTANCE = 18.44f; // 목표 거리(미터)

    [Tooltip("디버그 로그 출력 여부")]
    public bool debugMode = false;

    [Header("Distance Calculation")]
    public bool hasLanded = false;
    public float actualDistance = 0f;
    public Vector3 launchPosition;
    public bool isCalculatingDistance = false; // private에서 public으로 변경
    private Vector3 lastPosition;
    private float groundY = 0.01f; // 지면 높이 (필요에 따라 조정)

    /**
     * 매 프레임마다 호출되는 업데이트 함수
     * 오디오 볼륨을 업데이트하고 충돌 이후의 추가 물리 효과를 적용합니다.
     */
    private void Update()
    {
        UpdateAudioVolume();

        // 충돌 이후 추가적인 중력 영향 적용
        if (hasCollided && ballRigidbody != null)
        {
            // 타격 후 추가 중력 적용
            ballRigidbody.AddForce(Vector3.down * Physics.gravity.magnitude * gravityMultiplierAfterHit, ForceMode.Acceleration);

            // 너무 느린 공은 최소 속도 유지 (공이 멈춰서 떨어지는 것 방지)
            if (ballRigidbody.velocity.magnitude < minBallVelocityAfterHit)
            {
                // 현재 방향 유지하면서 최소 속도 적용
                ballRigidbody.velocity = ballRigidbody.velocity.normalized * minBallVelocityAfterHit;
            }
        }
        if (hasCollided && !hasLanded && !isCalculatingDistance)
        {
            // launchPosition이 설정되지 않았다면 원점으로 설정
            if (launchPosition == Vector3.zero)
            {
                launchPosition = Vector3.zero; // 또는 실제 홈플레이트 위치로 설정
            }

            isCalculatingDistance = true;
            lastPosition = transform.position;
            StartCoroutine(CalculateActualDistance());
        }

        // 디버깅용: 착지 후 발사 지점과 착지 지점 사이에 선 그리기
        if (hasLanded)
        {
            Debug.DrawLine(launchPosition, new Vector3(transform.position.x, launchPosition.y, transform.position.z), Color.red);
        }
    }

    /**
     * 공 설정을 적용하는 메서드
     * 외부에서 물리 설정값을 일괄 적용할 때 사용합니다.
     * 
     * @param settings 적용할 공 설정 값
     */
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

    /**
     * 구종 유형을 설정하는 메서드
     * 
     * @param type 설정할 구종 유형 문자열
     */
    public void SetPitchType(string type)
    {
        pitchTypeName = type;

        if (debugMode)
        {
            Debug.Log($"공 구종 설정: {pitchTypeName}");
        }
    }


    /**
     * 오디오 볼륨을 업데이트하는 메서드
     * 파워에 따라 볼륨을 조절합니다.
     */
    private void UpdateAudioVolume()
    {
        if (whooshClip == null) return;

        // throwPower 범위 정규화 (0~1)
        float powerFactor = Mathf.Clamp01((initialForce - minThrowPower) / (maxThrowPower - minThrowPower));

        // 곡선 적용
        float volume = Mathf.Lerp(minVolume, maxVolume, Mathf.Pow(powerFactor, volumeCurve));

        // 볼륨 설정
        whooshClip.volume = volume;
    }

    /**
     * 점수 판정기를 설정하는 메서드 (외부에서 호출됨)
     * 
     * @param detector 설정할 점수 판정기 객체
     */
    private void SetScoreDetector(ABSScoreDetector detector)
    {
        scoreDetector = detector;
    }

    /**
     * 외부 스크립트에서 발사 방향을 설정하는 메서드
     * 시작 위치와 목표 위치를 기반으로 방향을 계산합니다.
     * 
     * @param startPosition 시작 위치
     * @param targetPosition 목표 위치
     */
    public void CalculateDirection(Vector3 startPosition, Vector3 targetPosition)
    {
        launchDirection = (targetPosition - startPosition).normalized;

        if (debugMode)
        {
            float distance = Vector3.Distance(startPosition, targetPosition);
            Debug.Log(string.Format(
                "발사 방향 계산: 시작={0}, 목표={1}, 방향={2}, 거리={3:F2}m",
                startPosition, targetPosition, launchDirection, distance));
        }
    }

    /**
     * 공을 발사하는 메서드
     * 외부 스크립트에서 적절한 시점에 호출됩니다.
     */
    public void LaunchBall()
    {
        // Rigidbody 컴포넌트 확인
        if (ballRigidbody == null)
        {
            ballRigidbody = GetComponent<Rigidbody>();
            ballRigidbody.maxAngularVelocity = 4000;  // 최대 각속도 설정
        }

        // 발사각 조정
        Quaternion angleAdjustment = Quaternion.Euler(verticalAngleOffset, 0, horizontalAngleOffset);
        Vector3 adjustedDirection = angleAdjustment * launchDirection;

        // 발사 힘 정규화 및 적용
        float normalizedForce = 0.043306f * initialForce - 0.102461f;
        ballRigidbody.AddForce(adjustedDirection * normalizedForce, ForceMode.Impulse);

        // 초기 회전 설정
        ballRigidbody.rotation = Quaternion.Euler(initialRotationAngleX, 0, initialRotationAngleZ);

        // 이제 공이 발사되었음을 표시
        isBallLaunched = true;

        // 시작 시간 기록
        startTime = Time.time;

        // 감속 코루틴 시작
        StartCoroutine(ApplyBrakingAfterDelay(brakingDelay));

        // 디버그 모드일 때만 로그 출력
        if (debugMode)
        {
            Debug.Log(string.Format(
                "공 발사: 방향={0}, 힘={1:F2}, 수직각={2}도, 수평각={3}도",
                adjustedDirection, normalizedForce, verticalAngleOffset, horizontalAngleOffset));
        }

        whooshClip.Play();
    }

    /**
     * 수동으로 공을 초기화하는 메서드
     * 오브젝트 생성 시 자동으로 호출됩니다.
     */
    private void Awake()
    {
        hasCollided = false;

        // 오브젝트 생성 시 시작 시간 기록 (발사 전에도 시간 측정)
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

    /**
     * 지정된 시간 후 감속을 시작하는 코루틴
     * 
     * @param delay 감속 시작 전 대기 시간(초)
     */
    IEnumerator ApplyBrakingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ApplyBraking();
    }

    /**
     * 감속을 적용하는 메서드
     * 회전력을 적용하여 공의 속도를 제어합니다.
     */
    void ApplyBraking()
    {
        Vector3 torque = new Vector3(
            verticalRotationSpeed,
            0,
            horizontalRotationSpeed * horizontalRotationMultiplier
        );

        ballRigidbody.AddRelativeTorque(torque, ForceMode.Acceleration);

        if (debugMode)
        {
            Debug.Log(string.Format(
                "감속 회전력 적용: 회전력={0}, 현재속도={1:F2}m/s, 현재각속도={2}",
                torque, ballRigidbody.velocity.magnitude, ballRigidbody.angularVelocity));
        }
    }

    /**
     * 물리 업데이트 메서드
     * 공기 저항과 마그누스 효과를 적용합니다.
     */
    void FixedUpdate()
    {
        if (!hasCollided)
        {
            // 충돌 전에는 물리 효과 적용
            ballRigidbody.AddForce(CalculateMagnusForce());
            ballRigidbody.AddForce(CalculateAirResistance());

            // 수직 속도 제한 (너무 빠르게 위로 올라가는 것 방지)
            if (ballRigidbody.velocity.y > maxVerticalVelocity)
            {
                Vector3 newVelocity = ballRigidbody.velocity;
                newVelocity.y = maxVerticalVelocity;
                ballRigidbody.velocity = newVelocity;
            }
        }
        else
        {
            // 충돌 후에는 회전력 중단
            verticalRotationSpeed = 0;
            horizontalRotationSpeed = 0;
        }
    }

    /**
     * 공기 저항력을 계산하는 메서드
     * 속도의 제곱에 비례하는 저항력을 속도 반대 방향으로 발생합니다.
     * 
     * @return 계산된 공기 저항력 벡터
     */
    private Vector3 CalculateAirResistance()
    {
        float speed = ballRigidbody.velocity.magnitude;
        Vector3 direction = ballRigidbody.velocity.normalized;
        float resistanceMagnitude = airResistanceConstant * Mathf.Pow(speed, 2f);
        Vector3 resistance = resistanceMagnitude * -direction;

        if (debugMode && Time.frameCount % 30 == 0)  // 30프레임마다 한 번씩만 로그 출력 (너무 많은 로그 방지)
        {
            Debug.Log(string.Format(
                "공기 저항: 속도={0:F2}m/s, 저항크기={1:F4}N, 저항방향={2}",
                speed, resistanceMagnitude, -direction));
        }

        return resistance;
    }

    /**
     * 마그누스 효과를 계산하는 메서드
     * 회전하는 공이 공기 중에서 경험하는 힘을 시뮬레이션합니다.
     * 속도 벡터와 각속도 벡터의 외적으로 계산됩니다.
     * 
     * @return 계산된 마그누스 힘 벡터
     */
    private Vector3 CalculateMagnusForce()
    {
        Vector3 velocity = ballRigidbody.velocity;
        Vector3 angularVelocity = ballRigidbody.angularVelocity;
        Vector3 magnusForce = magnusEffectConstant * Vector3.Cross(velocity, angularVelocity);

        if (debugMode && Time.frameCount % 30 == 0)  // 30프레임마다 한 번씩만 로그 출력
        {
            Debug.Log(string.Format(
                "마그누스 효과: 속도={0:F2}m/s, 각속도={1:F2}rad/s, 힘={2:F4}N",
                velocity.magnitude, angularVelocity.magnitude, magnusForce.magnitude));
        }

        return magnusForce;
    }

    /**
     * 충돌 감지 메서드 - 개선된 버전
     * 공의 충돌 처리 및 점수 판정을 수행합니다.
     * 
     * @param collision 충돌 정보
     */
    private void OnCollisionEnter(Collision collision)
    {

        // 이미 충돌 상태이거나 아직 발사되지 않은 경우 또는 너무 빠른 연속 충돌인 경우 무시
        if (hasCollided || !isBallLaunched || (Time.time - lastCollisionTime < collisionCooldown))
            return;

        // 충돌 시간 기록
        lastCollisionTime = Time.time;

        // 충돌 발생 표시
        hasCollided = true;

        // 속도 계산 및 출력 (반환값은 km/h 단위 속도)
        float finalSpeed = CalculateSpeed();

        // 디버그 모드일 때 충돌 정보 출력
        if (debugMode)
        {
            string colliderName = collision.gameObject.name;
            string colliderTag = collision.gameObject.tag;

            Debug.Log(string.Format(
                "공 충돌 발생: 충돌체={0}(태그:{1}), 최종속도={2:F2}km/h, 충돌 위치={3}",
                colliderName,
                colliderTag,
                finalSpeed,
                collision.contacts[0].point));
        }

        // 씬에서 ScoreDetector 찾기 (프리팹에서 직접 참조할 수 없으므로)
        ABSScoreDetector scoreDetector = FindObjectOfType<ABSScoreDetector>();

        // 점수 감지기가 있는 경우
        if (scoreDetector != null)
        {
            // Bat 오브젝트와 충돌한 경우 - 타격 처리
            if (collision.gameObject.CompareTag("Bat"))
            {
                // 충돌 속도 계산 및 감쇠 적용
                Vector3 reflectedVelocity = Vector3.Reflect(ballRigidbody.velocity, collision.contacts[0].normal);

                // 충돌 지점 법선에 따른 반사 각도 조정
                reflectedVelocity = reflectedVelocity * collisionDampingFactor;

                // 최소 속도 보장 (정지 방지)
                if (reflectedVelocity.magnitude < minBallVelocityAfterHit)
                {
                    reflectedVelocity = reflectedVelocity.normalized * minBallVelocityAfterHit;
                }

                // 수직 속도 제한
                if (reflectedVelocity.y > maxVerticalVelocity)
                {
                    reflectedVelocity.y = maxVerticalVelocity;
                }

                // 속도 적용
                ballRigidbody.velocity = reflectedVelocity;

                if (debugMode)
                {
                    Debug.Log(string.Format(
                        "타격 후 반사: 원래속도={0}, 반사속도={1}, 감쇠계수={2}",
                        ballRigidbody.velocity.magnitude,
                        reflectedVelocity.magnitude,
                        collisionDampingFactor));
                }

                // 타격 이벤트 알림
                scoreDetector.OnBallHit(gameObject);
            }
            else
            {
                // 기타 오브젝트와 충돌한 경우 점수 판정 요청
                scoreDetector.CheckFinalScore(gameObject);
            }
        }

        // 효과음 중지
        if (whooshClip != null)
        {
            whooshClip.Stop();
        }
    }

    /**
     * 간단한 속도 계산 및 출력 메서드
     * 시간과 거리를 기반으로 평균 속도를 계산합니다.
     * 
     * @return 계산된 평균 속도 (km/h)
     */
    public float CalculateSpeed()
    {
        // 경과 시간 계산
        float travelTime = Time.time - startTime;

        // 목표 거리(18.44미터)를 기준으로 평균 속도 계산 (미터/초)
        float averageSpeed = TARGET_DISTANCE / travelTime;

        // 디버그 모드일 때만 로그 출력
        if (debugMode)
        {
            Debug.Log(string.Format(
                "공 속도 정보: 거리={0}m, 이동시간={1:F2}초, 평균속도={2:F2}km/h",
                TARGET_DISTANCE, travelTime, averageSpeed * 3.6f));
        }

        // 속도 반환 (km/h)
        return averageSpeed * 3.6f;
    }

    private IEnumerator CalculateActualDistance()
    {
        // 공이 타격된 후 최고점을 지나 내려오는 것을 기다림
        bool reachedPeak = false;
        while (!hasLanded)
        {
            // 현재 위치가 이전 위치보다 낮으면 최고점을 지난 것
            if (transform.position.y < lastPosition.y)
            {
                reachedPeak = true;
            }
            // 최고점을 지나고 지면에 닿았는지 확인
            if (reachedPeak && transform.position.y <= groundY)
            {
                // 착지 위치 저장
                Vector3 landingPosition = transform.position;

                // X-Z 평면에서의 직선 거리 계산 (2D 평면에서의 비거리)
                float xDistance = landingPosition.x - launchPosition.x;
                float zDistance = landingPosition.z - launchPosition.z;
                actualDistance = Mathf.Sqrt(xDistance * xDistance + zDistance * zDistance);

                hasLanded = true;
                // HitInfoDisplay에 실제 거리 업데이트
                UpdateHitInfoDisplay();
                if (debugMode)
                {
                    Debug.Log($"공이 착지했습니다! 실제 비거리: {actualDistance:F1}m");
                }
                break;
            }
            lastPosition = transform.position;
            yield return null;
        }
    }

    // HitInfoDisplay에 실제 측정된 거리 정보 업데이트
    private void UpdateHitInfoDisplay()
    {
        HitInfoDisplay hitInfoDisplay = FindObjectOfType<HitInfoDisplay>();
        if (hitInfoDisplay != null)
        {
            // 새로운 HitInfo 객체 생성 (기존 값들은 유지)
            HitInfo newHitInfo = new HitInfo();

            // 기존에 표시되던 정보 가져오기 (이 메서드가 HitInfoDisplay에 있다고 가정)
            HitInfo currentInfo = hitInfoDisplay.GetCurrentHitInfo();

            if (currentInfo != null)
            {
                // 기존 정보 복사
                newHitInfo.ExitVelocity = currentInfo.ExitVelocity;
                newHitInfo.LaunchAngle = currentInfo.LaunchAngle;
                newHitInfo.Direction = currentInfo.Direction;
                newHitInfo.SpinRate = currentInfo.SpinRate;
                newHitInfo.SpinAxis = currentInfo.SpinAxis;
            }

            // 실제 측정된 거리로 업데이트
            newHitInfo.EstimatedDistance = actualDistance;

            // 업데이트된 정보 표시 (이 메서드가 HitInfoDisplay에 있다고 가정)
            hitInfoDisplay.DisplayHitInfo(newHitInfo);
        }
    }

    public float GetStartTime()
    {
        return startTime;
    }

    /**
     * 공이 발사되었는지 확인하는 메서드
     * 
     * @return 공 발사 상태
     */
    public bool IsBallLaunched()
    {
        return isBallLaunched;
    }

    /**
     * 회전축 기즈모를 그리는 메서드
     * Unity 에디터에서만 표시됩니다.
     */

    private void OnDrawGizmos()
    {
        // 기즈모 표시가 꺼져있거나 플레이 모드가 아닌 경우 표시하지 않음
        if (!showRotationAxisGizmo || !Application.isPlaying)
            return;

        // 리지드바디가 없거나 공이 발사되지 않은 경우 표시하지 않음
        if (ballRigidbody == null || !isBallLaunched)
            return;

        // 현재 위치
        Vector3 position = transform.position;

        // 현재 각속도 벡터 가져오기
        Vector3 angularVelocity = ballRigidbody.angularVelocity;

        // 각속도가 너무 작으면 표시하지 않음
        if (angularVelocity.magnitude < 0.1f)
            return;

        // 현재 회전축 계산 (각속도 벡터의 방향이 회전축)
        Vector3 rotationAxis = angularVelocity.normalized;

        // 회전축 표시
        Gizmos.color = rotationAxisColor;
        Gizmos.DrawLine(position, position + rotationAxis * gizmoLength);

        // 회전축 끝에 작은 구체로 표시
        Gizmos.DrawSphere(position + rotationAxis * gizmoLength, 0.05f);

        // 속도 벡터도 표시 (디버그 모드일 때만)
        if (debugMode)
        {
            // 속도 벡터는 파란색으로 표시
            Gizmos.color = Color.blue;
            Vector3 velocityDirection = ballRigidbody.velocity.normalized;
            float velocityLength = Mathf.Min(ballRigidbody.velocity.magnitude * 0.1f, gizmoLength); // 속도에 비례한 길이

            Gizmos.DrawLine(position, position + velocityDirection * velocityLength);
            Gizmos.DrawSphere(position + velocityDirection * velocityLength, 0.03f);
        }
    }


}

/**
 * BallController.cs - 간소화된 공 물리 시뮬레이션 컨트롤러
 * 
 * 이 스크립트는 Unity에서 공의 기본적인 물리적 움직임을 담당합니다.
 * 주요 기능:
 * - 외부에서 제공된 시작 위치와 목표 위치를 사용하여 공을 발사
 * - 공기 저항과 마그누스 효과를 통한 현실적인 움직임 구현
 * - 지정된 시간 후 감속 적용
 * - 회전축 시각화를 위한 기즈모 표시 기능 추가
 * 
 * 참고: 이 버전은 최소한의 물리 시뮬레이션만 포함하도록 간소화되었습니다.
 * 
 */