/**
 * ABSIndicator.cs - 자동 심판 시스템(ABS) 인디케이터 컨트롤러
 * 
 * 이 스크립트는 공이 스트라이크 존을 통과할 때 위치를 표시하는 시각적 인디케이터를 관리합니다.
 * 주요 기능:
 * - 공과의 충돌 감지 (Collision/Trigger)
 * - 공 통과 위치에 시각적 인디케이터 생성
 * - 스코어 감지기에 통과 이벤트 전달
 * - 디버깅용 시각화 도구 제공
 */

using UnityEngine;
using System.Collections;

public class ABSIndicator : MonoBehaviour
{
    [Header("인디케이터 설정")]
    [Tooltip("공 위치를 표시할 인디케이터 프리팹")]
    public GameObject ballIndicatorPrefab;

    [Tooltip("인디케이터 생존 시간(초)")]
    [Range(1.0f, 10.0f)]
    public float indicatorLifetime = 4.0f;

    [Tooltip("인디케이터 생성 쿨다운 시간(초)")]
    [Range(0.1f, 5.0f)]
    public float indicatorCooldown = 3.0f;

    [Header("ABS 설정")]
    [Tooltip("이 ABS의 타입 (Front 또는 Back)")]
    public ABSType absType = ABSType.Front;

    [Header("디버그 설정")]
    [Tooltip("디버그 모드 활성화 여부")]
    public bool debugMode = true;

    [Tooltip("충돌 로그 출력 여부")]
    public bool logCollisions = false;

    [Tooltip("인디케이터 로그 출력 여부")]
    public bool logIndicators = true;

    [Tooltip("기즈모 표시 여부")]
    public bool showGizmos = true;

    [Tooltip("충돌 지점 표시 여부")]
    public bool showContactPoints = true;

    [Tooltip("충돌 지점 표시 지속 시간(초)")]
    [Range(0.1f, 10.0f)]
    public float contactPointDuration = 5.0f;

    [Tooltip("충돌 지점 기즈모 크기")]
    [Range(0.01f, 0.5f)]
    public float contactPointSize = 0.1f;

    [Tooltip("충돌 지점 컬러 (Front)")]
    public Color frontContactColor = new Color(1f, 0.5f, 0f, 0.8f); // 주황색

    [Tooltip("충돌 지점 컬러 (Back)")]
    public Color backContactColor = new Color(0f, 0.8f, 0.8f, 0.8f); // 청록색

    // 이 ABS가 Front인지 Back인지 구분하는 열거형
    public enum ABSType { Front, Back }

    // 점수 감지기 참조 - 런타임에 자동으로 할당됨
    [HideInInspector]
    public ABSScoreDetector scoreDetector;

    // 내부 상태 변수
    private float lastIndicatorTime = -999f;  // 마지막 인디케이터 생성 시간
    private Color contactColor;               // 현재 ABS 타입에 따른 접촉점 색상

    /**
     * 초기화 메서드
     * 컴포넌트가 활성화될 때 실행되며, 초기 설정을 수행합니다.
     */
    private void Start()
    {
        // 스코어 감지기 찾기
        InitializeScoreDetector();

        // 접촉점 색상 결정
        contactColor = (absType == ABSType.Front) ? frontContactColor : backContactColor;

        if (ShouldLogIndicators())
        {
            Debug.Log($"ABSIndicator 초기화 완료: 타입={absType}, 위치={transform.position}");
        }

        // 프리팹 유효성 검사
        ValidateIndicatorPrefab();
    }

    /**
     * 로그 출력 여부 확인 - 충돌 로그
     */
    private bool ShouldLogCollisions()
    {
        return debugMode && logCollisions;
    }

    /**
     * 로그 출력 여부 확인 - 인디케이터 로그
     */
    private bool ShouldLogIndicators()
    {
        return debugMode && logIndicators;
    }

    /**
     * 기즈모 표시 여부 확인
     */
    private bool ShouldShowGizmos()
    {
        return debugMode && showGizmos;
    }

    /**
     * 접촉점 표시 여부 확인
     */
    private bool ShouldShowContactPoints()
    {
        return debugMode && showContactPoints;
    }

    /**
     * 스코어 감지기 찾기
     */
    private void InitializeScoreDetector()
    {
        if (scoreDetector == null)
        {
            scoreDetector = FindObjectOfType<ABSScoreDetector>();

            if (scoreDetector == null && debugMode)
            {
                Debug.LogWarning("씬에서 ABSScoreDetector를 찾을 수 없습니다!");
            }
        }
    }

    /**
     * 인디케이터 프리팹 유효성 검사
     */
    private void ValidateIndicatorPrefab()
    {
        if (ballIndicatorPrefab == null)
        {
            Debug.LogError("Ball_Indicator 프리팹이 할당되지 않았습니다!");
        }
    }

