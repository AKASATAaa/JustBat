using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 스트라이크 존 및 공 위치를 UI에 표시하는 스크립트
/// </summary>
public class StrikeZoneDisplay : MonoBehaviour
{
    [Header("스트라이크 존 UI")]
    [Tooltip("스트라이크 존을 표시할 UI 패널")]
    public RectTransform strikeZonePanel;

    [Tooltip("공 위치를 표시할 프리팹")]
    public GameObject ballMarkerPrefab;

    [Tooltip("스트라이크 존 영역 이미지")]
    public Image strikeZoneImage;

    [Tooltip("투구 속도를 표시할 텍스트")]
    public TextMeshProUGUI pitchSpeedText;

    [Tooltip("구종을 표시할 텍스트")]
    public TextMeshProUGUI pitchTypeText;

    [Header("설정")]
    [Tooltip("최대 표시할 공의 개수")]
    public int maxBallsToShow = 5;

    [Tooltip("ABS 스크립트가 있는 오브젝트")]
    public ABSScoreDetector absScoreDetector;

    [Tooltip("패널에 공 마커 위치를 계산하기 위한 스트라이크 존 너비(미터 단위)")]
    public float strikeZoneRealWidth = 0.43f; // MLB 홈플레이트 너비 (17인치)

    [Tooltip("패널에 공 마커 위치를 계산하기 위한 스트라이크 존 높이(미터 단위)")]
    public float strikeZoneRealHeight = 0.58f; // 평균 스트라이크 존 높이

    [Header("디버그 설정")]
    [Tooltip("디버그 모드 활성화 여부")]
    public bool debugMode = false;

    [Tooltip("디버그 기즈모 표시 여부")]
    public bool showStrikeZoneGizmo = true;

    [Tooltip("스트라이크 존 기즈모 색상")]
    public Color strikeZoneGizmoColor = new Color(0, 1, 0, 0.3f);

    // 실제 월드 좌표의 스트라이크 존 위치와 크기
    private Vector3 strikeZoneWorldCenter;
    private Vector3 strikeZoneWorldSize;

    // 최근 공 위치 목록
    private Queue<BallData> recentBalls = new Queue<BallData>();
    private List<GameObject> ballMarkers = new List<GameObject>();

    // 공 데이터 구조체
    private struct BallData
    {
        public Vector3 position;   // 실제 공 위치
        public string pitchType;   // 구종
        public float speed;        // 속도 (km/h)
        public bool isStrike;      // 스트라이크 여부
        public int ballId;         // 공 ID (중복 검사용)
    }

    // 중복 처리 방지 플래그 (정적 변수로 모든 인스턴스에서 공유)
    private static bool isProcessingBall = false;

    /// <summary>
    /// 컴포넌트 초기화 시 호출되는 메서드
    /// </summary>
    void Start()
    {
        // ABSScoreDetector 참조 확인
        InitializeScoreDetector();

        // 스트라이크 존 정보 초기화
        InitializeStrikeZone();

        if (debugMode)
        {
            Debug.Log("StrikeZoneDisplay 초기화 완료");
        }
    }

    /// <summary>
    /// ABS 스코어 디텍터 참조 초기화
    /// </summary>
    private void InitializeScoreDetector()
    {
        if (absScoreDetector == null)
        {
            absScoreDetector = FindObjectOfType<ABSScoreDetector>();
            if (absScoreDetector == null && debugMode)
            {
                Debug.LogError("ABSScoreDetector를 찾을 수 없습니다!");
            }
        }
    }

    /// <summary>
    /// 스트라이크 존 위치 및 크기 초기화
    /// </summary>
    private void InitializeStrikeZone()
    {
        if (absScoreDetector != null && absScoreDetector.frontDetector != null && absScoreDetector.backDetector != null)
        {
            // 스트라이크 존의 중심 및 크기 계산
            Vector3 frontPos = absScoreDetector.frontDetector.transform.position;
            Vector3 backPos = absScoreDetector.backDetector.transform.position;
            strikeZoneWorldCenter = (frontPos + backPos) / 2f;
            strikeZoneWorldSize = new Vector3(
                absScoreDetector.strikeZoneWidth,
                absScoreDetector.strikeZoneHeight,
                Vector3.Distance(frontPos, backPos)
            );

            if (debugMode)
            {
                Debug.Log($"스트라이크 존 정보: 중심={strikeZoneWorldCenter}, 크기={strikeZoneWorldSize}");
            }
        }
        else
        {
            // 기본값 사용
            strikeZoneWorldCenter = new Vector3(0, 1, 0);
            strikeZoneWorldSize = new Vector3(strikeZoneRealWidth, strikeZoneRealHeight, 0.5f);

            if (debugMode)
            {
                Debug.LogWarning("ABSScoreDetector 정보를 사용할 수 없어 기본 스트라이크 존 설정이 적용됩니다.");
            }
        }
    }

