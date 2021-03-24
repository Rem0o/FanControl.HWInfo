using FanControl.Plugins;
using System;

namespace FanControl.HWInfo
{
    public class HWInfoPluginSensor : IPluginSensor
    {
        private readonly Func<HWInfo._HWiNFO_ELEMENT> _sensorElementGetter;

        public HWInfoPluginSensor(string originName, Func<HWInfo._HWiNFO_ELEMENT> sensorElementGetter)
        {
            var element = sensorElementGetter();

            Name = $"{element.szLabelUser} - {originName}";
            Id = $"{element.dwSensorIndex}_{element.dwSensorID}";

            _sensorElementGetter = sensorElementGetter;
        }

        public string Name { get; }

        public float? Value { get; private set; }

        public string Id { get; }

        public void Update() => Value = (float)_sensorElementGetter().Value;
    }
}
