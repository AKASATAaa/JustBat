namespace FewClicksDev.Core.Versioning
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class Changelog : ScriptableObject
    {
        private static string PREFS_PREFIX => $"{PlayerSettings.productName}.FewClicksDev.";

        [SerializeField] private PackageInformation packageInformation = null;
        [SerializeField] private List<VersionDescription> versions = new List<VersionDescription>();

        public PackageInformation PackageInfo => packageInformation;
        public List<VersionDescription> Versions => versions;

        public bool IsInitialRelease => versions.Count <= 1;

        public VersionDescription GetLatestVersion()
        {
            return versions.Count > 0 ? versions[0] : null;
        }

        public void ClearEditorPrefs()
        {
            foreach (var _version in versions)
            {
                string _versionString = $"{PREFS_PREFIX}{packageInformation.ToolName}.{_version.VersionString}";
                EditorPrefs.DeleteKey(_versionString);

            }

            BaseLogger.Log("CHANGELOG", $"Deleted EditorPrefs of {packageInformation.ToolName}!", Color.gray);
        }

        private void OnEnable()
        {
            tryToOpenTheChangelog();
        }

        private void tryToOpenTheChangelog()
        {
            if (packageInformation == null || IsInitialRelease)
            {
                return;
            }

            var _latestVersion = GetLatestVersion();

            if (_latestVersion == null)
            {
                return;
            }

            string _versionString = $"{PREFS_PREFIX}{packageInformation.ToolName}.{_latestVersion.VersionString}";
            bool _showVersionPopup = EditorPrefs.GetBool(_versionString, true);

            if (_showVersionPopup)
            {
                EditorPrefs.SetBool(_versionString, false);
                ChangelogWindow.ShowChangelog(this);
            }
        }
    }
}