    /// <summary>
    /// 월드 좌표 상의 공 위치가 스트라이크 존 안에 있는지 판정
    /// </summary>
    /// <param name="ballPosition">공의 월드 좌표 위치</param>
    /// <returns>스트라이크 여부</returns>
    public bool GetStrikeJudgment(Vector3 ballPosition)
    {
        if (absScoreDetector == null)
            return false;

        // 스트라이크 존의 경계 계산
        float minX = strikeZoneWorldCenter.x - strikeZoneWorldSize.x / 2;
        float maxX = strikeZoneWorldCenter.x + strikeZoneWorldSize.x / 2;
        float minY = strikeZoneWorldCenter.y - strikeZoneWorldSize.y / 2;
        float maxY = strikeZoneWorldCenter.y + strikeZoneWorldSize.y / 2;

        // 공 위치가 스트라이크 존 안에 있는지 확인
        bool isInStrikeZone = (ballPosition.x >= minX && ballPosition.x <= maxX &&
                              ballPosition.y >= minY && ballPosition.y <= maxY);

        if (debugMode)
        {
            Debug.Log($"판정: 위치={ballPosition}, 스트라이크 존=X({minX:F2}~{maxX:F2}), Y({minY:F2}~{maxY:F2}), 결과={isInStrikeZone}");
        }

        return isInStrikeZone;
    }

    /// <summary>
    /// 공을 UI에 표시하는 메서드 - 자동 판정 사용
    /// </summary>
    /// <param name="ball">표시할 공 게임오브젝트</param>
    public void AddBallPosition(GameObject ball)
    {
        // 이미 처리 중인 공이라면 무시 (중복 방지)
        if (isProcessingBall)
            return;

        try
        {
            // 처리 중 플래그 설정
            isProcessingBall = true;

            if (ball == null) return;

            // 공 ID 확인 (중복 방지)
            int ballId = ball.GetInstanceID();

            // 이미 등록된 공인지 확인
            if (IsBallAlreadyRegistered(ballId))
                return;

            // 공 컨트롤러에서 정보 가져오기
            BallController ballController = ball.GetComponent<BallController>();
            if (ballController == null)
            {
                // 리팩터링 전 클래스 이름으로 시도
                BallController oldBallController = ball.GetComponent<BallController>();
                if (oldBallController == null) return;

                // 공 위치 투영 및 속도 계산
                Vector3 projectedPosition = ProjectBallPositionToStrikeZonePlane(ball.transform.position);

                // CalculateSpeed 메서드 사용 (BallController에 추가됨)
                float speed = CalculateAverageSpeed(oldBallController);

                // 스트라이크 판정
                bool isStrike = GetStrikeJudgment(projectedPosition);

                // 데이터 추가
                AddBallData(ball, ballId, oldBallController.pitchTypeName, projectedPosition, speed, isStrike);
                return;
            }

            // 공 위치 투영
            Vector3 projectedPos = ProjectBallPositionToStrikeZonePlane(ball.transform.position);

            // 평균 속도 계산 (리팩터링된 BallController 사용)
            float averageSpeed = CalculateAverageSpeed(ballController);

            // 스트라이크 판정
            bool isStrikeJudgment = GetStrikeJudgment(projectedPos);

            // 데이터 추가
            AddBallData(ball, ballId, ballController.pitchTypeName, projectedPos, averageSpeed, isStrikeJudgment);
        }
        finally
        {
            // 처리 완료 플래그 해제
            isProcessingBall = false;
        }
    }

