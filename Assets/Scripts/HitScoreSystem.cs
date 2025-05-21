using UnityEngine;
using TMPro;

/// <summary>
/// 타구 점수 시스템 - 타격의 품질, 속도, 거리 등을 기반으로 점수를 계산합니다.
/// TextMeshPro 텍스트 오브젝트에 점수를 표시합니다.
/// </summary>
public class HitScoreSystem : MonoBehaviour
{
    [Header("점수 설정")]
    [SerializeField] private int baseScore = 100; // 기본 점수
    [SerializeField] private float qualityMultiplier = 500; // 품질 계수 승수
    [SerializeField] private float velocityMultiplier = 10; // 속도 승수
    [SerializeField] private float distanceMultiplier = 20; // 거리 승수
    [SerializeField] private float sweetSpotBonus = 200; // 스윗스팟 보너스

    [Header("기준 설정")]
    [SerializeField] private float perfectQualityThreshold = 0.9f; // 완벽한 타격 기준
    [SerializeField] private float goodQualityThreshold = 0.7f; // 좋은 타격 기준
    [SerializeField] private float highVelocityThreshold = 40f; // 높은 속도 기준 (km/h)
    [SerializeField] private float longDistanceThreshold = 80f; // 장타 기준 (m)

    [Header("TMPro 텍스트 참조")]
    [SerializeField] private TextMeshPro scoreTextMesh; // 점수 표시 TextMeshPro

    // 현재 점수 및 최고 점수
    private int currentScore = 0;
    private int highScore = 0;

    // 히트 인포 디스플레이 참조
    private HitInfoDisplay hitInfoDisplay;

    void Start()
    {
        // HitInfoDisplay 컴포넌트 찾기
        hitInfoDisplay = FindObjectOfType<HitInfoDisplay>();
        if (hitInfoDisplay == null)
        {
            Debug.LogWarning("HitInfoDisplay를 찾을 수 없습니다. 점수 계산에 영향을 줄 수 있습니다.");
        }
        else
        {
            // HitInfoDisplay에서 점수 업데이트 이벤트 구독
            hitInfoDisplay.OnHitInfoUpdated += CalculateScore;
        }

        // 저장된 최고 점수 로드
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        // TextMeshPro 확인 및 초기화
        if (scoreTextMesh == null)
        {
            // 컴포넌트가 없으면 자동으로 찾거나 생성
            scoreTextMesh = GetComponentInChildren<TextMeshPro>();
            if (scoreTextMesh == null)
            {
                GameObject textObj = new GameObject("ScoreText");
                textObj.transform.parent = transform;
                textObj.transform.localPosition = new Vector3(0, 0.5f, 0);

                // TextMeshPro 컴포넌트 추가
                scoreTextMesh = textObj.AddComponent<TextMeshPro>();

                // TextMeshPro 기본 설정
                scoreTextMesh.fontSize = 24;
                scoreTextMesh.alignment = TextAlignmentOptions.Center;
                scoreTextMesh.enableAutoSizing = true;
                scoreTextMesh.fontSizeMin = 8;
                scoreTextMesh.fontSizeMax = 24;

                // 텍스트 메시 기본 재질 설정 (필요한 경우)
                if (TMP_Settings.defaultFontAsset != null)
                {
                    scoreTextMesh.font = TMP_Settings.defaultFontAsset;
                }

                Debug.Log("ScoreText 오브젝트가 TextMeshPro로 자동 생성되었습니다.");
            }
        }

        UpdateScoreDisplay();
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (hitInfoDisplay != null)
        {
            hitInfoDisplay.OnHitInfoUpdated -= CalculateScore;
        }
    }

    /// <summary>
    /// 타격 정보를 기반으로 점수를 계산합니다.
    /// </summary>
    /// <param name="hitInfo">타격 정보</param>
    public void CalculateScore(HitInfo hitInfo)
    {
        // 기본 점수 설정
        int score = baseScore;

        // 품질 계수에 따른 점수 추가
        score += Mathf.RoundToInt(hitInfo.QualityFactor * qualityMultiplier);

        // 출구 속도에 따른 점수 추가
        score += Mathf.RoundToInt(hitInfo.ExitVelocity * velocityMultiplier);

        // 거리에 따른 점수 추가
        score += Mathf.RoundToInt(hitInfo.EstimatedDistance * distanceMultiplier);

        // 스윗스팟 보너스 (완벽한 타격인 경우)
        if (hitInfo.QualityFactor >= perfectQualityThreshold)
        {
            score += Mathf.RoundToInt(sweetSpotBonus);
        }

        // 고속 타구 보너스
        if (hitInfo.ExitVelocity >= highVelocityThreshold)
        {
            int speedBonus = Mathf.RoundToInt((hitInfo.ExitVelocity - highVelocityThreshold) * 5);
            score += speedBonus;
        }

        // 장타 보너스
        if (hitInfo.EstimatedDistance >= longDistanceThreshold)
        {
            int distanceBonus = Mathf.RoundToInt((hitInfo.EstimatedDistance - longDistanceThreshold) * 10);
            score += distanceBonus;
        }

        // 점수 업데이트 및 저장
        currentScore = score;
        UpdateHighScore();
        UpdateScoreDisplay();

        Debug.Log($"타격 점수: {score} (품질: {hitInfo.QualityFactor:F2}, 속도: {hitInfo.ExitVelocity:F1} km/h, 거리: {hitInfo.EstimatedDistance:F1} m)");
    }

    /// <summary>
    /// 최고 점수를 업데이트하고 저장합니다.
    /// </summary>
    private void UpdateHighScore()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            Debug.Log("새로운 최고 점수: " + highScore);
        }
    }

    /// <summary>
    /// TextMeshPro에 점수를 표시합니다.
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (scoreTextMesh != null)
        {
            scoreTextMesh.text = $"점수: {currentScore}\n최고: {highScore}";
        }
    }

    /// <summary>
    /// 현재 점수를 반환합니다.
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// 최고 점수를 반환합니다.
    /// </summary>
    public int GetHighScore()
    {
        return highScore;
    }

    /// <summary>
    /// 점수를 리셋합니다.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
    }
}