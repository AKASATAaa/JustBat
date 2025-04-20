namespace FewClicksDev.TimeScaler
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;
    using static TimeScaler;
    using static TimeScalerExtensions;
    using static TimeScalerUserPreferences;

    public class TimeScalerWindow : CustomEditorWindow
    {
        public enum WindowMode
        {
            Usage = 0,
            PresetsLists = 1,
            Settings = 2
        }

        public enum ExportMode
        {
            SaveIntoNewFile = 0,
            OverrideInAttached = 1
        }

        private const float TOOLBAR_WIDTH = 0.765f;
        private const float DEFAULT_LABELS_WIDTH = 150f;
        private const float INDEX_WIDTH = 30f;
        private const float DEFAULT_PRESET_TIME_SCALE_WIDTH = 75f;
        private const float APPLY_BUTTON_WIDTH = 75f;
        private const float LINE_HEIGHT = 20f;
        private const int RANGE_INDENT = 2;
        private const int MAX_NUMBER_OF_PRESETS = 10;
        private const int MAX_LINE_HEIGHT = 32;

        private static readonly GUIContent PRINT_USAGE_LOGS_CONTENT = new GUIContent("Print usage logs", "Print a log to the console every time there is a time scale change caused by this tool.");
        private static readonly GUIContent RESET_ON_GAME_START_CONTENT = new GUIContent("Reset on game start", "Reset the time scale to the default value on game start.");
        private static readonly GUIContent RESET_ON_GAME_STOP_CONTENT = new GUIContent("Reset on game stop", "Reset the time scale to the default value on game stop.");
        private static readonly GUIContent DEFAULT_TIME_SCALE_CONTENT = new GUIContent("Default time scale", "Default time scale value. Used when reset on game start/stop is enabled.");
        private static readonly GUIContent LINE_HEIGHT_CONTENT = new GUIContent("Line height", "Height of the preset line in the usage window.");
        private static readonly GUIContent RESET_TO_DEFAULTS_CONTENT = new GUIContent("Reset settings to defaults", "Reset all settings to their default values.");
        private static readonly GUIContent DELETE_ALL_PRESETS_CONTENT = new GUIContent("Reset presets to defaults", "Delete all presets from the editor preferences.");
        private static readonly GUIContent DISABLE_SHORTCUTS_CONTENT = new GUIContent("Disable shortcuts", "Disable all shortcuts. This will remove the shortcuts from the editor preferences and comment the USE_SHORTCUTS define from the TimeScalerShortcuts.cs file.");
        private static readonly GUIContent ENABLE_SHORTCUTS_CONTENT = new GUIContent("Enable shortcuts", "Enable all shortcuts. This will add the shortcuts to the editor preferences and uncomment the USE_SHORTCUTS define from the TimeScalerShortcuts.cs file.");
        private static readonly GUIContent REFRESH_AND_SORT_PRESETS_LIST_CONTENT = new GUIContent("Sort and refresh lists", "Find all presets list in the project, sort them by priority.");

        private const string CURRENT_TIME_SCALE = "Current Time Scale";
        private const string SLIDER_RANGE = "Slider Range";
        private const string MIN_RANGE = "Min";
        private const string MAX_RANGE = "Max";
        private const string UPDATE_TIME_SCALE = "Update Time Scale";
        private const string ADD_PRESET = "Add Preset";
        private const string NEW_PRESET = "New Preset";
        private const string MAIN_SETTINGS = "Main Settings";
        private const string SHORTCUTS = "Shortcuts";
        private const string ASSET = "asset";
        private const string DEFAULT_PRESETS_LIST_NAME = "New Presets List";
        private const string EXPORT_MODE = "Export mode";
        private const string PRESETS_TO_OVERRIDE = "Presets to override";
        private const string PLEASE_ASSING_SCRIPTABLE = "Please assign a scriptable to override the presets in it.";
        private const string FOLDER_PATH_ERROR = "The scriptable object must be saved within the current project assets folder!";
        private const string EXPORT_PRESETS_TO_SCRIPTABLE = "Export presets to the scriptable object";
        private const string OVERRIDE_PRESETS_IN_ATTACHED = "Override presets in the attached file";
        private const string SAVE_PRESETS_INTO_SCRIPTABLE = "Save presets into the scriptable object";
        private const string OPEN_PROJECT_SETTINGS = "Open the Project Settings on 'Time' tab";
        private const string AVAILABLE_LISTS = "Available lists";
        private const string ERROR_NO_PRESETS_LISTS = "Time Scaler didn't found any presets lists inside of your Unity project. Something went terribly wrong! Please reimport the tool package.";
        private const string INFO_CREATE_NEW_LISTS = "Here is a list of all Presets List that were found in the project. You can create a new one using the 'Create/FewClicks Dev/Time Scaler/Presets List' menu under the right mouse button.";

        public GUIStyle TextBoxStyle
        {
            get
            {
                if (textBoxStyle == null)
                {
                    textBoxStyle = new GUIStyle(EditorStyles.textField);
                    textBoxStyle.alignment = TextAnchor.MiddleLeft;
                    textBoxStyle.padding = new RectOffset(5, 0, 0, 0);
                }

                return textBoxStyle;
            }
        }

        private GUIStyle textBoxStyle = null;

        public GUIStyle LabelStyle
        {
            get
            {
                if (labelStyle == null)
                {
                    labelStyle = new GUIStyle(Styles.BoxButton);
                    labelStyle.alignment = TextAnchor.MiddleLeft;
                    labelStyle.padding = new RectOffset(5, 0, 0, 0);
                }

                return labelStyle;
            }
        }

        private GUIStyle labelStyle = null;

        protected override string windowName => "Time Scaler";
        protected override string version => VERSION;
        protected override Vector2 minWindowSize => new Vector2(550f, 660f);
        protected override Color mainColor => MAIN_COLOR;

        protected override bool hasDocumentation => true;
        protected override string documentationURL => "https://docs.google.com/document/d/13FmgVLPVjV_w-2MEYQVRgcr2pqCsbKdBUhseK8pNofc/edit?usp=sharing";
        protected override bool askForReview => true;
        protected override string reviewURL => "https://assetstore.unity.com/packages/tools/utilities/time-scaler-270830#reviews";

        private float currentTimeScale = 1f;
        private Vector2 usagePresetsScrollPosition = Vector2.zero;
        private Vector2 presetsListScrollPosition = Vector2.zero;

        private WindowMode windowMode = WindowMode.Usage;
        private List<TimeScalerPreset> presets = new List<TimeScalerPreset>();
        private TimeScalerPresetsListObject[] foundPresetsLists = null;

        private ExportMode presetsExportMode = ExportMode.SaveIntoNewFile;
        private TimeScalerPresetsListObject presetsToOverride = null;

        protected override void OnEnable()
        {
            base.OnEnable();

            EditorApplication.playModeStateChanged -= handleResetOnPlayModeChanges; //Just in case
            EditorApplication.playModeStateChanged += handleResetOnPlayModeChanges;

            loadPresets();
            findPresetsListObjects();
        }

        private void OnDisable()
        {
            SavePreferences();
            EditorApplication.playModeStateChanged -= handleResetOnPlayModeChanges;
        }

        protected override void drawWindowGUI()
        {
            NormalSpace();
            windowMode = this.DrawEnumToolbar(windowMode, TOOLBAR_WIDTH, mainColor);
            SmallSpace();
            DrawLine();
            SmallSpace();

            switch (windowMode)
            {
                case WindowMode.Usage:
                    drawUsageTab();
                    break;

                case WindowMode.PresetsLists:
                    drawPresetsListsTab();
                    break;

                case WindowMode.Settings:
                    drawSettingsTab();
                    break;
            }
        }

        private void drawUsageTab()
        {
            SmallSpace();
            drawTimeScaleSetup();

            NormalSpace();
            DrawLine();
            NormalSpace();
            drawTimeScalePresets();
            NormalSpace();
            DrawLine();
            NormalSpace();
            drawExportSavePresets();
        }

        private void drawTimeScaleSetup()
        {
            using (new LabelWidthScope(DEFAULT_LABELS_WIDTH))
            {
                using (new HorizontalScope())
                {
                    DrawDefaultLabel(CURRENT_TIME_SCALE);

                    using (new DisabledScope())
                    {
                        EditorGUILayout.FloatField(Time.timeScale);
                    }
                }

                SmallSpace();
                EditorGUILayout.LabelField(SLIDER_RANGE);

                using (var _changeScope = new ChangeCheckScope())
                {
                    using (new IndentScope(RANGE_INDENT))
                    {
                        MinRange = EditorGUILayout.FloatField(MIN_RANGE, MinRange);
                        MaxRange = EditorGUILayout.FloatField(MAX_RANGE, MaxRange);
                    }

                    if (_changeScope.changed)
                    {
                        if (MinRange > MaxRange)
                        {
                            MinRange = MaxRange;
                        }

                        if (MinRange < 0f)
                        {
                            MinRange = 0f;
                        }

                        if (MaxRange < MinRange)
                        {
                            MaxRange = MinRange;
                        }

                        SavePreferences();
                    }
                }

                SmallSpace();

                using (new HorizontalScope())
                {
                    currentTimeScale = EditorGUILayout.Slider(UPDATE_TIME_SCALE, currentTimeScale, MinRange, MaxRange);
                    SmallSpace();

                    if (DrawBoxButton(APPLY, FixedWidthAndHeight(APPLY_BUTTON_WIDTH, DEFAULT_LINE_HEIGHT)))
                    {
                        ChangeTimeScale(currentTimeScale, PrintLogs);
                    }

                    SmallSpace();
                }
            }
        }

        private void drawTimeScalePresets()
        {
            using (var _changeScope = new ChangeCheckScope())
            {
                using (var _scrollScope = new ScrollViewScope(usagePresetsScrollPosition))
                {
                    for (int i = 0; i < presets.Count; i++)
                    {
                        drawPreset(presets[i], i);
                    }

                    usagePresetsScrollPosition = _scrollScope.scrollPosition;
                }

                if (_changeScope.changed)
                {
                    saveAllPresets();
                }
            }

            if (presets.Count >= MAX_NUMBER_OF_PRESETS)
            {
                return;
            }

            NormalSpace();
            float _buttonWidth = windowWidthWithPaddings / 2f;

            using (new HorizontalScope())
            {
                FlexibleSpace();

                if (DrawClearBoxButton(ADD_PRESET, mainColor, FixedWidthAndHeight(_buttonWidth, LINE_HEIGHT)))
                {
                    presets.Add(new TimeScalerPreset(NEW_PRESET, 1f, presets.Count - 1));
                    saveAllPresets();
                }

                FlexibleSpace();
            }
        }

        private void drawExportSavePresets()
        {
            presetsExportMode = (ExportMode) EditorGUILayout.EnumPopup(EXPORT_MODE, presetsExportMode);

            if (presetsExportMode is ExportMode.OverrideInAttached)
            {
                presetsToOverride = EditorGUILayout.ObjectField(PRESETS_TO_OVERRIDE, presetsToOverride, typeof(TimeScalerPresetsListObject), false) as TimeScalerPresetsListObject;

                if (presetsToOverride == null)
                {
                    SmallSpace();
                    EditorGUILayout.HelpBox(PLEASE_ASSING_SCRIPTABLE, MessageType.Warning);
                    FlexibleSpace();
                    return;
                }
            }

            SmallSpace();

            using (new HorizontalScope())
            {
                FlexibleSpace();
                drawSaveExportButtons();
                FlexibleSpace();
            }

            FlexibleSpace();
        }

        private void drawSaveExportButtons()
        {
            float _buttonWidth = windowWidthScaled(0.72f);

            switch (presetsExportMode)
            {
                case ExportMode.SaveIntoNewFile:

                    if (DrawBoxButton(EXPORT_PRESETS_TO_SCRIPTABLE, FixedWidthAndHeight(_buttonWidth, DEFAULT_LINE_HEIGHT)))
                    {
                        var _newPreset = ScriptableObject.CreateInstance<TimeScalerPresetsListObject>();
                        _newPreset.name = DEFAULT_PRESETS_LIST_NAME;
                        _newPreset.ChangeName(DEFAULT_PRESETS_LIST_NAME);
                        _newPreset.AddPresets(presets);

                        var _path = EditorUtility.SaveFilePanel(SAVE_PRESETS_INTO_SCRIPTABLE, Application.dataPath, _newPreset.name, ASSET);

                        if (_path.IsNullEmptyOrWhitespace())
                        {
                            return;
                        }

                        if (_path.StartsWith(Application.dataPath))
                        {
                            AssetDatabase.CreateAsset(_newPreset, AssetsUtilities.ConvertAbsolutePathToDataPath(_path));
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();

                            AssetsUtilities.Ping(_newPreset);
                            findPresetsListObjects();
                        }
                        else
                        {
                            TimeScaler.Error(FOLDER_PATH_ERROR, true);
                        }
                    }

                    break;

                case ExportMode.OverrideInAttached:

                    if (DrawBoxButton(OVERRIDE_PRESETS_IN_ATTACHED, FixedWidthAndHeight(_buttonWidth, DEFAULT_LINE_HEIGHT)))
                    {
                        presetsToOverride.AddPresets(presets);
                        AssetsUtilities.Ping(presetsToOverride);
                    }

                    break;
            }
        }

        private void drawPreset(TimeScalerPreset _preset, int _index)
        {
            using (new HorizontalScope())
            {
                GUILayout.Label($"{(_index + 1).NumberToString(2)}", Styles.BoxButton, FixedWidthAndHeight(INDEX_WIDTH, LineHeight));

                using (new HorizontalScope(Styles.BoxButton, FixedHeight(LineHeight)))
                {
                    _preset.Label = EditorGUILayout.TextField(_preset.Label, TextBoxStyle, FixedHeight(LineHeight - 4));
                }

                using (new HorizontalScope(Styles.BoxButton, FixedWidthAndHeight(DEFAULT_PRESET_TIME_SCALE_WIDTH, LineHeight)))
                {
                    _preset.TimeScale = EditorGUILayout.FloatField(_preset.TimeScale, TextBoxStyle, FixedHeight(LineHeight - 4));
                }

                if (DrawBoxButton(APPLY, FixedWidthAndHeight(APPLY_BUTTON_WIDTH, LineHeight)))
                {
                    ChangeTimeScale(_preset.TimeScale, PrintLogs);
                }

                if (DrawCloseButton(FixedWidthAndHeight(LineHeight, LineHeight)))
                {
                    presets.RemoveAt(_index);
                    saveAllPresets();
                }

                SmallSpace();
            }
        }

        private void drawPresetsListsTab()
        {
            if (foundPresetsLists.IsNullOrEmpty())
            {
                EditorGUILayout.HelpBox(ERROR_NO_PRESETS_LISTS, MessageType.Error);
                return;
            }

            SmallSpace();
            EditorGUILayout.HelpBox(INFO_CREATE_NEW_LISTS, MessageType.Info);
            SmallSpace();

            if (DrawBoxButton(REFRESH_AND_SORT_PRESETS_LIST_CONTENT, FixedWidthAndHeight(wholeSizeButtonWidth, DEFAULT_LINE_HEIGHT)))
            {
                findPresetsListObjects();
            }

            NormalSpace();
            DrawBoldLabel(AVAILABLE_LISTS);
            SmallSpace();

            using (var _scrollScope = new ScrollViewScope(presetsListScrollPosition))
            {
                int _index = 0;

                foreach (var _list in foundPresetsLists)
                {
                    drawPresetListObject(_index, _list);
                    _index++;
                }

                presetsListScrollPosition = _scrollScope.scrollPosition;
            }
        }

        private void drawPresetListObject(int _index, TimeScalerPresetsListObject _listObject)
        {
            using (new HorizontalScope(FixedHeight(DEFAULT_LINE_HEIGHT)))
            {
                GUILayout.Label($"{(_index + 1).NumberToString(2)}", Styles.BoxButton, FixedWidthAndHeight(INDEX_WIDTH, DEFAULT_LINE_HEIGHT));
                GUILayout.Label(_listObject.ListName, LabelStyle, FixedHeight(DEFAULT_LINE_HEIGHT));

                float _width = Mathf.Clamp(windowWidthScaled(0.4f), 240f, 440f);

                using (new HorizontalScope(Styles.BoxButton, FixedWidthAndHeight(_width, DEFAULT_LINE_HEIGHT)))
                {
                    using (new DisabledScope())
                    {
                        EditorGUILayout.ObjectField(_listObject, typeof(TimeScalerPresetsListObject), false);
                    }
                }

                if (DrawBoxButton(APPLY, FixedWidthAndHeight(APPLY_BUTTON_WIDTH, DEFAULT_LINE_HEIGHT)))
                {
                    applyPresetsList(_listObject);
                }

                if (GUILayout.Button(string.Empty, Styles.FixedSelect(DEFAULT_LINE_HEIGHT), FixedWidthAndHeight(DEFAULT_LINE_HEIGHT)))
                {
                    AssetsUtilities.Ping(_listObject);
                }

                SmallSpace();
            }
        }

        private void applyPresetsList(TimeScalerPresetsListObject _listObject)
        {
            presets.Clear();

            foreach (var _preset in _listObject.Presets)
            {
                presets.Add(_preset.GetCopy());
            }

            while (presets.Count > MAX_NUMBER_OF_PRESETS)
            {
                presets.RemoveAt(presets.Count - 1);
            }

            windowMode = WindowMode.Usage;
            saveAllPresets();
        }

        private void drawSettingsTab()
        {
            using (var _changeScope = new ChangeCheckScope())
            {
                SmallSpace();
                DrawBoldLabel(MAIN_SETTINGS);
                SmallSpace();

                PrintLogs = EditorGUILayout.Toggle(PRINT_USAGE_LOGS_CONTENT, PrintLogs);
                ResetOnGameStart = EditorGUILayout.Toggle(RESET_ON_GAME_START_CONTENT, ResetOnGameStart);
                ResetOnGameStop = EditorGUILayout.Toggle(RESET_ON_GAME_STOP_CONTENT, ResetOnGameStop);

                SmallSpace();
                DefaultTimeScale = EditorGUILayout.FloatField(DEFAULT_TIME_SCALE_CONTENT, DefaultTimeScale);

                SmallSpace();
                EditorGUILayout.LabelField(SLIDER_RANGE);

                using (new IndentScope(RANGE_INDENT))
                {
                    MinRange = EditorGUILayout.FloatField(MIN_RANGE, MinRange);
                    MaxRange = EditorGUILayout.FloatField(MAX_RANGE, MaxRange);
                }

                SmallSpace();
                LineHeight = EditorGUILayout.IntSlider(LINE_HEIGHT_CONTENT, LineHeight, (int) DEFAULT_LINE_HEIGHT, MAX_LINE_HEIGHT);

                NormalSpace();
                DrawBoldLabel(HELPERS);
                SmallSpace();

                using (new HorizontalScope())
                {
                    if (DrawBoxButton(RESET_TO_DEFAULTS_CONTENT, FixedWidthAndHeight(halfSizeButtonWidth, DEFAULT_LINE_HEIGHT)))
                    {
                        ResetSettingsToDefaults();
                    }

                    NormalSpace();

                    if (DrawBoxButton(DELETE_ALL_PRESETS_CONTENT, FixedWidthAndHeight(halfSizeButtonWidth, DEFAULT_LINE_HEIGHT)))
                    {
                        deleteAllPresets();
                    }
                }

                SmallSpace();

                if (DrawBoxButton(OPEN_PROJECT_SETTINGS, FixedWidthAndHeight(wholeSizeButtonWidth, DEFAULT_LINE_HEIGHT)))
                {
                    SettingsService.OpenProjectSettings("Project/Time");
                }

                NormalSpace();
                DrawBoldLabel(SHORTCUTS);
                SmallSpace();

                if (ShortcutsEnabled)
                {
                    if (DrawBoxButton(DISABLE_SHORTCUTS_CONTENT, FixedWidthAndHeight(wholeSizeButtonWidth, DEFAULT_LINE_HEIGHT)))
                    {
                        ToggleShortcuts(false);
                    }
                }
                else
                {
                    if (DrawBoxButton(ENABLE_SHORTCUTS_CONTENT, FixedWidthAndHeight(wholeSizeButtonWidth, DEFAULT_LINE_HEIGHT)))
                    {
                        ToggleShortcuts(true);
                    }
                }

                if (_changeScope.changed)
                {
                    SavePreferences();
                }
            }
        }

        private void loadPresets()
        {
            presets.Clear();

            for (int i = 0; i < MAX_NUMBER_OF_PRESETS; i++)
            {
                TimeScalerPreset _newPreset = new TimeScalerPreset(string.Empty, 1f, i);
                _newPreset.LoadFromEditorPrefs(i);

                if (_newPreset.IsValid)
                {
                    presets.Add(_newPreset);
                    continue;
                }

                //All presets are loaded
                break;
            }

            if (presets.IsNullOrEmpty())
            {
                presets.Add(TimeScalerPreset.Default());
                presets.Add(TimeScalerPreset.Paused());
            }
        }

        private void findPresetsListObjects()
        {
            foundPresetsLists = AssetsUtilities.GetAssetsOfType<TimeScalerPresetsListObject>();
            foundPresetsLists = foundPresetsLists.OrderByDescending(_preset => _preset.PositionPriority).ToArray();
        }

        private void handleResetOnPlayModeChanges(PlayModeStateChange _state)
        {
            if (_state is PlayModeStateChange.ExitingEditMode && ResetOnGameStart)
            {
                ChangeTimeScale(DefaultTimeScale, PrintLogs);
            }

            if (_state is PlayModeStateChange.EnteredEditMode && ResetOnGameStop)
            {
                ChangeTimeScale(DefaultTimeScale, PrintLogs);
            }
        }

        private void saveAllPresets()
        {
            for (int i = 0; i < MAX_NUMBER_OF_PRESETS; i++)
            {
                if (i < presets.Count)
                {
                    presets[i].Index = i;
                    presets[i].SaveInEditorPrefs();
                }
                else
                {
                    TimeScalerPreset _newPreset = TimeScalerPreset.Invalid(i);
                    _newPreset.SaveInEditorPrefs();
                }
            }
        }

        private void deleteAllPresets()
        {
            presets.Clear();

            presets.Add(TimeScalerPreset.Default());
            presets.Add(TimeScalerPreset.Paused());

            saveAllPresets();
        }

        [MenuItem("Window/FewClicks Dev/Time Scaler", priority = 102)]
        public static void OpenWindow()
        {
            GetWindow<TimeScalerWindow>().Show();
        }

        public static void OpenWindowWithPreset(TimeScalerPresetsListObject _listObject)
        {
            var _window = GetWindow<TimeScalerWindow>();
            _window.applyPresetsList(_listObject);
            _window.Show();
        }
    }
}