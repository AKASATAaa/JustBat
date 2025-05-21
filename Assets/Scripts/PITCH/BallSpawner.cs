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

    [Header("애니메이션 설정")]
    [Tooltip("투수 애니메이터 컴포넌트")]
    public Animator pitcherAnimator;

    [Tooltip("애니메이션 트리거 파라미터 이름")]
    public string idleAnimTrigger = "Idle";
    public string readyAnimTrigger = "Ready";

    [Tooltip("투구 스타일 애니메이션 트리거")]
    public string overhandAnimTrigger = "PitchOverhand";
    public string sidearmAnimTrigger = "PitchSidearm";
    public string threeQuarterAnimTrigger = "PitchThreeQuarter";
    public string underhandAnimTrigger = "PitchUnderhand";

    [Header("버튼 오브젝트")]
    [Tooltip("구종 선택 버튼들")]
    public Button fastballButton;
    public Button sliderButton;
    public Button curveButton;
    public Button changeupButton;
    public Button knuckleballButton;

    [Tooltip("투구 스타일 버튼들")]
    public Button overhandButton;
    public Button sidearmButton;
    public Button threeQuarterButton;
    public Button underhandButton;

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

    // 투구 스타일 정보
    private enum PitchStyle
    {
        Overhand,
        Sidearm,
        ThreeQuarter,
        Underhand
    }


    [Header("공 제한 설정")]
    [Tooltip("최대 공 개수 (0은 무제한)")]
    public int maxBallCount = 10;

    // 현재까지 생성된 공의 개수
    private int currentBallCount = 0;


    // 게임 결과 패널 참조
    public GameObject resultPanel;

    // 모든 공을 던진 후 결과 표시 이벤트


    public UnityEvent onAllBallsThrown = new UnityEvent();

    private PitchStyle currentPitchStyle = PitchStyle.Overhand;

    // 구종 및 속도 설정 관련 변수
    private PitchSettingsManager pitchSettingsManager;
    private List<BallSettings> currentPitchSettings;
    public int speedIndex;
    private int[] speedValues = { 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170 };
    public bool isRandomMode;

    // 목표 위치 관련 변수
    private Vector3 initialTargetP;
    private Vector3 randomTargetP;
    private Vector3 pitchRangeSize; // PitchRange 오브젝트의 크기

    // 애니메이션 이벤트에서 사용할 변수
    private bool isIdleAnimationInProgress = false;
    private bool isReadyAnimationInProgress = false;
    private bool isPitchAnimationInProgress = false;
    private bool waitingToSpawn = false;
    private BallSettings pendingBallSettings;
    private string pendingPitchType = "";

    // 애니메이션 상태 관리
    private enum AnimationState
    {
        Idle,
        Ready,
        Pitching,
        None
    }

    private AnimationState currentAnimState = AnimationState.None;

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
        //speedIndex = 0;
        //isRandomMode = false;

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

        // 투구 스타일 버튼
        AttachUIButton(overhandButton, () => SelectPitchStyle(PitchStyle.Overhand));
        AttachUIButton(sidearmButton, () => SelectPitchStyle(PitchStyle.Sidearm));
        AttachUIButton(threeQuarterButton, () => SelectPitchStyle(PitchStyle.ThreeQuarter));
        AttachUIButton(underhandButton, () => SelectPitchStyle(PitchStyle.Underhand));

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
     * 투구 스타일을 선택하는 메서드
     */
    private void SelectPitchStyle(PitchStyle style)
    {
        currentPitchStyle = style;

        if (debugMode)
        {
            Debug.Log($"투구 스타일 변경: {currentPitchStyle}");
        }

        UpdatePitchInfoDisplay();
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
        if (isRandomMode && speedValues[speedIndex] < 70)
        {
            speedIndex = System.Array.IndexOf(speedValues, 70);
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
        if (isRandomMode && speedValues[speedIndex] < 70)
        {
            speedIndex = System.Array.IndexOf(speedValues, 70);
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
        if (isRandomMode && speedValues[speedIndex] < 70)
        {
            speedIndex = System.Array.IndexOf(speedValues, 70);
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
     * 애니메이션 상태에 따라 적절한 애니메이션을 시작합니다.
     */
    private IEnumerator AnimatedPitchingSequence()
    {
        yield return new WaitForSeconds(1.0f); // 초기 대기 시간

        // 처음에는 Idle 애니메이션으로 시작
        StartIdleAnimation();

        while (true)
        {
            // 현재 애니메이션 상태에 따라 다음 상태로 전환
            switch (currentAnimState)
            {
                case AnimationState.Idle:
                    if (!isIdleAnimationInProgress)
                    {
                        // Idle 애니메이션이 끝났으면 Ready 애니메이션 시작
                        StartReadyAnimation();
                    }
                    break;

                case AnimationState.Ready:
                    if (!isReadyAnimationInProgress)
                    {
                        // Ready 애니메이션이 끝났으면 Pitch 애니메이션 시작
                        StartPitchAnimation();
                    }
                    break;

                case AnimationState.Pitching:
                    if (!isPitchAnimationInProgress)
                    {
                        // Pitch 애니메이션이 끝났으면 다시 Idle 애니메이션 시작
                        StartIdleAnimation();
                    }
                    break;

                case AnimationState.None:
                    // 초기 상태인 경우 Idle 애니메이션 시작
                    StartIdleAnimation();
                    break;
            }

            yield return null;
        }
    }

    /**
     * Idle 애니메이션을 시작하는 메서드
     */
    private void StartIdleAnimation()
    {
        if (pitcherAnimator == null || isIdleAnimationInProgress)
            return;

        // 다른 트리거 리셋
        pitcherAnimator.ResetTrigger(readyAnimTrigger);
        pitcherAnimator.ResetTrigger(overhandAnimTrigger);
        pitcherAnimator.ResetTrigger(sidearmAnimTrigger);
        pitcherAnimator.ResetTrigger(threeQuarterAnimTrigger);
        pitcherAnimator.ResetTrigger(underhandAnimTrigger);

        isIdleAnimationInProgress = true;
        currentAnimState = AnimationState.Idle;

        // Idle 애니메이션 트리거 설정
        pitcherAnimator.SetTrigger(idleAnimTrigger);

        if (debugMode && logAnimationEvents)
        {
            Debug.Log("Idle 애니메이션 시작");
        }
    }

    /**
     * Ready 애니메이션을 시작하는 메서드
     */
    private void StartReadyAnimation()
    {
        if (pitcherAnimator == null || isReadyAnimationInProgress)
            return;

        isReadyAnimationInProgress = true;
        currentAnimState = AnimationState.Ready;

        // 투구 설정 준비
        PrepareNextPitch();

        // Ready 애니메이션 트리거 설정
        pitcherAnimator.SetTrigger(readyAnimTrigger);

        if (debugMode && logAnimationEvents)
        {
            Debug.Log("Ready 애니메이션 시작");
        }
    }

    /**
     * 다음 투구를 위한 설정 준비
     */
    private void PrepareNextPitch()
    {
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
    }

    /**
     * Pitch 애니메이션을 시작하는 메서드
     */
    private void StartPitchAnimation()
    {
        if (pitcherAnimator == null || isPitchAnimationInProgress)
            return;

        isPitchAnimationInProgress = true;
        currentAnimState = AnimationState.Pitching;

        // 현재 선택된 투구 스타일에 맞는 애니메이션 트리거 실행
        string pitchTrigger = GetPitchStyleTrigger();
        pitcherAnimator.SetTrigger(pitchTrigger);

        if (debugMode && logAnimationEvents)
        {
            Debug.Log($"{currentPitchStyle} 투구 애니메이션 시작 (구종: {pendingPitchType}, 트리거: {pitchTrigger})");
        }
    }

    /**
     * 현재 선택된 투구 스타일에 대한 애니메이션 트리거 반환
     */
    private string GetPitchStyleTrigger()
    {
        switch (currentPitchStyle)
        {
            case PitchStyle.Overhand:
                return overhandAnimTrigger;
            case PitchStyle.Sidearm:
                return sidearmAnimTrigger;
            case PitchStyle.ThreeQuarter:
                return threeQuarterAnimTrigger;
            case PitchStyle.Underhand:
                return underhandAnimTrigger;
            default:
                return overhandAnimTrigger; // 기본값
        }
    }

    /**
     * 애니메이션 이벤트에서 호출되는 Idle 애니메이션 완료 메서드
     */
    public void OnIdleAnimationComplete()
    {
        // 트리거 리셋
        pitcherAnimator.ResetTrigger(idleAnimTrigger);

        // 상태 업데이트
        isIdleAnimationInProgress = false;

        if (debugMode && logAnimationEvents)
        {
            Debug.Log("Idle 애니메이션 완료");
        }
    }

    /**
     * Ready 애니메이션 완료 후 트리거 리셋 메서드
     */
    public void OnReadyAnimationComplete()
    {
        // 트리거 리셋
        pitcherAnimator.ResetTrigger(readyAnimTrigger);

        // 상태 업데이트
        isReadyAnimationInProgress = false;

        if (debugMode && logAnimationEvents)
        {
            Debug.Log("Ready 애니메이션 완료");
        }
    }

    /**
     * Pitch 애니메이션 완료 후 트리거 리셋 메서드
     */
    public void OnPitchAnimationComplete()
    {
        // 모든 피치 스타일 트리거 리셋
        pitcherAnimator.ResetTrigger(overhandAnimTrigger);
        pitcherAnimator.ResetTrigger(sidearmAnimTrigger);
        pitcherAnimator.ResetTrigger(threeQuarterAnimTrigger);
        pitcherAnimator.ResetTrigger(underhandAnimTrigger);

        // 상태 업데이트
        isPitchAnimationInProgress = false;

        if (debugMode && logAnimationEvents)
        {
            Debug.Log("Pitch 애니메이션 완료");
        }
    }


    /**
     * 애니메이션 이벤트에서 호출되는 공 발사 메서드
     * 애니메이션의 특정 프레임에서 이 메서드를 호출하여 공을 생성하고 발사합니다.
     */
    public void OnAnimationReleaseBall()
    {
        // 최대 공 개수 확인
        if (maxBallCount > 0 && currentBallCount >= maxBallCount)
        {
            if (debugMode && logBallInfo)
            {
                Debug.Log($"최대 공 개수({maxBallCount}개)에 도달하여 더 이상 공을 생성하지 않습니다.");
            }
            return; // 최대 개수 도달 시 공 생성 안함
        }

        // 공 생성
        GameObject ball = Instantiate(ballPrefab, releasePoint.position, releasePoint.rotation);

        // 공 개수 증가
        currentBallCount++;

        // 공에 설정 적용
        ApplyBallSettings(ball);

        // 일정 시간 후 공 제거
        StartCoroutine(DestroyBallAfterTime(ball, destroyTime));

        if (debugMode && logBallInfo)
        {
            Debug.Log($"[{currentBallCount}/{maxBallCount}] {pendingPitchType} 공이 생성되고 발사되었습니다. 투구 스타일: {currentPitchStyle}, 속도: {pendingBallSettings.throwPower * 0.36f:F1}km/h");
        }

        // 최대 공 개수에 도달한 경우
        if (maxBallCount > 0 && currentBallCount >= maxBallCount)
        {
            if (debugMode)
            {
                Debug.Log($"최대 공 개수({maxBallCount}개)에 도달하여 투구를 종료합니다.");
            }

            // 마지막 공을 던진 후 2초 후에 결과 패널 표시
            StartCoroutine(ShowResultAfterDelay(2.0f));
        }
    }

    // 일정 시간 후 결과 패널을 표시하는 코루틴
    private IEnumerator ShowResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 이벤트 발생 - 모든 공을 던졌음을 알림
        onAllBallsThrown.Invoke();

        // 결과 패널이 설정되어 있으면 활성화
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            if (debugMode)
            {
                Debug.Log("결과 패널 표시");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("결과 패널이 설정되지 않았습니다.");
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
             * 투구 스타일에 따른 효과 적용
             * 각 투구 스타일에 따라 다른 물리적 특성 부여
             */
       

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

                // 투구 스타일에 따라 타겟 위치 약간 조정
                Vector3 styleOffset = Vector3.zero;
                switch (currentPitchStyle)
                {
                    case PitchStyle.Sidearm:
                        // 사이드암은 약간 좌우로 치우침
                        styleOffset = new Vector3(Random.Range(-0.1f, 0.1f), 0, 0);
                        break;

                    case PitchStyle.ThreeQuarter:
                        // 쓰리쿼터는 약간 대각선으로 치우침
                        styleOffset = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0);
                        break;

                    case PitchStyle.Underhand:
                        // 언더핸드는 약간 아래로 치우침
                        styleOffset = new Vector3(0, Random.Range(-0.1f, 0), 0);
                        break;
                }

                randomTargetP = initialTargetP + styleOffset;

                if (debugMode && logRandomGeneration)
                {
                    Debug.Log($"타겟 위치 재설정: {randomTargetP} (투구 스타일: {currentPitchStyle}, 오프셋: {styleOffset})");
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
                    Debug.Log($"공 충돌 감지: {pendingPitchType}, 투구 스타일: {currentPitchStyle}");
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

                // 투구 스타일 텍스트
                string styleText = GetPitchStyleText();

                // 랜덤 모드 상태에 따라 다른 스타일 적용
                if (isRandomMode)
                {
                    pitchInfoText.text = string.Format("<color=#FF6B00>Random</color>\n<color=#8A2BE2>{0}</color>\n{1} km/h",
                        styleText, speedValues[speedIndex]);
                }
                else
                {
                    // 구종별 색상 적용
                    string colorHex = "#FFFFFF"; // 기본 흰색
                    if (currentPitchType == "Fastball") colorHex = "#FF0000"; // 빨강
                    else if (currentPitchType == "Slider") colorHex = "#00AAFF"; // 파랑
                    else if (currentPitchType == "Curve") colorHex = "#00FF00"; // 초록
                    else if (currentPitchType == "Changeup") colorHex = "#FFAA00"; // 주황

                    pitchInfoText.text = string.Format("<color={0}>{1}</color>\n<color=#8A2BE2>{2}</color>\n{3} km/h",
                        colorHex, currentPitchType, styleText, currentSpeed);
                }
            }

            /**
             * 현재 투구 스타일의 텍스트 표현을 반환
             */
            private string GetPitchStyleText()
            {
                switch (currentPitchStyle)
                {
                    case PitchStyle.Overhand:
                        return "Over";
                    case PitchStyle.Sidearm:
                        return "Side";
                    case PitchStyle.ThreeQuarter:
                        return "3Q";
                    case PitchStyle.Underhand:
                        return "Under";
                    default:
                        return "Ovr";
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

                // 투구 스타일에 따라 기즈모 색상 변경
                Color styleColor = targetGizmoColor;
                switch (currentPitchStyle)
                {
                    case PitchStyle.Overhand:
                        styleColor = new Color(0.5f, 0f, 1f, 0.5f); // 보라색
                        break;
                    case PitchStyle.Sidearm:
                        styleColor = new Color(1f, 0.7f, 0f, 0.5f); // 주황색
                        break;
                    case PitchStyle.ThreeQuarter:
                        styleColor = new Color(0f, 0.7f, 0.7f, 0.5f); // 청록색
                        break;
                    case PitchStyle.Underhand:
                        styleColor = new Color(0.7f, 0.3f, 0.1f, 0.5f); // 갈색
                        break;
                }

                Gizmos.color = styleColor;
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

                    // 투구 스타일에 따른 예상 경로 표시
                    Vector3 releasePos = releasePoint != null ? releasePoint.position : Vector3.zero;
                    Gizmos.color = styleColor;
                    Gizmos.DrawLine(releasePos, targetPos);

                    // 스타일별 특성을 기즈모로 표시
                    switch (currentPitchStyle)
                    {
                        case PitchStyle.Sidearm:
                            // 사이드암 회전 표시
                            DrawRotationGizmo(targetPos, new Vector3(0, 0, 1), 0.3f, styleColor);
                            break;

                        case PitchStyle.ThreeQuarter:
                            // 쓰리쿼터 회전 표시
                            DrawRotationGizmo(targetPos, new Vector3(0.5f, 0.5f, 0), 0.2f, styleColor);
                            break;

                        case PitchStyle.Underhand:
                            // 언더핸드 회전 표시
                            DrawRotationGizmo(targetPos, new Vector3(1, 0, 0), 0.4f, styleColor);
                            break;
                    }
                }
            }

            /**
             * 회전 방향을 나타내는 기즈모 그리기
             */
            private void DrawRotationGizmo(Vector3 position, Vector3 axis, float strength, Color color)
            {
                // 회전 축을 기준으로 원형 기즈모 표시
                Color rotColor = color;
                rotColor.a = 0.3f;
                Gizmos.color = rotColor;

                int segments = 16;
                float radius = 0.15f * strength;
                Vector3 lastPoint = position + Vector3.ProjectOnPlane(Vector3.right, axis).normalized * radius;

                for (int i = 1; i <= segments; i++)
                {
                    float angle = i * (360f / segments) * Mathf.Deg2Rad;
                    Vector3 direction = Vector3.ProjectOnPlane(
                        new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0),
                        axis
                    ).normalized;

                    Vector3 nextPoint = position + direction * radius;
                    Gizmos.DrawLine(lastPoint, nextPoint);
                    lastPoint = nextPoint;
                }

                // 회전 방향 화살표 표시
                Gizmos.DrawRay(position, axis * 0.2f);
            }



    public bool SetSpeed(int speed)
    {
        // speedValues 배열에서 해당 속도에 맞는 인덱스 찾기
        int index = -1;
        for (int i = 0; i < speedValues.Length; i++)
        {
            if (speedValues[i] == speed)
            {
                index = i;
                break;
            }
        }

        // 유효한 인덱스가 아니면 실패
        if (index < 0 || index >= speedValues.Length)
        {
            if (debugMode)
            {
                Debug.LogWarning($"유효하지 않은 구속입니다: {speed}km/h");
            }
            return false;
        }

        // 인덱스 설정 및 UI 업데이트
        speedIndex = index;
        UpdatePitchInfoDisplay();

        if (debugMode)
        {
            Debug.Log($"구속 설정: {speed}km/h (인덱스: {speedIndex})");
        }

        return true;
    }

    /**
     * 외부에서 구질 모드를 설정하는 메서드
     * @param isRandom true면 랜덤 모드, false면 직구(현재 선택된 구종) 모드
     */
    public void SetPitchMode(bool isRandom)
    {
        isRandomMode = isRandom;

        // 랜덤 모드일 때는 최소 속도가 90km/h이어야 함
        if (isRandomMode && speedValues[speedIndex] < 90)
        {
            int newIndex = 0;
            for (int i = 0; i < speedValues.Length; i++)
            {
                if (speedValues[i] >= 90)
                {
                    newIndex = i;
                    break;
                }
            }
            speedIndex = newIndex;
        }

        // 직구 모드일 경우 패스트볼 선택
        if (!isRandom)
        {
            currentPitchSettings = pitchSettingsManager.fastballSettings;
            pitchTypeName = "Fastball";
        }

        // UI 업데이트
        UpdatePitchInfoDisplay();

        if (debugMode)
        {
            Debug.Log($"구질 모드 설정: {(isRandom ? "랜덤" : "직구(패스트볼)")}");
        }
    }

    /**
     * 외부에서 투구 스타일을 설정하는 메서드
     * @param style 설정할 투구 스타일 (0: 오버핸드, 1: 사이드암, 2: 쓰리쿼터, 3: 언더핸드)
     * @return 설정 성공 여부
     */
    public bool SetPitchStyle(int style)
    {
        switch (style)
        {
            case 0:
                currentPitchStyle = PitchStyle.Overhand;
                break;
            case 1:
                currentPitchStyle = PitchStyle.Sidearm;
                break;
            case 2:
                currentPitchStyle = PitchStyle.ThreeQuarter;
                break;
            case 3:
                currentPitchStyle = PitchStyle.Underhand;
                break;
            default:
                if (debugMode)
                {
                    Debug.LogWarning($"유효하지 않은 투구 스타일 인덱스: {style}");
                }
                return false;
        }

        // UI 업데이트
        UpdatePitchInfoDisplay();

        if (debugMode)
        {
            Debug.Log($"투구 스타일 설정: {currentPitchStyle}");
        }

        return true;
    }

    /**
     * 특정 구종을 선택하는 공개 메서드
     * @param pitchType 구종 유형 (0: 패스트볼, 1: 슬라이더, 2: 커브, 3: 체인지업)
     * @return 설정 성공 여부
     */
    public bool SelectPitchTypeByIndex(int pitchType)
    {
        switch (pitchType)
        {
            case 0:
                currentPitchSettings = pitchSettingsManager.fastballSettings;
                pitchTypeName = "Fastball";
                break;
            case 1:
                currentPitchSettings = pitchSettingsManager.sliderSettings;
                pitchTypeName = "Slider";
                break;
            case 2:
                currentPitchSettings = pitchSettingsManager.curveSettings;
                pitchTypeName = "Curve";
                break;
            case 3:
                currentPitchSettings = pitchSettingsManager.changeupSettings;
                pitchTypeName = "Changeup";
                break;
            default:
                if (debugMode)
                {
                    Debug.LogWarning($"유효하지 않은 구종 유형 인덱스: {pitchType}");
                }
                return false;
        }

        isRandomMode = false;
        UpdatePitchInfoDisplay();

        if (debugMode)
        {
            Debug.Log($"구종 선택: {pitchTypeName}");
        }

        return true;
    }

    /**
     * 현재 설정 정보 로그 출력 메서드
     */
    public void LogCurrentSettings()
    {
        if (!debugMode)
            return;

        Debug.Log("======= 현재 BallSpawner 설정 =======");
        Debug.Log($"구종: {(isRandomMode ? "랜덤" : pitchTypeName)}");
        Debug.Log($"구속: {speedValues[speedIndex]}km/h");
        Debug.Log($"투구 스타일: {currentPitchStyle}");
        Debug.Log("===================================");
    }



}
