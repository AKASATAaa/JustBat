using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배트 컨트롤러 클래스 - 배트의 움직임과 공과의 상호작용을 관리합니다.
/// </summary>
public class BatController : MonoBehaviour
{
    [Header("배트 기본 속성")]
    [SerializeField] public float hitForceMultiplier = 0.5f; // 타격 힘 계수 (값이 클수록 더 강한 타격)
    [SerializeField] public Transform batSweetSpot; // 배트의 스윗스팟 위치
    [SerializeField] private LayerMask ballLayer; // 공 레이어 설정
    [SerializeField] private float sweepRadius = 0.1f; // 스윕 감지 반경
    [SerializeField] private int sweepResolution = 10; // 스윕 테스트 해상도

    [Header("스윗스팟 설정")]
    [SerializeField] private float sweetSpotRadius = 0.05f; // 스윗스팟 최적 영향 반경
    [SerializeField] private float sweetSpotMaxDistance = 0.15f; // 스윗스팟 최대 영향 거리
    [SerializeField] private float sweetSpotFalloffRate = 10f; // 스윗스팟 효과 감소율
    [SerializeField] private float minQualityFactor = 0.5f; // 최소 타격 품질 계수

    [Header("시각화 설정")]
    [SerializeField] private Color sweetSpotColor = Color.red; // 스윗스팟 색상
    [SerializeField] private Color effectRangeColor = new Color(1, 0.5f, 0, 0.3f); // 영향 범위 색상
    [SerializeField] private bool showSweetSpotGizmos = true; // 기즈모 표시 여부
    [SerializeField] private bool showDebugRays = true; // 디버그 레이 표시 여부

    [Header("타격 정보 표시")]
    [SerializeField] private HitInfoDisplay hitInfoDisplay; // 타격 정보 표시 UI 참조

    [Header("공 물리 매개변수")]
    [SerializeField] private float gravity = 9.81f; // 중력 가속도 (m/s^2)
    [SerializeField] private float dragCoefficient = 0.3f; // 공의 항력 계수 (0.2~0.5 정도가 일반적)
    [SerializeField] private float ballMass = 0.145f; // 공의 질량 (kg) (야구공 질량)
    [SerializeField] private float ballRadius = 0.0366f; // 공의 반지름 (m) (야구공 반지름)
    [SerializeField] private float airDensity = 1.225f; // 공기 밀도 (kg/m³) (표준 대기압)
    [SerializeField] private float spinFactor = 0.1f; // 회전에 의한 영향도

    [Header("속도 제한")]
    [SerializeField] private float minExitVelocity = 5.0f; // 최소 출구 속도 (m/s)
    [SerializeField] private float maxExitVelocity = 50.0f; // 최대 출구 속도 (m/s)
    [SerializeField] private float minVerticalVelocity = -10.0f; // 최소 수직 속도 (m/s)
    [SerializeField] private float corMinimum = 0.4f; // 최소 반발계수
    [SerializeField] private float corMaximum = 0.6f; // 최대 반발계수

    [Header("사운드 효과")]
    [SerializeField] private AudioSource batAudioSource; // 오디오 소스 컴포넌트
    [SerializeField] private AudioClip[] hitSounds; // 여러 타격 사운드를 배열로 관리 (다양성 부여)
    [SerializeField] private float minVolume = 0.1f; // 최소 볼륨
    [SerializeField] private float maxVolume = 1.0f; // 최대 볼륨
    [SerializeField] private float minPitch = 0.8f; // 최소 피치
    [SerializeField] private float maxPitch = 1.2f; // 최대 피치

    [Header("거리 계산")]
    public bool hasLanded = false; // 공이 착지했는지 여부
    public float actualDistance = 0f; // 실제 타구 거리
    public Vector3 launchPosition; // 타구 시작 위치
    public GameObject PlatePosition; // 홈 플레이트 위치
    public bool hasCollided = false; // 공이 충돌했는지 여부

    // 거리 계산 관련 비공개 변수
    private bool isCalculatingDistance = false; // 거리 계산 중인지 여부
    private Vector3 lastPosition; // 공의 마지막 위치
    private float groundY = 0.01f; // 지면 높이 (필요에 따라 조정)

    // 배트 이동 관련 변수
    private Vector3 previousPosition; // 이전 프레임 위치
    public Vector3 currentVelocity; // 현재 속도 벡터
    private Rigidbody batRigidbody; // 배트의 리지드바디
    private List<Vector3> positionHistory = new List<Vector3>(); // 위치 기록 저장
    private int positionHistoryLength = 10; // 위치 기록 길이

