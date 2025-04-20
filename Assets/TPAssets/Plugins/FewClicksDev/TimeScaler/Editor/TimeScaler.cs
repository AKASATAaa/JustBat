namespace FewClicksDev.TimeScaler
{
    using FewClicksDev.Core;
    using UnityEngine;

    public static class TimeScaler
    {
        public const string TIME_SCALER_NAME = "TIME SCALER";
        public const string VERSION = "1.2.0";
        public static readonly Color MAIN_COLOR = new Color(0.6226415f, 0.4259938f, 0.2378961f, 1f);

        public static void ChangeTimeScale(float _newTimeScale, bool _printLogs = false)
        {
            Time.timeScale = _newTimeScale;
            Log($"Time scale changed to {_newTimeScale}.", _printLogs);
        }

        public static void Log(string _message, bool _printLogs)
        {
            if (_printLogs == false)
            {
                return;
            }

            BaseLogger.Log(TIME_SCALER_NAME, _message, MAIN_COLOR);
        }

        public static void Warning(string _message, bool _printLogs)
        {
            if (_printLogs == false)
            {
                return;
            }

            BaseLogger.Warning(TIME_SCALER_NAME, _message, MAIN_COLOR);
        }

        public static void Error(string _message, bool _printLogs)
        {
            if (_printLogs == false)
            {
                return;
            }

            BaseLogger.Error(TIME_SCALER_NAME, _message, MAIN_COLOR);
        }
    }
}