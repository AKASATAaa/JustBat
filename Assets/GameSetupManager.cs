using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 시작 전 설정 관리와 게임 시작 후 BallSpawner 활성화를 담당하는 스크립트
/// </summary>
public class GameSetupManager : MonoBehaviour
{
    [Header("BallSpawner 참조")]
    [SerializeField] private BallSpawner ballSpawner; // 공 스포너 참조

    [Header("설정 UI 요소")]
    [SerializeField] private GameObject setupPanel; // 설정 패널 (모든 버튼들 포함)
    [SerializeField] private Button startGameButton; // 게임 시작 버튼

    [Header("속도 선택 버튼")]
    [SerializeField] private Button speed80Button;
    [SerializeField] private Button speed100Button;
    [SerializeField] private Button speed120Button;
    [SerializeField] private Button speed150Button;

    [Header("모드 선택 버튼")]
    [SerializeField] private Button fastballButton; // 직구 모드
    [SerializeField] private Button randomModeButton; // 랜덤 모드

    [Header("투구 폼 선택 버튼")]
    [SerializeField] private Button overhandButton; // 오버핸드
    [SerializeField] private Button sidearmButton; // 사이드암
    [SerializeField] private Button threeQuarterButton; // 쓰리쿼터
    [SerializeField] private Button underhandButton; // 언더핸드

    [Header("선택 정보 표시")]
    [SerializeField] private TextMeshProUGUI selectedSpeedText;
    [SerializeField] private TextMeshProUGUI selectedModeText;
    [SerializeField] private TextMeshProUGUI selectedStyleText;

    [Header("결과 패널")]
    [SerializeField] private GameObject resultPanel; // 모든 공을 던진 후 표시할 결과 패널
    [SerializeField] private Button restartButton; // 재시작 버튼



    [Header("디버그 설정")]
    [SerializeField] private bool debugMode = false;

    // 선택된 값들
    private int selectedSpeedIndex = 1; // 기본값 100km/h (인덱스 1)
    private int[] speedOptions = { 1, 3, 5, 8 };
    private int selectedModeIndex = 0; // 기본값 직구 모드
    private int selectedStyleIndex = 0; // 기본값 오버핸드

    // 선택 상태를 나타내는 색상
    private Color selectedColor = new Color(0.2f, 0.8f, 0.2f); // 녹색
    private Color unselectedColor = Color.white;

    void Awake()
    {
        // BallSpawner 찾기 (필요한 경우)
        if (ballSpawner == null)
        {
            ballSpawner = FindObjectOfType<BallSpawner>();
        }

        // BallSpawner 비활성화 - 게임 시작 전까지 공 던지지 않음
        if (ballSpawner != null)
        {
            ballSpawner.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // 초기 설정
        InitializeSetup();
    }

    /// <summary>
    /// 설정 초기화 및 버튼 이벤트 연결
    /// </summary>
    private void InitializeSetup()
    {
        // BallSpawner 참조 확인
        if (ballSpawner == null)
        {
            ballSpawner = FindObjectOfType<BallSpawner>();
            if (ballSpawner == null)
            {
                Debug.LogError("BallSpawner를 찾을 수 없습니다. 참조를 설정하거나 씬에 BallSpawner 컴포넌트가 있는지 확인하세요.");
                return;
            }
        }

        // 속도 버튼 이벤트 연결
        if (speed80Button != null) speed80Button.onClick.AddListener(() => SelectSpeed(0)); // 80km/h
        if (speed100Button != null) speed100Button.onClick.AddListener(() => SelectSpeed(1)); // 100km/h
        if (speed120Button != null) speed120Button.onClick.AddListener(() => SelectSpeed(2)); // 120km/h
        if (speed150Button != null) speed150Button.onClick.AddListener(() => SelectSpeed(3)); // 150km/h

        // 모드 버튼 이벤트 연결
        if (fastballButton != null) fastballButton.onClick.AddListener(() => SelectMode(0)); // 직구 모드
        if (randomModeButton != null) randomModeButton.onClick.AddListener(() => SelectMode(1)); // 랜덤 모드

        // 투구 폼 버튼 이벤트 연결
        if (overhandButton != null) overhandButton.onClick.AddListener(() => SelectStyle(0)); // 오버핸드
        if (sidearmButton != null) sidearmButton.onClick.AddListener(() => SelectStyle(1)); // 사이드암
        if (threeQuarterButton != null) threeQuarterButton.onClick.AddListener(() => SelectStyle(2)); // 쓰리쿼터
        if (underhandButton != null) underhandButton.onClick.AddListener(() => SelectStyle(3)); // 언더핸드

        // 게임 시작 버튼 이벤트 연결
        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);

        // 기본 선택된 버튼 표시
        UpdateButtonVisuals();
        UpdateInfoTexts();
    }

