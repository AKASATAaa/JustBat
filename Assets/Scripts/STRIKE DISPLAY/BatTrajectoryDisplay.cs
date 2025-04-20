using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StrikeZoneDisplay와 연동하여 배트의 궤적을 UI에 표시하는 컴포넌트
/// </summary>
public class BatTrajectoryDisplay : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("스트라이크 존 디스플레이 스크립트")]
    public StrikeZoneDisplay strikeZoneDisplay;

    [Tooltip("배트 궤적 라인 프리팹")]
    public GameObject batTrajectoryLinePrefab;

    [Tooltip("궤적 라인 두께")]
    public float lineThickness = 0.05f;

    [Tooltip("궤적 표시 지속 시간")]
    public float displayDuration = 5.0f;

    [Tooltip("궤적 라인 색상")]
    public Color trajectoryColor = new Color(1.0f, 0.5f, 0.0f, 0.8f); // 오렌지색

    [Header("감지 설정")]
    [Tooltip("배트 컨트롤러")]
    public BatController batController;

    [Tooltip("스트라이크 존 평면의 Z 위치")]
    public float strikeZonePlaneZ;

    [Tooltip("궤적 감지 간격 (초)")]
    public float detectionInterval = 0.01f;

    [Tooltip("최소 스윙 속도 (감지 시작)")]
    public float minSwingSpeed = 5.0f;

    [Tooltip("스윙 종료 감지 지연시간 (초)")]
    public float swingEndDelay = 0.2f;

    [Tooltip("디버그 모드")]
    public bool debugMode = false;

    // 현재 스윙 데이터
    private List<Vector3> currentSwingPoints = new List<Vector3>();
    private bool isSwinging = false;
    private float lastDetectionTime = 0f;
    private float swingEndTime = 0f;

    // 활성 궤적 목록
    private List<SwingTrajectory> activeTrajectories = new List<SwingTrajectory>();

    // 스윙 궤적 클래스
    private class SwingTrajectory
    {
        public GameObject upperLine;
        public GameObject lowerLine;
        public float endTime;
        public float startAlpha;

        public SwingTrajectory(GameObject upper, GameObject lower, float duration, float alpha)
        {
            upperLine = upper;
            lowerLine = lower;
            endTime = Time.time + duration;
            startAlpha = alpha;
        }
    }

    private void Start()
    {
        // StrikeZoneDisplay가 설정되지 않았다면 찾기
        if (strikeZoneDisplay == null)
        {
            strikeZoneDisplay = FindObjectOfType<StrikeZoneDisplay>();
            if (strikeZoneDisplay == null)
            {
                Debug.LogError("StrikeZoneDisplay를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }

        // BatController가 설정되지 않았다면 찾기
        if (batController == null)
        {
            batController = FindObjectOfType<BatController>();
            if (batController == null)
            {
                Debug.LogError("BatController를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }

        // 스트라이크 존 Z 위치 자동 설정
        if (strikeZonePlaneZ == 0 && strikeZoneDisplay.absScoreDetector != null
            && strikeZoneDisplay.absScoreDetector.frontDetector != null)
        {
            strikeZonePlaneZ = strikeZoneDisplay.absScoreDetector.frontDetector.transform.position.z;

            if (debugMode)
            {
                Debug.Log($"스트라이크 존 Z 위치 자동 설정: {strikeZonePlaneZ}");
            }
        }

        // 궤적 라인 프리팹이 없으면 생성
        if (batTrajectoryLinePrefab == null)
        {
            CreateDefaultLinePrefab();
        }

        // 페이드 아웃 코루틴 시작
        StartCoroutine(FadeOutTrajectories());
    }

    private void Update()
    {
        // 배트 스윗 스팟 위치 및 속도 가져오기
        if (batController == null || batController.batSweetSpot == null)
            return;

        Vector3 batPosition = batController.batSweetSpot.position;
        Vector3 batVelocity = batController.currentVelocity;
        float batSpeed = batVelocity.magnitude;

        // 스윙 감지 로직
        if (batSpeed >= minSwingSpeed)
        {
            // 스윙 시작 또는 계속 중
            if (!isSwinging)
            {
                isSwinging = true;
                currentSwingPoints.Clear();
                lastDetectionTime = Time.time;

                if (debugMode)
                {
                    Debug.Log($"스윙 시작 감지: 속도 {batSpeed} m/s");
                }
            }

            // 일정 간격으로 위치 샘플링
            if (Time.time - lastDetectionTime >= detectionInterval)
            {
                // 스트라이크 존 평면과의 교차점 계산
                Vector3 intersection = CalculateIntersectionWithPlane(batPosition, batVelocity);

                if (intersection != Vector3.zero)
                {
                    // 교차점 저장
                    currentSwingPoints.Add(intersection);

                    if (debugMode)
                    {
                        Debug.Log($"스윙 경로 포인트 추가: {intersection}");
                        Debug.DrawRay(batPosition, batVelocity.normalized * 0.5f, Color.blue, 0.1f);
                        Debug.DrawLine(batPosition, intersection, Color.yellow, 0.1f);
                    }
                }

                lastDetectionTime = Time.time;
            }

            // 스윙 지속시간 갱신
            swingEndTime = Time.time;
        }
        else if (isSwinging && (Time.time - swingEndTime > swingEndDelay))
        {
            // 스윙 종료 (배트 속도가 낮아지고 일정 시간이 지나면)
            if (currentSwingPoints.Count >= 2)
            {
                if (debugMode)
                {
                    Debug.Log($"스윙 종료 감지: 포인트 수 {currentSwingPoints.Count}");
                }
                CreateTrajectoryLine(currentSwingPoints);
            }

            isSwinging = false;
            currentSwingPoints.Clear();
        }

        // 만료된 궤적 제거
        RemoveExpiredTrajectories();
    }

    // 스트라이크 존 평면과의 교차점 계산
    private Vector3 CalculateIntersectionWithPlane(Vector3 position, Vector3 velocity)
    {
        if (Mathf.Abs(velocity.z) < 0.001f)
            return Vector3.zero; // Z 방향 속도가 거의 없으면 교차하지 않음

        // 현재 위치에서 스트라이크 존 평면까지의 거리
        float distanceToPlane = strikeZonePlaneZ - position.z;

        // 평면에 도달하는 시간
        float timeToPlane = distanceToPlane / velocity.z;

        // 시간이 음수거나 너무 큰 값이면 무시 (평면과 반대 방향으로 이동 중이거나 너무 느린 경우)
        if (timeToPlane < 0 || timeToPlane > 1.0f)
            return Vector3.zero;

        // 교차점 계산
        Vector3 intersection = position + velocity * timeToPlane;

        return intersection;
    }

    // 궤적 라인 생성
    private void CreateTrajectoryLine(List<Vector3> points)
    {
        if (points.Count < 2)
            return;

        // 첫 번째와 마지막 포인트 사용 (직선 경로)
        Vector3 startPoint = points[0];
        Vector3 endPoint = points[points.Count - 1];

        // 월드 좌표를 UI 좌표로 변환
        Vector2 startUI = strikeZoneDisplay.WorldToStrikeZoneUI(startPoint);
        Vector2 endUI = strikeZoneDisplay.WorldToStrikeZoneUI(endPoint);

        // 두 개의 라인 생성 (배트 두께 표현)
        GameObject upperLine = Instantiate(batTrajectoryLinePrefab, strikeZoneDisplay.strikeZonePanel);
        GameObject lowerLine = Instantiate(batTrajectoryLinePrefab, strikeZoneDisplay.strikeZonePanel);

        upperLine.SetActive(true);
        lowerLine.SetActive(true);

        // 라인 방향 벡터 계산
        Vector2 direction = (endUI - startUI).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x); // 수직 벡터

        // 라인 길이 계산
        float lineLength = Vector2.Distance(startUI, endUI);

        // 라인 회전 각도 계산
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 위쪽 라인 설정
        Image upperImage = upperLine.GetComponent<Image>();
        RectTransform upperRect = upperLine.GetComponent<RectTransform>();
        upperImage.color = trajectoryColor;
        upperRect.sizeDelta = new Vector2(lineLength, lineThickness);
        upperRect.anchoredPosition = (startUI + endUI) / 2 + perpendicular * lineThickness / 2;
        upperRect.localRotation = Quaternion.Euler(0, 0, angle);

        // 아래쪽 라인 설정
        Image lowerImage = lowerLine.GetComponent<Image>();
        RectTransform lowerRect = lowerLine.GetComponent<RectTransform>();
        lowerImage.color = trajectoryColor;
        lowerRect.sizeDelta = new Vector2(lineLength, lineThickness);
        lowerRect.anchoredPosition = (startUI + endUI) / 2 - perpendicular * lineThickness / 2;
        lowerRect.localRotation = Quaternion.Euler(0, 0, angle);

        // 활성 궤적 목록에 추가
        activeTrajectories.Add(new SwingTrajectory(upperLine, lowerLine, displayDuration, trajectoryColor.a));

        if (debugMode)
        {
            Debug.Log($"배트 궤적 생성: 길이 {lineLength}, 각도 {angle}도");
        }
    }

    // 만료된 궤적 제거
    private void RemoveExpiredTrajectories()
    {
        float currentTime = Time.time;
        activeTrajectories.RemoveAll(traj =>
        {
            if (currentTime > traj.endTime)
            {
                Destroy(traj.upperLine);
                Destroy(traj.lowerLine);
                return true;
            }
            return false;
        });
    }

    // 페이드 아웃 효과
    private IEnumerator FadeOutTrajectories()
    {
        while (true)
        {
            float currentTime = Time.time;

            foreach (SwingTrajectory traj in activeTrajectories)
            {
                // 남은 시간 비율 계산
                float remainingTimeFactor = (traj.endTime - currentTime) / displayDuration;
                float alpha = remainingTimeFactor * traj.startAlpha;

                // 알파 값 설정
                if (traj.upperLine != null)
                {
                    Image upperImage = traj.upperLine.GetComponent<Image>();
                    if (upperImage != null)
                    {
                        Color color = upperImage.color;
                        color.a = alpha;
                        upperImage.color = color;
                    }
                }

                if (traj.lowerLine != null)
                {
                    Image lowerImage = traj.lowerLine.GetComponent<Image>();
                    if (lowerImage != null)
                    {
                        Color color = lowerImage.color;
                        color.a = alpha;
                        lowerImage.color = color;
                    }
                }
            }

            yield return new WaitForSeconds(0.05f); // 20fps로 페이드 아웃 업데이트
        }
    }

    // 기본 라인 프리팹 생성
    private void CreateDefaultLinePrefab()
    {
        batTrajectoryLinePrefab = new GameObject("BatTrajectoryLine");
        batTrajectoryLinePrefab.AddComponent<RectTransform>();
        Image image = batTrajectoryLinePrefab.AddComponent<Image>();
        image.color = trajectoryColor;

        // 보이지 않게 설정
        batTrajectoryLinePrefab.SetActive(false);

        if (debugMode)
        {
            Debug.Log("기본 배트 궤적 라인 프리팹 생성됨");
        }
    }

    // 디버그 시각화
    void OnDrawGizmos()
    {
        if (Application.isPlaying && debugMode && batController != null && batController.batSweetSpot != null)
        {
            // 스트라이크 존 평면 표시
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            if (strikeZoneDisplay != null && strikeZoneDisplay.absScoreDetector != null)
            {
                // 스트라이크 존 크기 가져오기
                Vector3 size = new Vector3(
                    strikeZoneDisplay.absScoreDetector.strikeZoneWidth,
                    strikeZoneDisplay.absScoreDetector.strikeZoneHeight,
                    0.01f
                );
                Gizmos.DrawCube(
                    new Vector3(0, 1, strikeZonePlaneZ),
                    size
                );
            }
            else
            {
                // 기본 평면
                Gizmos.DrawCube(
                    new Vector3(0, 1, strikeZonePlaneZ),
                    new Vector3(1, 1, 0.01f)
                );
            }

            // 현재 스윙 포인트 표시
            Gizmos.color = Color.green;
            foreach (Vector3 point in currentSwingPoints)
            {
                Gizmos.DrawSphere(point, 0.02f);
            }
        }
    }
}