    /**
     * 충돌 감지 메서드 - 일반 콜라이더
     * 물리 엔진에 의해 호출되며, 공이 ABS 평면을 통과할 때 충돌을 감지합니다.
     * 
     * @param collision 충돌 정보
     */
    private void OnCollisionEnter(Collision collision)
    {
        if (ShouldLogCollisions())
        {
            Debug.Log($"충돌 감지됨: {collision.gameObject.name}, 태그: {collision.gameObject.tag}");
        }

        // 'Ball' 태그를 가진 오브젝트와 충돌했는지 확인
        if (collision.gameObject.CompareTag("Ball"))
        {
            // 볼 상태 확인
            if (!ValidateBall(collision.gameObject))
                return;

            // 충돌 위치 구하기 (첫 번째 접촉점 사용)
            Vector3 contactPoint = collision.contacts[0].point;

            // 충돌 위치 시각화 (디버그 모드일 때)
            VisualizeContactPoint(contactPoint);

            // 인디케이터 생성
            CreateIndicator(contactPoint);

            // 점수 감지기에 볼 통과 알림
            NotifyBallPassed(collision.gameObject);
        }
    }

    /**
     * 대체 충돌 감지 메서드 - 트리거 콜라이더
     * 트리거 콜라이더를 사용할 때 호출되며, 공이 ABS 평면을 통과할 때 감지합니다.
     * 
     * @param other 진입한 콜라이더
     */
    private void OnTriggerEnter(Collider other)
    {
        if (ShouldLogCollisions())
        {
            Debug.Log($"트리거 진입 감지됨: {other.gameObject.name}, 태그: {other.gameObject.tag}");
        }

        // 'Ball' 태그를 가진 오브젝트가 트리거에 들어왔는지 확인
        if (other.CompareTag("Ball"))
        {
            // 볼 상태 확인
            if (!ValidateBall(other.gameObject))
                return;

            // 공의 위치를 ABS 평면에 투영
            Vector3 projectedPosition = ProjectBallPositionToABSPlane(other.transform.position);

            // 접촉 위치 시각화 (디버그 모드일 때)
            VisualizeContactPoint(projectedPosition);

            // 인디케이터 생성
            CreateIndicator(projectedPosition);

            // 점수 감지기에 볼 통과 알림
            NotifyBallPassed(other.gameObject);
        }
    }

    /**
     * 볼 상태 확인
     * 발사된 상태인지 확인하고 BallController 컴포넌트가 있는지 확인합니다.
     * 
     * @param ball 확인할 볼 게임오브젝트
     * @return 유효한 볼인지 여부
     */
    private bool ValidateBall(GameObject ball)
    {
        BallController ballController = ball.GetComponent<BallController>();
        if (ballController == null || !ballController.IsBallLaunched())
        {
            if (ShouldLogCollisions())
            {
                Debug.Log("유효하지 않은 공 감지: BallController가 없거나 발사되지 않음");
            }
            return false;
        }
        return true;
    }

    /**
     * 공 위치를 ABS 평면에 투영
     * 공의 3D 위치를 ABS 평면(Z축 고정)에 투영합니다.
     * 
     * @param ballPosition 공의 현재 위치
     * @return ABS 평면에 투영된 위치
     */
    private Vector3 ProjectBallPositionToABSPlane(Vector3 ballPosition)
    {
        Vector3 projectedPosition = new Vector3(
            ballPosition.x,
            ballPosition.y,
            transform.position.z
        );

        if (ShouldLogCollisions())
        {
            Debug.Log($"공 위치 투영: 원래={ballPosition}, 투영={projectedPosition}");
        }

        return projectedPosition;
    }

    /**
     * 접촉 위치 시각화
     * 디버그 모드에서 접촉 위치를 시각적으로 표시합니다.
     * 
     * @param position 시각화할 위치
     */
    private void VisualizeContactPoint(Vector3 position)
    {
        if (ShouldShowContactPoints())
        {
            // Debug.DrawLine을 사용하여 접촉 위치에 선 그리기
            Debug.DrawLine(
                position,
                position + Vector3.up * 0.5f,
                contactColor,
                contactPointDuration
            );

            if (ShouldLogCollisions())
            {
                Debug.Log($"접촉 위치 시각화: {position}, 타입: {absType}");
            }

            // 디버그 구체를 그리는 코루틴 시작
            StartCoroutine(DrawDebugSphere(position, contactPointSize, contactColor, contactPointDuration));
        }
    }

    /**
     * 디버그 구체 그리기 코루틴
     * 지정된 위치에 일정 시간 동안 디버그 구체를 그립니다.
     * 디버그 모드가 꺼지면 즉시 그리기 중단합니다.
     * 
     * @param position 구체 위치
     * @param radius 구체 반지름
     * @param color 구체 색상
     * @param duration 지속 시간
     */
    private IEnumerator DrawDebugSphere(Vector3 position, float radius, Color color, float duration)
    {
        float endTime = Time.time + duration;

        while (Time.time < endTime && ShouldShowContactPoints())
        {
            // OnDrawGizmos에서는 그릴 수 없으므로 별도의 방법 사용
            Debug.DrawLine(position + Vector3.up * radius, position - Vector3.up * radius, color);
            Debug.DrawLine(position + Vector3.right * radius, position - Vector3.right * radius, color);
            Debug.DrawLine(position + Vector3.forward * radius, position - Vector3.forward * radius, color);

            yield return new WaitForSeconds(0.01f);
        }
    }