    /// <summary>
    /// 속도 선택 메서드
    /// </summary>
    public void SelectSpeed(int speedIndex)
    {
        selectedSpeedIndex = speedIndex;
        if (debugMode)
        {
            Debug.Log($"속도 선택: {speedOptions[speedIndex]}km/h");
        }
        UpdateButtonVisuals();
        UpdateInfoTexts();
    }

    /// <summary>
    /// 모드 선택 메서드
    /// </summary>
    public void SelectMode(int modeIndex)
    {
        selectedModeIndex = modeIndex;
        if (debugMode)
        {
            Debug.Log($"모드 선택: {(modeIndex == 0 ? "직구" : "랜덤")}");
        }
        UpdateButtonVisuals();
        UpdateInfoTexts();
    }

    /// <summary>
    /// 투구 폼 선택 메서드
    /// </summary>
    public void SelectStyle(int styleIndex)
    {
        selectedStyleIndex = styleIndex;
        if (debugMode)
        {
            Debug.Log($"투구 폼 선택: {GetStyleName(styleIndex)}");
        }
        UpdateButtonVisuals();
        UpdateInfoTexts();
    }

    /// <summary>
    /// 버튼 시각적 상태 업데이트
    /// </summary>
    private void UpdateButtonVisuals()
    {
        // 속도 버튼 색상 업데이트
        UpdateButtonColor(speed80Button, selectedSpeedIndex == 0);
        UpdateButtonColor(speed100Button, selectedSpeedIndex == 1);
        UpdateButtonColor(speed120Button, selectedSpeedIndex == 2);
        UpdateButtonColor(speed150Button, selectedSpeedIndex == 3);

        // 모드 버튼 색상 업데이트
        UpdateButtonColor(fastballButton, selectedModeIndex == 0);
        UpdateButtonColor(randomModeButton, selectedModeIndex == 1);

        // 투구 폼 버튼 색상 업데이트
        UpdateButtonColor(overhandButton, selectedStyleIndex == 0);
        UpdateButtonColor(sidearmButton, selectedStyleIndex == 1);
        UpdateButtonColor(threeQuarterButton, selectedStyleIndex == 2);
        UpdateButtonColor(underhandButton, selectedStyleIndex == 3);
    }

