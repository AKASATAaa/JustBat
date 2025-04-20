namespace FewClicksDev.TimeScaler
{
    using UnityEditor;

    [System.Serializable]
    public class TimeScalerPreset
    {
        private const string INVALID_PRESET = "Invalid Preset";
        private const string LABEL = "Time Scaler";
        private static readonly string PREFS_PREFIX = $"{PlayerSettings.productName}.FewClicksDev.{LABEL}.";

        public string Label = "New Preset";
        public float TimeScale = 1f;
        public int Index = 0;

        public bool IsValid => Label != INVALID_PRESET;

        private string labelKey => $"{PREFS_PREFIX}{Index}_Label";
        private string timeScaleKey => $"{PREFS_PREFIX}{Index}_TimeScale";

        public TimeScalerPreset(string _label, float _timeScale, int index)
        {
            Label = _label;
            TimeScale = _timeScale;
            Index = index;
        }

        public TimeScalerPreset GetCopy()
        {
            return new TimeScalerPreset(Label, TimeScale, Index);
        }

        public void SetAsInvalid()
        {
            Label = INVALID_PRESET;
        }

        public void SaveInEditorPrefs()
        {
            EditorPrefs.SetString(labelKey, Label);
            EditorPrefs.SetFloat(timeScaleKey, TimeScale);
        }

        public void LoadFromEditorPrefs(int _index)
        {
            Index = _index;
            Label = EditorPrefs.GetString(labelKey, INVALID_PRESET);
            TimeScale = EditorPrefs.GetFloat(timeScaleKey, 1f);
        }

        public static TimeScalerPreset Invalid(int _index)
        {
            return new TimeScalerPreset(INVALID_PRESET, 1f, _index);
        }

        public static TimeScalerPreset Default()
        {
            return new TimeScalerPreset("Default", 1f, 0);
        }

        public static TimeScalerPreset Paused()
        {
            return new TimeScalerPreset("Paused", 0f, 1);
        }
    }
}