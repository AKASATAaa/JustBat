namespace FewClicksDev.TimeScaler
{
    using FewClicksDev.Core;
    using UnityEditor;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    [CustomEditor(typeof(TimeScalerPresetsListObject))]
    public class TimeScalerPresetObjectEditor : CustomInspectorBase
    {
        private const string PRESETS = "Presets";
        private const string APPLY_AND_OPEN_WINDOW = "Apply those presets and open window";

        private TimeScalerPresetsListObject timeScalerPresetObject = null;

        private SerializedProperty listNameProperty = null;
        private SerializedProperty listPriorityProperty = null;

        private TimeScalerPresetReorderableList reorderableList = null;

        protected override void OnEnable()
        {
            base.OnEnable();

            timeScalerPresetObject = (TimeScalerPresetsListObject) target;

            listNameProperty = serializedObject.FindProperty("listName");
            listPriorityProperty = serializedObject.FindProperty("positionPriority");

            reorderableList = ScriptableObject.CreateInstance<TimeScalerPresetReorderableList>();
            reorderableList.Init(timeScalerPresetObject, timeScalerPresetObject.Presets, PRESETS);
        }

        protected override void drawInspectorGUI()
        {
            drawScript();
            SmallSpace();
            EditorGUILayout.PropertyField(listNameProperty);
            EditorGUILayout.PropertyField(listPriorityProperty);

            NormalSpace();
            reorderableList.Draw();
            serializedObject.ApplyModifiedProperties();

            NormalSpace();
            float _buttonWidth = windowWidthWithPaddings * 0.7f;

            using (new HorizontalScope())
            {
                FlexibleSpace();

                if (DrawClearBoxButton(APPLY_AND_OPEN_WINDOW, TimeScaler.MAIN_COLOR, FixedWidthAndHeight(_buttonWidth, DEFAULT_LINE_HEIGHT)))
                {
                    TimeScalerWindow.OpenWindowWithPreset(timeScalerPresetObject);
                }

                FlexibleSpace();
            }
        }
    }
}
