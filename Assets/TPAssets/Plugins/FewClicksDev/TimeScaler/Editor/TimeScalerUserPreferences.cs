namespace FewClicksDev.TimeScaler
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    public static class TimeScalerUserPreferences
    {
        private const string PREFS_PATH = "FewClicks Dev/Time Scaler";
        private const string LABEL = "Time Scaler";
        private const SettingsScope SETTINGS_SCOPE = SettingsScope.User;

        private static readonly string PREFS_PREFIX = $"{PlayerSettings.productName}.FewClicksDev.{LABEL}.";
        private static readonly string[] KEYWORDS = new string[] { "FewClicks Dev", LABEL, "Time", "Scale" };

        private const float LABEL_WIDTH = 250f;
        private const string SETTINGS = "Settings";
        private const string RESET_TO_DEFAULTS = "Reset to defaults";

        private const string MIN_RANGE_LABEL = "Min range";
        private const string MAX_RANGE_LABEL = "Max range";
        private const string PRINT_LOGS_LABEL = "Print logs";
        private const string RESET_ON_GAME_START_LABEL = "Reset on game start";
        private const string RESET_ON_GAME_STOP_LABEL = "Reset on game stop";
        private const string DEFAULT_TIME_SCALE_LABEL = "Default time scale";
        private const string LINE_HEIGHT_LABEL = "Line height";
        private const bool DEFAULT_SHORTCUTS_ENABLED = false;

        private const float DEFAULT_MIN_RANGE = 0f;
        private const float DEFAULT_MAX_RANGE = 5f;
        private const bool DEFAULT_PRINT_LOGS = false;
        private const bool DEFAULT_RESET_ON_GAME_START = true;
        private const bool DEFAULT_RESET_ON_GAME_STOP = true;
        private const float DEFAULT_TIME_SCALE = 1f;
        private const int DEFAULT_LINE_HEIGHT = 26;

        public static float MinRange = DEFAULT_MIN_RANGE;
        public static float MaxRange = DEFAULT_MAX_RANGE;
        public static bool PrintLogs = DEFAULT_PRINT_LOGS;
        public static bool ResetOnGameStart = DEFAULT_RESET_ON_GAME_START;
        public static bool ResetOnGameStop = DEFAULT_RESET_ON_GAME_STOP;
        public static float DefaultTimeScale = DEFAULT_TIME_SCALE;
        public static int LineHeight = DEFAULT_LINE_HEIGHT;
        public static bool ShortcutsEnabled = DEFAULT_SHORTCUTS_ENABLED;

        private static bool arePrefsLoaded = false;

        static TimeScalerUserPreferences()
        {
            LoadPreferences();
        }

        [SettingsProvider]
        public static SettingsProvider PreferencesSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider(PREFS_PATH, SETTINGS_SCOPE)
            {
                label = LABEL,
                guiHandler = (searchContext) =>
                {
                    OnGUI();
                },

                keywords = new HashSet<string>(KEYWORDS)
            };

            return provider;
        }

        public static void OnGUI()
        {
            using (new IndentScope(1))
            {
                using (new LabelWidthScope(LABEL_WIDTH))
                {
                    if (arePrefsLoaded == false)
                    {
                        LoadPreferences();
                    }

                    DrawHeader(SETTINGS);
                    MinRange = EditorGUILayout.FloatField(MIN_RANGE_LABEL, MinRange);

                    if (MinRange > MaxRange)
                    {
                        MinRange = MaxRange;
                    }

                    if (MinRange < 0f)
                    {
                        MinRange = 0f;
                    }

                    MaxRange = EditorGUILayout.FloatField(MAX_RANGE_LABEL, MaxRange);

                    if (MaxRange < MinRange)
                    {
                        MaxRange = MinRange;
                    }

                    SmallSpace();
                    PrintLogs = EditorGUILayout.Toggle(PRINT_LOGS_LABEL, PrintLogs);
                    ResetOnGameStart = EditorGUILayout.Toggle(RESET_ON_GAME_START_LABEL, ResetOnGameStart);
                    ResetOnGameStop = EditorGUILayout.Toggle(RESET_ON_GAME_STOP_LABEL, ResetOnGameStop);

                    SmallSpace();
                    DefaultTimeScale = EditorGUILayout.FloatField(DEFAULT_TIME_SCALE_LABEL, DefaultTimeScale);

                    NormalSpace();

                    using (new HorizontalScope())
                    {
                        FlexibleSpace();

                        if (DrawBoxButton(RESET_TO_DEFAULTS, FixedWidthAndHeight(EditorGUIUtility.currentViewWidth / 2f, 22f)))
                        {
                            ResetSettingsToDefaults();
                        }

                        FlexibleSpace();
                    }

                    if (GUI.changed == true)
                    {
                        SavePreferences();
                    }
                }
            }
        }

        public static void SavePreferences()
        {
            EditorPrefs.SetFloat(PREFS_PREFIX + MIN_RANGE_LABEL, MinRange);
            EditorPrefs.SetFloat(PREFS_PREFIX + MAX_RANGE_LABEL, MaxRange);

            EditorPrefs.SetBool(PREFS_PREFIX + PRINT_LOGS_LABEL, PrintLogs);
            EditorPrefs.SetBool(PREFS_PREFIX + RESET_ON_GAME_START_LABEL, ResetOnGameStart);
            EditorPrefs.SetBool(PREFS_PREFIX + RESET_ON_GAME_STOP_LABEL, ResetOnGameStop);
            EditorPrefs.SetFloat(PREFS_PREFIX + DEFAULT_TIME_SCALE_LABEL, DefaultTimeScale);

            EditorPrefs.SetInt(PREFS_PREFIX + LINE_HEIGHT_LABEL, LineHeight);
        }

        public static void LoadPreferences()
        {
            MinRange = EditorPrefs.GetFloat(PREFS_PREFIX + MIN_RANGE_LABEL, DEFAULT_MIN_RANGE);
            MaxRange = EditorPrefs.GetFloat(PREFS_PREFIX + MAX_RANGE_LABEL, DEFAULT_MAX_RANGE);

            PrintLogs = EditorPrefs.GetBool(PREFS_PREFIX + PRINT_LOGS_LABEL, DEFAULT_PRINT_LOGS);
            ResetOnGameStart = EditorPrefs.GetBool(PREFS_PREFIX + RESET_ON_GAME_START_LABEL, DEFAULT_RESET_ON_GAME_START);
            ResetOnGameStop = EditorPrefs.GetBool(PREFS_PREFIX + RESET_ON_GAME_STOP_LABEL, DEFAULT_RESET_ON_GAME_STOP);
            DefaultTimeScale = EditorPrefs.GetFloat(PREFS_PREFIX + DEFAULT_TIME_SCALE_LABEL, DEFAULT_TIME_SCALE);

            LineHeight = EditorPrefs.GetInt(PREFS_PREFIX + LINE_HEIGHT_LABEL, (int) DEFAULT_LINE_HEIGHT);

            ShortcutsEnabled = TimeScalerExtensions.AreShortcutsEnabled();
            arePrefsLoaded = true;
        }

        public static void ResetSettingsToDefaults()
        {
            MinRange = DEFAULT_MIN_RANGE;
            MaxRange = DEFAULT_MAX_RANGE;
            PrintLogs = DEFAULT_PRINT_LOGS;

            ResetOnGameStart = DEFAULT_RESET_ON_GAME_START;
            ResetOnGameStop = DEFAULT_RESET_ON_GAME_STOP;
            DefaultTimeScale = DEFAULT_TIME_SCALE;

            LineHeight = DEFAULT_LINE_HEIGHT;

            SavePreferences();
        }
    }
}