    /**
     * 점수 감지기에 볼 통과 알림
     * ABS 타입에 따라 적절한 메서드를 호출하여 점수 감지기에 알립니다.
     * 
     * @param ball 통과한 볼 오브젝트
     */
    private void NotifyBallPassed(GameObject ball)
    {
        // scoreDetector가 없다면 씬에서 찾아보기
        if (scoreDetector == null)
        {
            InitializeScoreDetector();

            if (scoreDetector == null)
            {
                return; // 이미 경고 메시지가 출력되었으므로 여기서는 조용히 반환
            }
        }

        // Front 또는 Back에 따라 적절한 메서드 호출
        if (absType == ABSType.Front)
        {
            scoreDetector.OnBallPassedFront(ball);

            if (ShouldLogCollisions())
            {
                Debug.Log("Front ABS 통과 감지됨");
            }
        }
        else
        {
            scoreDetector.OnBallPassedBack(ball);

            if (ShouldLogCollisions())
            {
                Debug.Log("Back ABS 통과 감지됨");
            }
        }
    }

    /**
     * 인디케이터 생성 메서드
     * 지정된 위치에 인디케이터를 생성합니다.
     * 
     * @param position 인디케이터 생성 위치
     */
    private void CreateIndicator(Vector3 position)
    {
        // 프리팹이 할당되었는지 확인
        if (ballIndicatorPrefab == null)
        {
            Debug.LogError("Ball_Indicator 프리팹이 할당되지 않았습니다!");
            return;
        }

        // 쿨다운 체크: 마지막 인디케이터 생성 이후 쿨다운 시간이 지났는지 확인
        if (!CheckIndicatorCooldown())
        {
            return; // 쿨다운 중이면 인디케이터 생성하지 않고 함수 종료
        }

        try
        {
            // 인디케이터 생성
            GameObject indicator = Instantiate(
                ballIndicatorPrefab,
                position,
                Quaternion.identity
            );

            // 인디케이터 이름 지정
            string typeSuffix = (absType == ABSType.Front) ? "Front" : "Back";
            indicator.name = $"BallIndicator_{typeSuffix}_{Time.time:F2}";

            // 마지막 인디케이터 생성 시간 기록
            lastIndicatorTime = Time.time;

            if (ShouldLogIndicators())
            {
                Debug.Log($"인디케이터 생성됨: {indicator.name}, 위치: {position}");
                Debug.Log($"다음 인디케이터 생성 가능 시간: {Time.time + indicatorCooldown:F1}초");
            }

            // 인디케이터 일정 시간 후 제거
            Destroy(indicator, indicatorLifetime);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"인디케이터 생성 실패: {e.Message}");
        }
    }

    /**
     * 인디케이터 쿨다운 체크
     * 마지막 인디케이터 생성 이후 쿨다운 시간이 지났는지 확인합니다.
     * 
     * @return 인디케이터 생성 가능 여부
     */
    private bool CheckIndicatorCooldown()
    {
        // 마지막 인디케이터 생성 이후 쿨다운 시간이 지났는지 확인
        float timeElapsed = Time.time - lastIndicatorTime;

        if (timeElapsed < indicatorCooldown)
        {
            if (ShouldLogIndicators())
            {
                Debug.Log($"인디케이터 쿨다운 중: 남은 시간 {indicatorCooldown - timeElapsed:F1}초");
            }
            return false;
        }

        return true;
    }

    /**
     * 시각적 디버깅을 위한 기즈모 그리기
     * 에디터에서 ABS의 위치와 타입을 시각적으로 표시합니다.
     * 디버그 모드가 켜져 있을 때만 표시됩니다.
     */
    private void OnDrawGizmos()
    {
        if (!ShouldShowGizmos())
            return;

        // ABS 타입에 따라 색상 설정
        Color gizmoColor = (absType == ABSType.Front) ?
            new Color(1f, 0.8f, 0f, 0.8f) :  // 진한 노랑색 (Front)
            new Color(0f, 0.8f, 1f, 0.8f);   // 진한 청록색 (Back)

        Gizmos.color = gizmoColor;

        // 기즈모 크기
        float size = 0.2f;
        float boxSize = 0.4f; // 박스 크기

        // 십자가 그리기
        // 가로 선
        Gizmos.DrawLine(
            transform.position - new Vector3(size, 0, 0),
            transform.position + new Vector3(size, 0, 0)
        );

        // 세로 선
        Gizmos.DrawLine(
            transform.position - new Vector3(0, size, 0),
            transform.position + new Vector3(0, size, 0)
        );

        // Z축 선
        Gizmos.DrawLine(
            transform.position - new Vector3(0, 0, size * 0.5f),
            transform.position + new Vector3(0, 0, size * 0.5f)
        );

        // 반투명 박스 그리기
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        Gizmos.DrawCube(transform.position, new Vector3(boxSize, boxSize, 0.01f));

        // ABS 타입 텍스트 표시 (디버그 모드에서만)
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f,
            $"{absType} ABS\n{transform.position.z:F2}m");
#endif
    }
}