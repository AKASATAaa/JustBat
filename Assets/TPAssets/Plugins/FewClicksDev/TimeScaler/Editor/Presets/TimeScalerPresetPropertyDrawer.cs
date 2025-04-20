namespace FewClicksDev.TimeScaler
{
    using FewClicksDev.Core;
    using UnityEditor;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    [CustomPropertyDrawer(typeof(TimeScalerPreset))]
    public class TimeScalerPresetPropertyDrawer : CustomPropertyDrawerBase
    {
        private const float TIME_SCALE_WIDTH = 60f;

        private const string LABEL_PROPERTY = "Label";
        private const string TIME_SCALER_PROPERTY = "TimeScale";

        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
        {
            if (_property.serializedObject == null)
            {
                return;
            }

            EditorGUI.BeginProperty(_position, _label, _property);

            float _labelWidth = _position.width - TIME_SCALE_WIDTH;

            SerializedProperty _labelProperty = _property.FindPropertyRelative(LABEL_PROPERTY);
            Rect _labelRect = new Rect(_position.x, _position.y, _labelWidth - SMALL_SPACE, singleLineHeight);
            EditorGUI.PropertyField(_labelRect, _labelProperty, GUIContent.none);

            SerializedProperty _timeScaleProperty = _property.FindPropertyRelative(TIME_SCALER_PROPERTY);
            Rect _timeScaleRect = new Rect(_labelRect.x + _labelWidth + SMALL_SPACE, _position.y, TIME_SCALE_WIDTH - SMALL_SPACE, singleLineHeight);
            EditorGUI.PropertyField(_timeScaleRect, _timeScaleProperty, GUIContent.none);

            EditorGUI.EndProperty();
            _property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
        {
            return lineHeightWithSpacing;
        }
    }
}
