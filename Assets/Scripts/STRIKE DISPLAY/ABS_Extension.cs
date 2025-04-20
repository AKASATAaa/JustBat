using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ABSIndicator의 기능을 확장하여 StrikeZoneDisplay와 연동하는 컴포넌트
/// 기존 ABSIndicator 코드를 수정하지 않고 새로운 기능 추가
/// </summary>
public class ABS_Extension : MonoBehaviour
{
    [Tooltip("스트라이크 존 디스플레이 스크립트")]
    public StrikeZoneDisplay strikeZoneDisplay;

    // 원본 ABSIndicator 컴포넌트
    private ABSIndicator absIndicator;

    // 마지막으로 생성된 인디케이터 시간
    private float lastIndicatorTime = -999f;

    // 감지된 공 추적용 HashSet
    private HashSet<int> processedBalls = new HashSet<int>();

    void Start()
    {
        // 같은 게임 오브젝트에서 ABSIndicator 컴포넌트 가져오기
        absIndicator = GetComponent<ABSIndicator>();
        if (absIndicator == null)
        {
            Debug.LogError("이 게임 오브젝트에 ABSIndicator가 없습니다!");
            enabled = false;
            return;
        }

        // 스트라이크 존 디스플레이가 지정되지 않은 경우 찾기
        if (strikeZoneDisplay == null)
        {
            strikeZoneDisplay = FindObjectOfType<StrikeZoneDisplay>();
            if (strikeZoneDisplay == null)
            {
                Debug.LogWarning("StrikeZoneDisplay를 찾을 수 없습니다!");
            }
        }

        // 공 감지를 위한 트리거 설정
        SetupTriggerDetection();

        Debug.Log($"ABS_Extension initialized with {absIndicator.absType} ABS");
    }

    void SetupTriggerDetection()
    {
        // 이미 트리거가 있는지 확인
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            // Collider가 없는 경우, ABSIndicator의 크기와 유사한 트리거 추가
            BoxCollider triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;

            // ABSScoreDetector에서 스트라이크 존 크기 가져오기
            ABSScoreDetector scoreDetector = absIndicator.scoreDetector;
            if (scoreDetector != null)
            {
                triggerCollider.size = new Vector3(
                    scoreDetector.strikeZoneWidth * 1.5f, // 약간 더 크게
                    scoreDetector.strikeZoneHeight * 1.5f,
                    0.1f
                );
            }
            else
            {
                // 기본 크기
                triggerCollider.size = new Vector3(1.5f, 1.5f, 0.1f);
            }

            Debug.Log("ABS_Extension에 트리거 콜라이더 추가됨");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("ABS_Extension에 이미 콜라이더가 있지만 트리거가 아닙니다.");
        }
    }

    void Update()
    {
        // 새로운 인디케이터 감시
        MonitorIndicatorCreation();
    }

    // 인디케이터 생성 감시
    private void MonitorIndicatorCreation()
    {
        // Ball_Indicator 태그를 가진 새로운 오브젝트 찾기
        GameObject[] indicators = GameObject.FindGameObjectsWithTag("Ball_Indicator");
        foreach (GameObject indicator in indicators)
        {
            // 인디케이터 이름에서 생성 시간 추출 (Ball_Indicator_123.45 형식)
            string[] nameParts = indicator.name.Split('_');
            if (nameParts.Length >= 3)
            {
                float creationTime;
                if (float.TryParse(nameParts[2], out creationTime))
                {
                    // 이 인디케이터가 마지막으로 감지한 것보다 최근에 생성되었는지 확인
                    if (creationTime > lastIndicatorTime)
                    {
                        lastIndicatorTime = creationTime;

                        // 인디케이터 위치 기준으로 공 찾기
                        FindBallAndNotify(indicator.transform.position);
                    }
                }
            }
        }
    }

    // 공 충돌 감지 - 직접 트리거 이벤트 사용
    private void OnTriggerEnter(Collider other)
    {
        // 'Ball' 태그를 가진 객체만 처리
        if (other.CompareTag("Ball"))
        {
            // 발사된 공만 처리
            BallController ballController = other.GetComponent<BallController>();
            if (ballController != null && ballController.IsBallLaunched())
            {
                // 공의 고유 ID
                int ballId = other.gameObject.GetInstanceID();

                // 이 공을 이미 처리했는지 확인
                if (!processedBalls.Contains(ballId))
                {
                    // 처리된 공으로 표시
                    processedBalls.Add(ballId);

                    // 디스플레이에 알림
                    if (strikeZoneDisplay != null)
                    {
                        strikeZoneDisplay.AddBallPosition(other.gameObject);
                        Debug.Log($"Ball passed through {absIndicator.absType} ABS, notifying StrikeZoneDisplay");
                    }
                }
            }
        }
    }

    // 위치를 기준으로 근처 공 찾기
    private void FindBallAndNotify(Vector3 indicatorPosition)
    {
        // 반경 내 콜라이더 검색
        Collider[] colliders = Physics.OverlapSphere(indicatorPosition, 0.5f);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Ball"))
            {
                // 공의 고유 ID
                int ballId = collider.gameObject.GetInstanceID();

                // 이 공을 이미 처리했는지 확인
                if (!processedBalls.Contains(ballId))
                {
                    // 처리된 공으로 표시
                    processedBalls.Add(ballId);

                    // 디스플레이에 알림
                    if (strikeZoneDisplay != null)
                    {
                        strikeZoneDisplay.AddBallPosition(collider.gameObject);
                        Debug.Log($"Ball detected at indicator position, notifying StrikeZoneDisplay");
                    }

                    // 한 개만 처리하고 종료
                    break;
                }
            }
        }
    }

    // 메모리 관리를 위해 정기적으로 오래된 공 ID 제거
    private IEnumerator CleanupProcessedBalls()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f); // 30초마다 정리
            processedBalls.Clear();
        }
    }
}