    /// <summary>
    /// 공을 UI에 표시하는 메서드 - 외부에서 직접 판정 결과 제공
    /// </summary>
    /// <param name="ball">표시할 공 게임오브젝트</param>
    /// <param name="isStrike">스트라이크 여부</param>
    public void AddBallPositionWithStrikeJudgment(GameObject ball, bool isStrike)
    {
        // 이미 처리 중인 공이라면 무시 (중복 방지)
        if (isProcessingBall)
            return;

        try
        {
            // 처리 중 플래그 설정
            isProcessingBall = true;

            if (ball == null) return;

            // 공 ID 확인 (중복 방지)
            int ballId = ball.GetInstanceID();

            // 이미 등록된 공인지 확인
            if (IsBallAlreadyRegistered(ballId))
                return;

            // 공 컨트롤러에서 정보 가져오기
            BallController ballController = ball.GetComponent<BallController>();
            if (ballController == null)
            {
                // 리팩터링 전 클래스 이름으로 시도
                BallController oldBallController = ball.GetComponent<BallController>();
                if (oldBallController == null) return;

                // 공 위치 투영 및 속도 계산
                Vector3 projectedPosition = ProjectBallPositionToStrikeZonePlane(ball.transform.position);

                // CalculateSpeed 메서드 사용 (BallController에 추가됨)
                float speed = CalculateAverageSpeed(oldBallController);

                // 데이터 추가 (외부에서 받은 isStrike 사용)
                AddBallData(ball, ballId, oldBallController.pitchTypeName, projectedPosition, speed, isStrike);
                return;
            }

            // 공 위치 투영
            Vector3 projectedPos = ProjectBallPositionToStrikeZonePlane(ball.transform.position);

            // 평균 속도 계산 (리팩터링된 BallController 사용)
            float averageSpeed = CalculateAverageSpeed(ballController);

            // 데이터 추가 (외부에서 받은 isStrike 사용)
            AddBallData(ball, ballId, ballController.pitchTypeName, projectedPos, averageSpeed, isStrike);
        }
        finally
        {
            // 처리 완료 플래그 해제
            isProcessingBall = false;
        }
    }

    /// <summary>
    /// BallController 또는 BallController에서 평균 속도를 계산
    /// </summary>
    /// <param name="ballController">볼 컨트롤러</param>
    /// <returns>평균 속도 (km/h)</returns>
    private float CalculateAverageSpeed(object ballController)
    {
        float speed = 0f;

        // 리팩터링된 BallController 타입인 경우
        if (ballController is BallController)
        {
            BallController controller = (BallController)ballController;

            // 리팩터링된 CalculateSpeed 메서드 호출 (km/h 반환)
            speed = controller.CalculateSpeed();
        }
        // 기존 BallController 타입인 경우
        else if (ballController is BallController)
        {
            BallController controller = (BallController)ballController;

            // 이동 시간 기반으로 직접 계산
            float travelTime = Time.time - controller.GetStartTime();
            if (travelTime > 0)
            {
                // TARGET_DISTANCE를 직접 사용 (18.44m)
                const float TARGET_DISTANCE = 18.44f;
                float averageSpeed = TARGET_DISTANCE / travelTime;
                speed = averageSpeed * 3.6f; // m/s -> km/h 변환

                if (debugMode)
                {
                    Debug.Log($"계산된 평균 속도: {speed:F2} km/h (거리: {TARGET_DISTANCE}m, 시간: {travelTime:F2}초)");
                }
            }
            else
            {
                // 초기 발사 속도 기반 추정 (기존 방식)
                speed = controller.initialForce * 0.36f;

                if (debugMode)
                {
                    Debug.Log($"초기 힘 기반 추정 속도: {speed:F2} km/h (initialForce: {controller.initialForce})");
                }
            }
        }

        return speed;
    }

