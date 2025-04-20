//#define USE_SHORTCUTS

namespace FewClicksDev.TimeScaler
{
    using UnityEditor.ShortcutManagement;
    using UnityEngine;

    using static TimeScaler;

    public static class TimeScalerShortcuts
    {
#if USE_SHORTCUTS

        [Shortcut("FCD - Time Scaler/Apply Preset 1", null, KeyCode.Alpha1, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_01(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(0);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 2", null, KeyCode.Alpha2, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_02(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(1);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 3", null, KeyCode.Alpha3, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_03(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(2);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 4", null, KeyCode.Alpha4, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_04(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(3);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 5", null, KeyCode.Alpha5, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_05(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(4);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 6", null, KeyCode.Alpha6, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_06(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(5);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 7", null, KeyCode.Alpha7, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_07(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(6);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 8", null, KeyCode.Alpha8, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_08(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(7);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 9", null, KeyCode.Alpha9, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_09(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(8);
        }

        [Shortcut("FCD - Time Scaler/Apply Preset 10", null, KeyCode.Alpha0, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void ApplyPreset_10(ShortcutArguments shortcutArguments)
        {
            loadPresetAndTryToApply(9);
        }

        private static void loadPresetAndTryToApply(int _index)
        {
            TimeScalerPreset _newPreset = new TimeScalerPreset(string.Empty, 0f, 0);
            _newPreset.LoadFromEditorPrefs(_index);

            if (_newPreset.IsValid)
            {
                ChangeTimeScale(_newPreset.TimeScale);
            }
        }

#endif
    }
}
