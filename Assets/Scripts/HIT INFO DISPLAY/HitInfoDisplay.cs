using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 타구 정보를 TextMeshPro로 표시하기 위한 컴포넌트
/// </summary>
public class HitInfoDisplay : MonoBehaviour
{
    [Header("디스플레이 설정")]
    [SerializeField] private TextMeshPro hitInfoText;

    [Header("표시 설정")]
    [SerializeField] private float displayDuration = 5.0f;
    [SerializeField] private bool showDirection = true;
    [SerializeField] private float directionArrowScale = 1.0f;

    [Header("방향 표시 게임오브젝트")]
    [SerializeField] private GameObject directionIndicator;

    private HitInfo currentHitInfo; // 현재 표시 중인 타격 정보 저장
    // 마지막 타구 정보
    private HitInfo lastHitInfo;
    private bool isDisplaying = false;

    // 타이머 관련
    private Coroutine clearTextCoroutine;

    private void Start()
    {
        // TextMeshPro가 연결되어 있지 않다면 자식에서 찾기
        if (hitInfoText == null)
        {
            hitInfoText = GetComponentInChildren<TextMeshPro>();
            if (hitInfoText == null)
            {
                Debug.LogError("HitInfoDisplay에 TextMeshPro가 연결되어 있지 않습니다.");
            }
        }

        // 방향 표시기가 없다면 생성
        if (showDirection && directionIndicator == null)
        {
            directionIndicator = new GameObject("DirectionIndicator");
            directionIndicator.transform.SetParent(transform);
            directionIndicator.transform.localPosition = Vector3.zero;

            // 간단한 화살표 모양 생성
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrow.transform.SetParent(directionIndicator.transform);
            arrow.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f) * directionArrowScale;
            arrow.transform.localPosition = new Vector3(0, 0, 0.25f) * directionArrowScale;

            // 콜라이더 제거 (충돌 방지)
            Destroy(arrow.GetComponent<Collider>());

            // 방향 표시기 초기 비활성화
            directionIndicator.SetActive(false);
        }

        // 초기 텍스트 설정
        ClearText();
    }

    /// <summary>
    /// 타구 정보를 표시
    /// </summary>
    public void DisplayHitInfo(HitInfo hitInfo)
    {
        lastHitInfo = hitInfo;
        isDisplaying = true;
        currentHitInfo = hitInfo;

        // 텍스트 업데이트
        UpdateInfoText();

        // 방향 표시기 업데이트
        UpdateDirectionIndicator();

        // 이전 코루틴이 있다면 중지
        if (clearTextCoroutine != null)
        {
            StopCoroutine(clearTextCoroutine);
        }

        // 새 코루틴 시작
        clearTextCoroutine = StartCoroutine(ClearAfterDelay());
    }

    // 현재 표시 중인 타격 정보 반환
    public HitInfo GetCurrentHitInfo()
    {
        return currentHitInfo;
    }

    // 텍스트 업데이트
    private void UpdateInfoText()
    {
        if (hitInfoText == null || lastHitInfo == null) return;

        // 비거리 텍스트에 추가 표시 (예상치 또는 실측치 표시)
        string distanceLabel = "dist";
        string distanceValue = "wait"; //= $"{lastHitInfo.EstimatedDistance:F1}";
        string distanceColor = "#00FF00"; // 기본 색상

        // BallController 확인
        bool distanceFound = false;
        BallController[] balls = FindObjectsOfType<BallController>();
        foreach (BallController ball in balls)
        {
            if (ball.hasCollided)
            {
                if (ball.hasLanded)
                {
                    // 실제 측정된 거리가 있으면 표시 형식 변경
                    distanceLabel = "dist(actual)";
                    if (ball.actualDistance > 120)
                        distanceValue = $"HOMERUN! {ball.actualDistance:F1}";
                    else
                        distanceValue = $"{ball.actualDistance:F1}";
                    distanceColor = "#FFFF00"; // 실측치는 다른 색상으로 표시
                    distanceFound = true;
                    break;
                }
                else if (ball.isCalculatingDistance)
                {
                    // 계산 중인 경우 표시
                    distanceLabel = "dist(calc)";
                    distanceFound = true;
                    break;
                }
            }
        }

        // TeeBallController 확인 (추가된 부분)
        if (!distanceFound)
        {
            TeeBallController[] teeBalls = FindObjectsOfType<TeeBallController>();
            foreach (TeeBallController teeBall in teeBalls)
            {
                if (teeBall.hasCollided)
                {
                    if (teeBall.hasLanded)
                    {
                        // 실제 측정된 거리가 있으면 표시 형식 변경
                        distanceLabel = "dist(actual)";
                        if (teeBall.actualDistance > 120)
                            distanceValue = $"HOMERUN! {teeBall.actualDistance:F1}";
                        else
                            distanceValue = $"{teeBall.actualDistance:F1}";
                        distanceColor = "#FFFF00"; // 실측치는 다른 색상으로 표시
                        break;
                    }
                    else if (teeBall.isCalculatingDistance)
                    {
                        // 계산 중인 경우 표시
                        distanceLabel = "dist(calc)";
                        break;
                    }
                }
            }
        }

        // 기본 정보 (HitInfo의 EstimatedDistance)가 있으면 표시
        if (distanceValue == "wait" && lastHitInfo.EstimatedDistance > 0)
        {
            distanceValue = $"{lastHitInfo.EstimatedDistance:F1}";
        }

        hitInfoText.text = $"<b>Hitinfo</b>\n" +
                          $"speed: <color=#FF6B00>{lastHitInfo.ExitVelocity * 0.7f:F1}</color> km/h\n" +
                          $"angle: <color=#00A0FF>{lastHitInfo.LaunchAngle:F1}</color>°\n" +
                          $"{distanceLabel}: <color={distanceColor}>{distanceValue}</color> m";

        // 텍스트 활성화 확인
        if (!hitInfoText.gameObject.activeSelf)
        {
            hitInfoText.gameObject.SetActive(true);
        }
    }

    // 방향 표시기 업데이트
    private void UpdateDirectionIndicator()
    {
        if (!showDirection || directionIndicator == null || lastHitInfo == null) return;

        // XZ 평면에서의 방향 계산 (Y축은 무시)
        Vector3 horizontalDirection = new Vector3(
            lastHitInfo.Direction.x,
            0,
            lastHitInfo.Direction.z
        ).normalized;

        // 방향 표시기 회전
        directionIndicator.transform.rotation = Quaternion.LookRotation(horizontalDirection);

        // 표시기 활성화
        directionIndicator.SetActive(true);
    }

    // 일정 시간 후 표시 지우기
    private IEnumerator ClearAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        ClearText();
    }

    // 텍스트 및 방향 표시 지우기
    private void ClearText()
    {
        isDisplaying = false;

        if (hitInfoText != null)
        {
            hitInfoText.text = "waitforhit";
        }

        if (directionIndicator != null)
        {
            directionIndicator.SetActive(false);
        }
    }

    // 에디터에서 시각화
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && isDisplaying && lastHitInfo != null)
        {
            // 타구 방향 그리기
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, lastHitInfo.Direction * 2f);

            // 타구 거리 표시 (수평 투영)
            Gizmos.color = Color.green;
            Vector3 horizontalDir = new Vector3(lastHitInfo.Direction.x, 0, lastHitInfo.Direction.z).normalized;
            Gizmos.DrawLine(transform.position,
                           transform.position + horizontalDir * lastHitInfo.EstimatedDistance);
        }
    }
}