    /// <summary>
    /// 이미 등록된 공인지 확인
    /// </summary>
    /// <param name="ballId">공 인스턴스 ID</param>
    /// <returns>등록 여부</returns>
    private bool IsBallAlreadyRegistered(int ballId)
    {
        foreach (BallData existingBall in recentBalls)
        {
            if (existingBall.ballId == ballId)
            {
                if (debugMode)
                {
                    Debug.Log($"이미 UI에 표시된 공: {ballId}");
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 공 위치를 스트라이크 존 평면에 투영
    /// </summary>
    /// <param name="ballPosition">공의 원래 위치</param>
    /// <returns>투영된 위치</returns>
    private Vector3 ProjectBallPositionToStrikeZonePlane(Vector3 ballPosition)
    {
        // ABS 위치가 있으면 해당 Z 위치에 투영
        if (absScoreDetector != null && absScoreDetector.frontDetector != null)
        {
            Vector3 projectedPosition = new Vector3(
                ballPosition.x,
                ballPosition.y,
                absScoreDetector.frontDetector.transform.position.z
            );

            if (debugMode)
            {
                Debug.Log($"공 위치 투영: 원래={ballPosition}, 투영={projectedPosition}");
            }

            return projectedPosition;
        }
        else
        {
            return ballPosition;
        }
    }

    /// <summary>
    /// 공 데이터 추가 및 UI 업데이트 내부 메서드
    /// </summary>
    private void AddBallData(GameObject ball, int ballId, string pitchType,
                           Vector3 position, float speed, bool isStrike)
    {
        // 공 데이터 생성
        BallData ballData = new BallData
        {
            position = position,
            pitchType = pitchType,
            speed = speed,
            isStrike = isStrike,
            ballId = ballId
        };

        // 큐에 추가하고 오래된 것 제거
        recentBalls.Enqueue(ballData);
        if (recentBalls.Count > maxBallsToShow)
        {
            recentBalls.Dequeue();
        }

        // UI 업데이트
        UpdateStrikeZoneUI();

        // 속도 및 구종 텍스트 업데이트
        if (pitchSpeedText != null)
        {
            pitchSpeedText.text = $"{speed:F0} km/h";
        }

        if (pitchTypeText != null)
        {
            pitchTypeText.text = pitchType;
        }

        if (debugMode)
        {
            Debug.Log($"공 위치 추가: 위치={ballData.position}, 속도={speed:F1}km/h, 스트라이크={ballData.isStrike}, ID={ballData.ballId}");
        }
    }

    /// <summary>
    /// UI에 스트라이크 존과 공 위치 업데이트
    /// </summary>
    private void UpdateStrikeZoneUI()
    {
        // 기존 마커 제거
        foreach (GameObject marker in ballMarkers)
        {
            Destroy(marker);
        }
        ballMarkers.Clear();

        // 큐에 있는 공들의 위치를 UI에 표시
        foreach (BallData ballData in recentBalls)
        {
            // 월드 좌표에서 스트라이크 존 상대 좌표로 변환
            Vector2 relativePosition = WorldToStrikeZoneUI(ballData.position);

            // 마커 생성
            GameObject marker = Instantiate(ballMarkerPrefab, strikeZonePanel);
            RectTransform markerRect = marker.GetComponent<RectTransform>();

            // 위치 설정
            markerRect.anchoredPosition = relativePosition;

            // 스트라이크/볼 표시 설정
            BallMarker ballMarker = marker.GetComponent<BallMarker>();
            if (ballMarker != null)
            {
                ballMarker.SetStrike(ballData.isStrike);
            }
            else
            {
                // BallMarker 스크립트가 없으면 직접 이미지 색상 설정
                Image markerImage = marker.GetComponent<Image>();
                if (markerImage != null)
                {
                    markerImage.color = ballData.isStrike ? Color.yellow : Color.red;
                }
            }

            // 마커 리스트에 추가
            ballMarkers.Add(marker);

            if (debugMode)
            {
                Debug.Log($"UI 마커 추가: 월드위치={ballData.position}, UI좌표={relativePosition}, 스트라이크={ballData.isStrike}");
            }
        }
    }

    /// <summary>
    /// 월드 좌표를 UI 좌표로 변환
    /// </summary>
    /// <param name="worldPosition">월드 좌표</param>
    /// <returns>UI 좌표</returns>
    public Vector2 WorldToStrikeZoneUI(Vector3 worldPosition)
    {
        // 스트라이크 존 중심을 기준으로 한 상대 위치 계산
        float relativeX = (worldPosition.x - strikeZoneWorldCenter.x) / (strikeZoneWorldSize.x / 2);
        float relativeY = (worldPosition.y - strikeZoneWorldCenter.y) / (strikeZoneWorldSize.y / 2);

        // UI 좌표로 변환 (스트라이크 존 이미지 크기 기준)
        RectTransform targetRect = strikeZoneImage != null ?
                                 strikeZoneImage.rectTransform :
                                 strikeZonePanel;

        float uiX = relativeX * (targetRect.rect.width / 2);
        float uiY = relativeY * (targetRect.rect.height / 2);

        return new Vector2(uiX, uiY);
    }

    /// <summary>
    /// 스트라이크 존을 시각적으로 표시 (기즈모)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showStrikeZoneGizmo) return;

        // 스트라이크 존 박스 그리기
        Gizmos.color = strikeZoneGizmoColor;
        Gizmos.DrawCube(strikeZoneWorldCenter, strikeZoneWorldSize);

        // 등록된 공 위치도 표시
        if (debugMode)
        {
            foreach (BallData ballData in recentBalls)
            {
                // 스트라이크는 녹색, 볼은 빨간색으로 표시
                Gizmos.color = ballData.isStrike ? Color.green : Color.red;
                Gizmos.DrawSphere(ballData.position, 0.03f);
            }
        }
    }
}