    // 충돌 관련 변수
    private HashSet<Collider> hitBalls = new HashSet<Collider>(); // 이미 타격한 공 목록
    private float hitCooldown = 0.5f; // 타격 쿨다운 시간

    // 마지막 타격에서 생성된 스핀 정보를 저장하는 변수들
    private Vector3 lastSpinAxis = Vector3.up; // 마지막 스핀 축
    private float lastSpinRate = 0f; // 마지막 스핀 속도

    /// <summary>
    /// 초기화 작업을 수행합니다.
    /// </summary>
    void Start()
    {

        TrailRenderer trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer != null)
        {
            // 트레일 렌더러가 있는 경우 머티리얼 설정
            ConfigureTrailRenderer(trailRenderer, 1.0f); // 기본 품질 1.0
            trailRenderer.enabled = false; // 시작 시 비활성화
            Debug.Log("시작 시 트레일 렌더러 구성됨");
        }
        // 리지드바디 컴포넌트 가져오기
        batRigidbody = GetComponent<Rigidbody>();

        // 충돌 설정
        if (batRigidbody != null)
        {
            batRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            batRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // 이전 위치 초기화
        previousPosition = transform.position;

        // 위치 기록 초기화
        for (int i = 0; i < positionHistoryLength; i++)
        {
            positionHistory.Add(transform.position);
        }

        // 배트 스윗 스팟이 설정되지 않았다면 자동 생성
        if (batSweetSpot == null)
        {
            GameObject sweetSpot = new GameObject("BatSweetSpot");
            sweetSpot.transform.parent = transform;
            sweetSpot.transform.localPosition = new Vector3(0, 0, 0.8f); // 배트 앞쪽에 위치
            batSweetSpot = sweetSpot.transform;
        }

        // HitInfoDisplay가 지정되지 않았다면 씬에서 찾기
        if (hitInfoDisplay == null)
        {
            hitInfoDisplay = FindObjectOfType<HitInfoDisplay>();
            if (hitInfoDisplay == null)
            {
                Debug.LogWarning("HitInfoDisplay가 씬에 없습니다. 타구 정보가 표시되지 않을 수 있습니다.");
            }
        }

        // 오디오 소스가 없으면 자동 생성
        if (batAudioSource == null)
        {
            // 컴포넌트가 존재하는지 확인
            batAudioSource = GetComponent<AudioSource>();

            // 없으면 추가
            if (batAudioSource == null)
            {
                batAudioSource = gameObject.AddComponent<AudioSource>();
                batAudioSource.playOnAwake = false;
                batAudioSource.spatialBlend = 1.0f; // 3D 사운드로 설정
                batAudioSource.dopplerLevel = 0.0f; // 도플러 효과 비활성화
                batAudioSource.rolloffMode = AudioRolloffMode.Linear; // 거리에 따른 감쇠 설정
                batAudioSource.maxDistance = 20.0f; // 최대 들리는 거리
            }
        }

        // 히트 사운드가 없으면 경고
        if (hitSounds == null || hitSounds.Length == 0)
        {
            Debug.LogWarning("배트 타격 사운드가 설정되지 않았습니다. 타격 사운드가 재생되지 않을 수 있습니다.");
        }

        // 홈 플레이트 위치 설정
        if (PlatePosition != null)
        {
            launchPosition = PlatePosition.transform.position;
        }
        else
        {
            launchPosition = Vector3.zero;
            Debug.LogWarning("홈 플레이트 위치가 설정되지 않았습니다. 거리 계산에 영향을 줄 수 있습니다.");
        }

        lastPosition = transform.position;

        // 타격 기록 초기화 코루틴 시작
        StartCoroutine(ClearHitBallsRoutine());
    }

    /// <summary>
    /// 매 프레임마다 실행되는 업데이트 함수입니다.
    /// </summary>
    void Update()
    {
        // 위치 기록 업데이트
        positionHistory.RemoveAt(0);
        positionHistory.Add(batSweetSpot.position);

        // 배트의 고속 이동 경로를 따라 공과의 충돌 검사 (스윕 테스트)
        PerformSweepTest();

        // 공이 타격된 후에만 거리 계산 시작
        if (hasCollided && !hasLanded && !isCalculatingDistance)
        {
            isCalculatingDistance = true;
            StartCoroutine(CalculateActualDistance());
        }

        // 디버깅 용도 - 타구 경로 시각화
        if (hasLanded)
        {
            Debug.DrawLine(launchPosition, new Vector3(transform.position.x, launchPosition.y, transform.position.z), Color.red);
        }
    }

