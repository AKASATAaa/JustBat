using UnityEngine;
using System.Collections;

/// <summary>
/// 공에 트레일 렌더러를 추가하고 관리하는 스크립트 (품질 기반 색상 변경)
/// </summary>
public class BallTrailController : MonoBehaviour
{
    [Header("트레일 속성")]
    [SerializeField] private Color perfectHitColor = new Color(1f, 0f, 0f, 0.9f); // 완벽한 타격 색상 (빨간색)
    [SerializeField] private Color goodHitColor = new Color(1f, 0.5f, 0f, 0.8f);  // 좋은 타격 색상 (주황색)
    [SerializeField] private Color badHitColor = new Color(0.7f, 1f, 0.2f, 0.7f);  // 나쁜 타격 색상 (연두색)
    [SerializeField] private float trailTime = 1.0f;          // 트레일 지속 시간
    [SerializeField] private float trailWidth = 0.1f;         // 트레일 너비

    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = true;       // 디버그 정보 표시 여부

    private TrailRenderer trailRenderer;
    private float hitQuality = 0f;                           // 타격 품질 (0.0~1.0)
    private bool hasBeenHit = false;                         // 타격되었는지 여부

    // 싱글톤 패턴 (같은 공에 BallController가 첨부되어 있을 경우 접근용)
    public static BallTrailController Instance { get; private set; }

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // 트레일 렌더러 찾기 또는 생성하기
        InitializeTrailRenderer();
    }

    /// <summary>
    /// 트레일 렌더러 초기화
    /// </summary>
    private void InitializeTrailRenderer()
    {
        // 트레일 렌더러 찾기
        trailRenderer = GetComponent<TrailRenderer>();

        if (trailRenderer == null)
        {
            // 트레일 렌더러가 없으면 생성
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            if (showDebugInfo) Debug.Log("공에 TrailRenderer 컴포넌트가 추가되었습니다.");
        }

        // 트레일 렌더러 초기 설정
        ConfigureTrailRenderer();

        // 시작 시 비활성화
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 트레일 렌더러 설정
    /// </summary>
    private void ConfigureTrailRenderer()
    {
        if (trailRenderer == null) return;

        // 기본 속성 설정
        trailRenderer.time = trailTime;
        trailRenderer.minVertexDistance = 0.01f;
        trailRenderer.widthMultiplier = trailWidth;

        // 너비 커브 설정 (시작부터 끝까지 점점 좁아짐)
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0.0f, 1.0f);   // 시작 지점: 원래 너비
        widthCurve.AddKey(1.0f, 0.0f);   // 끝 지점: 너비 0
        trailRenderer.widthCurve = widthCurve;

        // 머티리얼 셰이더 설정
        CreateTrailMaterial();

        // 기본 색상 그라데이션 설정
        SetTrailColor(goodHitColor);
    }

    /// <summary>
    /// 트레일 머티리얼 생성
    /// </summary>
    private void CreateTrailMaterial()
    {
        // 여러 셰이더 시도
        Shader trailShader = null;

        // 셰이더 목록 시도
        string[] shaderNames = new string[] {
            "Particles/Standard Unlit",
            "Sprites/Default",
            "Mobile/Particles/Additive",
            "Legacy Shaders/Particles/Additive",
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Surface"
        };

        foreach (string shaderName in shaderNames)
        {
            trailShader = Shader.Find(shaderName);
            if (trailShader != null) break;
        }

        if (trailShader != null)
        {
            // 새 머티리얼 생성
            Material trailMat = new Material(trailShader);
            trailMat.name = "BallTrailMaterial";

            // 기본 색상 설정
            trailMat.color = goodHitColor;

            // 머티리얼 할당
            trailRenderer.material = trailMat;

            if (showDebugInfo) Debug.Log("공 트레일 머티리얼이 생성되었습니다. 셰이더: " + trailShader.name);
        }
        else
        {
            Debug.LogError("트레일에 사용할 수 있는 셰이더를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 트레일 색상 설정
    /// </summary>
    /// <param name="color">트레일 메인 색상</param>
    public void SetTrailColor(Color color)
    {
        if (trailRenderer == null) return;

        // 그라데이션 생성
        Gradient gradient = new Gradient();

        // 그라데이션 색상 키 설정
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0].color = color;
        colorKeys[0].time = 0.0f;

        Color endColor = new Color(color.r, color.g, color.b, 0f);
        colorKeys[1].color = endColor;
        colorKeys[1].time = 1.0f;

        // 그라데이션 알파 키 설정
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0].alpha = color.a;
        alphaKeys[0].time = 0.0f;
        alphaKeys[1].alpha = 0.0f;
        alphaKeys[1].time = 1.0f;

        // 그라데이션 적용
        gradient.SetKeys(colorKeys, alphaKeys);
        trailRenderer.colorGradient = gradient;

        // 머티리얼 색상도 설정
        if (trailRenderer.material != null)
        {
            trailRenderer.material.color = color;
        }
    }

    /// <summary>
    /// 트레일 활성화 - 외부에서 호출하는 공개 메서드
    /// </summary>
    /// <param name="quality">타격 품질 (0.0~1.0)</param>
    public void ActivateTrail(float quality)
    {
        hitQuality = quality;
        hasBeenHit = true;

        if (trailRenderer == null)
        {
            InitializeTrailRenderer();
        }

        // 이전 트레일 지우기
        trailRenderer.Clear();

        // 품질에 따른 색상 설정
        Color hitColor;

        if (quality > 0.9f)
        {
            // 완벽한 타격 - 빨간색 트레일
            hitColor = perfectHitColor;
            trailRenderer.time = trailTime * 1.2f;
            trailRenderer.widthMultiplier = trailWidth * 1.2f;
            if (showDebugInfo) Debug.Log("최고 품질 타격 감지: 빨간색 트레일 설정");
        }
        else if (quality > 0.7f)
        {
            // 좋은 타격 - 주황색 트레일
            hitColor = goodHitColor;
            trailRenderer.time = trailTime;
            trailRenderer.widthMultiplier = trailWidth;
            if (showDebugInfo) Debug.Log("중간 품질 타격 감지: 주황색 트레일 설정");
        }
        else
        {
            // 중심에서 벗어난 타격 - 연두색 트레일
            hitColor = badHitColor;
            trailRenderer.time = trailTime * 0.8f;
            trailRenderer.widthMultiplier = trailWidth * 0.8f;
            if (showDebugInfo) Debug.Log("낮은 품질 타격 감지: 연두색 트레일 설정");
        }

        // 색상 설정
        SetTrailColor(hitColor);

        // 트레일 활성화
        trailRenderer.enabled = true;

        if (showDebugInfo) Debug.Log($"공 트레일 활성화됨: 품질={quality:F2}, 색상={hitColor}");
    }

    /// <summary>
    /// 인스펙터에서 테스트용 함수
    /// </summary>
    [ContextMenu("Test Perfect Trail")]
    public void TestPerfectTrail()
    {
        ActivateTrail(1.0f);
    }

    [ContextMenu("Test Good Trail")]
    public void TestGoodTrail()
    {
        ActivateTrail(0.8f);
    }

    [ContextMenu("Test Bad Trail")]
    public void TestBadTrail()
    {
        ActivateTrail(0.6f);
    }
}