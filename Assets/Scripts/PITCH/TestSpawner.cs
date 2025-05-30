using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestSpawner : MonoBehaviour
{
    [Header("공 생성 설정")]
    [Tooltip("공 자동 소멸 시간 (초)")]
    public float destroyTime = 5.0f;

    [Tooltip("생성할 공의 프리팹")]
    public GameObject ballPrefab;

    [Tooltip("공 발사 위치")]
    public Transform releasePoint;

    [Tooltip("공 발사 간격 (초)")]
    public float spawnInterval = 1.0f;

    [Header("타겟 설정")]
    [Tooltip("타겟할 스트라이크 존 오브젝트")]
    public GameObject pitchRangeObject;

    [Header("버튼 오브젝트")]
    [Tooltip("구종 선택 버튼들")]
    public Button fastballButton;
    public Button sliderButton;
    public Button curveButton;
    public Button changeupButton;

    [Tooltip("속도 조절 및 모드 버튼들")]
    public Button increaseSpeedButton;
    public Button decreaseSpeedButton;
    public Button randomModeButton;

    [Header("디버그 설정")]
    [Tooltip("디버그 모드 활성화 여부")]
    public bool debugMode = false;

    // 현재 투구 정보
    private string pitchTypeName = "Fastball";

    // 구종 및 속도 설정 관련 변수
    private PitchSettingsManager pitchSettingsManager;
    private List<BallSettings> currentPitchSettings;
    public int speedIndex = 0;
    private int[] speedValues = { 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170 };
    public bool isRandomMode = false;

    // 목표 위치 관련 변수
    private Vector3 targetPosition;
    private Vector3 pitchRangeSize;

    void Start()
    {
        // 구종 설정 관리자 찾기
        pitchSettingsManager = FindObjectOfType<PitchSettingsManager>();
        if (pitchSettingsManager == null && debugMode)
        {
            Debug.LogError("PitchSettingsManager를 찾을 수 없습니다!");
        }

        // 초기 설정으로 패스트볼 선택
        if (pitchSettingsManager != null)
        {
            currentPitchSettings = pitchSettingsManager.fastballSettings;
        }

        // PitchRange 오브젝트 설정
        InitializePitchRange();

        // 버튼 이벤트 연결
        AttachButtonListeners();

        // 공 생성 코루틴 시작
        StartCoroutine(SpawnBallRoutine());

        if (debugMode)
        {
            Debug.Log("TestSpawner 초기화 완료");
        }
    }

    private void InitializePitchRange()
    {
        if (pitchRangeObject == null)
        {
            pitchRangeObject = GameObject.Find("PitchRange");
            if (pitchRangeObject == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("PitchRange 오브젝트를 찾을 수 없습니다. 기본 위치값을 사용합니다.");
                }
                targetPosition = new Vector3(5f, 0.822f, -1.81f);
                pitchRangeSize = new Vector3(0.3f, 0.3f, 0f);
                return;
            }
        }

        // PitchRange의 위치와 크기 가져오기
        targetPosition = pitchRangeObject.transform.position;

        // Collider나 Renderer로부터 오브젝트 크기 얻기
        Collider rangeCollider = pitchRangeObject.GetComponent<Collider>();
        Renderer rangeRenderer = pitchRangeObject.GetComponent<Renderer>();

        if (rangeCollider != null)
        {
            pitchRangeSize = rangeCollider.bounds.size;
        }
        else if (rangeRenderer != null)
        {
            pitchRangeSize = rangeRenderer.bounds.size;
        }
        else
        {
            pitchRangeSize = new Vector3(0.3f, 0.3f, 0f);
        }
    }

    private void AttachButtonListeners()
    {
        // 구종 선택 버튼
        if (fastballButton != null)
            fastballButton.onClick.AddListener(() => SelectPitchType(pitchSettingsManager?.fastballSettings, "Fastball"));
        if (sliderButton != null)
            sliderButton.onClick.AddListener(() => SelectPitchType(pitchSettingsManager?.sliderSettings, "Slider"));
        if (curveButton != null)
            curveButton.onClick.AddListener(() => SelectPitchType(pitchSettingsManager?.curveSettings, "Curve"));
        if (changeupButton != null)
            changeupButton.onClick.AddListener(() => SelectPitchType(pitchSettingsManager?.changeupSettings, "Changeup"));

        // 속도 조절 및 모드 버튼
        if (increaseSpeedButton != null)
            increaseSpeedButton.onClick.AddListener(IncreaseSpeed);
        if (decreaseSpeedButton != null)
            decreaseSpeedButton.onClick.AddListener(DecreaseSpeed);
        if (randomModeButton != null)
            randomModeButton.onClick.AddListener(ToggleRandomMode);
    }

    private void SelectPitchType(List<BallSettings> settings, string typeName)
    {
        if (settings == null) return;

        currentPitchSettings = settings;
        pitchTypeName = typeName;
        isRandomMode = false;

        if (debugMode)
        {
            Debug.Log($"구종 선택: {typeName}");
        }
    }

    private void IncreaseSpeed()
    {
        if (speedIndex < speedValues.Length - 1)
        {
            speedIndex++;
            if (debugMode)
            {
                Debug.Log($"속도 증가: {speedValues[speedIndex]}km/h");
            }
        }
    }

    private void DecreaseSpeed()
    {
        if (speedIndex > 0)
        {
            speedIndex--;
            if (debugMode)
            {
                Debug.Log($"속도 감소: {speedValues[speedIndex]}km/h");
            }
        }
    }

    private void ToggleRandomMode()
    {
        isRandomMode = !isRandomMode;
        if (debugMode)
        {
            Debug.Log($"랜덤 모드: {(isRandomMode ? "켜짐" : "꺼짐")}");
        }
    }

    private IEnumerator SpawnBallRoutine()
    {
        while (true)
        {
            // 공 생성 및 발사
            SpawnAndLaunchBall();

            // 다음 발사까지 대기
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnAndLaunchBall()
    {
        if (ballPrefab == null || releasePoint == null)
        {
            if (debugMode)
            {
                Debug.LogError("공 프리팹 또는 발사 위치가 설정되지 않았습니다!");
            }
            return;
        }

        // 공 생성
        GameObject ball = Instantiate(ballPrefab, releasePoint.position, releasePoint.rotation);

        // 설정 결정
        BallSettings settings = GetBallSettings();
        Vector3 currentTargetPosition = GetTargetPosition();

        // BallController 컴포넌트 확인 및 설정
        BallController ballController = ball.GetComponent<BallController>();
        if (ballController != null)
        {
            // 설정 적용
            ballController.SetBallSettings(settings);
            ballController.SetPitchType(GetPitchTypeName(settings));

            // 발사 방향 계산 및 발사
            ballController.CalculateDirection(releasePoint.position, currentTargetPosition);
            ballController.LaunchBall();

            if (debugMode)
            {
                Debug.Log($"공 발사: {GetPitchTypeName(settings)}, 속도: {settings.throwPower * 0.36f:F1}km/h");
                Debug.Log($"발사 위치: {releasePoint.position}, 목표 위치: {currentTargetPosition}");
                Debug.Log($"계산된 방향: {currentTargetPosition - releasePoint.position}");

                // BallController의 Rigidbody 속도 확인
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    StartCoroutine(CheckVelocityAfterFrame(rb));
                }
            }
        }
        else if (debugMode)
        {
            Debug.LogError("공 프리팹에 BallController 컴포넌트가 없습니다!");
        }

        // 일정 시간 후 공 제거
        Destroy(ball, destroyTime);
    }

    private BallSettings GetBallSettings()
    {
        if (pitchSettingsManager == null || currentPitchSettings == null || currentPitchSettings.Count == 0)
        {
            if (debugMode)
            {
                Debug.LogWarning("구종 설정이 없습니다. 기본값을 반환합니다.");
            }
            // 기본 패스트볼 설정 반환
            return new BallSettings(90, 250f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        if (isRandomMode)
        {
            // 랜덤 모드: 랜덤 구종 선택
            return GetRandomBallSettings();
        }
        else
        {
            // 일반 모드: 현재 선택된 구종과 속도
            int settingIndex = speedIndex % currentPitchSettings.Count;
            return currentPitchSettings[settingIndex];
        }
    }

    private BallSettings GetRandomBallSettings()
    {
        if (pitchSettingsManager == null)
            return new BallSettings(90, 250f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

        float randomValue = Random.value * 100;
        List<BallSettings> selectedSettings;

        // 확률에 따라 구종 선택
        if (randomValue < 40) // Fastball 40%
        {
            selectedSettings = pitchSettingsManager.fastballSettings;
        }
        else if (randomValue < 60) // Changeup 20%
        {
            selectedSettings = pitchSettingsManager.changeupSettings;
        }
        else if (randomValue < 80) // Slider 20%
        {
            selectedSettings = pitchSettingsManager.sliderSettings;
        }
        else // Curve 20%
        {
            selectedSettings = pitchSettingsManager.curveSettings;
        }

        if (selectedSettings == null || selectedSettings.Count == 0)
        {
            return new BallSettings(90, 250f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        // 선택된 구종에서 랜덤 설정 선택
        return selectedSettings[Random.Range(0, selectedSettings.Count)];
    }

    private Vector3 GetTargetPosition()
    {
        if (isRandomMode)
        {
            // 랜덤 모드: PitchRange 내에서 랜덤 위치
            float offsetX = Random.Range(-pitchRangeSize.x * 0.5f, pitchRangeSize.x * 0.5f);
            float offsetY = Random.Range(-pitchRangeSize.y * 0.5f, pitchRangeSize.y * 0.5f);

            return new Vector3(
                targetPosition.x + offsetX,
                targetPosition.y + offsetY,
                targetPosition.z
            );
        }
        else
        {
            // 일반 모드: 중앙 위치
            return targetPosition;
        }
    }

    private string GetPitchTypeName(BallSettings settings)
    {
        if (pitchSettingsManager == null) return "Unknown";

        if (pitchSettingsManager.fastballSettings?.Contains(settings) == true)
            return "Fastball";
        if (pitchSettingsManager.sliderSettings?.Contains(settings) == true)
            return "Slider";
        if (pitchSettingsManager.curveSettings?.Contains(settings) == true)
            return "Curve";
        if (pitchSettingsManager.changeupSettings?.Contains(settings) == true)
            return "Changeup";

        return "Unknown";
    }

    // 외부에서 호출 가능한 메서드들
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.1f, interval); // 최소 0.1초
        if (debugMode)
        {
            Debug.Log($"발사 간격 설정: {spawnInterval}초");
        }
    }

    public void SetRandomMode(bool isRandom)
    {
        isRandomMode = isRandom;
        if (debugMode)
        {
            Debug.Log($"랜덤 모드 설정: {(isRandom ? "켜짐" : "꺼짐")}");
        }
    }

    public void SetSpeed(int speed)
    {
        for (int i = 0; i < speedValues.Length; i++)
        {
            if (speedValues[i] == speed)
            {
                speedIndex = i;
                if (debugMode)
                {
                    Debug.Log($"속도 설정: {speed}km/h");
                }
                return;
            }
        }

        if (debugMode)
        {
            Debug.LogWarning($"유효하지 않은 속도: {speed}km/h");
        }
    }

    private IEnumerator CheckVelocityAfterFrame(Rigidbody rb)
    {
        yield return new WaitForFixedUpdate();
        Debug.Log($"공의 실제 속도: {rb.velocity}, 크기: {rb.velocity.magnitude}");
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !debugMode) return;

        // 타겟 위치 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.1f);

        // PitchRange 영역 표시
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawCube(targetPosition, pitchRangeSize);

        // 발사 위치에서 타겟까지 선 그리기
        if (releasePoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(releasePoint.position, targetPosition);
        }
    }
}