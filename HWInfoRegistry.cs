using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FanControl.HWInfo
{
    internal class HWInfoRegistry: IDisposable
    {
        const string SENSOR_REGISTRY_NAME = "Sensor";
        const string LABEL_REGISTRY_NAME = "Label";
        const string VALUE_REGISTRY_NAME = "Value";
        const string VALUE_RAW_REGISTRY_NAME = "ValueRaw";
        const string MAIN_KEY = @"SOFTWARE\HWiNFO64\VSB";

        private RegistryKey _key = null;

        public HWInfoRegistry()
        {
            _key = Registry.CurrentUser.OpenSubKey(MAIN_KEY);
        }

        public bool IsActive()
        {
            return _key != null;
        }

        public HWInfoPluginSensor[] GetSensors()
        {
            using (var subKey = Registry.CurrentUser.OpenSubKey(MAIN_KEY))
            {
                if (subKey == null)
                {
                    return Array.Empty<HWInfoPluginSensor>();
                }

                var names = subKey.GetValueNames();
                var sensors = names.Where(x => x.StartsWith(SENSOR_REGISTRY_NAME, StringComparison.InvariantCultureIgnoreCase));

                var list = new List<HWInfoPluginSensor>();

                foreach (var sensor in sensors)
                {
                    if (int.TryParse(sensor.Replace(SENSOR_REGISTRY_NAME, string.Empty), out int index))
                    {
                        var kind = GetKind(subKey, index);
                        string id = GetId(subKey, index);
                        string name = GetName(subKey, index);

                        var hwInfoSensor = new HWInfoPluginSensor(index, kind, id, name);

                        list.Add(hwInfoSensor);
                    }
                }

                return list.ToArray();
            }
        }

        internal bool UpdateValues(HWInfoPluginSensor[] sensors)
        {
            foreach(var sensor in sensors)
            {
                object valueRaw = _key.GetValue(VALUE_RAW_REGISTRY_NAME + sensor.Index);

                if (valueRaw == null)
                {
                    return false;
                }

                sensor.Value = float.Parse((String)valueRaw);
            }

            return true;
        }

        public void Dispose()
        {
            _key?.Dispose();
            _key = null;
        }

        private static HwInfoSensorType GetKind(RegistryKey key, int index)
        {
            var value = (string)key.GetValue(VALUE_REGISTRY_NAME + index);
            var unit = value.Split(' ')[1];

            switch (unit.ToUpperInvariant())
            {
                case "°C":
                // maybe should not support F since no conversion is available
                case "°F":
                    return HwInfoSensorType.Temperature;
                case "RPM":
                    return HwInfoSensorType.RPM;
                default:
                    return HwInfoSensorType.NotSupported;
            }
        }

        private static string GetName(RegistryKey subKey, int index)
        {
            var sensor = subKey.GetValue(SENSOR_REGISTRY_NAME + index);
            var label = subKey.GetValue(LABEL_REGISTRY_NAME + index);

            return $"{label} - {sensor}";
        }

        private static string GetId(RegistryKey subKey, int index)
        {
            var sensor = subKey.GetValue(SENSOR_REGISTRY_NAME + index);
            var label = subKey.GetValue(LABEL_REGISTRY_NAME + index);

            return $"HWInfo/{sensor}/{label}";
        }
    }
}