    /// <summary>
    /// 버튼 색상 업데이트
    /// </summary>
    private void UpdateButtonColor(Button button, bool isSelected)
    {
        if (button == null) return;

        // 이미지 컴포넌트 가져오기
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? selectedColor : unselectedColor;
        }
    }

    /// <summary>
    /// 선택 정보 텍스트 업데이트
    /// </summary>
    private void UpdateInfoTexts()
    {
        if (selectedSpeedText != null)
        {
            selectedSpeedText.text = $"속도: {speedOptions[selectedSpeedIndex]}km/h";
        }

        if (selectedModeText != null)
        {
            selectedModeText.text = $"모드: {(selectedModeIndex == 0 ? "직구" : "랜덤")}";
        }

        if (selectedStyleText != null)
        {
            selectedStyleText.text = $"투구 폼: {GetStyleName(selectedStyleIndex)}";
        }
    }

    /// <summary>
    /// 스타일 인덱스에 해당하는 이름 반환
    /// </summary>
    private string GetStyleName(int styleIndex)
    {
        switch (styleIndex)
        {
            case 0: return "오버핸드";
            case 1: return "사이드암";
            case 2: return "쓰리쿼터";
            case 3: return "언더핸드";
            default: return "알 수 없음";
        }
    }

    /// <summary>
    /// 게임 시작 메서드
    /// </summary>
    /// <summary>
    /// 게임 시작 메서드
    /// </summary>
    public void StartGame()
    {
        if (debugMode)
        {
            Debug.Log($"게임 시작! 설정값 적용 및 BallSpawner 활성화");
            Debug.Log($"선택된 설정: 속도={speedOptions[selectedSpeedIndex]}km/h, 모드={selectedModeIndex}, 투구 폼={selectedStyleIndex}");
        }

        // 선택된 설정을 BallSpawner에 적용
        if (ballSpawner != null)
        {

            ballSpawner.speedIndex = speedOptions[selectedSpeedIndex];

            if(selectedModeIndex== 1)
            {
                ballSpawner.isRandomMode = true;
            }
            else
            {
                ballSpawner.isRandomMode = false;
            }
     
           // 3. 투구 폼 설정
            var currentPitchStyleField = ballSpawner.GetType().GetField("currentPitchStyle",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            if (currentPitchStyleField != null)
            {
                // Enum 타입 가져오기
                var enumType = currentPitchStyleField.FieldType;

                // 선택한 스타일에 맞는 Enum 값 이름
                string styleName = "";
                switch (selectedStyleIndex)
                {
                    case 0: styleName = "Overhand"; break;
                    case 1: styleName = "Sidearm"; break;
                    case 2: styleName = "ThreeQuarter"; break;
                    case 3: styleName = "Underhand"; break;
                    default: styleName = "Overhand"; break;
                }

                // Enum 값 파싱하여 설정
                var enumValue = System.Enum.Parse(enumType, styleName);
                currentPitchStyleField.SetValue(ballSpawner, enumValue);

                if (debugMode) Debug.Log($"투구 폼 설정: {styleName}");
            }

            // 4. UI 정보 업데이트
            ballSpawner.SendMessage("UpdatePitchInfoDisplay", null, SendMessageOptions.DontRequireReceiver);

            // 5. BallSpawner 활성화
            ballSpawner.gameObject.SetActive(true);
        }

        // 설정 UI 패널 비활성화
        if (setupPanel != null)
        {
            setupPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 현재 선택된 설정을 BallSpawner에 적용하는 메서드
    /// </summary>
    private void ApplySettingsToBallSpawner()
    {
        try
        {
            // 1. 속도(구속) 설정
            int selectedSpeed = speedOptions[selectedSpeedIndex];
            ballSpawner.SendMessage("SetSpeed", selectedSpeed, SendMessageOptions.DontRequireReceiver);

            // 2. 모드(직구/랜덤) 설정
            bool isRandom = (selectedModeIndex == 1);
            ballSpawner.SendMessage("SetPitchMode", isRandom, SendMessageOptions.DontRequireReceiver);

            // 3. 투구 폼 설정
            ballSpawner.SendMessage("SetPitchStyle", selectedStyleIndex, SendMessageOptions.DontRequireReceiver);

            if (debugMode)
            {
                Debug.Log($"BallSpawner에 설정 적용 완료: 속도={selectedSpeed}km/h, 모드={selectedModeIndex}, 투구 폼={selectedStyleIndex}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BallSpawner에 설정을 적용하는 중 오류 발생: {e.Message}");

            // Reflection을 사용한 대체 방법으로 시도
            ApplySettingsUsingReflection();
        }
    }

    /// <summary>
    /// Reflection을 사용하여 BallSpawner 설정을 적용하는 대체 메서드
    /// (SendMessage가 실패할 경우 사용)
    /// </summary>
    private void ApplySettingsUsingReflection()
    {
        if (debugMode)
        {
            Debug.Log("Reflection을 사용한 대체 방법으로 BallSpawner 설정 적용 시도");
        }

        try
        {
            // 1. 속도(구속) 설정
            SetBallSpawnerSpeedByReflection();

            // 2. 모드(직구/랜덤) 설정
            SetBallSpawnerModeByReflection();

            // 3. 투구 폼 설정
            SetBallSpawnerStyleByReflection();

            // UI 업데이트
            ballSpawner.SendMessage("UpdatePitchInfoDisplay", null, SendMessageOptions.DontRequireReceiver);

            if (debugMode)
            {
                Debug.Log("Reflection을 통한 BallSpawner 설정 적용 완료");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Reflection을 사용한 설정 적용 중 오류 발생: {e.Message}");
        }
    }

    /// <summary>
    /// Reflection을 사용하여 속도 설정
    /// </summary>
    private void SetBallSpawnerSpeedByReflection()
    {
        int selectedSpeed = speedOptions[selectedSpeedIndex];

        // BallSpawner의 speedValues 배열에서 해당하는 인덱스 찾기
        var speedValuesField = ballSpawner.GetType().GetField("speedValues",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (speedValuesField != null)
        {
            int[] values = (int[])speedValuesField.GetValue(ballSpawner);
            int targetIndex = -1;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == selectedSpeed)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex >= 0)
            {
                var speedIndexField = ballSpawner.GetType().GetField("speedIndex",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (speedIndexField != null)
                {
                    speedIndexField.SetValue(ballSpawner, targetIndex);
                    if (debugMode) Debug.Log($"Reflection으로 speedIndex 설정: {targetIndex} ({selectedSpeed}km/h)");
                }
            }
        }
    }

    /// <summary>
    /// Reflection을 사용하여 모드 설정
    /// </summary>
    private void SetBallSpawnerModeByReflection()
    {
        bool isRandom = (selectedModeIndex == 1);

        // isRandomMode 필드에 접근하여 설정
        var isRandomModeField = ballSpawner.GetType().GetField("isRandomMode",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (isRandomModeField != null)
        {
            isRandomModeField.SetValue(ballSpawner, isRandom);
            if (debugMode) Debug.Log($"Reflection으로 isRandomMode 설정: {isRandom}");
        }

        // 직구 모드일 경우 Fastball 구종 선택
        if (!isRandom)
        {
            // currentPitchSettings 필드에 접근
            var pitchSettingsField = ballSpawner.GetType().GetField("currentPitchSettings",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            // pitchSettingsManager 필드에 접근
            var managerField = ballSpawner.GetType().GetField("pitchSettingsManager",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            if (pitchSettingsField != null && managerField != null)
            {
                var manager = managerField.GetValue(ballSpawner);
                if (manager != null)
                {
                    // fastballSettings 필드 가져오기
                    var fastballField = manager.GetType().GetField("fastballSettings",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public);

                    if (fastballField != null)
                    {
                        var fastballSettings = fastballField.GetValue(manager);
                        pitchSettingsField.SetValue(ballSpawner, fastballSettings);

                        // pitchTypeName 필드 설정
                        var nameField = ballSpawner.GetType().GetField("pitchTypeName",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic);

                        if (nameField != null)
                        {
                            nameField.SetValue(ballSpawner, "Fastball");
                        }

                        if (debugMode) Debug.Log("Reflection으로 구종을 Fastball로 설정");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reflection을 사용하여 투구 폼 설정
    /// </summary>
    private void SetBallSpawnerStyleByReflection()
    {
        // currentPitchStyle 열거형 필드에 접근
        var field = ballSpawner.GetType().GetField("currentPitchStyle",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (field != null)
        {
            // 열거형 타입 가져오기
            var enumType = field.FieldType;

            // 선택된 스타일에 맞는 열거형 값 구하기
            string styleName = "";
            switch (selectedStyleIndex)
            {
                case 0: styleName = "Overhand"; break;
                case 1: styleName = "Sidearm"; break;
                case 2: styleName = "ThreeQuarter"; break;
                case 3: styleName = "Underhand"; break;
                default: styleName = "Overhand"; break;
            }

            // 열거형 값 파싱
            if (System.Enum.TryParse(enumType, styleName, out object enumValue))
            {
                field.SetValue(ballSpawner, enumValue);
                if (debugMode) Debug.Log($"Reflection으로 currentPitchStyle 설정: {styleName}");
            }
            else if (debugMode)
            {
                Debug.LogWarning($"열거형 값 '{styleName}'을 파싱할 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// 게임 재시작 시 설정 UI 패널 다시 보여주기
    /// </summary>
    public void RestartSetup()
    {
        // BallSpawner 비활성화
        if (ballSpawner != null)
        {
            ballSpawner.gameObject.SetActive(false);
        }

        // 설정 UI 패널 활성화
        if (setupPanel != null)
        {
            setupPanel.SetActive(true);
        }

        if (debugMode)
        {
            Debug.Log("게임 설정 UI 재활성화");
        }
    }


}