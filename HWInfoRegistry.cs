using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;

namespace FanControl.HWInfo
{
    internal class HWInfoRegistry : IDisposable
    {
        private static CultureInfo _format = new CultureInfo("en-us");

        const string SENSOR_REGISTRY_NAME = "Sensor";
        const string LABEL_REGISTRY_NAME = "Label";
        const string VALUE_REGISTRY_NAME = "Value";
        const string VALUE_RAW_REGISTRY_NAME = "ValueRaw";
        const string MAIN_KEY = @"SOFTWARE\HWiNFO64\VSB";

        private RegistryKey _key;

        public HWInfoRegistry() => _key = Registry.CurrentUser.OpenSubKey(MAIN_KEY);

        public bool IsActive() => _key != null;

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
                        var type = GetSensorType(subKey, index);
                        string id = GetId(subKey, index);
                        string name = GetName(subKey, index);

                        var hwInfoSensor = new HWInfoPluginSensor(index, type, id, name);

                        list.Add(hwInfoSensor);
                    }
                }

                return list.ToArray();
            }
        }

        internal bool UpdateValues(HWInfoPluginSensor[] sensors)
        {
            foreach (var sensor in sensors)
            {
                object valueRaw = _key.GetValue(VALUE_RAW_REGISTRY_NAME + sensor.Index);

                if (valueRaw == null)
                    return false;

                sensor.Value = float.TryParse((string)valueRaw, NumberStyles.Float, _format, out float res) ? res : default(float?);
            }

            return true;
        }

        public void Dispose()
        {
            _key?.Dispose();
            _key = null;
        }

        private static HwInfoSensorType GetSensorType(RegistryKey key, int index)
        {
            var value = (string)key.GetValue(VALUE_REGISTRY_NAME + index);
            var unit = value.Trim().Split(' ').Skip(1).FirstOrDefault() ?? string.Empty;

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