    /// <summary>
    /// 물리 업데이트마다 실행되는 함수입니다.
    /// </summary>
    void FixedUpdate()
    {
        // 배트의 속도 계산
        currentVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;
    }

    /// <summary>
    /// 물리 충돌이 발생했을 때 호출되는 함수입니다.
    /// </summary>
    /// <param name="collision">충돌 정보</param>
    void OnCollisionEnter(Collision collision)
    {
        HandleBallCollision(collision.collider, collision.contacts[0].point);
    }

    /// <summary>
    /// 트리거 충돌이 발생했을 때 호출되는 함수입니다.
    /// </summary>
    /// <param name="other">충돌한 콜라이더</param>
    void OnTriggerEnter(Collider other)
    {
        Vector3 closestPoint = other.ClosestPoint(batSweetSpot.position);
        HandleBallCollision(other, closestPoint);
    }

    /// <summary>
    /// 배트의 이동 경로를 따라 수동으로 충돌 검사를 수행합니다.
    /// 고속에서 물리 시스템이 놓칠 수 있는 충돌을 감지합니다.
    /// </summary>
    private void PerformSweepTest()
    {
        // 위치 기록이 충분하지 않으면 건너뜀
        if (positionHistory.Count < 2) return;

        // 이동 속도 계산
        float batSpeed = currentVelocity.magnitude;

        // 배트가 빠르게 움직일 때만 스윕 테스트 수행 (성능 최적화)
        if (batSpeed > 1.0f)
        {
            // 스윕 테스트 해상도 조정 (더 빠를수록 더 정밀하게)
            int actualResolution = Mathf.Max(2, Mathf.RoundToInt(sweepResolution * Mathf.Clamp01(batSpeed / 20f)));

            // 이전 위치에서 현재 위치까지의 경로를 따라 검사
            for (int i = 1; i < positionHistory.Count; i++)
            {
                Vector3 startPos = positionHistory[i - 1];
                Vector3 endPos = positionHistory[i];
                Vector3 direction = endPos - startPos;
                float distance = direction.magnitude;

                // 유의미한 이동이 있을 때만 검사
                if (distance > 0.001f)
                {
                    direction.Normalize();

                    // 경로를 여러 단계로 나누어 테스트
                    for (int step = 0; step < actualResolution; step++)
                    {
                        float t = (float)step / actualResolution;
                        Vector3 testPos = Vector3.Lerp(startPos, endPos, t);

                        // 구형 캐스트로 공 검사
                        RaycastHit[] hits = Physics.SphereCastAll(
                            testPos,
                            sweepRadius,
                            direction,
                            distance / actualResolution,
                            ballLayer
                        );

                        // 디버그 레이 시각화
                        if (showDebugRays)
                        {
                            Debug.DrawLine(
                                testPos,
                                testPos + direction * (distance / actualResolution),
                                Color.yellow,
                                0.1f
                            );
                        }

                        // 감지된 모든 충돌에 대해 처리
                        foreach (RaycastHit hit in hits)
                        {
                            if (hit.collider != null && hit.collider.CompareTag("Ball"))
                            {
                                HandleBallCollision(hit.collider, hit.point);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 공과의 충돌을 처리하는 메인 함수입니다.
    /// 타격 품질을 계산하고 공에 적절한 힘과 방향을 적용합니다.
    /// </summary>
    /// <param name="ballCollider">공의 콜라이더</param>
    /// <param name="contactPoint">충돌 지점</param>
    private void HandleBallCollision(Collider ballCollider, Vector3 contactPoint)
    {
        // TeeBall 디버깅 코드
        if (ballCollider.CompareTag("TeeBall"))
        {
            Debug.Log("티볼 충돌 감지! 위치: " + contactPoint);
        }

        // "Ball" 태그를 가진 오브젝트이고, 이미 타격한 목록에 없을 때, 또는 "TeeBall"인 경우
        if ((ballCollider.CompareTag("Ball") && !hitBalls.Contains(ballCollider)) || (ballCollider.CompareTag("TeeBall")))
        {
            // 충돌 기록에 추가
            hitBalls.Add(ballCollider);

            // 공의 Rigidbody 가져오기
            Rigidbody ballRigidbody = ballCollider.GetComponent<Rigidbody>();
            BallController ballController = ballCollider.GetComponent<BallController>();
            TeeBallController teeBallController = ballCollider.GetComponent<TeeBallController>();

            if (ballRigidbody != null)
            {
                // TeeBall 태그를 가진 경우 상태 변경
                if (ballCollider.CompareTag("TeeBall"))
                {
                    Debug.Log("티볼 상태 변경 시도 전: isKinematic=" + ballRigidbody.isKinematic + ", useGravity=" + ballRigidbody.useGravity);
                    ballRigidbody.isKinematic = false;
                    ballRigidbody.useGravity = true;
                    Debug.Log("티볼 상태 변경 후: isKinematic=" + ballRigidbody.isKinematic + ", useGravity=" + ballRigidbody.useGravity);
                }

                // 배트의 스윙 속력 및 방향 계산
                Vector3 swingVelocity = currentVelocity;
                float batSpeed = swingVelocity.magnitude;



                // === 스윗스팟 거리 계산 (개선) ===
                // 충돌 지점과 스윗스팟 간의 거리 계산
                Vector3 vectorToSweetSpot = batSweetSpot.position - contactPoint;
                float distanceToSweetSpot = vectorToSweetSpot.magnitude;

                // 개선된 스윗스팟 효과 계산
                // 품질 계수 계산
                float qualityFactor;
                if (distanceToSweetSpot <= sweetSpotRadius)
                {
                    qualityFactor = 1.0f;
                }
                else
                {
                    float adjustedDistance = (distanceToSweetSpot - sweetSpotRadius) / (sweetSpotMaxDistance - sweetSpotRadius);
                    float normalizedDistance = Mathf.Clamp01(adjustedDistance);
                    qualityFactor = Mathf.Lerp(1.0f, minQualityFactor, normalizedDistance);
                }

                // 또는 지수 감소 함수 사용 (더 현실적)
                // float qualityFactor = Mathf.Max(minQualityFactor, Mathf.Exp(-distanceToSweetSpot * sweetSpotFalloffRate));

                Debug.Log($"타격 지점과 스윗스팟 거리: {distanceToSweetSpot:F3}m, 품질 계수: {qualityFactor:F2}");

                // 타격 품질 텍스트 계산
                string qualityText = "";
                if (qualityFactor > 0.9f) qualityText = "Perfect!";
                else if (qualityFactor > 0.7f) qualityText = "Good";
                else qualityText = "Off-center";
                // === 스윗스팟 계산 끝 ===

                // 간단한 반발력 계산 - 배트 방향을 기준으로 공이 날아가도록 함
                Vector3 batDirection = swingVelocity.normalized;

                // 수직 성분 제한 (너무 위로 튀지 않도록)
                float verticalComponent = Mathf.Clamp(batDirection.y, -0.2f, 0.3f);
                batDirection.y = verticalComponent;
                batDirection = batDirection.normalized;

                // === 방향 오차 추가 (신규 추가) ===
                // 품질이 낮을수록 더 많은 오차 발생
                float directionErrorFactor = (1.0f - qualityFactor) * 0.3f; // 최대 30% 오차
                Vector3 randomError = new Vector3(
                    Random.Range(-directionErrorFactor, directionErrorFactor),
                    Random.Range(-directionErrorFactor, directionErrorFactor),
                    Random.Range(-directionErrorFactor, directionErrorFactor)
                );

                // 최종 타구 방향 계산
                Vector3 adjustedDirection = (batDirection + randomError).normalized;
                // === 방향 오차 끝 ===

                // 출구 속도 = 배트 속도 * 반발 계수 * 품질 계수
                float baseExitVelocity = batSpeed * hitForceMultiplier;
                float adjustedExitVelocity = baseExitVelocity * qualityFactor; // 품질 계수 적용

                // 속도 제한 적용
                adjustedExitVelocity = Mathf.Clamp(adjustedExitVelocity, minExitVelocity, maxExitVelocity);

                // 최종 타구 벡터 계산
                Vector3 exitVelocity = adjustedDirection * adjustedExitVelocity;

                // TeeBall인 경우 상태 재확인
                if (ballCollider.CompareTag("TeeBall"))
                {
                    Debug.Log("티볼 속도 적용 전 상태: isKinematic=" + ballRigidbody.isKinematic);
                }

                // 공에 속도 적용
                ballRigidbody.velocity = exitVelocity;

                // TeeBall인 경우 속도 확인
                if (ballCollider.CompareTag("TeeBall"))
                {
                    Debug.Log("티볼 속도 적용됨: " + ballRigidbody.velocity.magnitude);
                }

                // 회전 단순화 - 배트 방향에 수직인 축을 중심으로 회전
                Vector3 spinAxis = Vector3.Cross(Vector3.up, adjustedDirection).normalized;
                float spinRate = adjustedExitVelocity * spinFactor;
                ballRigidbody.angularVelocity = spinAxis * spinRate;

                // 마지막 스핀 정보 저장
                lastSpinAxis = spinAxis;
                lastSpinRate = spinRate;

                // 만약 BallController가 있다면 타격 상태로 설정
                if (ballController != null)
                {
                    ballController.hasCollided = true;

                    BallTrailController ballTrail = ballCollider.GetComponent<BallTrailController>();
                    if (ballTrail == null)
                    {
                        // 없으면 추가
                        ballTrail = ballCollider.gameObject.AddComponent<BallTrailController>();
                        Debug.Log("공에 BallTrailController 컴포넌트 추가됨");
                    }

                    // 트레일 활성화 (타격 품질 전달)
                    if (ballTrail != null)
                    {
                        ballTrail.ActivateTrail(qualityFactor);
                        Debug.Log($"공 트레일 활성화: 품질={qualityFactor:F2}");
                    }
                    else
                    {
                        Debug.LogWarning("공에 BallTrailController를 찾을 수 없습니다!");
                    }
                }

                if (teeBallController != null)
                {

                    BallTrailController ballTrail = teeBallController.GetComponent<BallTrailController>();
                    if (ballTrail == null)
                    {
                        // 없으면 추가
                        ballTrail = teeBallController.gameObject.AddComponent<BallTrailController>();
                        Debug.Log("공에 BallTrailController 컴포넌트 추가됨");
                    }

                    // 트레일 활성화 (타격 품질 전달)
                    if (ballTrail != null)
                    {
                        ballTrail.ActivateTrail(qualityFactor);
                        Debug.Log($"공 트레일 활성화: 품질={qualityFactor:F2}");
                    }
                    else
                    {
                        Debug.LogWarning("공에 BallTrailController를 찾을 수 없습니다!");
                    }
                }

                BatTrailFix trailFix = GetComponent<BatTrailFix>();
                if (trailFix != null)
                {
                    trailFix.ActivateTrail(qualityFactor);
                    Debug.Log($"BatTrailFix를 통해 트레일 활성화: 품질={qualityFactor}");
                }
                else
                {
                    Debug.LogWarning("BatTrailFix 컴포넌트를 찾을 수 없습니다.");
                }

                // 출구 속도 (km/h로 변환)
                float exitVelocityKMH = adjustedExitVelocity * 3.6f;

                // 타구 각도 계산 (수평 기준, 위+, 아래-)
                float launchAngle = Mathf.Atan2(exitVelocity.y,
                                               Mathf.Sqrt(exitVelocity.x * exitVelocity.x +
                                                          exitVelocity.z * exitVelocity.z)) * Mathf.Rad2Deg;

                // 단순화된 비거리 계산
                float estimatedDistance = CalculateSimpleDistance(exitVelocity);

                // 타구 정보 업데이트
                if (hitInfoDisplay != null)
                {
                    HitInfo hitInfo = new HitInfo
                    {
                        ExitVelocity = exitVelocityKMH,
                        LaunchAngle = launchAngle,
                        EstimatedDistance = estimatedDistance,
                        Direction = exitVelocity.normalized,
                        SpinRate = spinRate,
                        SpinAxis = spinAxis,
                        QualityFactor = qualityFactor, // 품질 계수 추가
                        QualityText = qualityText // 품질 텍스트 추가
                    };

                    hitInfoDisplay.DisplayHitInfo(hitInfo);
                }

                // 타격 사운드 재생
                PlayHitSound(batSpeed, qualityFactor); // 품질 계수 전달

                // 타격 데이터 로깅
                Debug.Log($"Ball hit: Speed: {exitVelocityKMH:F1} km/h, Angle: {launchAngle:F1}°, Est. Distance: {estimatedDistance:F1}m, Quality: {qualityFactor:F2} ({qualityText})");
            }
            else
            {
                // Rigidbody가 없는 경우
                Debug.LogError("충돌한 공에 Rigidbody가 없습니다! 이름: " + ballCollider.gameObject.name);
            }

            
        }

    }

    private IEnumerator DisableTrailAfterDelay(TrailRenderer trail, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (trail != null)
        {
            trail.enabled = false;
            Debug.Log("트레일 렌더러 비활성화");
        }
    }

    private void ConfigureTrailRenderer(TrailRenderer trail, float qualityFactor)
    {
        // 기존 속성 초기화
        trail.Clear(); // 이전 트레일 초기화
        trail.time = 1.0f;  // 트레일 지속 시간 (초)
        trail.minVertexDistance = 0.1f;  // 꼭지점 사이의 최소 거리
        trail.widthMultiplier = 0.1f;  // 트레일 너비

        // 머티리얼 생성 및 설정 (만약 없거나 기본 머티리얼이면)
        if (trail.material == null || trail.material.name == "Default-Material")
        {
            // 파티클 머티리얼 생성 (투명도 지원)
            Material trailMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (trailMat == null)
            {
                // 대체 셰이더 시도
                trailMat = new Material(Shader.Find("Mobile/Particles/Additive"));
            }

            if (trailMat != null)
            {
                trail.material = trailMat;
                Debug.Log("트레일 렌더러에 새 머티리얼 적용됨");
            }
        }

        // 품질에 따른 색상 설정 - 뚜렷한 색상 차이로 변경
        if (qualityFactor > 0.9f)
        {
            // 완벽한 타격 - 금색/빨간색 (매우 밝고 선명하게)
            Debug.Log("최고 품질 타격 감지: 금색/빨간색 트레일 설정");
            trail.startColor = new Color(1.0f, 0.9f, 0.0f, 1.0f);  // 밝은 금색
            trail.endColor = new Color(1.0f, 0.0f, 0.0f, 0.0f);  // 빨간색에서 투명하게
            trail.time = 1.5f;  // 더 긴 지속 시간
            trail.widthMultiplier = 0.15f;  // 더 넓은 트레일
        }
        else if (qualityFactor > 0.7f)
        {
            // 좋은 타격 - 파란색/하늘색 
            Debug.Log("중간 품질 타격 감지: 파란색/하늘색 트레일 설정");
            trail.startColor = new Color(0.0f, 0.0f, 1.0f, 1.0f);  // 선명한 파란색
            trail.endColor = new Color(0.0f, 0.7f, 1.0f, 0.0f);  // 하늘색에서 투명하게
            trail.time = 1.0f;
            trail.widthMultiplier = 0.12f;
        }
        else
        {
            // 중심에서 벗어난 타격 - 초록색/회색
            Debug.Log("낮은 품질 타격 감지: 초록색/회색 트레일 설정");
            trail.startColor = new Color(0.0f, 0.8f, 0.2f, 1.0f);  // 선명한 초록색
            trail.endColor = new Color(0.5f, 0.5f, 0.5f, 0.0f);  // 회색에서 투명하게
            trail.time = 0.8f;  // 짧은 지속 시간
            trail.widthMultiplier = 0.08f;  // 더 좁은 트레일
        }

        // 트레일 너비 변화 - 시작은 더 넓고 끝으로 갈수록 좁아짐
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0.0f, 1.0f);  // 시작 지점: 원래 너비의 100%
        widthCurve.AddKey(1.0f, 0.0f);  // 끝 지점: 원래 너비의 0%
        trail.widthCurve = widthCurve;

        // 알파값 설정 확인
        trail.colorGradient = CreateGradient(trail.startColor, trail.endColor);

        Debug.Log($"트레일 색상 설정 완료 - 시작색: {trail.startColor}, 끝색: {trail.endColor}, 품질: {qualityFactor}");
    }

    private Gradient CreateGradient(Color startColor, Color endColor)
    {
        Gradient gradient = new Gradient();

        // 그라데이션 키 설정
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0].color = new Color(startColor.r, startColor.g, startColor.b); // 알파 제외한 색상
        colorKeys[0].time = 0.0f;
        colorKeys[1].color = new Color(endColor.r, endColor.g, endColor.b); // 알파 제외한 색상
        colorKeys[1].time = 1.0f;

        // 알파 키 설정 (투명도 변화)
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0].alpha = startColor.a; // 시작 알파값
        alphaKeys[0].time = 0.0f;
        alphaKeys[1].alpha = endColor.a; // 끝 알파값
        alphaKeys[1].time = 1.0f;

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }


    /// <summary>
    /// 타격 사운드를 재생합니다. 품질 계수에 따라 사운드 특성이 조정됩니다.
    /// </summary>
    /// <param name="hitSpeed">타격 속도</param>
    /// <param name="qualityFactor">타격 품질 계수 (0.0~1.0)</param>
    private void PlayHitSound(float hitSpeed, float qualityFactor = 1.0f)
    {
        // 사운드 재생 조건 확인
        if (batAudioSource != null && hitSounds != null && hitSounds.Length > 0)
        {
            // 랜덤 사운드 선택 (여러 사운드 클립이 있을 경우 다양성 부여)
            AudioClip hitSound = hitSounds[Random.Range(0, hitSounds.Length)];

            if (hitSound != null)
            {
                // 충격 강도에 따른 볼륨 계산
                // 충격 강도를 0~1 사이로 정규화 (최소 속도 2.0, 최대 속도 30.0 기준)
                float hitIntensity = Mathf.Clamp01((hitSpeed - 2.0f) / 28.0f);

                // 품질 계수 적용 - 품질이 낮을수록 볼륨 감소
                hitIntensity *= qualityFactor;

                // 볼륨 계산 (최소 볼륨에서 최대 볼륨까지)
                float volume = Mathf.Lerp(minVolume, maxVolume, hitIntensity);

                // 피치 변화 (약한 타격은 낮은 피치, 강한 타격은 높은 피치)
                // 품질이 낮을수록 피치 감소 (둔탁한 소리)
                float qualityPitchFactor = Mathf.Lerp(0.9f, 1.0f, qualityFactor);
                float pitch = Mathf.Lerp(minPitch, maxPitch, hitIntensity) * qualityPitchFactor;

                // 사운드 설정 및 재생
                batAudioSource.pitch = pitch;
                batAudioSource.PlayOneShot(hitSound, volume);

                Debug.Log($"재생된 타격 사운드: 볼륨 {volume:F2}, 피치 {pitch:F2}, 충격 강도 {hitIntensity:F2}, 품질 {qualityFactor:F2}");
            }
        }
    }

    /// <summary>
    /// 간단한 물리 공식으로 예상 타구 거리를 계산합니다.
    /// </summary>
    /// <param name="initialVelocity">초기 타구 속도 벡터</param>
    /// <returns>예상 비거리 (미터)</returns>
    private float CalculateSimpleDistance(Vector3 initialVelocity)
    {
        // 수평 속도와 수직 속도 분리
        float vx = Mathf.Sqrt(initialVelocity.x * initialVelocity.x + initialVelocity.z * initialVelocity.z);
        float vy = initialVelocity.y;

        // 간단한 물리 공식으로 비거리 계산 (공기 저항 무시)
        // 거리 = v_x * (v_y + sqrt(v_y^2 + 2gy)) / g
        float time = (vy + Mathf.Sqrt(vy * vy + 2 * gravity * 0)) / gravity;
        float distance = vx * time;

        // 너무 작은 값이나 음수가 나오지 않도록 보정
        return Mathf.Max(5f, distance);
    }

    /// <summary>
    /// 공이 실제로 땅에 착지할 때까지 기다리고 거리를 계산합니다.
    /// </summary>
    /// <returns>코루틴</returns>
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
                // 착지 위치에서 원점까지의 수평 거리 계산
                Vector3 landingPosition = transform.position;
                Vector3 horizontalLandingPos = new Vector3(landingPosition.x, launchPosition.y, landingPosition.z);
                actualDistance = Vector3.Distance(launchPosition, horizontalLandingPos);

                hasLanded = true;

                // HitInfoDisplay에 실제 거리 업데이트
                UpdateHitInfoDisplay();

                Debug.Log($"공이 착지했습니다! 실제 비거리: {actualDistance:F1}m");
                break;
            }

            lastPosition = transform.position;
            yield return null;
        }
    }

    /// <summary>
    /// 공이 착지한 후 실제 거리로 타격 정보 디스플레이를 업데이트합니다.
    /// </summary>
    private void UpdateHitInfoDisplay()
    {
        if (hitInfoDisplay != null)
        {
            // 기존 히트 정보를 가져와서 실제 거리만 업데이트
            HitInfo currentInfo = hitInfoDisplay.GetCurrentHitInfo();
            if (currentInfo != null)
            {
                // 새 HitInfo 객체 생성하고 기존 값 복사
                HitInfo updatedInfo = new HitInfo
                {
                    ExitVelocity = currentInfo.ExitVelocity,
                    LaunchAngle = currentInfo.LaunchAngle,
                    EstimatedDistance = actualDistance, // 실제 거리로 업데이트
                    Direction = currentInfo.Direction,
                    SpinRate = currentInfo.SpinRate,
                    SpinAxis = currentInfo.SpinAxis,
                    QualityFactor = currentInfo.QualityFactor, // 품질 계수 보존
                    QualityText = currentInfo.QualityText // 품질 텍스트 보존
                };

                // DisplayHitInfo 메서드 호출
                hitInfoDisplay.DisplayHitInfo(updatedInfo);
            }
        }
    }

    /// <summary>
    /// 일정 시간마다 타격 기록을 초기화하는 코루틴입니다.
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator ClearHitBallsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(hitCooldown);
            hitBalls.Clear();
        }
    }

    /// <summary>
    /// 마지막 스핀 축을 반환합니다.
    /// </summary>
    /// <returns>스핀 축 벡터</returns>
    public Vector3 GetLastSpinAxis()
    {
        return lastSpinAxis;
    }

    /// <summary>
    /// 마지막 스핀 속도를 반환합니다.
    /// </summary>
    /// <returns>스핀 속도 (rad/s)</returns>
    public float GetLastSpinRate()
    {
        return lastSpinRate;
    }

    /// <summary>
    /// 현재 스핀 정보를 문자열로 반환합니다.
    /// </summary>
    /// <returns>스핀 정보 문자열</returns>
    public string GetSpinInfoString()
    {
        return $"스핀 축: {lastSpinAxis}, 스핀 속도: {lastSpinRate:F2}";
    }

    /// <summary>
    /// 배트의 콜라이더, 스윗스팟, 영향 범위 등을 시각화합니다.
    /// </summary>
    void OnDrawGizmos()
    {
        // 배트 콜라이더 시각화 (항상 표시)
        if (showSweetSpotGizmos)
        {
            Collider batCollider = GetComponent<Collider>();
            if (batCollider != null)
            {
                // 콜라이더 유형에 따라 다르게 시각화
                if (batCollider is BoxCollider boxCol)
                {
                    // 콜라이더 와이어프레임 표시
                    Gizmos.color = new Color(0, 1, 1, 0.7f);
                    Matrix4x4 originalMatrix = Gizmos.matrix;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCol.center, boxCol.size);

                    // 콜라이더 반투명 표면 표시
                    Gizmos.color = new Color(0, 1, 1, 0.2f);
                    Gizmos.DrawCube(boxCol.center, boxCol.size);
                    Gizmos.matrix = originalMatrix;
                }
                else if (batCollider is CapsuleCollider capCol)
                {
                    // 캡슐 콜라이더 와이어프레임 표시
                    Gizmos.color = new Color(0, 1, 1, 0.7f);

                    // 캡슐의 방향 설정 (0 = X, 1 = Y, 2 = Z)
                    Vector3 dir = Vector3.zero;
                    dir[capCol.direction] = 1f;

                    // 캡슐의 끝점 계산
                    Vector3 localPoint1 = capCol.center + dir * (capCol.height * 0.5f - capCol.radius);
                    Vector3 localPoint2 = capCol.center - dir * (capCol.height * 0.5f - capCol.radius);
                    Vector3 worldPoint1 = transform.TransformPoint(localPoint1);
                    Vector3 worldPoint2 = transform.TransformPoint(localPoint2);

                    // 캡슐 끝 구체 그리기
                    Gizmos.DrawWireSphere(worldPoint1, capCol.radius);
                    Gizmos.DrawWireSphere(worldPoint2, capCol.radius);

                    // 캡슐 중간 부분 그리기 (선으로 단순화)
                    Gizmos.DrawLine(worldPoint1, worldPoint2);

                    // 캡슐 전체를 감싸는 와이어 표시 (단순화)
                    Gizmos.DrawWireSphere(transform.TransformPoint(capCol.center), capCol.radius);
                }
                else if (batCollider is MeshCollider meshCol && meshCol.sharedMesh != null)
                {
                    // 메시 콜라이더 와이어프레임 표시
                    Gizmos.color = new Color(0, 1, 1, 0.7f);
                    Gizmos.DrawWireMesh(meshCol.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
                }
            }
        }

        // 스윗스팟 표시
        if (showSweetSpotGizmos && batSweetSpot != null)
        {
            // 스윗스팟 중심점
            Gizmos.color = sweetSpotColor;
            Gizmos.DrawSphere(batSweetSpot.position, sweetSpotRadius * 0.5f);

            // 스윗스팟 최적 영향 범위
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(batSweetSpot.position, sweetSpotRadius);

            // 스윗스팟 최대 영향 범위
            Gizmos.color = effectRangeColor;
            Gizmos.DrawWireSphere(batSweetSpot.position, sweetSpotMaxDistance);

            // 영향도에 따른 그라데이션 구역 (반투명)
            Gizmos.color = new Color(effectRangeColor.r, effectRangeColor.g, effectRangeColor.b, 0.1f);
            Gizmos.DrawSphere(batSweetSpot.position, sweetSpotMaxDistance);

            // 스윗스팟에서 배트 방향 화살표
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(batSweetSpot.position, -batSweetSpot.forward * 0.2f);
        }

        // 게임 실행 중에만 표시할 추가 정보
        if (Application.isPlaying && batSweetSpot != null)
        {
            // 배트 속도 벡터 표시
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(batSweetSpot.position, currentVelocity.normalized * 0.3f);

            // 스윙 경로 표시
            if (positionHistory != null && positionHistory.Count > 1)
            {
                Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                for (int i = 1; i < positionHistory.Count; i++)
                {
                    Gizmos.DrawLine(positionHistory[i - 1], positionHistory[i]);
                }
            }
        }
    }
}