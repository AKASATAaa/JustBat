using UnityEngine;
using System.Collections;

/// <summary>
/// 배트 트레일 렌더러 문제를 해결하기 위한 컴포넌트
/// </summary>
public class BatTrailFix : MonoBehaviour
{
    [Header("트레일 설정")]
    public Color perfectHitColor = new Color(1.0f, 0.84f, 0.0f, 1.0f);  // 완벽한 타격 색상 (금색)
    public Color goodHitColor = new Color(0.0f, 0.5f, 1.0f, 1.0f);      // 좋은 타격 색상 (파란색)
    public Color offCenterHitColor = new Color(0.0f, 0.8f, 0.2f, 1.0f); // 중심에서 벗어난 타격 색상 (초록색)

    [Header("트레일 속성")]
    public float trailTime = 1.0f;        // 트레일 지속 시간
    public float trailWidth = 0.05f;      // 트레일 너비
    public float displayDuration = 2.0f;  // 표시 지속 시간

    private TrailRenderer trail;
    private Material trailMaterial;
    private bool initialized = false;

    void Awake()
    {
        // 트레일 렌더러 초기화
        InitializeTrail();
    }

    /// <summary>
    /// 트레일 렌더러 초기화
    /// </summary>
    private void InitializeTrail()
    {
        // 기존 트레일 렌더러 확인
        trail = GetComponent<TrailRenderer>();

        // 없으면 새로 생성
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            Debug.Log("새로운 TrailRenderer 컴포넌트가 추가되었습니다.");
        }

        // 기본 속성 설정
        trail.time = trailTime;
        trail.minVertexDistance = 0.01f;
        trail.widthMultiplier = trailWidth;
        trail.emitting = false;  // 시작 시 비활성화

        // 너비 곡선 설정 (시작에서 끝으로 갈수록 좁아짐)
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0.0f, 1.0f);
        widthCurve.AddKey(1.0f, 0.0f);
        trail.widthCurve = widthCurve;

        // 머티리얼 생성
        CreateTrailMaterial();

        initialized = true;
        Debug.Log("트레일 렌더러 초기화 완료");
    }

    /// <summary>
    /// 트레일 머티리얼 생성
    /// </summary>
    private void CreateTrailMaterial()
    {
        // 여러 셰이더 시도
        Shader trailShader = Shader.Find("Sprites/Default");

        if (trailShader == null)
            trailShader = Shader.Find("Particles/Standard Unlit");

        if (trailShader == null)
            trailShader = Shader.Find("Mobile/Particles/Additive");

        if (trailShader == null)
            trailShader = Shader.Find("Legacy Shaders/Particles/Additive");

        if (trailShader != null)
        {
            // 새 머티리얼 생성
            trailMaterial = new Material(trailShader);
            trailMaterial.name = "BatTrailMaterial";

            // 기본 색상 설정
            trailMaterial.color = Color.white;

            // 머티리얼 할당
            trail.material = trailMaterial;

            Debug.Log("트레일 머티리얼이 생성되었습니다. 셰이더: " + trailShader.name);
        }
        else
        {
            Debug.LogError("트레일에 사용할 수 있는 셰이더를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 타격 품질에 따라 트레일 활성화
    /// </summary>
    /// <param name="qualityFactor">타격 품질 계수 (0.0~1.0)</param>
    public void ActivateTrail(float qualityFactor)
    {
        if (!initialized)
            InitializeTrail();

        if (trail == null)
        {
            Debug.LogError("트레일 렌더러를 찾을 수 없습니다!");
            return;
        }

        // 트레일 초기화
        trail.Clear();

        // 품질에 따른 색상 및 속성 설정
        Color trailColor;
        float durationMultiplier;
        float widthMultiplier;

        if (qualityFactor > 0.9f)
        {
            // 완벽한 타격
            trailColor = perfectHitColor;
            durationMultiplier = 1.2f;
            widthMultiplier = 1.2f;
            Debug.Log("최고 품질 타격 감지: 트레일 색상 = 금색");
        }
        else if (qualityFactor > 0.7f)
        {
            // 좋은 타격
            trailColor = goodHitColor;
            durationMultiplier = 1.0f;
            widthMultiplier = 1.0f;
            Debug.Log("중간 품질 타격 감지: 트레일 색상 = 파란색");
        }
        else
        {
            // 중심에서 벗어난 타격
            trailColor = offCenterHitColor;
            durationMultiplier = 0.8f;
            widthMultiplier = 0.8f;
            Debug.Log("낮은 품질 타격 감지: 트레일 색상 = 초록색");
        }

        // 트레일 지속 시간 및 너비 설정
        trail.time = trailTime * durationMultiplier;
        trail.widthMultiplier = trailWidth * widthMultiplier;

        // 그라데이션 색상 설정
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0].color = trailColor;
        colorKeys[0].time = 0.0f;
        colorKeys[1].color = trailColor;
        colorKeys[1].time = 1.0f;

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0].alpha = 1.0f;
        alphaKeys[0].time = 0.0f;
        alphaKeys[1].alpha = 0.0f;
        alphaKeys[1].time = 1.0f;

        gradient.SetKeys(colorKeys, alphaKeys);
        trail.colorGradient = gradient;

        // 머티리얼 색상도 설정 (추가적인 보장)
        if (trailMaterial != null)
        {
            trailMaterial.color = trailColor;
        }

        // 트레일 활성화
        trail.emitting = true;

        Debug.Log($"트레일 활성화됨: 품질={qualityFactor:F2}, 색상={trailColor}, 너비={trail.widthMultiplier:F2}");

        // 일정 시간 후 트레일 비활성화
        StartCoroutine(DisableTrailAfterDelay());
    }

    /// <summary>
    /// 일정 시간 후 트레일 비활성화
    /// </summary>
    private IEnumerator DisableTrailAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        if (trail != null)
        {
            trail.emitting = false;
            Debug.Log("트레일 비활성화됨");
        }
    }

    // 테스트용 기능 (인스펙터에서 버튼으로 활성화 가능)
    [ContextMenu("Test Perfect Hit Trail")]
    public void TestPerfectHitTrail()
    {
        ActivateTrail(1.0f);
    }

    [ContextMenu("Test Good Hit Trail")]
    public void TestGoodHitTrail()
    {
        ActivateTrail(0.8f);
    }

    [ContextMenu("Test Off-Center Hit Trail")]
    public void TestOffCenterHitTrail()
    {
        ActivateTrail(0.6f);
    }
}