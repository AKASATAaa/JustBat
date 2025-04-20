namespace FewClicksDev.TimeScaler
{
    using FewClicksDev.Core.ReorderableList;
    using UnityEditor;

    public class TimeScalerPresetReorderableList : ReorderableList<TimeScalerPreset>
    {
        protected override SerializedObject getSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}
