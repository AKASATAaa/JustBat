namespace FewClicksDev.TimeScaler
{
    using FewClicksDev.Core;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "FewClicks Dev/Time Scaler/Presets List", fileName = "timeScalerPresetsList_Default")]
    public class TimeScalerPresetsListObject : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] private string listName = "Default";
        [SerializeField] private int positionPriority = 0;

        public string ListName => listName;
        public int PositionPriority => positionPriority;

        [Header("Presets")]
        [SerializeField] private List<TimeScalerPreset> presets = new List<TimeScalerPreset>();

        public List<TimeScalerPreset> Presets => presets;

        public void AddPresets(List<TimeScalerPreset> _presets)
        {
            if (_presets.IsNullOrEmpty())
            {
                return;
            }

            presets.Clear();

            foreach (var _preset in _presets)
            {
                presets.Add(_preset.GetCopy());
            }

            this.SetAsDirty();
        }

        public void ChangeName(string _name) 
        {
            listName = _name;
            this.SetAsDirty();
        }
    }
}
