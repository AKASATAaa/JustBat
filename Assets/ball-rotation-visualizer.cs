using UnityEngine;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 공의 회전축을 시각화하고 테스트하기 위한 디버깅 도구
/// 인스펙터에서 값 변경 시 씬 뷰에서 기즈모가 즉시 업데이트됩니다.
/// </summary>
[ExecuteInEditMode] // 에디터 모드에서도 Update 함수가 실행되도록 설정
public class BallRotationVisualizer : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("공의 Rigidbody 컴포넌트")]
    public Rigidbody ballRigidbody;

    [Header("회전 설정")]
    [Tooltip("수직 방향 회전 속도 (X축)")]
    [Range(-100f, 100f)]
    public float verticalRotationSpeed = 0f;
    [Tooltip("수평 방향 회전 속도 (Z축)")]
    [Range(-100f, 100f)]
    public float horizontalRotationSpeed = 0f;
    [Tooltip("수평 회전 배율")]
    [Range(1f, 5f)]
    public float horizontalRotationMultiplier = 3f;
    [Tooltip("회전 활성화")]
    public bool enableRotation = false;

    [Header("초기 각도 설정")]
    [Tooltip("X축 초기 회전 각도 (도)")]
    [Range(0f, 360f)]
    public float initialRotationAngleX = 0f;
    [Tooltip("Z축 초기 회전 각도 (도)")]
    [Range(0f, 360f)]
    public float initialRotationAngleZ = 0f;

    [Header("시각화 설정")]
    [Tooltip("회전축 기즈모 색상")]
    public Color axisColor = Color.red;
    [Tooltip("기즈모 길이")]
    [Range(0.1f, 5f)]
    public float gizmoLength = 1.0f;

    // 계산된 단일 회전축
    private Vector3 calculatedRotationAxis = Vector3.up;

    private void Awake()
    {
        // Rigidbody가 지정되지 않았으면 현재 게임 오브젝트에서 찾기
        if (ballRigidbody == null && Application.isPlaying)
        {
            ballRigidbody = GetComponent<Rigidbody>();

            // 없으면 생성
            if (ballRigidbody == null)
            {
                ballRigidbody = gameObject.AddComponent<Rigidbody>();
                Debug.Log("Rigidbody가 자동으로 추가되었습니다.");
                ConfigureRigidbody();
            }
        }

        // 초기 회전 각도 적용
        if (Application.isPlaying)
        {
            ApplyInitialRotation();
        }

        // 초기 회전축 계산
        CalculateRotationAxis();
    }

    private void ConfigureRigidbody()
    {
        if (ballRigidbody != null && Application.isPlaying)
        {
            // 물리 속성 설정
            ballRigidbody.useGravity = false;   // 중력 비활성화
            ballRigidbody.isKinematic = false;  // 물리 영향을 받음
            ballRigidbody.drag = 0.0f;          // 공기 저항 0으로 설정
            ballRigidbody.angularDrag = 0.0f;   // 회전 저항도 0으로 설정
            ballRigidbody.maxAngularVelocity = 4000f; // 최대 각속도 설정
        }
    }

    private void OnValidate()
    {
        // 인스펙터에서 값이 변경될 때마다 호출됨
        CalculateRotationAxis();

        // 에디터에서 씬 뷰 갱신 요청
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    private void Update()
    {
        // 에디터 모드에서도 회전축 계산 업데이트
        if (!Application.isPlaying)
        {
            CalculateRotationAxis();
        }
    }

    /// <summary>
    /// 초기 회전 각도를 적용합니다.
    /// </summary>
    public void ApplyInitialRotation()
    {
        transform.rotation = Quaternion.Euler(initialRotationAngleX, 0, initialRotationAngleZ);
    }

    /// <summary>
    /// 회전속도 값을 기반으로 단일 회전축을 계산합니다.
    /// </summary>
    public void CalculateRotationAxis()
    {
        // 수직(X축)과 수평(Z축) 회전 값으로 통합된 회전축 계산
        Vector3 combinedRotation = new Vector3(
            verticalRotationSpeed,
            0,
            horizontalRotationSpeed * horizontalRotationMultiplier
        );

        // 회전값이 있는 경우에만 회전축 업데이트
        if (combinedRotation.magnitude > 0.001f)
        {
            calculatedRotationAxis = combinedRotation.normalized;
        }
        else
        {
            // 회전값이 거의 없는 경우, Y축을 기본 회전축으로 사용
            calculatedRotationAxis = new Vector3(0, 1, 0);
        }
    }

    void FixedUpdate()
    {
        if (Application.isPlaying && enableRotation && ballRigidbody != null)
        {
            // 회전력 적용
            ApplyRotation();
        }
    }

    /// <summary>
    /// Ball_Controller와 동일한 방식으로 회전력을 적용합니다.
    /// </summary>
    private void ApplyRotation()
    {
        if (ballRigidbody != null)
        {
            // X축(수직)과 Z축(수평) 회전 적용
            ballRigidbody.AddRelativeTorque(
                new Vector3(
                    verticalRotationSpeed,
                    0,
                    horizontalRotationSpeed * horizontalRotationMultiplier
                ),
                ForceMode.Acceleration
            );
        }
    }

    /// <summary>
    /// 현재 설정을 초기화하고 공의 회전을 멈춥니다.
    /// </summary>
    public void ResetRotation()
    {
        if (ballRigidbody != null)
        {
            ballRigidbody.angularVelocity = Vector3.zero;
        }
        enableRotation = false;
        ApplyInitialRotation();
    }

    /// <summary>
    /// 현재 설정을 Ball_Controller에 사용할 수 있는 형식으로 출력합니다.
    /// </summary>
    public string GetSettingsAsText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// Ball_Controller 설정값");
        sb.AppendLine($"verticalRotationSpeed = {verticalRotationSpeed}f;");
        sb.AppendLine($"horizontalRotationSpeed = {horizontalRotationSpeed}f;");
        sb.AppendLine($"horizontalRotationMultiplier = {horizontalRotationMultiplier}f;");
        sb.AppendLine($"initialRotationAngleX = {initialRotationAngleX}f;");
        sb.AppendLine($"initialRotationAngleZ = {initialRotationAngleZ}f;");

        sb.AppendLine("\n// 계산된 회전축 정보");
        sb.AppendLine($"// 회전축 벡터: {calculatedRotationAxis}");

        if (Application.isPlaying && ballRigidbody != null &&
            ballRigidbody.angularVelocity.magnitude > 0.1f)
        {
            sb.AppendLine("\n// 현재 각속도 정보");
            sb.AppendLine($"// 각속도: {ballRigidbody.angularVelocity}");
            sb.AppendLine($"// 크기: {ballRigidbody.angularVelocity.magnitude:F2}");
        }

        return sb.ToString();
    }

    // 기즈모를 통한 회전축 시각화
    private void OnDrawGizmos()
    {
        // 현재 위치
        Vector3 position = transform.position;

        // 계산된 회전축 표시 (항상 보임)
        Gizmos.color = axisColor;
        Gizmos.DrawLine(position, position + calculatedRotationAxis * gizmoLength);
        Gizmos.DrawSphere(position + calculatedRotationAxis * gizmoLength, 0.05f);

        // 플레이 모드에서 실제 각속도 벡터 표시 (회전 중일 때만)
        if (Application.isPlaying && ballRigidbody != null &&
            ballRigidbody.angularVelocity.magnitude > 0.1f)
        {
            Vector3 actualAxis = ballRigidbody.angularVelocity.normalized;

            // 실제 회전축은 노란색으로 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(position, position + actualAxis * gizmoLength * 0.8f);
            Gizmos.DrawSphere(position + actualAxis * gizmoLength * 0.8f, 0.04f);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BallRotationVisualizer))]
