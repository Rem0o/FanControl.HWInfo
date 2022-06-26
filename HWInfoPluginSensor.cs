using FanControl.Plugins;

namespace FanControl.HWInfo
{
    public class HWInfoPluginSensor : IPluginSensor
    {
        internal HWInfoPluginSensor(int index, HwInfoSensorType type, string id, string name)
        {
            Index = index;
            Type = type;
            Id = id;
            Name = name;
        }

        internal HwInfoSensorType Type { get; }

        internal int Index { get; }


        #region IPluginSensor Implementation

        public string Name { get; }

        public float? Value { get; internal set; }

        public string Id { get; }

        public void Update() { }

        #endregion
    }
}
