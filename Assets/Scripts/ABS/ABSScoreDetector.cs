/**
 * ABSScoreDetector.cs - 자동 심판 시스템(ABS) 점수 감지기
 * 
 * 이 스크립트는 두 개의 감지기(Front와 Back)를 사용하여 공이 스트라이크 존을 통과하는지
 * 판정하는 역할을 담당합니다. 스트라이크 존의 크기를 설정하고 시각화하며,
 * 공의 통과 여부를 추적하여 최종 점수(스트라이크/볼)를 판정합니다.
 * 
 * 주요 기능:
 * - Front/Back 감지기 관리
 * - 스트라이크 존 크기 설정 및 시각화
 * - 공 통과 추적 및 판정
 * - 타임아웃 기반 안전장치 제공
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABSScoreDetector : MonoBehaviour
{
    [Header("감지기 설정")]
    [Tooltip("전면(Front) 감지기")]
    public ABSIndicator frontDetector;

    [Tooltip("후면(Back) 감지기")]
    public ABSIndicator backDetector;

    [Header("스트라이크 존 설정")]
    [Tooltip("스트라이크 존 너비 (미터)")]
    [Range(0.1f, 2.0f)]
    public float strikeZoneWidth = 1.0f;

    [Tooltip("스트라이크 존 높이 (미터)")]
    [Range(0.1f, 2.0f)]
    public float strikeZoneHeight = 1.0f;

    [Header("디버그 설정")]
    [Tooltip("디버그 모드 활성화 여부")]
    public bool debugMode = true;

    [Tooltip("콘솔 로그 출력 여부")]
    public bool logEvents = true;

    [Tooltip("기즈모 표시 여부")]
    public bool showGizmos = true;

    [Tooltip("스트라이크 존 시각화 색상")]
    public Color strikeZoneColor = new Color(0, 1, 0, 0.2f); // 투명한 초록색

    [Tooltip("스트라이크 경계선 색상")]
    public Color strikeBorderColor = Color.white;

    [Tooltip("감지기 연결선 색상")]
    public Color detectorConnectionColor = Color.yellow;

    [Tooltip("스트라이크 로그 색상")]
    public string strikeLogColor = "#FF0000"; // 빨간색

    [Tooltip("볼 로그 색상")]
    public string ballLogColor = "#0000FF"; // 파란색

    [Tooltip("히트 로그 색상")]
    public string hitLogColor = "#FF00FF"; // 마젠타

    [Header("타임아웃 설정")]
    [Tooltip("판정 타임아웃 시간 (초)")]
    [Range(1.0f, 20.0f)]
    public float judgmentTimeout = 10.0f;

    // 볼 통과 상태 추적 (Key: 볼 오브젝트, Value: [0]=Front 통과, [1]=Back 통과)
    private Dictionary<GameObject, bool[]> ballPassTracking = new Dictionary<GameObject, bool[]>();

    // 볼 판정 상태 열거형
    public enum BallJudgment { None, Strike, Ball, Hit }

    /**
     * 초기화 메서드
     * 컴포넌트가 활성화될 때 실행되며, 감지기 설정 및 초기화를 수행합니다.
     */
    private void Start()
    {
        // ABS 오브젝트가 할당되었는지 확인
        ValidateDetectors();

        // 감지기 초기화
        InitializeDetectors();

        // 이미 존재하는 공들에 대해 스코어 감지기 등록
        RegisterToAllBalls();

        if (ShouldLog())
        {
            Debug.Log("ABSScoreDetector 초기화 완료");
        }
    }

    /**
     * 디버그 로그 출력 여부 확인
     */
    private bool ShouldLog()
    {
        return debugMode && logEvents;
    }

    /**
     * 기즈모 표시 여부 확인
     */
    private bool ShouldShowGizmos()
    {
        return debugMode && showGizmos;
    }

    /**
     * 감지기 유효성 검사
     * Front와 Back 감지기가 모두 할당되었는지 확인합니다.
     */
    private void ValidateDetectors()
    {
        if (frontDetector == null || backDetector == null)
        {
            Debug.LogError("Front 또는 Back ABSIndicator가 할당되지 않았습니다!");
        }
    }

    /**
     * 감지기 초기화
     * 필요한 컴포넌트를 추가하고 참조를 설정합니다.
     */
    private void InitializeDetectors()
    {
        if (frontDetector != null)
        {
            // 필요한 컴포넌트 추가
            AddRequiredComponents(frontDetector.gameObject);

            // ScoreDetector 참조 설정
            frontDetector.scoreDetector = this;

            // 디버그 모드 설정 전파
            if (frontDetector.debugMode != debugMode)
            {
                frontDetector.debugMode = debugMode;
            }

            if (ShouldLog())
            {
                Debug.Log($"Front 감지기 초기화: {frontDetector.name}");
            }
        }

        if (backDetector != null)
        {
            // 필요한 컴포넌트 추가
            AddRequiredComponents(backDetector.gameObject);

            // ScoreDetector 참조 설정
            backDetector.scoreDetector = this;

            // 디버그 모드 설정 전파
            if (backDetector.debugMode != debugMode)
            {
                backDetector.debugMode = debugMode;
            }

            if (ShouldLog())
            {
                Debug.Log($"Back 감지기 초기화: {backDetector.name}");
            }
        }
    }

    /**
     * 모든 공에 스코어 감지기 등록
     * 씬에 이미 존재하는 모든 공에 대해 이 감지기를 등록합니다.
     */
    private void RegisterToAllBalls()
    {
        // 기존 공들에 ScoreDetector 연결
        BallController[] existingBalls = FindObjectsOfType<BallController>();
        foreach (BallController ball in existingBalls)
        {
            RegisterBall(ball.gameObject);
        }

        if (ShouldLog() && existingBalls.Length > 0)
        {
            Debug.Log($"기존 공 {existingBalls.Length}개에 ScoreDetector 등록 완료");
        }
    }

    /**
     * 필요한 컴포넌트 추가
     * 트리거 이벤트를 사용하기 위해 필요한 컴포넌트를 확인하고 추가합니다.
     * 콜라이더의 크기는 변경하지 않습니다.
     * 
     * @param detector 감지기 오브젝트
     */
    private void AddRequiredComponents(GameObject detector)
    {
        // 콜라이더가 없는 경우 추가 (크기는 기본값 사용)
        if (!detector.GetComponent<Collider>())
        {
            if (ShouldLog())
            {
                Debug.LogWarning($"{detector.name}에 Collider가 없습니다. BoxCollider를 추가합니다.");
            }

            BoxCollider collider = detector.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            // 기본 크기는 유지하고 트리거만 활성화
        }
        // 콜라이더가 트리거로 설정되지 않은 경우 트리거로 변경
        else if (!detector.GetComponent<Collider>().isTrigger)
        {
            if (ShouldLog())
            {
                Debug.LogWarning($"{detector.name}의 Collider가 트리거로 설정되어 있지 않습니다. 트리거로 변경합니다.");
            }

            detector.GetComponent<Collider>().isTrigger = true;
            // 트리거 속성만 변경하고 크기는 그대로 유지
        }
    }

    /**
     * 업데이트 메서드
     * 더 이상 존재하지 않는 공에 대한 추적 정보를 정리하고 디버그 모드 상태를 관리합니다.
     */
    private void Update()
    {
        // 더 이상 필요 없는 볼 추적 정보 정리
        CleanupBallTracking();

        // 디버그 모드 변경 시 감지기에도 적용
        SyncDebugModeToDetectors();
    }

    /**
     * 디버그 모드 상태를 감지기에 동기화
     */
    private void SyncDebugModeToDetectors()
    {
        if (frontDetector != null && frontDetector.debugMode != debugMode)
        {
            frontDetector.debugMode = debugMode;
        }

        if (backDetector != null && backDetector.debugMode != debugMode)
        {
            backDetector.debugMode = debugMode;
        }
    }

    /**
     * 볼 추적 정보 정리
     * 더 이상 존재하지 않는 공에 대한 추적 정보를 제거합니다.
     */
    private void CleanupBallTracking()
    {
        List<GameObject> ballsToRemove = new List<GameObject>();

        foreach (var ball in ballPassTracking.Keys)
        {
            if (ball == null)
            {
                ballsToRemove.Add(ball);
            }
        }

        foreach (var ball in ballsToRemove)
        {
            ballPassTracking.Remove(ball);
        }

        if (ShouldLog() && ballsToRemove.Count > 0)
        {
            Debug.Log($"추적 정보 정리: {ballsToRemove.Count}개 항목 제거");
        }
    }

    /**
     * 새 공 등록 메서드
     * 외부에서 호출하여 새로 생성된 공에 이 감지기를 등록합니다.
     * 
     * @param ball 등록할 공 오브젝트
     */
    public void RegisterBall(GameObject ball)
    {
        if (ball != null)
        {
            ball.SendMessage("SetScoreDetector", this, SendMessageOptions.DontRequireReceiver);

            if (ShouldLog())
            {
                Debug.Log($"새 공 등록: {ball.name}");
            }
        }
    }

    /**
     * 공이 Front ABS를 통과했을 때 호출
     * Front 감지기에서 감지된 공의 통과 정보를 기록합니다.
     * 
     * @param ball 통과한 공 오브젝트
     */
    public void OnBallPassedFront(GameObject ball)
    {
        // 공이 유효한지 확인
        if (!IsValidBall(ball))
            return;

        // 아직 추적 중이 아니면 추적 시작
        if (!ballPassTracking.ContainsKey(ball))
        {
            StartBallTracking(ball);
        }

        // Front 통과 기록
        ballPassTracking[ball][0] = true;

        if (ShouldLog())
        {
            Debug.Log($"볼 '{ball.name}'이(가) Front ABS를 통과했습니다.");
        }
    }

    /**
     * 공이 Back ABS를 통과했을 때 호출
     * Back 감지기에서 감지된 공의 통과 정보를 기록합니다.
     * 
     * @param ball 통과한 공 오브젝트
     */
    public void OnBallPassedBack(GameObject ball)
    {
        // 공이 유효한지 확인
        if (!IsValidBall(ball))
            return;

        // 아직 추적 중이 아니면 추적 시작
        if (!ballPassTracking.ContainsKey(ball))
        {
            StartBallTracking(ball);
        }

        // Back 통과 기록
        ballPassTracking[ball][1] = true;

        if (ShouldLog())
        {
            Debug.Log($"볼 '{ball.name}'이(가) Back ABS를 통과했습니다.");
        }
    }

    /**
     * 공이 유효한지 확인
     * 공이 null이 아니고 발사된 상태인지 확인합니다.
     * 
     * @param ball 확인할 공 오브젝트
     * @return 유효한 공인지 여부
     */
    private bool IsValidBall(GameObject ball)
    {
        // 공이 유효한지 확인
        if (ball == null)
            return false;

        // 공이 발사된 상태인지 확인
        BallController ballController = ball.GetComponent<BallController>();
        if (ballController == null || !ballController.IsBallLaunched())
            return false;

        return true;
    }

    /**
     * 공 추적 시작
     * 공의 통과 정보 추적을 시작하고 타임아웃 코루틴을 시작합니다.
     * 
     * @param ball 추적할 공 오브젝트
     */
    private void StartBallTracking(GameObject ball)
    {
        // [0] = Front 통과, [1] = Back 통과
        ballPassTracking[ball] = new bool[2] { false, false };

        // 처음 ABS를 통과했을 때 안전 타임아웃 시작
        StartCoroutine(JudgmentTimeoutCoroutine(ball, judgmentTimeout));

        if (ShouldLog())
        {
            Debug.Log($"공 추적 시작: {ball.name}, 타임아웃: {judgmentTimeout}초");
        }
    }

    /**
     * 공이 배트와 충돌했을 때 호출
     * 타격된 공의 추적 정보를 초기화합니다.
     * 
     * @param ball 타격된 공 오브젝트
     */
    public void OnBallHit(GameObject ball)
    {
        // 추적 정보 초기화 (Hit는 다른 모든 판정보다 우선)
        ResetBallTracking(ball);

        if (ShouldLog())
        {
            Debug.Log($"<color={hitLogColor}>Hit</color> - 공 '{ball.name}'이(가) 타격되었습니다.");
        }
    }

    /**
     * 최종 점수 확인
     * 공의 통과 정보를 기반으로 최종 점수(스트라이크/볼)를 판정합니다.
     * 
     * @param ball 판정할 공 오브젝트
     */
    public void CheckFinalScore(GameObject ball)
    {
        if (ballPassTracking.TryGetValue(ball, out bool[] passStatus))
        {
            bool frontPassed = passStatus[0];
            bool backPassed = passStatus[1];

            // 두 ABS를 모두 통과한 경우 스트라이크
            if (frontPassed && backPassed)
            {
                if (debugMode)
                {
                    Debug.Log($"<color={strikeLogColor}>Strike</color> - 공 '{ball.name}'이(가) 스트라이크 존을 완전히 통과했습니다.");
                }
            }
            // 하나만 통과했거나 둘 다 통과하지 않은 경우 볼
            else
            {
                if (debugMode)
                {
                    Debug.Log($"<color={ballLogColor}>Ball</color> - 공 '{ball.name}'이(가) 스트라이크 존을 완전히 통과하지 않았습니다. (Front: {frontPassed}, Back: {backPassed})");
                }
            }

            // 추적 정보 초기화
            ResetBallTracking(ball);
        }
        else
        {
            // 공이 발사되었는지 확인
            BallController ballController = ball.GetComponent<BallController>();
            if (ballController != null && ballController.IsBallLaunched())
            {
                // 발사된 공만 Ball 판정
                if (debugMode)
                {
                    Debug.Log($"<color={ballLogColor}>Ball</color> - 공 '{ball.name}'이(가) ABS를 통과하지 않았습니다.");
                }
            }
        }
    }

    /**
     * 공 추적 정보 초기화
     * 더 이상 추적이 필요 없는 공의 추적 정보를 제거합니다.
     * 
     * @param ball 초기화할 공 오브젝트
     */
    private void ResetBallTracking(GameObject ball)
    {
        if (ballPassTracking.ContainsKey(ball))
        {
            ballPassTracking.Remove(ball);

            if (ShouldLog())
            {
                Debug.Log($"공 추적 초기화: {ball.name}");
            }
        }
    }

    /**
     * 안전장치: 오랜동안 충돌이 없으면 판정하는 코루틴
     * 공이 시간 내에 판정되지 않으면 자동으로 판정합니다.
     * 
     * @param ball 판정할 공 오브젝트
     * @param timeout 대기 시간 (초)
     */
    private IEnumerator JudgmentTimeoutCoroutine(GameObject ball, float timeout)
    {
        yield return new WaitForSeconds(timeout);

        // 시간이 지났지만 아직 추적 중인 볼이라면
        if (ball != null && ballPassTracking.ContainsKey(ball))
        {
            BallController ballController = ball.GetComponent<BallController>();

            // 아직 충돌하지 않은 경우에만 처리
            if (ballController != null && !ballController.hasCollided)
            {
                if (ShouldLog())
                {
                    Debug.Log($"타임아웃 발생: 공 '{ball.name}'이(가) {timeout}초 동안 충돌하지 않았습니다.");
                }

                CheckFinalScore(ball);

                // hasCollided 플래그 설정
                ballController.hasCollided = true;
            }
        }
    }

    /**
     * 기즈모 그리기
     * 스트라이크 존과 감지기 연결을 시각화합니다.
     * 디버그 모드가 활성화된 경우에만 표시됩니다.
     */
    private void OnDrawGizmos()
    {
        if (!ShouldShowGizmos())
            return;

        if (frontDetector != null && backDetector != null)
        {
            // 스트라이크 존 시각화
            DrawStrikeZone(strikeZoneColor, false);
        }
    }

    /**
     * 선택 시 기즈모 그리기
     * 오브젝트가 선택되었을 때 더 상세한 시각화를 제공합니다.
     * 디버그 모드가 활성화된 경우에만 표시됩니다.
     */
    private void OnDrawGizmosSelected()
    {
        if (!ShouldShowGizmos())
            return;

        if (frontDetector != null && backDetector != null)
        {
            // 선택됐을 때는 더 뚜렷한 경계선 표시
            DrawStrikeZone(strikeZoneColor, true);

            // 추가적인 정보 표시 (라벨)
            Vector3 center = GetStrikeZoneCenter();

#if UNITY_EDITOR
            // 감지기 설정 정보 표시
            UnityEditor.Handles.Label(
                center + Vector3.up * (strikeZoneHeight / 2 + 0.2f),
                $"스트라이크 존 설정\n너비: {strikeZoneWidth:F2}m, 높이: {strikeZoneHeight:F2}m"
            );
#endif
        }
    }

    /**
     * 스트라이크 존 그리기
     * 감지기 사이의 공간을 스트라이크 존으로 시각화합니다.
     * 
     * @param fillColor 채우기 색상
     * @param drawWireframe 와이어프레임 그리기 여부
     */
    private void DrawStrikeZone(Color fillColor, bool drawWireframe)
    {
        Vector3 frontPos = frontDetector.transform.position;
        Vector3 backPos = backDetector.transform.position;

        // 두 ABS 사이의 거리
        float distance = Vector3.Distance(frontPos, backPos);

        // 스트라이크 존 크기 설정
        Vector3 size = new Vector3(strikeZoneWidth, strikeZoneHeight, distance);

        // 스트라이크 존 중심 계산
        Vector3 center = GetStrikeZoneCenter();

        // 스트라이크 존 채우기
        Gizmos.color = fillColor;
        Gizmos.DrawCube(center, size);

        if (drawWireframe)
        {
            // 스트라이크 존 경계선 그리기
            Gizmos.color = strikeBorderColor;
            Gizmos.DrawWireCube(center, size);

            // ABS 연결선 표시
            Gizmos.color = detectorConnectionColor;
            Gizmos.DrawLine(frontPos, backPos);

            // 코너 포인트 강조
            float halfWidth = strikeZoneWidth / 2;
            float halfHeight = strikeZoneHeight / 2;

            // 각 코너 포인트에 작은 구체 그리기
            Vector3[] cornerOffsets = new Vector3[] {
                new Vector3(halfWidth, halfHeight, 0),
                new Vector3(-halfWidth, halfHeight, 0),
                new Vector3(halfWidth, -halfHeight, 0),
                new Vector3(-halfWidth, -halfHeight, 0)
            };

            foreach (var offset in cornerOffsets)
            {
                Gizmos.DrawSphere(frontPos + offset, 0.02f);
                Gizmos.DrawSphere(backPos + offset, 0.02f);
            }
        }
    }

    /**
     * 스트라이크 존 중심 계산
     * Front와 Back 감지기 사이의 중심 위치를 계산합니다.
     * 
     * @return 스트라이크 존 중심 좌표
     */
    private Vector3 GetStrikeZoneCenter()
    {
        return (frontDetector.transform.position + backDetector.transform.position) / 2f;
    }
}