public class BallRotationVisualizerEditor : Editor
{
    private BallRotationVisualizer visualizer;
    private bool showExportSettings = true;

    void OnEnable()
    {
        visualizer = (BallRotationVisualizer)target;
    }

    public override void OnInspectorGUI()
    {
        // 인스펙터 UI가 변경될 때마다 기즈모 업데이트
        EditorGUI.BeginChangeCheck();

        // 기본 인스펙터 그리기
        DrawDefaultInspector();

        // 값이 변경되었으면 씬 뷰 업데이트
        if (EditorGUI.EndChangeCheck())
        {
            // 회전축 재계산
            visualizer.CalculateRotationAxis();
            // 씬 뷰 즉시 갱신
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(10);

        // 플레이 모드일 때만 회전 제어 버튼 표시
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("회전 시작", GUILayout.Height(30)))
            {
                visualizer.enableRotation = true;
            }
            if (GUILayout.Button("회전 정지", GUILayout.Height(30)))
            {
                visualizer.enableRotation = false;
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("회전 리셋", GUILayout.Height(30)))
            {
                visualizer.ResetRotation();
            }
        }

        if (GUILayout.Button("초기 회전 적용", GUILayout.Height(30)))
        {
            visualizer.ApplyInitialRotation();
        }

        EditorGUILayout.Space(10);

        // 설정 내보내기 섹션
        showExportSettings = EditorGUILayout.Foldout(showExportSettings, "설정 내보내기", true);
        if (showExportSettings)
        {
            EditorGUILayout.HelpBox(visualizer.GetSettingsAsText(), MessageType.Info);

            if (GUILayout.Button("클립보드에 복사"))
            {
                EditorGUIUtility.systemCopyBuffer = visualizer.GetSettingsAsText();
                Debug.Log("설정값이 클립보드에 복사되었습니다.");
            }
        }
    }
}
#endif