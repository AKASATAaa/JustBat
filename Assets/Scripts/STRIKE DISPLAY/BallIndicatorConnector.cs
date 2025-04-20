using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ABSIndicator와 StrikeZoneDisplay를 연결하여 공 통과 이벤트를 전달하는 컴포넌트
/// </summary>
public class BallIndicatorConnector : MonoBehaviour
{
    [Tooltip("스트라이크 존 디스플레이 스크립트")]
    public StrikeZoneDisplay strikeZoneDisplay;

    // Awake에서 컴포넌트 찾기
    void Awake()
    {
        // 스트라이크 존 디스플레이가 지정되지 않은 경우 찾기
        if (strikeZoneDisplay == null)
        {
            strikeZoneDisplay = FindObjectOfType<StrikeZoneDisplay>();
            if (strikeZoneDisplay == null)
            {
                Debug.LogError("StrikeZoneDisplay를 찾을 수 없습니다!");
            }
        }

        // ABSIndicator 스크립트 찾기
        ABSIndicator[] absIndicators = FindObjectsOfType<ABSIndicator>();
        foreach (ABSIndicator absIndicator in absIndicators)
        {
            // Front ABS에만 연결 (공이 처음 통과하는 것만 감지)
            if (absIndicator.absType == ABSIndicator.ABSType.Front)
            {
                // 모니터링 시작
                MonitorBallIndicator(absIndicator);
                Debug.Log("Front ABSIndicator와 연결됨");
            }
        }
    }

    // ABSIndicator를 모니터링하는 코루틴 시작
    private void MonitorBallIndicator(ABSIndicator absIndicator)
    {
        StartCoroutine(MonitorIndicatorCreation(absIndicator));
    }

    // 인디케이터 생성을 모니터링하는 코루틴
    private IEnumerator MonitorIndicatorCreation(ABSIndicator absIndicator)
    {
        // 인디케이터 생성 시점을 모니터링하기 위한 방법
        // 원래 코드를 수정하지 않고 외부에서 감지하기 위해 약간의 트릭이 필요

        // 마지막으로 확인한 시간 저장
        float lastDetectedTime = Time.time;

        while (true)
        {
            yield return new WaitForSeconds(0.05f); // 50ms마다 체크

            // ABSIndicator 스크립트 내의 lastIndicatorTime 접근
            // (private 필드라 직접 접근은 불가능하므로 인디케이터의 생성 감지에 다른 방법 사용)

            // Scene에서 가장 최근에 생성된 Ball_Indicator 찾기
            GameObject[] ballIndicators = GameObject.FindGameObjectsWithTag("Ball_Indicator");

            foreach (GameObject indicator in ballIndicators)
            {
                // 이름에서 시간 정보 추출 (Ball_Indicator_123.45 형식 가정)
                string[] nameParts = indicator.name.Split('_');
                if (nameParts.Length >= 3)
                {
                    float indicatorCreationTime;
                    if (float.TryParse(nameParts[2], out indicatorCreationTime))
                    {
                        // 새로 생성된 인디케이터인지 확인
                        if (indicatorCreationTime > lastDetectedTime)
                        {
                            // 새로운 인디케이터 발견
                            lastDetectedTime = indicatorCreationTime;

                            // 관련 공 객체 찾기 시도
                            // 인디케이터가 있는 위치에서 Ray를 쏴서 공 객체를 찾을 수 있음
                            Collider[] nearbyColliders = Physics.OverlapSphere(indicator.transform.position, 0.5f);
                            foreach (Collider collider in nearbyColliders)
                            {
                                if (collider.CompareTag("Ball"))
                                {
                                    // 공 객체 발견, 스트라이크 존 디스플레이에 알림
                                    if (strikeZoneDisplay != null)
                                    {
                                        strikeZoneDisplay.AddBallPosition(collider.gameObject);
                                        Debug.Log($"Ball detected at {indicator.transform.position}, notifying StrikeZoneDisplay");
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}