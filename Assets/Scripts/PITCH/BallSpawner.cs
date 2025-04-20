/**
 * BallSpawner.cs - 공 생성 및 발사 제어 컴포넌트
 * 
 * 이 스크립트는 다양한 구종의 공을 주기적으로 생성하고 발사하는 역할을 담당합니다.
 * 여러 종류의 공(패스트볼, 슬라이더, 커브, 체인지업)과 다양한 
 * 속도 설정을 통해 다양한 물리적 특성을 가진 공을 만들어냅니다.
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BallSpawner : MonoBehaviour
{
    [Header("공 생성 설정")]
    [Tooltip("공 자동 소멸 시간 (초)")]
    public float destroyTime = 5.0f;

    [Tooltip("생성할 공의 프리팹")]
    public GameObject ballPrefab;

    [Tooltip("공 발사 위치")]
    public Transform releasePoint;

    //

    [Header("애니메이션 설정")]
    [Tooltip("투수 애니메이터 컴포넌트")]
    public Animator pitcherAnimator;

    [Tooltip("투구 사이 대기 시간 (초)")]
    public float pitchDelay = 2.0f;

    [Tooltip("애니메이션 트리거 파라미터 이름")]
    public string[] pitchAnimTriggers = { "ThrowFastball", "ThrowSlider", "ThrowCurve", "ThrowChangeup" };

    [Tooltip("준비 자세 애니메이션 트리거")]
    public string readyAnimTrigger = "Ready";

    [Header("버튼 오브젝트")]
    [Tooltip("구종 선택 버튼들")]
    public Button fastballButton;
    public Button sliderButton;
    public Button curveButton;
    public Button changeupButton;
    public Button knuckleballButton;

    [Tooltip("속도 조절 및 모드 버튼들")]
    public Button increaseSpeedButton;
    public Button decreaseSpeedButton;
    public Button randomModeButton;

    [Header("물리 정보")]
    [Tooltip("공의 충돌 각도")]
    public float hitAngle;

    [Tooltip("공의 속도 (km/h)")]
    public float ballSpeed;

    [Tooltip("타격 속도 (km/h)")]
    public float hitSpeed;

    [Tooltip("발사 각도 (도)")]
    public float launchAngle;

    [Tooltip("예상 비행 거리 (m)")]
    public float expectedDistance;

    [Header("타겟 설정")]
    [Tooltip("타겟할 스트라이크 존 오브젝트")]
    public GameObject pitchRangeObject;

    // 구종별 PitchRange 오브젝트 추가
    [Tooltip("패스트볼용 스트라이크 존 오브젝트")]
    public GameObject fastballPitchRangeObject;

    [Tooltip("슬라이더용 스트라이크 존 오브젝트")]
    public GameObject sliderPitchRangeObject;

    [Tooltip("커브용 스트라이크 존 오브젝트")]
    public GameObject curvePitchRangeObject;

    [Tooltip("체인지업용 스트라이크 존 오브젝트")]
    public GameObject changeupPitchRangeObject;

    [Header("UI 정보 표시")]
    [Tooltip("구종과 구속 정보를 표시할 TextMeshPro 컴포넌트")]
    public TextMeshProUGUI pitchInfoText;

    [Header("디버그 설정")]
    [Tooltip("디버그 모드 활성화 여부")]
    public bool debugMode = false;

    [Tooltip("공 발사 정보 로그 출력 여부")]
    public bool logBallInfo = true;

    [Tooltip("랜덤 생성 관련 로그 출력 여부")]
    public bool logRandomGeneration = true;

    [Tooltip("애니메이션 이벤트 로그 출력 여부")]
    public bool logAnimationEvents = true;

    [Tooltip("타겟 위치 기즈모 표시 여부")]
    public bool showTargetGizmo = true;

    [Tooltip("타겟 위치 기즈모 색상")]
    public Color targetGizmoColor = new Color(1f, 0.5f, 0f, 0.5f);

    [Tooltip("타겟 위치 기즈모 크기")]
    public float targetGizmoSize = 0.1f;

    // 현재 투구 정보
    public string pitchTypeName;

    // 구종 및 속도 설정 관련 변수
    private PitchSettingsManager pitchSettingsManager;
    private List<BallSettings> currentPitchSettings;
    private int speedIndex;
    private int[] speedValues = { 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170 };
    private bool isRandomMode;

    // 목표 위치 관련 변수
    private Vector3 initialTargetP;
    private Vector3 randomTargetP;
    private Vector3 pitchRangeSize; // PitchRange 오브젝트의 크기

    // 애니메이션 이벤트에서 사용할 변수
    private bool isAnimationInProgress = false;
    private bool waitingToSpawn = false;
    private BallSettings pendingBallSettings;
    private string pendingPitchType = "";

    // 투구 범위 데이터 구조체
    private struct PitchRangeData
    {
        public Vector3 position;
        public Vector3 size;
    }

    private Dictionary<string, PitchRangeData> pitchRangeDataMap = new Dictionary<string, PitchRangeData>();

    /**
     * 초기화 메서드
     * 구종 설정 관리자를 찾고, 버튼 이벤트를 설정하며, 공 생성 코루틴을 시작합니다.
     */
    void Start()
    {
        // 구종 설정 관리자 찾기
        InitializePitchSettingsManager();

        // UI 정보 업데이트
        UpdatePitchInfoDisplay();

        // 애니메이터 컴포넌트 확인
        CheckAnimatorComponent();

        // 초기 설정으로 패스트볼 선택
        currentPitchSettings = pitchSettingsManager.fastballSettings;
        speedIndex = 0;
        isRandomMode = false;

        // PitchRange 오브젝트 설정
        InitializePitchRange();
        InitializePitchRangeForAllTypes();

        // 버튼 이벤트 연결
        AttachButtonListeners();

        // 공 생성 코루틴 시작
        StartCoroutine(AnimatedPitchingSequence());

        if (debugMode)
        {
            Debug.Log("BallSpawner 초기화 완료");
        }
    }

    /**
     * 투구 설정 관리자 초기화
     */
    private void InitializePitchSettingsManager()
    {
        pitchSettingsManager = FindObjectOfType<PitchSettingsManager>();

        if (pitchSettingsManager == null && debugMode)
        {
            Debug.LogError("PitchSettingsManager를 찾을 수 없습니다!");
        }
    }

    /**
     * 애니메이터 컴포넌트 확인
     */
    private void CheckAnimatorComponent()
    {
        if (pitcherAnimator == null)
        {
            pitcherAnimator = GetComponent<Animator>();
            if (pitcherAnimator == null && debugMode)
            {
                Debug.LogError("투수 애니메이터를 찾을 수 없습니다. 컴포넌트를 추가하거나 참조를 설정해주세요.");
            }
        }
    }

    /**
     * 구종별 PitchRange 데이터 초기화
     */
    private void InitializePitchRangeForAllTypes()
    {
        // 기본 PitchRange 데이터 저장
        PitchRangeData defaultData = new PitchRangeData
        {
            position = initialTargetP,
            size = pitchRangeSize
        };
        pitchRangeDataMap["Default"] = defaultData;

        // 패스트볼 PitchRange 설정
        InitializePitchRangeForType(fastballPitchRangeObject, "Fastball", defaultData);

        // 슬라이더 PitchRange 설정
        InitializePitchRangeForType(sliderPitchRangeObject, "Slider", defaultData);

        // 커브 PitchRange 설정
        InitializePitchRangeForType(curvePitchRangeObject, "Curve", defaultData);

        // 체인지업 PitchRange 설정
        InitializePitchRangeForType(changeupPitchRangeObject, "Changeup", defaultData);
    }

    /**
     * 특정 구종의 PitchRange 초기화
     */
    private void InitializePitchRangeForType(GameObject pitchRangeObj, string typeName, PitchRangeData defaultData)
    {
        if (pitchRangeObj != null)
        {
            pitchRangeDataMap[typeName] = GetPitchRangeData(pitchRangeObj);

            if (debugMode && logRandomGeneration)
            {
                Debug.Log($"{typeName} PitchRange 초기화: 위치={pitchRangeDataMap[typeName].position}, 크기={pitchRangeDataMap[typeName].size}");
            }
        }
        else
        {
            pitchRangeDataMap[typeName] = defaultData;

            if (debugMode && logRandomGeneration)
            {
                Debug.Log($"{typeName}용 PitchRange 오브젝트가 없어 기본값 사용");
            }
        }
    }

    /**
     * PitchRange 데이터 가져오기
     */
    private PitchRangeData GetPitchRangeData(GameObject pitchRangeObj)
    {
        PitchRangeData data = new PitchRangeData();
        data.position = pitchRangeObj.transform.position;

        // Collider나 Renderer로부터 오브젝트 크기 얻기
        Collider rangeCollider = pitchRangeObj.GetComponent<Collider>();
        Renderer rangeRenderer = pitchRangeObj.GetComponent<Renderer>();

        if (rangeCollider != null)
        {
            // Collider의 bounds 크기 사용
            data.size = rangeCollider.bounds.size;
        }
        else if (rangeRenderer != null)
        {
            // Renderer의 bounds 크기 사용
            data.size = rangeRenderer.bounds.size;
        }
        else
        {
            // 기본 크기 설정
            data.size = new Vector3(0.3f, 0.3f, 0f);

            if (debugMode)
            {
                Debug.LogWarning($"{pitchRangeObj.name}에서 크기를 가져올 수 없습니다. 기본 크기를 사용합니다.");
            }
        }

        return data;
    }

    /**
     * 특정 구종에 대한 랜덤 위치 설정
     */
    private void SetRandomTargetPForPitchType(string pitchType)
    {
        // 해당 구종의 PitchRange 데이터 가져오기
        PitchRangeData rangeData;
        if (!pitchRangeDataMap.TryGetValue(pitchType, out rangeData))
        {
            rangeData = pitchRangeDataMap["Default"];

            if (debugMode && logRandomGeneration)
            {
                Debug.LogWarning($"{pitchType}의 PitchRange 데이터가 없어 기본값 사용");
            }
        }

        // PitchRange의 너비와 높이의 절반을 오프셋 최대값으로 사용 (X, Y축만)
        float maxOffsetX = rangeData.size.x * 0.5f;
        float maxOffsetY = rangeData.size.y * 0.5f;

        // 해당 구종의 PitchRange 내에서 무작위 위치 선택
        randomTargetP = new Vector3(
            rangeData.position.x + Random.Range(-maxOffsetX, maxOffsetX),
            rangeData.position.y + Random.Range(-maxOffsetY, maxOffsetY),
            rangeData.position.z
        );

        if (debugMode && logRandomGeneration)
        {
            Debug.Log($"{pitchType} 랜덤 타겟 위치: {randomTargetP} (범위: X ±{maxOffsetX:F2}, Y ±{maxOffsetY:F2})");
        }
    }

    /** 
     * PitchRange 오브젝트 초기화 메서드
     * PitchRange 오브젝트의 중심 위치와 크기를 가져옵니다.
     */
    private void InitializePitchRange()
    {
        // PitchRange 오브젝트가 설정되지 않은 경우 찾아보기
        if (pitchRangeObject == null)
        {
            pitchRangeObject = GameObject.Find("PitchRange");
            if (pitchRangeObject == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("PitchRange 오브젝트를 찾을 수 없습니다. 기본 위치값을 사용합니다.");
                }
                initialTargetP = new Vector3(5f, 0.822f, -1.81f);
                pitchRangeSize = new Vector3(0.3f, 0.3f, 0f);
                return;
            }
        }

        // PitchRange의 위치와 크기 가져오기
        initialTargetP = pitchRangeObject.transform.position;

        // Collider나 Renderer로부터 오브젝트 크기 얻기
        Collider rangeCollider = pitchRangeObject.GetComponent<Collider>();
        Renderer rangeRenderer = pitchRangeObject.GetComponent<Renderer>();

        if (rangeCollider != null)
        {
            // Collider의 bounds 크기 사용
            pitchRangeSize = rangeCollider.bounds.size;
        }
        else if (rangeRenderer != null)
        {
            // Renderer의 bounds 크기 사용
            pitchRangeSize = rangeRenderer.bounds.size;
        }
        else
        {
            // 기본 크기 설정
            pitchRangeSize = new Vector3(0.3f, 0.3f, 0f);

            if (debugMode)
            {
                Debug.LogWarning("PitchRange 오브젝트에서 크기를 가져올 수 없습니다. 기본 크기를 사용합니다.");
            }
        }

        if (debugMode)
        {
            Debug.Log($"기본 PitchRange 초기화: 위치={initialTargetP}, 크기={pitchRangeSize}");
        }
    }

    /**
     * 버튼에 이벤트 리스너를 연결하는 메서드
     * 각 버튼이 눌렸을 때 해당 기능을 실행합니다.
     */
    private void AttachButtonListeners()
    {
        // 구종 선택 버튼
        AttachUIButton(fastballButton, () => SelectPitchType(pitchSettingsManager.fastballSettings));
        AttachUIButton(sliderButton, () => SelectPitchType(pitchSettingsManager.sliderSettings));
        AttachUIButton(curveButton, () => SelectPitchType(pitchSettingsManager.curveSettings));
        AttachUIButton(changeupButton, () => SelectPitchType(pitchSettingsManager.changeupSettings));

        // 널클볼 버튼은 요청에 따라 비활성화
        if (knuckleballButton != null)
            knuckleballButton.gameObject.SetActive(false);

        // 속도 조절 및 모드 버튼
        AttachUIButton(increaseSpeedButton, IncreaseSpeed);
        AttachUIButton(decreaseSpeedButton, DecreaseSpeed);
        AttachUIButton(randomModeButton, ToggleRandomMode);
    }

    /**
     * 버튼 오브젝트에 기능을 연결하는 유틸리티 메서드
     */
    private void AttachUIButton(Button button, UnityAction onClickAction)
    {
        if (button == null)
            return;

        // 기존 리스너 제거 후 새로 추가
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClickAction);
    }

    /**
     * 구종 유형을 선택하는 메서드
     */
    private void SelectPitchType(List<BallSettings> pitchSettings)
    {
        currentPitchSettings = pitchSettings;
        isRandomMode = false;
        pitchTypeName = GetPitchTypeName(pitchSettings[0]);
        UpdatePitchInfoDisplay();
    }

    /**
     * 랜덤 모드를 켜고 끄는 메서드
     */
    private void ToggleRandomMode()
    {
        isRandomMode = !isRandomMode;

        // 랜덤 모드에서는 최소 속도가 90km/h여야 함
        if (isRandomMode && speedValues[speedIndex] < 90)
        {
            speedIndex = System.Array.IndexOf(speedValues, 90);
        }

        UpdatePitchInfoDisplay();
    }

    /**
     * 속도를 증가시키는 메서드
     */
    private void IncreaseSpeed()
    {
        if (speedIndex < speedValues.Length - 1)
        {
            speedIndex++;
        }

        // 랜덤 모드에서는 최소 속도가 90km/h여야 함
        if (isRandomMode && speedValues[speedIndex] < 90)
        {
            speedIndex = System.Array.IndexOf(speedValues, 90);
        }

        UpdatePitchInfoDisplay();
    }

    /**
     * 속도를 감소시키는 메서드
     */
    private void DecreaseSpeed()
    {
        if (speedIndex > 0)
        {
            speedIndex--;
        }

        // 랜덤 모드에서는 최소 속도가 90km/h여야 함
        if (isRandomMode && speedValues[speedIndex] < 90)
        {
            speedIndex = System.Array.IndexOf(speedValues, 90);
        }

        UpdatePitchInfoDisplay();
    }

    /**
     * 구종의 이름을 반환하는 메서드
     */
    private string GetPitchTypeName(BallSettings settings)
    {
        if (pitchSettingsManager == null)
            return "Unknown";

        if (pitchSettingsManager.fastballSettings.Contains(settings))
            return "Fastball";
        if (pitchSettingsManager.sliderSettings.Contains(settings))
            return "Slider";
        if (pitchSettingsManager.curveSettings.Contains(settings))
            return "Curve";
        if (pitchSettingsManager.changeupSettings.Contains(settings))
            return "Changeup";

        return "Unknown";
    }

    /**
     * 애니메이션 기반의 투구 시퀀스를 제어하는 코루틴
     * 주기적으로 투구 애니메이션을 시작하고 애니메이션 이벤트를 통해 공을 발사합니다.
     */
    private IEnumerator AnimatedPitchingSequence()
    {
        yield return new WaitForSeconds(1.0f); // 초기 대기 시간

        while (true)
        {
            // 다음 투구를 위한 대기
            waitingToSpawn = true;
            yield return new WaitForSeconds(pitchDelay);
            waitingToSpawn = false;

            // 투구 애니메이션 시작
            TriggerPitchAnimation();

            // 애니메이션이 완료될 때까지 대기
            while (isAnimationInProgress)
            {
                yield return null;
            }
        }
    }

    /**
     * 현재 선택된 구종에 따라 적절한 투구 애니메이션을 시작하는 메서드
     */
    private void TriggerPitchAnimation()
    {
        if (pitcherAnimator == null || isAnimationInProgress)
            return;

        isAnimationInProgress = true;

        // 투구 설정 및 타입 결정
        pendingBallSettings = isRandomMode ? GetRandomBallSettings() :
                             currentPitchSettings[speedIndex % currentPitchSettings.Count];
        pendingPitchType = GetPitchTypeName(pendingBallSettings);

        // 모드에 따라 목표 위치 결정
        if (isRandomMode)
        {
            SetRandomTargetPForPitchType(pendingPitchType);
        }
        else
        {
            ResetTargetP();
        }

        // 준비 자세 애니메이션 실행
        pitcherAnimator.SetTrigger(readyAnimTrigger);

        // 구종에 맞는 투구 애니메이션 실행
        int animIndex = GetAnimationIndexForPitchType(pendingPitchType);

        // 애니메이션 트리거 실행
        if (animIndex < pitchAnimTriggers.Length)
        {
            pitcherAnimator.SetTrigger(pitchAnimTriggers[animIndex]);

            if (debugMode && logAnimationEvents)
            {
                Debug.Log($"{pendingPitchType} 투구 애니메이션 시작 (트리거: {pitchAnimTriggers[animIndex]})");
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.LogError($"애니메이션 트리거가 설정되지 않았습니다. (구종: {pendingPitchType})");
            }
            isAnimationInProgress = false;
        }
    }

    /**
     * 구종에 따른 애니메이션 인덱스 반환
     */
    private int GetAnimationIndexForPitchType(string pitchType)
    {
        switch (pitchType)
        {
            case "Fastball": return 0;
            case "Slider": return 1;
            case "Curve": return 2;
            case "Changeup": return 3;
            default: return 0; // 기본값은 패스트볼
        }
    }

    /**
     * 애니메이션 이벤트에서 호출되는 공 발사 메서드
     * 애니메이션의 특정 프레임에서 이 메서드를 호출하여 공을 생성하고 발사합니다.
     */
    public void OnAnimationReleaseBall()
    {
        // 공 생성
        GameObject ball = Instantiate(ballPrefab, releasePoint.position, releasePoint.rotation);

        // 공에 설정 적용
        ApplyBallSettings(ball);

        // 일정 시간 후 공 제거
        StartCoroutine(DestroyBallAfterTime(ball, destroyTime));

        if (debugMode && logBallInfo)
        {
            Debug.Log($"{pendingPitchType} 공이 생성되고 발사되었습니다. 속도: {pendingBallSettings.throwPower * 0.36f:F1}km/h");
        }
    }

    /**
     * 생성된 공에 설정 적용
     */
    private void ApplyBallSettings(GameObject ball)
    {
        // BallController 컴포넌트 확인
        BallController ballController = ball.GetComponent<BallController>();
        if (ballController != null)
        {
            // 설정 적용
            ballController.SetBallSettings(pendingBallSettings);
            ballController.SetPitchType(pendingPitchType);

            // 발사 방향 계산 및 발사
            ballController.CalculateDirection(releasePoint.position, isRandomMode ? randomTargetP : initialTargetP);
            ballController.LaunchBall();
            StartCoroutine(CheckBallHit(ballController));
        }
        else if (debugMode)
        {
            Debug.LogError("볼 프리팹에 BallController 컴포넌트가 없습니다!");
        }
    }

    /**
     * 애니메이션 이벤트에서 호출되는 애니메이션 완료 메서드
     * 애니메이션의 마지막 프레임에서 이 메서드를 호출하여 다음 투구를 준비합니다.
     */
    public void OnAnimationComplete()
    {
        isAnimationInProgress = false;

        if (debugMode && logAnimationEvents)
        {
            Debug.Log("투구 애니메이션 완료");
        }
    }

    /**
     * 목표 위치를 PitchRange 오브젝트의 중심으로 재설정하는 메서드
     */
    private void ResetTargetP()
    {
        // PitchRange 오브젝트의 위치가 변경되었을 수 있으므로 현재 위치 확인
        if (pitchRangeObject != null)
        {
            initialTargetP = pitchRangeObject.transform.position;
        }

        randomTargetP = initialTargetP;

        if (debugMode && logRandomGeneration)
        {
            Debug.Log($"타겟 위치 재설정: {initialTargetP}");
        }
    }

    /**
     * 랜덤 모드에서 무작위 공 설정을 생성하는 메서드
     * 각 구종마다 다른 확률로 선택되며, 속도도 조정됩니다.
     */
    private BallSettings GetRandomBallSettings()
    {
        float randomValue = Random.value * 100;
        int randomSpeed = speedValues[speedIndex];
        List<BallSettings> possibleSettings = new List<BallSettings>();

        // 확률에 따라 다른 구종 선택
        if (randomValue < 40) // Fastball 40%
        {
            possibleSettings = FindSettingsForSpeedAndType(randomSpeed, pitchSettingsManager.fastballSettings, "Fastball");
        }
        else if (randomValue < 60) // Changeup 20%
        {
            randomSpeed -= 10; // 체인지업은 속도 감소
            possibleSettings = FindSettingsForSpeedAndType(randomSpeed, pitchSettingsManager.changeupSettings, "Changeup");
        }
        else if (randomValue < 80) // Slider 20%
        {
            randomSpeed -= 10; // 슬라이더는 속도 감소
            possibleSettings = FindSettingsForSpeedAndType(randomSpeed, pitchSettingsManager.sliderSettings, "Slider");
        }
        else // Curve 20%
        {
            randomSpeed -= 20; // 커브는 속도 크게 감소
            possibleSettings = FindSettingsForSpeedAndType(randomSpeed, pitchSettingsManager.curveSettings, "Curve");
        }

        // 가능한 설정들 중에서 랜덤 선택
        if (possibleSettings.Count == 0)
        {
            if (debugMode && logRandomGeneration)
            {
                Debug.LogWarning("랜덤 모드에서 적합한 설정을 찾지 못했습니다. 기본값 사용");
            }
            return pitchSettingsManager.fastballSettings[0]; // 기본값 사용
        }

        BallSettings selectedSettings = possibleSettings[Random.Range(0, possibleSettings.Count)];

        if (debugMode && logRandomGeneration)
        {
            Debug.Log($"랜덤 설정 선택: {GetPitchTypeName(selectedSettings)}, 속도: {selectedSettings.throwPower * 0.36f:F1}km/h");
        }

        return selectedSettings;
    }

    /**
     * 특정 속도와 구종에 대한 설정 찾기
     */
    private List<BallSettings> FindSettingsForSpeedAndType(int targetSpeed, List<BallSettings> typeSettings, string typeName)
    {
        List<BallSettings> result = new List<BallSettings>();

        foreach (var settings in typeSettings)
        {
            if (settings.name == targetSpeed)
            {
                result.Add(settings);
            }
        }

        if (result.Count == 0 && debugMode && logRandomGeneration)
        {
            Debug.LogWarning($"{typeName} 구종에서 속도 {targetSpeed}km/h에 맞는 설정을 찾지 못했습니다.");
        }

        return result;
    }

    /**
     * 공의 충돌 여부를 확인하는 코루틴
     */
    private IEnumerator CheckBallHit(BallController ballController)
    {
        // 충돌 확인
        while (!ballController.hasCollided)
        {
            yield return null;
        }

        if (debugMode && logBallInfo)
        {
            Debug.Log($"공 충돌 감지: {pendingPitchType}");
        }
    }

    /**
     * 일정 시간 후 공을 제거하는 코루틴
     */
    private IEnumerator DestroyBallAfterTime(GameObject ball, float time)
    {
        yield return new WaitForSeconds(time);

        if (ball != null)
        {
            Destroy(ball);

            if (debugMode && logBallInfo)
            {
                Debug.Log($"공 자동 제거: {time}초 후");
            }
        }
    }

    /**
     * 현재 선택된 구종 이름을 반환하는 메서드
     * @return 현재 구종 이름 (Fastball, Slider, Curve, Changeup 등)
     */
    public string GetCurrentPitchTypeName()
    {
        // 랜덤 모드이면 "Random"을 반환
        if (isRandomMode)
        {
            return "Random";
        }

        // 현재 선택된 구종의 이름 가져오기
        if (currentPitchSettings == pitchSettingsManager.fastballSettings)
        {
            return "Fastball";
        }
        else if (currentPitchSettings == pitchSettingsManager.sliderSettings)
        {
            return "Slider";
        }
        else if (currentPitchSettings == pitchSettingsManager.curveSettings)
        {
            return "Curve";
        }
        else if (currentPitchSettings == pitchSettingsManager.changeupSettings)
        {
            return "Changeup";
        }

        return "Unknown";
    }

    /**
     * 현재 선택된 구속을 반환하는 메서드
     * @return 현재 구속 (km/h)
     */
    public int GetCurrentPitchSpeed()
    {
        return speedValues[speedIndex];
    }

    /**
     * 현재 선택된 구종과 구속 정보를 TextMeshPro로 출력하는 메서드
     */
    public void UpdatePitchInfoDisplay()
    {
        if (pitchInfoText == null)
            return;

        // 현재 선택된 구종 이름 가져오기
        string currentPitchType = GetCurrentPitchTypeName();

        // 현재 선택된 구속 가져오기
        int currentSpeed = speedValues[speedIndex];

        // 랜덤 모드 상태에 따라 다른 스타일 적용
        if (isRandomMode)
        {
            pitchInfoText.text = string.Format("<color=#FF6B00>RandomMode</color>\n{0} km/h",
                speedValues[speedIndex], speedValues[speedValues.Length - 1]);
        }
        else
        {
            // 구종별 색상 적용 (선택사항)
            string colorHex = "#FFFFFF"; // 기본 흰색
            if (currentPitchType == "Fastball") colorHex = "#FF0000"; // 빨강
            else if (currentPitchType == "Slider") colorHex = "#00AAFF"; // 파랑
            else if (currentPitchType == "Curve") colorHex = "#00FF00"; // 초록
            else if (currentPitchType == "Changeup") colorHex = "#FFAA00"; // 주황

            pitchInfoText.text = string.Format("<color={0}>{1}</color>\n{2} km/h",
                colorHex, currentPitchType, currentSpeed);
        }
    }

    /**
     * 타겟 위치를 시각적으로 표시
     */
    private void OnDrawGizmos()
    {
        if (!showTargetGizmo || !Application.isPlaying)
            return;

        // 현재 타겟 위치 표시
        Vector3 targetPos = isRandomMode ? randomTargetP : initialTargetP;
        Gizmos.color = targetGizmoColor;
        Gizmos.DrawSphere(targetPos, targetGizmoSize);

        // 디버그 모드일 때 추가 정보 표시
        if (debugMode)
        {
            // 각 구종별 범위 표시
            foreach (var entry in pitchRangeDataMap)
            {
                if (entry.Key != "Default")
                {
                    // 각 구종별로 다른 투명도로 표시
                    Color rangeColor = targetGizmoColor;
                    rangeColor.a = 0.2f;

                    switch (entry.Key)
                    {
                        case "Fastball": rangeColor = new Color(1f, 0f, 0f, 0.2f); break; // 빨강
                        case "Slider": rangeColor = new Color(0f, 0.5f, 1f, 0.2f); break; // 파랑
                        case "Curve": rangeColor = new Color(0f, 1f, 0f, 0.2f); break; // 초록
                        case "Changeup": rangeColor = new Color(1f, 0.5f, 0f, 0.2f); break; // 주황
                    }

                    Gizmos.color = rangeColor;
                    Gizmos.DrawCube(entry.Value.position, entry.Value.size);
                }
            }
        }
    }
}