using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeeBallController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float destroyDelay = 5.0f; // 위치 벗어난 후 파괴까지 시간
    private bool isMarkedForDestroy = false;

    [Header("거리 계산")]
    public bool hasCollided = false; // 공이 충돌했는지 여부
    public bool hasLanded = false; // 공이 착지했는지 여부
    public float actualDistance = 0f; // 실제 타구 거리
    public Vector3 launchPosition; // 타격 시작 위치
    public GameObject PlatePosition; // 홈 플레이트 위치 참조

    [Header("물리 속성")]
    [SerializeField] private float bounceDamping = 0.6f; // 바닥 충돌 시 속도 감쇠 계수 (0.0~1.0)
    [SerializeField] private float frictionDamping = 0.98f; // 지면 마찰 계수 (0.0~1.0)
    [SerializeField] private float minimumSpeed = 0.5f; // 최소 속도 (이것보다 느려지면 멈춤)
    [SerializeField] private float stoppingTime = 1.0f; // 최소 속도 이하로 유지된 후 멈출 때까지 시간
    [SerializeField] private float gravityMultiplier = 1.0f; // 중력 영향 배수
    [SerializeField] private string groundTag = "Ground"; // 지면 태그

    // 거리 계산 관련 변수
    public bool isCalculatingDistance = false; // 거리 계산 중인지 여부
    private Vector3 lastPosition; // 공의 마지막 위치
    private float groundY = 0.01f; // 지면 높이 (필요에 따라 조정)

    // 물리 관련 변수
    private float lastBounceTime = 0f; // 마지막 바운스 시간
    private float slowSpeedStartTime = 0f; // 최소 속도 이하로 떨어진 시작 시간
    private bool isStopped = false; // 공이 멈췄는지 여부
    private int bounceCount = 0; // 바운스 횟수

    void Start()
    {
        // 리지드바디 참조 가져오기
        rb = GetComponent<Rigidbody>();
        // 리지드바디가 없으면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // 태그 설정 확인
        if (!CompareTag("TeeBall"))
        {
            Debug.LogWarning("이 오브젝트의 태그가 'TeeBall'로 설정되어 있지 않습니다. 현재 태그: " + tag);
            gameObject.tag = "TeeBall";
        }

        // 초기 설정
        rb.isKinematic = false;
        rb.useGravity = false;
        Debug.Log("티볼 초기화 완료: isKinematic=" + rb.isKinematic + ", useGravity=" + rb.useGravity);

        // 홈 플레이트 위치 설정
        if (PlatePosition != null)
        {
            launchPosition = PlatePosition.transform.position;
        }
        else
        {
            launchPosition = transform.position; // 기본값은 공의 시작 위치
            Debug.LogWarning("홈 플레이트 위치가 설정되지 않았습니다. 거리 계산에 영향을 줄 수 있습니다.");
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        // 타격 후 공의 움직임이 있고 공이 멈추지 않은 경우
        if (hasCollided && !isStopped && rb != null)
        {
            float currentSpeed = rb.velocity.magnitude;

            // 최소 속도 이하로 떨어졌는지 확인
            if (currentSpeed < minimumSpeed)
            {
                // 처음으로 느려진 경우 시간 기록
                if (slowSpeedStartTime == 0)
                {
                    slowSpeedStartTime = Time.time;
                }
                // 일정 시간 이상 느린 상태 유지되면 정지
                else if (Time.time - slowSpeedStartTime > stoppingTime)
                {
                    StopBall();
                }
            }
            else
            {
                // 다시 빨라지면 타이머 리셋
                slowSpeedStartTime = 0;
            }
        }

        // 거리 계산 시작
        if (hasCollided && !hasLanded && !isCalculatingDistance)
        {
            isCalculatingDistance = true;
            StartCoroutine(CalculateActualDistance());
        }

        // 디버깅 - 타구 경로 시각화
        if (hasLanded && hasCollided)
        {
            Debug.DrawLine(launchPosition, new Vector3(transform.position.x, launchPosition.y, transform.position.z), Color.red);
        }

        // 타격된 경우 추가 중력 적용
        if (hasCollided && !isStopped && rb != null)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }

    // 공 정지시키기
    private void StopBall()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            isStopped = true;
            Debug.Log("공이 너무 느려져서 정지했습니다. 총 바운스 횟수: " + bounceCount);
        }
    }

    // 배트 스크립트와 별도로 충돌 감지
    void OnCollisionEnter(Collision collision)
    {
        // 배트와 충돌했는지 확인
        if (collision.gameObject.CompareTag("Bat"))
        {
            HandleBatHit();
        }

        // 지면과 충돌 처리
        if (collision.gameObject.CompareTag(groundTag) && hasCollided)
        {
            HandleGroundCollision(collision);
        }
    }

    // 지면 충돌 처리
    private void HandleGroundCollision(Collision collision)
    {
        // 충돌 카운트 증가
        bounceCount++;

        // 마지막 바운스 시간 기록
        lastBounceTime = Time.time;

        // 첫 충돌이면 거리 측정 및 착지 처리
        if (!hasLanded)
        {
            // 착지 위치에서 원점까지의 수평 거리 계산
            Vector3 landingPosition = transform.position;
            Vector3 horizontalLandingPos = new Vector3(landingPosition.x, launchPosition.y, landingPosition.z);
            actualDistance = Vector3.Distance(launchPosition, horizontalLandingPos);

            hasLanded = true;
            Debug.Log($"티볼이 지면과 충돌! 실제 비거리: {actualDistance:F1}m");

            // 실제 측정된 거리로 HitInfoDisplay 업데이트
            UpdateHitInfoDisplay();
        }

        // 현재 속도 가져오기
        Vector3 velocity = rb.velocity;

        // 속도 감쇠 적용
        // 수직 속도 (Y)는 보다 큰 감쇠 적용
        velocity.y *= bounceDamping;

        // 수평 속도 (X, Z)는 마찰 계수 적용
        velocity.x *= frictionDamping;
        velocity.z *= frictionDamping;

        // 감쇠된 속도 적용
        rb.velocity = velocity;

        // 회전 속도도 감쇠
        rb.angularVelocity *= frictionDamping;

        // 디버그 로그
        Debug.Log($"지면 충돌 #{bounceCount}: 감쇠 후 속도={rb.velocity.magnitude:F2}, 높이={transform.position.y:F2}");

        // 바운스가 많아지면 추가 감쇠 적용
        if (bounceCount > 3)
        {
            // 바운스 횟수가 늘어날수록 더 많이 감쇠
            float additionalDamping = Mathf.Clamp01(0.1f * (bounceCount - 3));
            rb.velocity *= (1f - additionalDamping);
        }
    }

    // 트리거 충돌도 체크
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bat"))
        {
            HandleBatHit();
        }
    }

    private void HandleBatHit()
    {
        Debug.Log("티볼: 배트와 충돌 감지됨!");

        // 직접 물리 속성 변경
        rb.isKinematic = false;
        rb.useGravity = true;

        // 충돌 상태 설정
        hasCollided = true;

        // 타격 위치 기록 (홈 플레이트가 설정되지 않은 경우)
        if (PlatePosition == null)
        {
            launchPosition = transform.position;
        }

        // 거리 계산 코루틴 시작
        if (!isCalculatingDistance)
        {
            isCalculatingDistance = true;
            StartCoroutine(CalculateActualDistance());
        }

        // 충돌/멈춤 관련 변수 초기화
        isStopped = false;
        bounceCount = 0;
        slowSpeedStartTime = 0f;

        Debug.Log("티볼: 물리 속성 변경 완료 - isKinematic=" + rb.isKinematic + ", useGravity=" + rb.useGravity);
    }

    private void OnTriggerExit(Collider other)
    {
        // 지정된 영역(TeeBallArea)을 벗어났을 때
        if (other.CompareTag("TeeBallArea") && !isMarkedForDestroy)
        {
            isMarkedForDestroy = true;
            StartCoroutine(DestroyAfterDelay());
        }
    }

    /// <summary>
    /// 공이 실제로 땅에 착지할 때까지 기다리고 거리를 계산합니다.
    /// </summary>
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

                // 실제 측정된 거리로 HitInfoDisplay 업데이트
                UpdateHitInfoDisplay();

                Debug.Log($"티볼이 착지했습니다! 실제 비거리: {actualDistance:F1}m");
                break;
            }

            lastPosition = transform.position;
            yield return null;
        }
    }

    /// <summary>
    /// HitInfoDisplay에 실제 측정된 거리 정보를 업데이트합니다.
    /// </summary>
    private void UpdateHitInfoDisplay()
    {
        HitInfoDisplay hitInfoDisplay = FindObjectOfType<HitInfoDisplay>();
        if (hitInfoDisplay != null)
        {
            // 새로운 HitInfo 객체 생성 (기존 값들은 유지)
            HitInfo newHitInfo = new HitInfo();

            // 기존에 표시되던 정보 가져오기
            HitInfo currentInfo = hitInfoDisplay.GetCurrentHitInfo();
            if (currentInfo != null)
            {
                // 기존 정보 복사
                newHitInfo.ExitVelocity = currentInfo.ExitVelocity;
                newHitInfo.LaunchAngle = currentInfo.LaunchAngle;
                newHitInfo.Direction = currentInfo.Direction;
                newHitInfo.SpinRate = currentInfo.SpinRate;
                newHitInfo.SpinAxis = currentInfo.SpinAxis;
                newHitInfo.QualityFactor = currentInfo.QualityFactor;
                newHitInfo.QualityText = currentInfo.QualityText;
            }

            // 실제 측정된 거리로 업데이트
            newHitInfo.EstimatedDistance = actualDistance;

            // 업데이트된 정보 표시
            hitInfoDisplay.DisplayHitInfo(newHitInfo);
        }
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        // 여전히 파괴 대상이면 파괴
        if (isMarkedForDestroy)
        {
            Destroy(gameObject);
        }
    }
}