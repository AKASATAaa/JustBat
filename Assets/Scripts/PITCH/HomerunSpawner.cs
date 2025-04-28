/**
 * BallSpawner.cs - 공 생성 및 발사 제어 컴포넌트
 * 
 * 이 스크립트는 홈런 더비 모드에서 공을 주기적으로 생성하고 발사하는 역할을 담당합니다.
 * 시간 제한 내에 자동으로 공을 생성하고, 이전 공이 지면에 닿으면 다음 공을 발사합니다.
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HomerunSpawner : MonoBehaviour
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
    public string pitchAnimTrigger = "Pitch";

    [Header("버튼 오브젝트")]
    [Tooltip("다시 시작 버튼")]
    public Button againButton;

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

    [Header("UI 정보 표시")]
    [Tooltip("구종과 구속 정보를 표시할 TextMeshPro 컴포넌트")]
    public TextMeshProUGUI pitchInfoText;

    [Header("시간 제한 설정")]
    [Tooltip("게임 시간 제한 (분)")]
    public float timeLimitInMinutes = 4.0f;
    [Tooltip("남은 시간을 표시할 TextMeshPro 컴포넌트")]
    public TextMeshPro timeRemainingText;

    [Header("디버그 설정")]
    [Tooltip("디버그 모드 활성화 여부")]
    public bool debugMode = false;

    [Tooltip("공 발사 정보 로그 출력 여부")]
    public bool logBallInfo = true;

    [Tooltip("애니메이션 이벤트 로그 출력 여부")]
    public bool logAnimationEvents = true;

    [Tooltip("타겟 위치 기즈모 표시 여부")]
    public bool showTargetGizmo = true;

    [Tooltip("타겟 위치 기즈모 색상")]
    public Color targetGizmoColor = new Color(1f, 0.5f, 0f, 0.5f);

    [Tooltip("타겟 위치 기즈모 크기")]
    public float targetGizmoSize = 0.1f;

    [Header("홈런 정보")]
    [Tooltip("홈런 카운트를 표시할 TextMeshPro 컴포넌트")]
    public TextMeshPro homeRunCountText;


    private int homeRunCount = 0;
    private int totalPitchCount = 0;


    public AudioSource homeRunSound;
    // 현재 투구 정보
    public string pitchTypeName = "HomeRunDerby";

    // 구종 및 속도 설정 관련 변수
    private PitchSettingsManager pitchSettingsManager;
    private List<BallSettings> currentPitchSettings;
    private int speedIndex;
    private int[] speedValues = { 130 }; // 홈런 더비 모드에서는 고정 속도 사용

    // 목표 위치 관련 변수
    private Vector3 initialTargetP;
    private Vector3 targetP;
    private Vector3 pitchRangeSize; // PitchRange 오브젝트의 크기

    // 애니메이션 이벤트에서 사용할 변수
    private bool isIdleAnimationInProgress = false;
    private bool isPitchAnimationInProgress = false;
    private BallSettings pendingBallSettings;

    // 시간 제한 관련 변수
    private float timeRemaining;
    private bool isGameActive = false;
    private bool canSpawnBall = true;
    private GameObject currentBall;

    // 애니메이션 상태 관리
    private enum AnimationState
    {
        Idle,
        Pitching,
        None
    }

    private AnimationState currentAnimState = AnimationState.None;

    /**
     * 초기화 메서드
     */
    void Start()
    {
        // 구종 설정 관리자 찾기
        InitializePitchSettingsManager();

        // UI 정보 업데이트
        UpdatePitchInfoDisplay();

        UpdateHomeRunDisplay();


        // 애니메이터 컴포넌트 확인
        CheckAnimatorComponent();

        // 홈런 더비 설정 선택
        currentPitchSettings = pitchSettingsManager.homeRunDerbySettings;
        speedIndex = 0;

        homeRunCount = 0;
        totalPitchCount = 0;

        // PitchRange 오브젝트 설정
        InitializePitchRange();

        // 버튼 이벤트 연결
        if (againButton != null)
        {
            againButton.onClick.RemoveAllListeners();
            againButton.onClick.AddListener(RestartGame);
        }

        // 게임 시작
        RestartGame();

        if (debugMode)
        {
            Debug.Log("BallSpawner 초기화 완료");
        }
    }

    /**
     * 게임 재시작 메서드
     */
    public void RestartGame()
    {
        // 기존에 생성된 공 제거
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
        }

        homeRunCount = 0;
        totalPitchCount = 0;
        UpdateHomeRunDisplay();

        // 시간 초기화
        timeRemaining = timeLimitInMinutes * 60;

        // 게임 활성화
        isGameActive = true;
        canSpawnBall = true;

        // 애니메이션 초기화
        StartIdleAnimation();

        if (debugMode)
        {
            Debug.Log($"게임 재시작: 제한 시간 {timeRemaining}초");
        }

        // 자동 투구 코루틴 시작
        StartCoroutine(AutomaticPitchingSequence());
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

        // PitchSettingsManager에 HomeRunDerbySettings가 없는 경우 확인
        if (pitchSettingsManager != null &&
            (pitchSettingsManager.homeRunDerbySettings == null ||
             pitchSettingsManager.homeRunDerbySettings.Count == 0))
        {
            Debug.LogError("HomeRunDerbySettings가 설정되어 있지 않습니다!");

            // 기본값으로 fastballSettings 사용
            pitchSettingsManager.homeRunDerbySettings = pitchSettingsManager.fastballSettings;
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
     * PitchRange 오브젝트 초기화 메서드
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

        // 타겟 위치 초기화
        targetP = initialTargetP;

        if (debugMode)
        {
            Debug.Log($"PitchRange 초기화: 위치={initialTargetP}, 크기={pitchRangeSize}");
        }
    }

    /**
     * 업데이트 메서드
     * 시간 제한 및 UI 업데이트를 처리합니다.
     */
    void Update()
    {
        // 게임이 활성화된 상태라면 시간 카운트다운
        if (isGameActive)
        {
            timeRemaining -= Time.deltaTime;

            // 시간 종료 처리
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                isGameActive = false;

                if (debugMode)
                {
                    Debug.Log("게임 시간 종료!");
                }
            }

            // 시간 표시 UI 업데이트
            UpdateTimeDisplay();
        }

        CheckCurrentBall();

        // 현재 볼이 지면에 닿았는지 확인
    }

    /**
 * 현재 볼 상태 확인
 * 착지 및 홈런 여부 체크
 */
    private void CheckCurrentBall()
    {
        if (currentBall != null)
        {
            BallController ballController = currentBall.GetComponent<BallController>();
            if (ballController != null)
            {
                // 홈런 체크
                if (ballController.IsHomeRun() && !ballController.isHomeRunCounted)
                {
                    // 홈런 카운트 증가
                    homeRunCount++;
                    ballController.isHomeRunCounted = true;
                    UpdateHomeRunDisplay();

                    // 홈런 사운드 재생
                    if (homeRunSound != null && !homeRunSound.isPlaying)
                    {
                        homeRunSound.Play();
                    }

                    if (debugMode)
                    {
                        Debug.Log($"홈런! 현재 홈런 수: {homeRunCount} / {totalPitchCount}");
                    }
                }

                // 착지 체크
                if (ballController.hasLanded)
                {
                    // 볼이 지면에 닿았으면 다음 볼을 투구할 수 있도록 설정
                    canSpawnBall = true;

                    // 기존 공을 변수에서 분리 (나중에 제거 예정)
                    GameObject oldBall = currentBall;
                    currentBall = null;

                    // 지면에 닿은 공은 더 빨리 제거
                    StartCoroutine(DestroyBallAfterTime(oldBall, 1.0f));

                    // 즉시 다음 투구 준비 시작
                    if (isGameActive && currentAnimState == AnimationState.Idle && !isIdleAnimationInProgress)
                    {
                        PrepareNextPitch();
                        StartPitchAnimation();
                    }

                    if (debugMode)
                    {
                        Debug.Log("공이 지면에 닿았습니다. 다음 투구 즉시 준비.");
                    }
                }
            }
        }
        else
        {
            // 현재 볼이 없으면 다음 볼을 투구할 수 있도록 설정
            canSpawnBall = true;
        }
    }

    /**
     * 시간 표시 UI 업데이트
     */
    private void UpdateTimeDisplay()
    {
        if (timeRemainingText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timeRemainingText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // 남은 시간이 30초 이하일 때 색상 변경
            if (timeRemaining <= 30)
            {
                timeRemainingText.color = Color.red;
            }
            else
            {
                timeRemainingText.color = Color.white;
            }
        }
    }

    /**
     * 현재 볼이 지면에 닿았는지 확인
     */
    /*
    private void CheckCurrentBallLanded()
    {
        if (currentBall != null)
        {
            BallController ballController = currentBall.GetComponent<BallController>();
            if (ballController != null && ballController.hasLanded)
            {
                // 볼이 지면에 닿았으면 다음 볼을 투구할 수 있도록 설정
                canSpawnBall = true;

                if (debugMode)
                {
                    Debug.Log("공이 지면에 닿았습니다. 다음 투구 준비.");
                }
            }
        }
        else
        {
            // 현재 볼이 없으면 다음 볼을 투구할 수 있도록 설정
            canSpawnBall = true;
        }
    }
    */

    /**
     * 자동 투구 시퀀스를 제어하는 코루틴
     */
    private IEnumerator AutomaticPitchingSequence()
    {
        while (isGameActive)
        {
            // 새 볼을 투구할 수 있는 상태라면
            if (canSpawnBall && currentBall == null)  // 현재 공이 없을 때만
            {
                // 아이들 상태인 경우 투구 애니메이션 시작
                if (currentAnimState == AnimationState.Idle && !isIdleAnimationInProgress)
                {
                    // 투구 설정 준비
                    PrepareNextPitch();

                    // 투구 애니메이션 시작
                    StartPitchAnimation();

                    // 다음 볼을 투구할 수 없는 상태로 설정
                    canSpawnBall = false;
                }
                // 애니메이션 상태가 없는 경우 아이들 애니메이션 시작
                else if (currentAnimState == AnimationState.None)
                {
                    StartIdleAnimation();
                }
            }

            // 투구 애니메이션이 끝났고 다음 투구를 위해 대기 중인 경우
            if (currentAnimState == AnimationState.Pitching && !isPitchAnimationInProgress)
            {
                // 아이들 애니메이션으로 전환
                StartIdleAnimation();
            }

            yield return null;
        }
    }

    /**
     * 다음 투구를 위한 설정 준비
     */
    private void PrepareNextPitch()
    {
        // 투구 설정 선택
        pendingBallSettings = currentPitchSettings[speedIndex % currentPitchSettings.Count];

        targetP = initialTargetP;

        if (debugMode)
        {
            Debug.Log($"다음 투구 준비: 속도={speedValues[speedIndex]} km/h, 타겟 위치={targetP}");
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
        pitcherAnimator.ResetTrigger(pitchAnimTrigger);

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
     * Pitch 애니메이션을 시작하는 메서드
     */
    private void StartPitchAnimation()
    {
        if (pitcherAnimator == null || isPitchAnimationInProgress)
            return;

        isPitchAnimationInProgress = true;
        currentAnimState = AnimationState.Pitching;

        // Pitch 애니메이션 트리거 설정
        pitcherAnimator.SetTrigger(pitchAnimTrigger);

        if (debugMode && logAnimationEvents)
        {
            Debug.Log($"투구 애니메이션 시작 (구종: {pitchTypeName})");
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
     * Pitch 애니메이션 완료 후 트리거 리셋 메서드
     */
    public void OnPitchAnimationComplete()
    {
        // 트리거 리셋
        pitcherAnimator.ResetTrigger(pitchAnimTrigger);

        // 상태 업데이트
        isPitchAnimationInProgress = false;

        if (debugMode && logAnimationEvents)
        {
            Debug.Log("Pitch 애니메이션 완료");
        }
    }

    /**
     * 애니메이션 이벤트에서 호출되는 공 발사 메서드
     */
    public void OnAnimationReleaseBall()
    {
        // 게임이 활성화된 상태일 때만 공 생성
        if (!isGameActive)
        {
            Debug.Log("게임이 비활성화 상태여서 공 생성을 건너뜁니다.");
            return;
        }
        if(isGameActive)
        {
            Debug.Log("게임이 생성을 건너뜁니다.");
        }

        // 공 생성
        currentBall = Instantiate(ballPrefab, releasePoint.position, releasePoint.rotation);

        // 공에 설정 적용
        ApplyBallSettings(currentBall);

        totalPitchCount++;

        Debug.Log("" + totalPitchCount + "");


        UpdateHomeRunDisplay();

        // 일정 시간 후 공 제거
        StartCoroutine(DestroyBallAfterTime(currentBall, destroyTime));

            Debug.Log($"{pitchTypeName} 공이 생성되고 발사되었습니다. 속도: {pendingBallSettings.throwPower * 0.36f:F1}km/h");

    }

    private void UpdateHomeRunDisplay()
    {
        if (homeRunCountText != null)
        {
            homeRunCountText.text = string.Format("HomeRun: {0} / {1}", homeRunCount, totalPitchCount);
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
            ballController.SetPitchType(pitchTypeName);

            // 발사 방향 계산 및 발사
            ballController.CalculateDirection(releasePoint.position, targetP);
            ballController.LaunchBall();
        }
        else if (debugMode)
        {
            Debug.LogError("볼 프리팹에 BallController 컴포넌트가 없습니다!");
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
            if (ball == currentBall)
            {
                currentBall = null;
                canSpawnBall = true;
            }

            Destroy(ball);

            if (debugMode && logBallInfo)
            {
                Debug.Log($"공 자동 제거: {time}초 후");
            }
        }
    }

    /**
     * 현재 선택된 구종과 구속 정보를 TextMeshPro로 출력하는 메서드
     */
    public void UpdatePitchInfoDisplay()
    {
        if (pitchInfoText == null)
            return;

        // 현재 선택된 구속 가져오기
        int currentSpeed = speedValues[speedIndex];

        // 홈런 더비 모드 텍스트 표시
        pitchInfoText.text = string.Format("<color=#FF6B00>HomeRun Derby</color>\n{0} km/h", currentSpeed);
    }

    /**
     * 타겟 위치를 시각적으로 표시
     */
    private void OnDrawGizmos()
    {
        if (!showTargetGizmo || !Application.isPlaying)
            return;

        // 타겟 위치 표시
        Gizmos.color = targetGizmoColor;
        Gizmos.DrawSphere(targetP, targetGizmoSize);

        // 디버그 모드일 때 추가 정보 표시
        if (debugMode && releasePoint != null)
        {
            // 투구 경로 표시
            Gizmos.DrawLine(releasePoint.position, targetP);
        }
    }
}