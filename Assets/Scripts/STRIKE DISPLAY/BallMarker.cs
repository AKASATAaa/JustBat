using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스트라이크 존 UI에 표시되는 공 마커의 기능을 관리하는 컴포넌트
/// </summary>
public class BallMarker : MonoBehaviour
{
    [Tooltip("마커 이미지 컴포넌트")]
    public Image markerImage;

    [Tooltip("마커가 사라지는 애니메이션 시간")]
    public float fadeOutTime = 5.0f;

    [Tooltip("마커의 초기 크기")]
    public float initialSize = 1.0f;

    [Tooltip("마커의 최종 크기")]
    public float finalSize = 0.8f;

    [Tooltip("마커의 초기 알파값")]
    public float initialAlpha = 1.0f;

    [Tooltip("마커의 최종 알파값")]
    public float finalAlpha = 0.6f;

    // 마커의 RectTransform
    private RectTransform rectTransform;

    // 스트라이크/볼 여부
    private bool isStrike = true;

    void Awake()
    {
        // 컴포넌트 캐싱
        if (markerImage == null)
        {
            markerImage = GetComponent<Image>();
        }

        rectTransform = GetComponent<RectTransform>();

        // 초기 설정
        SetInitialAppearance();
    }

    void Start()
    {
        // 페이드 아웃 시작
        StartCoroutine(FadeOutRoutine());
    }

    // 마커의 초기 모습 설정
    public void SetInitialAppearance()
    {
        if (markerImage == null) return;

        // 초기 크기 설정
        rectTransform.localScale = Vector3.one * initialSize;

        // 초기 알파값 설정
        Color color = markerImage.color;
        color.a = initialAlpha;
        markerImage.color = color;
    }

    // 마커의 스트라이크/볼 여부 설정
    public void SetStrike(bool strike)
    {
        isStrike = strike;

        // 색상 설정
        if (markerImage != null)
        {
            Color color = isStrike ? Color.yellow : Color.red;
            color.a = markerImage.color.a; // 현재 알파값 유지
            markerImage.color = color;
        }
    }

    // 페이드 아웃 코루틴
    private IEnumerator FadeOutRoutine()
    {
        float elapsedTime = 0;

        // 초기값 저장
        float startSize = initialSize;
        float endSize = finalSize;
        float startAlpha = initialAlpha;
        float endAlpha = finalAlpha;

        while (elapsedTime < fadeOutTime)
        {
            // 경과 시간 업데이트
            elapsedTime += Time.deltaTime;

            // 진행 비율 계산 (0~1)
            float t = Mathf.Clamp01(elapsedTime / fadeOutTime);

            // 크기 업데이트
            float currentSize = Mathf.Lerp(startSize, endSize, t);
            rectTransform.localScale = Vector3.one * currentSize;

            // 알파값 업데이트
            if (markerImage != null)
            {
                Color color = markerImage.color;
                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                markerImage.color = color;
            }

            yield return null;
        }

        // 페이드 아웃 완료 후 최종 상태 설정
        rectTransform.localScale = Vector3.one * endSize;
        if (markerImage != null)
        {
            Color color = markerImage.color;
            color.a = endAlpha;
            markerImage.color = color;
        }
    }
}