/**
 * Ball_Spawner.cs - 공 생성 및 발사 제어 컴포넌트
 * 
 * 이 스크립트는 다양한 구종의 공을 주기적으로 생성하고 발사하는 역할을 담당합니다.
 * 여러 종류의 공(패스트볼, 슬라이더, 커브, 체인지업)과 다양한 
 * 속도 설정을 통해 다양한 물리적 특성을 가진 공을 만들어냅니다.
 * 
 * 키보드 조작:
 * Q - 패스트볼
 * W - 슬라이더
 * E - 커브
 * R - 체인지업
 * A - 속도 증가
 * S - 속도 감소
 * D - 랜덤 모드 토글
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TEST : MonoBehaviour
{
    [Header("공 생성 설정")]
    [Tooltip("공 생성 간격 (초)")]
    public float spawnRate = 5.0f;

    [Tooltip("공 자동 소멸 시간 (초)")]
    public float destroyTime = 5.0f;

    [Tooltip("생성할 공의 프리팹")]
    public GameObject ballPrefab;

    [Tooltip("공 발사 위치")]
    public Transform releasePoint;

    [Header("버튼 오브젝트")]
    [Tooltip("구종 선택 버튼들")]
    public GameObject fastballObject;
    public GameObject fastballObject2;
    public GameObject sliderObject;
    public GameObject sliderObject2;
    public GameObject curveObject;
    public GameObject curveObject2;
    public GameObject changeupObject;
    public GameObject changeupObject2;
    public GameObject knuckleballObject;
    public GameObject knuckleballObject2;

    [Tooltip("속도 조절 및 모드 버튼들")]
    public GameObject increaseSpeedObject;
    public GameObject increaseSpeedObject2;
    public GameObject decreaseSpeedObject;
    public GameObject decreaseSpeedObject2;
    public GameObject randomModeObject;
    public GameObject randomModeObject2;

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
    [Tooltip("타겟팅할 스트라이크 존 오브젝트")]
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

    [Header("키보드 조작")]
    [Tooltip("활성화된 키보드 조작 키를 표시합니다")]
    public string activeKeyboardControls = "Q:패스트볼, W:슬라이더, E:커브, R:체인지업, A:속도증가, S:속도감소, D:랜덤모드";

    public string pitchtypename;
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

    // 키보드 입력 제어 변수
    private float keyInputCooldown = 0.2f;  // 입력 간 쿨다운 시간(초)
    private float lastKeyInputTime = 0f;    // 마지막 키 입력 시간


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
        pitchSettingsManager = FindObjectOfType<PitchSettingsManager>();

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
        StartCoroutine(SpawnBalls());
    }

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
        if (fastballPitchRangeObject != null)
        {
            pitchRangeDataMap["Fastball"] = GetPitchRangeData(fastballPitchRangeObject);
            Debug.Log($"Fastball PitchRange initialized at {pitchRangeDataMap["Fastball"].position}");
        }
        else
        {
            pitchRangeDataMap["Fastball"] = defaultData;
        }

        // 슬라이더 PitchRange 설정
        if (sliderPitchRangeObject != null)
        {
            pitchRangeDataMap["Slider"] = GetPitchRangeData(sliderPitchRangeObject);
            Debug.Log($"Slider PitchRange initialized at {pitchRangeDataMap["Slider"].position}");
        }
        else
        {
            pitchRangeDataMap["Slider"] = defaultData;
        }

        // 커브 PitchRange 설정
        if (curvePitchRangeObject != null)
        {
            pitchRangeDataMap["Curve"] = GetPitchRangeData(curvePitchRangeObject);
            Debug.Log($"Curve PitchRange initialized at {pitchRangeDataMap["Curve"].position}");
        }
        else
        {
            pitchRangeDataMap["Curve"] = defaultData;
        }

        // 체인지업 PitchRange 설정
        if (changeupPitchRangeObject != null)
        {
            pitchRangeDataMap["Changeup"] = GetPitchRangeData(changeupPitchRangeObject);
            Debug.Log($"Changeup PitchRange initialized at {pitchRangeDataMap["Changeup"].position}");
        }
        else
        {
            pitchRangeDataMap["Changeup"] = defaultData;
        }
    }

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
            Debug.LogWarning($"{pitchRangeObj.name}에서 크기를 가져올 수 없습니다. 기본 크기를 사용합니다.");
        }

        return data;
    }

    private void SetRandomTargetPForPitchType(string pitchType)
    {
        // 해당 구종의 PitchRange 데이터 가져오기
        PitchRangeData rangeData;
        if (!pitchRangeDataMap.TryGetValue(pitchType, out rangeData))
        {
            rangeData = pitchRangeDataMap["Default"];
            Debug.LogWarning($"No PitchRange data for {pitchType}, using default");
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

        Debug.Log($"{pitchType} RandomTargetP set to: {randomTargetP} (범위: X ±{maxOffsetX}, Y ±{maxOffsetY})");
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
                Debug.LogWarning("PitchRange 오브젝트를 찾을 수 없습니다. 기본 위치값을 사용합니다.");
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
            Debug.LogWarning("PitchRange 오브젝트에서 크기를 가져올 수 없습니다. 기본 크기를 사용합니다.");
        }

        Debug.Log($"PitchRange 위치: {initialTargetP}, 크기: {pitchRangeSize}");
    }

    /**
     * 매 프레임마다 실행되는 업데이트 메서드
     * 키보드 입력을 처리합니다.
     */
    void Update()
    {
        // 키 입력 쿨다운 확인
        if (Time.time - lastKeyInputTime < keyInputCooldown)
        {
            return;
        }

        // 구종 선택 키
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SelectPitchType(pitchSettingsManager.fastballSettings);
            Debug.Log("패스트볼 선택됨 (키: Q)");
            lastKeyInputTime = Time.time;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            SelectPitchType(pitchSettingsManager.sliderSettings);
            Debug.Log("슬라이더 선택됨 (키: W)");
            lastKeyInputTime = Time.time;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SelectPitchType(pitchSettingsManager.curveSettings);
            Debug.Log("커브 선택됨 (키: E)");
            lastKeyInputTime = Time.time;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            SelectPitchType(pitchSettingsManager.changeupSettings);
            Debug.Log("체인지업 선택됨 (키: R)");
            lastKeyInputTime = Time.time;
        }

        // 속도 조절 키
        if (Input.GetKeyDown(KeyCode.A))
        {
            IncreaseSpeed();
            Debug.Log($"속도 증가: {speedValues[speedIndex]} km/h (키: A)");
            lastKeyInputTime = Time.time;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            DecreaseSpeed();
            Debug.Log($"속도 감소: {speedValues[speedIndex]} km/h (키: S)");
            lastKeyInputTime = Time.time;
        }

        // 랜덤 모드 토글 키
        if (Input.GetKeyDown(KeyCode.D))
        {
            ToggleRandomMode();
            Debug.Log($"랜덤 모드: {(isRandomMode ? "켜짐" : "꺼짐")} (키: D)");
            lastKeyInputTime = Time.time;
        }
    }

    /**
     * 버튼에 이벤트 리스너를 연결하는 메서드
     * 각 버튼이 눌렸을 때 해당 기능을 실행합니다.
     */
    private void AttachButtonListeners()
    {
        // 구종 선택 버튼
        AttachButton(fastballObject, () => SelectPitchType(pitchSettingsManager.fastballSettings));
        AttachButton(fastballObject2, () => SelectPitchType(pitchSettingsManager.fastballSettings));
        AttachButton(sliderObject, () => SelectPitchType(pitchSettingsManager.sliderSettings));
        AttachButton(sliderObject2, () => SelectPitchType(pitchSettingsManager.sliderSettings));
        AttachButton(curveObject, () => SelectPitchType(pitchSettingsManager.curveSettings));
        AttachButton(curveObject2, () => SelectPitchType(pitchSettingsManager.curveSettings));
        AttachButton(changeupObject, () => SelectPitchType(pitchSettingsManager.changeupSettings));
        AttachButton(changeupObject2, () => SelectPitchType(pitchSettingsManager.changeupSettings));

        // 너클볼 버튼은 요청에 따라 비활성화
        if (knuckleballObject != null)
            knuckleballObject.SetActive(false);
        if (knuckleballObject2 != null)
            knuckleballObject2.SetActive(false);

        // 속도 조절 및 모드 버튼
        AttachButton(increaseSpeedObject, IncreaseSpeed);
        AttachButton(increaseSpeedObject2, IncreaseSpeed);
        AttachButton(decreaseSpeedObject, DecreaseSpeed);
        AttachButton(decreaseSpeedObject2, DecreaseSpeed);
        AttachButton(randomModeObject, ToggleRandomMode);
        AttachButton(randomModeObject2, ToggleRandomMode);
    }

    /**
     * 버튼 오브젝트에 기능을 연결하는 유틸리티 메서드
     */
    private void AttachButton(GameObject buttonObject, UnityAction onPressAction, UnityAction onReleaseAction = null)
    {
        if (buttonObject == null)
            return;

        buttonVR buttonVRScript = buttonObject.GetComponent<buttonVR>();
        if (buttonVRScript != null)
        {
            buttonVRScript.onPress.AddListener(onPressAction);
            if (onReleaseAction != null)
            {
                buttonVRScript.onRelease.AddListener(onReleaseAction);
            }
        }
        else
        {
            Debug.LogError($"No buttonVR script found on {buttonObject.name}");
        }
    }

    /**
     * 구종 유형을 선택하는 메서드
     */
    private void SelectPitchType(List<BallSettings> pitchSettings)
    {
        currentPitchSettings = pitchSettings;
        isRandomMode = false;
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
    }

    /**
     * 구종의 이름을 반환하는 메서드
     */
    private string GetPitchTypeName(BallSettings settings)
    {
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
     * 공을 생성하고 발사하는 코루틴
     * 설정된 간격으로 공을 생성하고 물리 특성을 적용합니다.
     */
    private IEnumerator SpawnBalls()
    {
        while (true)
        {
            // 공 생성
            GameObject ball = Instantiate(ballPrefab, releasePoint.position, releasePoint.rotation);
            BallSettings ballSettings;
            string pitchType = "";

            // 모드에 따라 설정 및 목표 위치 결정
            if (isRandomMode)
            {
                ballSettings = GetRandomBallSettings();
                pitchType = GetPitchTypeName(ballSettings);
                SetRandomTargetPForPitchType(pitchType);  // 구종별 다른 PitchRange 오브젝트 사용
            }
            else
            {
                ballSettings = currentPitchSettings[speedIndex % currentPitchSettings.Count];
                pitchType = GetPitchTypeName(ballSettings);
                ResetTargetP();
            }

            // 공에 설정 적용
            BallController ballController = ball.GetComponent<BallController>();
            if (ballController != null)
            {
                ballController.SetBallSettings(ballSettings);
                ballController.SetPitchType(pitchType);

                // 발사 방향 계산 및 발사
                ballController.CalculateDirection(releasePoint.position, isRandomMode ? randomTargetP : initialTargetP);
                ballController.LaunchBall();
                StartCoroutine(CheckBallHit(ballController, ballSettings));
            }
            else
            {
                Debug.LogError("Ball_Controller component not found on ball prefab!");
            }

            // 일정 시간 후 공 제거
            StartCoroutine(DestroyBallAfterTime(ball, destroyTime));

            // 다음 공 생성까지 대기
            yield return new WaitForSeconds(spawnRate);
        }
    }

    /**
     * 랜덤 모드에서 목표 위치를 무작위로 설정하는 메서드
     * PitchRange 오브젝트의 위치와 크기를 기반으로 랜덤한 위치를 설정합니다.
     */
    private void SetRandomTargetP()
    {
        // PitchRange의 너비와 높이의 절반을 오프셋 최대값으로 사용 (X, Y축만)
        float maxOffsetX = pitchRangeSize.x * 0.5f;
        float maxOffsetY = pitchRangeSize.y * 0.5f;

        // PitchRange 오브젝트 내부에서 무작위 위치 선택
        randomTargetP = new Vector3(
            initialTargetP.x + Random.Range(-maxOffsetX, maxOffsetX),
            initialTargetP.y + Random.Range(-maxOffsetY, maxOffsetY),
            initialTargetP.z
        );

        Debug.Log($"Random TargetP set to: {randomTargetP} (범위: X ±{maxOffsetX}, Y ±{maxOffsetY})");
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
        Debug.Log($"Reset TargetP to PitchRange center: {initialTargetP}");
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
            foreach (var settings in pitchSettingsManager.fastballSettings)
            {
                if (settings.name == randomSpeed)
                {
                    possibleSettings.Add(settings);
                }
            }
        }
        else if (randomValue < 60) // Changeup 20%
        {
            randomSpeed -= 10; // 체인지업은 속도 감소
            foreach (var settings in pitchSettingsManager.changeupSettings)
            {
                if (settings.name == randomSpeed)
                {
                    possibleSettings.Add(settings);
                }
            }
        }
        else if (randomValue < 80) // Slider 20%
        {
            randomSpeed -= 10; // 슬라이더는 속도 감소
            foreach (var settings in pitchSettingsManager.sliderSettings)
            {
                if (settings.name == randomSpeed)
                {
                    possibleSettings.Add(settings);
                }
            }
        }
        else // Curve 20%
        {
            randomSpeed -= 20; // 커브는 속도 크게 감소
            foreach (var settings in pitchSettingsManager.curveSettings)
            {
                if (settings.name == randomSpeed)
                {
                    possibleSettings.Add(settings);
                }
            }
        }

        // 가능한 설정들 중에서 랜덤 선택
        if (possibleSettings.Count == 0)
        {
            Debug.LogWarning("No matching ball settings found for random mode, using fallback");
            return pitchSettingsManager.fastballSettings[0]; // 기본값 사용
        }

        return possibleSettings[Random.Range(0, possibleSettings.Count)];
    }

    /**
     * 공의 충돌 여부를 확인하는 코루틴
     */
    private IEnumerator CheckBallHit(BallController ballController, BallSettings ballSettings)
    {
        while (!ballController.hasCollided)
        {
            yield return null;
        }

        // 타격 정보 저장
        if (isRandomMode)
        {
            // Ball_Controller에서 필요한 속도 정보 가져오기
            // 참고: Speed 속성이 Ball_Controller에 없으면 아래 코드는 수정이 필요함
            // hitSpeed = ballController.Speed;
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
        }
    }
}