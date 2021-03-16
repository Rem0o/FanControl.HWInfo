using FanControl.Plugins;
using System;

namespace FanControl.HWInfo
{
    public class HWInfoPluginSensor : IPluginSensor
    {
        private readonly Func<HWInfo._HWiNFO_ELEMENT> _sensorElementGetter;

        public HWInfoPluginSensor(string originName, string name, Func<HWInfo._HWiNFO_ELEMENT> sensorElementGetter)
        {
            Name = $"{name} - {originName}";
            _sensorElementGetter = sensorElementGetter;
        }

        public string Name { get; }

        public float? Value { get; private set; }

        public void Update() => Value = (float)_sensorElementGetter().Value;
    }
}
