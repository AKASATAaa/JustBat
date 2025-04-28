/**
 * HomerunSpawner.cs - 홈런 더비용 공 생성 및 발사 제어 컴포넌트
 * 
 * 이 스크립트는 홈런 더비 모드에서 공을 생성하고 발사하는 역할을 담당합니다.
 * 지정된 횟수(15개)만큼 공을 생성하며, 다시하기 기능을 통해 초기화할 수 있습니다.
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    public string readyAnimTrigger = "Ready";
    public string pitchAnimTrigger = "PitchOverhand";

    [Header("홈런 더비 설정")]
    [Tooltip("총 발사할 공의 개수")]
    public int totalBallCount = 15;

    [Tooltip("공 발사 간격 (초)")]
    public float pitchInterval = 5.0f;

    [Tooltip("다시하기 버튼")]
    public Button restartButton;

    [Tooltip("남은 공 개수 표시 텍스트")]
    public TextMeshProUGUI ballCountText;

    [Header("공 물리 설정")]
    [Tooltip("투구 파워 (속도)")]
    public float initialForce = 100.0f;

    [Tooltip("수직 회전 속도")]
    public float verticalRotationSpeed = 0.0f;

    [Tooltip("Z축 회전 속도")]
    public float horizontalRotationSpeed = 0.0f;

    [Tooltip("수직 오프셋")]
    public float verticalAngleOffset = 0.0f;

    [Tooltip("수평 오프셋")]
    public float horizontalAngleOffset = 0.0f;

    [Tooltip("공 X축 각도")]
    public float initialRotationAngleX = 0.0f;

    [Tooltip("공 Z축 각도")]
    public float initialRotationAngleZ = 0.0f;

    [Tooltip("제동 시작 시간")]
    public float brakingDelay = 0.0f;

    [Tooltip("최대 흔들림")]
    public float maxWobble = 0.0f;

    [Header("타겟 설정")]
    [Tooltip("타겟할 스트라이크 존 오브젝트")]
    public GameObject pitchRangeObject;

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

    // 현재 남은 공 개수
    private int remainingBallCount;

    // 투구 시퀀스 진행 중 여부
    private bool isPitchingSequenceActive = false;

    // 목표 위치 관련 변수
    private Vector3 targetPosition;

    // 애니메이션 상태 관리
    private enum AnimationState
    {
        Idle,
        Ready,
        Pitching,
        None
    }

    private AnimationState currentAnimState = AnimationState.None;

    // 애니메이션 이벤트에서 사용할 변수
    private bool isIdleAnimationInProgress = false;
    private bool isReadyAnimationInProgress = false;
    private bool isPitchAnimationInProgress = false;

    /**
     * 초기화 메서드
     * 초기 설정 및 버튼 이벤트를 설정합니다.
     */
    void Start()
    {
        // 애니메이터 컴포넌트 확인
        CheckAnimatorComponent();

        // 타겟 위치 초기화
        InitializeTargetPosition();

        // 버튼 이벤트 연결
        SetupRestartButton();

        // 초기 공 개수 설정
        InitializeBallCount();

        // 투구 시퀀스 시작
        StartPitchingSequence();

        if (debugMode)
        {
            Debug.Log("HomerunSpawner 초기화 완료");
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
     * 타겟 위치 초기화
     */
    private void InitializeTargetPosition()
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
                return;
            }
        }

        targetPosition = pitchRangeObject.transform.position;
    }

    /**
     * 다시하기 버튼 설정
     */
    private void SetupRestartButton()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartHomerunDerby);
        }
        else if (debugMode)
        {
            Debug.LogWarning("다시하기 버튼이 설정되지 않았습니다.");
        }
    }

    /**
     * 공 개수 초기화
     */
    private void InitializeBallCount()
    {
        remainingBallCount = totalBallCount;
        UpdateBallCountDisplay();
    }

    /**
     * 공 개수 표시 업데이트
     */
    private void UpdateBallCountDisplay()
    {
        if (ballCountText != null)
        {
            ballCountText.text = $"남은 공: {remainingBallCount} / {totalBallCount}";
        }
    }

    /**
     * 홈런 더비 다시 시작
     */
    public void RestartHomerunDerby()
    {
        // 진행 중인 코루틴 정지
        StopAllCoroutines();

        // 공 개수 초기화
        InitializeBallCount();

        // 투구 시퀀스 재시작
        StartPitchingSequence();

        if (debugMode)
        {
            Debug.Log("홈런 더비 다시 시작");
        }
    }

    /**
     * 투구 시퀀스 시작
     */
    private void StartPitchingSequence()
    {
        if (isPitchingSequenceActive)
            return;

        isPitchingSequenceActive = true;
        StartCoroutine(PitchingSequence());
    }

    /**
     * 투구 시퀀스 코루틴
     */
    private IEnumerator PitchingSequence()
    {
        yield return new WaitForSeconds(1.0f); // 초기 대기 시간

        while (remainingBallCount > 0)
        {
            // 애니메이션 시퀀스 시작
            StartCoroutine(AnimatedPitchingSequence());

            // 한 투구 완료 후 대기
            yield return new WaitForSeconds(pitchInterval);
        }

        isPitchingSequenceActive = false;

        if (debugMode)
        {
            Debug.Log("모든 공 소진, 투구 시퀀스 종료");
        }
    }

    /**
     * 애니메이션 기반의 투구 시퀀스를 제어하는 코루틴
     */
    private IEnumerator AnimatedPitchingSequence()
    {
        // 초기 상태 설정
        currentAnimState = AnimationState.None;
        isIdleAnimationInProgress = false;
        isReadyAnimationInProgress = false;
        isPitchAnimationInProgress = false;

        // 처음에는 Idle 애니메이션으로 시작
        StartIdleAnimation();

        // 애니메이션 시퀀스가 완료될 때까지 대기
        while (currentAnimState != AnimationState.None)
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
                        // Pitch 애니메이션이 완료되면 시퀀스 종료
                        currentAnimState = AnimationState.None;
                    }
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
     * Ready 애니메이션을 시작하는 메서드
     */
    private void StartReadyAnimation()
    {
        if (pitcherAnimator == null || isReadyAnimationInProgress)
            return;

        isReadyAnimationInProgress = true;
        currentAnimState = AnimationState.Ready;

        // Ready 애니메이션 트리거 설정
        pitcherAnimator.SetTrigger(readyAnimTrigger);

        if (debugMode && logAnimationEvents)
        {
            Debug.Log("Ready 애니메이션 시작");
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
            Debug.Log("Pitch 애니메이션 시작");
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
        // 남은 공 개수 확인
        if (remainingBallCount <= 0)
            return;

        // 공 생성
        GameObject ball = Instantiate(ballPrefab, releasePoint.position, releasePoint.rotation);

        // 공에 설정 적용
        ApplyBallSettings(ball);

        // 일정 시간 후 공 제거
        StartCoroutine(DestroyBallAfterTime(ball, destroyTime));

        // 남은 공 개수 감소
        remainingBallCount--;
        UpdateBallCountDisplay();

        if (debugMode && logBallInfo)
        {
            Debug.Log($"공이 발사되었습니다. 남은 공: {remainingBallCount}/{totalBallCount}");
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
            // 직접 설정 적용
            ballController.initialForce = initialForce;
            ballController.verticalRotationSpeed = verticalRotationSpeed;
            ballController.horizontalRotationSpeed = horizontalRotationSpeed;
            ballController.verticalAngleOffset = verticalAngleOffset;
            ballController.horizontalAngleOffset = horizontalAngleOffset;
            ballController.initialRotationAngleX = initialRotationAngleX;
            ballController.initialRotationAngleZ = initialRotationAngleZ;
            ballController.brakingDelay = brakingDelay;

            // 발사 방향 계산 및 발사
            ballController.CalculateDirection(releasePoint.position, targetPosition);
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
            Destroy(ball);

            if (debugMode && logBallInfo)
            {
                Debug.Log($"공 자동 제거: {time}초 후");
            }
        }
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
        Gizmos.DrawSphere(targetPosition, targetGizmoSize);

        // 디버그 모드일 때 추가 정보 표시
        if (debugMode && releasePoint != null)
        {
            // 투구 경로 표시
            Gizmos.DrawLine(releasePoint.position, targetPosition);
        }
    }
}