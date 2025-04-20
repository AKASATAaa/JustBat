namespace FewClicksDev.TimeScaler
{
    using FewClicksDev.Core;
    using System.IO;
    using System.Linq;
    using UnityEditor;

    using static TimeScaler;
    using static TimeScalerUserPreferences;

    public static class TimeScalerExtensions
    {
        private const string TIME_SCALER_SHORTCUTS = "TimeScalerShortcuts";
        private const string SHORTCUTS_ENABLED_STRING = "#define USE_SHORTCUTS";
        private const string SHORTCUTS_DISABLED_STRING = "//#define USE_SHORTCUTS";

        private const string SHORTCUTS_SCRIPT_NOT_FOUND_WARNING = "Shortcuts script couldn't be found.";
        private const string SHORTCUTS_FIRST_LINE_INVALID_WARNING = "Shortcuts script first line is invalid.";

        public static void ToggleShortcuts(bool _enable)
        {
            try
            {
                string _scriptPath = getShortcutsScriptPath();

                if (_scriptPath.IsNullEmptyOrWhitespace())
                {
                    Warning(SHORTCUTS_SCRIPT_NOT_FOUND_WARNING, PrintLogs);
                    return;
                }

                var _lines = File.ReadAllLines(_scriptPath);
                _lines[0] = _enable ? SHORTCUTS_ENABLED_STRING : SHORTCUTS_DISABLED_STRING;
                File.WriteAllLines(_scriptPath, _lines);

                AssetDatabase.Refresh();
            }
            catch (System.Exception _exception)
            {
                Error($"Error while toggling shortcuts to {_enable}. Exception: {_exception.Message}", PrintLogs);
            }
        }

        public static bool AreShortcutsEnabled()
        {
            try
            {
                string _scriptPath = getShortcutsScriptPath();

                if (_scriptPath.IsNullEmptyOrWhitespace())
                {
                    Warning(SHORTCUTS_SCRIPT_NOT_FOUND_WARNING, PrintLogs);
                    return false;
                }

                string _firstLine = File.ReadLines(_scriptPath).First().Trim();

                if (_firstLine == SHORTCUTS_ENABLED_STRING)
                {
                    return true;
                }
                else if (_firstLine == SHORTCUTS_DISABLED_STRING)
                {
                    return false;
                }
                else
                {
                    Warning(SHORTCUTS_FIRST_LINE_INVALID_WARNING, PrintLogs);
                    return false;
                }
            }
            catch (System.Exception _exception)
            {
                Error($"Error while checking if shortcuts are enabled. Exception: {_exception.Message}", PrintLogs);
                return false;
            }
        }

        private static string getShortcutsScriptPath()
        {
            var _script = AssetsUtilities.GetAssetOfType<MonoScript>(TIME_SCALER_SHORTCUTS);

            if (_script == null)
            {
                return string.Empty;
            }

            return AssetDatabase.GetAssetPath(_script);
        }
    }
}
