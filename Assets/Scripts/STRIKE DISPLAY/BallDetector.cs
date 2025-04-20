/**
 * BallDetector.cs - 공 감지 및 스트라이크/볼 판정 컴포넌트
 * 
 * 이 스크립트는 특정 영역 내에서 공을 감지하고, 그 위치 정보를 스트라이크 존 디스플레이에 전달합니다.
 * 주요 기능:
 * - 공의 통과 감지 (트리거 콜라이더 사용)
 * - 스트라이크/볼 판정
 * - UI 표시를 위한 정보 전달
 * - 중복 감지 방지 및 쿨다운 시스템
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallDetector : MonoBehaviour
{
    [Header("참조 설정")]
    [Tooltip("스트라이크 존 디스플레이 스크립트")]
    public StrikeZoneDisplay strikeZoneDisplay;

    [Tooltip("ABS 스코어 감지기 참조")]
    public ABSScoreDetector absScoreDetector;

    [Header("감지 영역 설정")]
    [Tooltip("트리거 영역 크기 (Box Collider 사용)")]
    public Vector3 detectionBoxSize = new Vector3(5f, 5f, 0.5f);

    [Tooltip("인디케이터 쿨다운 시간 (초)")]
    [Range(0.1f, 10f)]
    public float indicatorCooldown = 3.0f;

    [Header("디버그 설정")]
    [Tooltip("디버그 모드 활성화 여부")]
    public bool debugMode = true;

    [Tooltip("로그 출력 여부")]
    public bool showLogs = true;

    [Tooltip("기즈모 표시 여부")]
    public bool showGizmos = true;

    [Tooltip("감지 영역 색상")]
    public Color detectionAreaColor = new Color(1, 0.5f, 0, 0.3f); // 반투명 주황색

    [Tooltip("감지 영역 테두리 색상")]
    public Color detectionBorderColor = new Color(1, 0.5f, 0, 0.8f); // 진한 주황색

    // 내부 상태 변수
    private HashSet<int> detectedBallIds = new HashSet<int>();  // 이미 감지된 공 목록
    private BoxCollider triggerCollider;                        // 콜라이더 컴포넌트
    private float lastIndicatorTime = -999f;                    // 마지막 인디케이터 생성 시간
    private static bool isProcessingBall = false;               // 공 처리 중 플래그

    /**
     * 초기화 메서드
     * 필요한 컴포넌트와 참조를 설정합니다.
     */
    void Awake()
    {
        // 필요한 참조 찾기
        InitializeReferences();

        // 트리거 콜라이더 설정
        SetupTriggerCollider();

        // 정리 코루틴 시작
        StartCoroutine(CleanupOldBalls());
    }

    /**
     * 필요한 참조 초기화
     */
    private void InitializeReferences()
    {
        // 스트라이크 존 디스플레이 찾기
        if (strikeZoneDisplay == null)
        {
            strikeZoneDisplay = FindObjectOfType<StrikeZoneDisplay>();
            if (strikeZoneDisplay == null)
            {
                LogError("StrikeZoneDisplay를 찾을 수 없습니다.");
                enabled = false;
                return;
            }
        }

        // ABS 스코어 감지기 찾기
        if (absScoreDetector == null)
        {
            absScoreDetector = FindObjectOfType<ABSScoreDetector>();
            if (absScoreDetector == null)
            {
                LogWarning("ABSScoreDetector를 찾을 수 없습니다. 판정이 부정확할 수 있습니다.");
            }
        }
    }

    /**
     * 디버그 로그 출력 여부 확인
     */
    private bool ShouldLog()
    {
        return debugMode && showLogs;
    }

    /**
     * 기즈모 표시 여부 확인
     */
    private bool ShouldShowGizmos()
    {
        return debugMode && showGizmos;
    }

    /**
     * 일반 로그 출력
     */
    private void Log(string message)
    {
        if (ShouldLog())
        {
            Debug.Log($"[BallDetector] {message}");
        }
    }

    /**
     * 경고 로그 출력
     */
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[BallDetector] {message}");
    }

    /**
     * 에러 로그 출력
     */
    private void LogError(string message)
    {
        Debug.LogError($"[BallDetector] {message}");
    }

    /**
     * 트리거 콜라이더 설정
     * 공 감지를 위한 영역을 설정합니다.
     */
    private void SetupTriggerCollider()
    {
        // 기존 콜라이더 확인
        triggerCollider = GetComponent<BoxCollider>();

        // 없으면 새로 생성
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
        }

        // 트리거로 설정
        triggerCollider.isTrigger = true;
        triggerCollider.size = detectionBoxSize;

        if (ShouldLog())
        {
            Log($"트리거 콜라이더 설정 완료: 크기={detectionBoxSize}");
        }
    }

    /**
     * 콜라이더가 트리거 영역에 들어왔을 때 호출되는 메서드
     * 공의 유효성을 검사하고 처리합니다.
     * 
     * @param other 진입한 콜라이더
     */
    void OnTriggerEnter(Collider other)
    {
        // 이미 다른 곳에서 처리 중인 공이라면 무시
        if (isProcessingBall)
            return;

        // 공 태그 확인
        if (other.CompareTag("Ball"))
        {
            // 쿨다운 체크: 마지막 인디케이터 생성 이후 쿨다운 시간이 지났는지 확인
            if (!CheckIndicatorCooldown())
            {
                return; // 쿨다운 중이면 무시
            }

            // 공 처리
            ProcessBall(other.gameObject);
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
        float timeElapsed = Time.time - lastIndicatorTime;

        if (timeElapsed < indicatorCooldown)
        {
            if (ShouldLog())
            {
                Log($"인디케이터 쿨다운 중: 남은 시간 {indicatorCooldown - timeElapsed:F1}초");
            }
            return false;
        }

        return true;
    }

    /**
     * 공 처리 메서드
     * 공의 유효성 검사, 중복 감지 체크, 스트라이크/볼 판정을 수행합니다.
     * 
     * @param ball 처리할 공 오브젝트
     */
    private void ProcessBall(GameObject ball)
    {
        try
        {
            // 처리 중 플래그 설정
            isProcessingBall = true;

            // 공 ID 가져오기
            int ballId = ball.GetInstanceID();

            // 이미 감지된 공인지 확인
            if (detectedBallIds.Contains(ballId))
            {
                if (ShouldLog())
                {
                    Log($"이미 감지된 공: {ballId}");
                }
                return;
            }

            // 공 컨트롤러 확인
            BallController ballController = ball.GetComponent<BallController>();
            if (ballController == null || !ballController.IsBallLaunched())
            {
                return; // 유효하지 않은 공이라면 무시
            }

            // 마지막 인디케이터 생성 시간 기록
            lastIndicatorTime = Time.time;

            // 감지된 공으로 표시
            detectedBallIds.Add(ballId);

            if (ShouldLog())
            {
                Log($"새 공 감지: {ball.name}, ID: {ballId}");
                Log($"다음 인디케이터 생성 가능 시간: {Time.time + indicatorCooldown:F1}초");
            }

            // 스트라이크 존 디스플레이에 전달
            if (strikeZoneDisplay != null)
            {
                // 스트라이크/볼 판정 수행
                bool isStrike = JudgeStrikeOrBall(ball);

                // 판정 결과와 함께 공 위치 전달
                strikeZoneDisplay.AddBallPositionWithStrikeJudgment(ball, isStrike);

                if (ShouldLog())
                {
                    Log($"스트라이크/볼 판정: {(isStrike ? "스트라이크" : "볼")}");
                }
            }
        }
        finally
        {
            // 처리 완료 플래그 해제
            isProcessingBall = false;
        }
    }

    /**
     * 스트라이크/볼 판정 메서드
     * ABS 스코어 감지기 정보를 기반으로 판정을 수행합니다.
     * 
     * @param ball 판정할 공 오브젝트
     * @return 스트라이크 여부
     */
    private bool JudgeStrikeOrBall(GameObject ball)
    {
        // ABS 스코어 감지기가 없으면 볼로 처리
        if (absScoreDetector == null)
            return false;

        // 스트라이크 존의 경계 계산
        Vector3 frontPos = absScoreDetector.frontDetector.transform.position;
        Vector3 backPos = absScoreDetector.backDetector.transform.position;
        Vector3 strikeZoneCenter = (frontPos + backPos) / 2f;

        float minX = strikeZoneCenter.x - absScoreDetector.strikeZoneWidth / 2;
        float maxX = strikeZoneCenter.x + absScoreDetector.strikeZoneWidth / 2;
        float minY = strikeZoneCenter.y - absScoreDetector.strikeZoneHeight / 2;
        float maxY = strikeZoneCenter.y + absScoreDetector.strikeZoneHeight / 2;

        // 공 위치가 스트라이크 존 안에 있는지 확인
        Vector3 ballPosition = ball.transform.position;
        bool isInStrikeZone = (ballPosition.x >= minX && ballPosition.x <= maxX &&
                               ballPosition.y >= minY && ballPosition.y <= maxY);

        if (ShouldLog())
        {
            Log($"판정 상세: 공 위치={ballPosition}, 스트라이크 존=X({minX:F2}~{maxX:F2}), Y({minY:F2}~{maxY:F2})");
        }

        return isInStrikeZone;
    }

    /**
     * 기즈모 그리기
     * 에디터에서 감지 영역을 시각화합니다.
     * 디버그 모드가 켜져 있을 때만 표시됩니다.
     */
    void OnDrawGizmos()
    {
        if (!ShouldShowGizmos())
            return;

        // 감지 영역 표시
        Gizmos.color = detectionAreaColor;
        Gizmos.DrawCube(transform.position, detectionBoxSize);

        // 감지 영역 윤곽선
        Gizmos.color = detectionBorderColor;
        Gizmos.DrawWireCube(transform.position, detectionBoxSize);

        // 라벨 표시
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * detectionBoxSize.y * 0.5f, "볼 감지 영역");
#endif
    }

    /**
     * 오래된 공 ID 정리 코루틴
     * 메모리 관리를 위해 주기적으로 감지된 공 목록을 비웁니다.
     */
    private IEnumerator CleanupOldBalls()
    {
        while (true)
        {
            // 30초마다 정리
            yield return new WaitForSeconds(30f);

            int countBefore = detectedBallIds.Count;
            detectedBallIds.Clear();

            if (ShouldLog())
            {
                Log($"감지된 공 목록 정리: {countBefore}개 제거");
            }
        }
    }

    /**
     * 인스펙터에서 값이 변경될 때 호출
     * 콜라이더 크기가 변경되면 트리거 콜라이더도 업데이트합니다.
     */
    private void OnValidate()
    {
        // 런타임에서 실행 중일 때만 콜라이더 크기 업데이트
        if (Application.isPlaying && triggerCollider != null)
        {
            triggerCollider.size = detectionBoxSize;
        }
    }
}