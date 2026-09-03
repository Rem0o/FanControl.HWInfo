using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;

namespace FanControl.HWInfo
{
    internal class HWInfoRegistry : IDisposable
    {
        const string SENSOR_REGISTRY_NAME = "Sensor";
        const string LABEL_REGISTRY_NAME = "Label";
        const string VALUE_REGISTRY_NAME = "Value";
        const string VALUE_RAW_REGISTRY_NAME = "ValueRaw";
        const string MAIN_KEY = @"SOFTWARE\HWiNFO64\VSB";
        const string SECOND_KEY = @"SOFTWARE\HWiNFO32\VSB";

        private RegistryKey _key;
        private int _count;

        public HWInfoRegistry()
        {
            _key = Registry.LocalMachine.OpenSubKey(MAIN_KEY) ?? Registry.LocalMachine.OpenSubKey(SECOND_KEY);
            _count = _key?.ValueCount ?? 0;
        }

        public bool IsActive()
        {
            try
            {
                return _key?.ValueCount > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public HWInfoPluginSensor[] GetSensors()
        {
            if (_key == null)
            {
                return Array.Empty<HWInfoPluginSensor>();
            }

            var names = _key.GetValueNames();
            var sensors = names.Where(x => x.StartsWith(SENSOR_REGISTRY_NAME, StringComparison.InvariantCultureIgnoreCase));

            var list = new List<HWInfoPluginSensor>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var sensor in sensors)
            {
                if (int.TryParse(sensor.Replace(SENSOR_REGISTRY_NAME, string.Empty), out int index))
                {
                    var type = GetSensorType(_key, index);

                    if (type == HwInfoSensorType.NotSupported)
                    {
                        continue;
                    }

                    if (!TryGetIdentity(_key, index, out var id, out var name))
                    {
                        // missing sensor/label or invalid id/name; skip
                        continue;
                    }

                    if (!seen.Add(id))
                    {
                        // duplicate
                        continue;
                    }

                    list.Add(new HWInfoPluginSensor(index, type, id, name));
                }
            }

            return list.ToArray();
        }

        internal HWInfoRegistryUpdateResult UpdateValues(HWInfoPluginSensor[] sensors)
        {
            if (_key.ValueCount != _count)
            {
                _count = _key.ValueCount;
                var newSensors = GetSensors().ToDictionary(x => x.Id, x => x);

                foreach (var sensor in sensors)
                {
                    if (newSensors.TryGetValue(sensor.Id, out var corresponding))
                    {
                        sensor.Index = corresponding.Index;
                    }
                    else
                    {
                        sensor.Invalidate();
                    }
                }
            }

            var failed = new List<FailedSensor>();

            foreach (var sensor in sensors)
            {
                if (!sensor.IsValid)
                {
                    failed.Add(new FailedSensor { Id = sensor.Id });
                    continue;
                }

                object valueRaw = _key.GetValue(VALUE_RAW_REGISTRY_NAME + sensor.Index);

                if (valueRaw is string str && !string.IsNullOrEmpty(str) && TryParseValue(str, out float res))
                    sensor.Value = res;
                else
                    failed.Add(new FailedSensor { Id = sensor.Id, ValueRaw = valueRaw });
            }

            return failed.Any() ?
                HWInfoRegistryUpdateResult.Failure(failed) :
                HWInfoRegistryUpdateResult.Success();
        }

        /// <summary>
        /// Parses a ValueRaw string without assuming a decimal separator.
        /// The previous hard-coded "en-us" culture only accepted a dot, so on installs
        /// where HWiNFO writes ValueRaw with a comma every sensor failed to parse and the
        /// plugin shut itself down after ten update cycles.
        /// NumberStyles.Float excludes AllowThousands, so "1,234" cannot be read as a
        /// group separator here and the fallbacks stay unambiguous.
        /// </summary>
        private static bool TryParseValue(string raw, out float result)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                return true;

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
                return true;

            return float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public void Dispose()
        {
            _key?.Dispose();
            _key = null;
        }

        private static HwInfoSensorType GetSensorType(RegistryKey key, int index)
        {
            // HWiNFO rewrites the VSB key on every polling cycle, so a value listed by
            // GetValueNames() can already be gone by the time GetValue() runs here.
            var value = key.GetValue(VALUE_REGISTRY_NAME + index) as string;

            if (string.IsNullOrEmpty(value))
            {
                return HwInfoSensorType.NotSupported;
            }

            var unit = value.Trim().Split(' ').Skip(1).FirstOrDefault() ?? string.Empty;

            switch (unit.ToUpperInvariant())
            {
                case "°C":
                case "℃":
                case "°F":  // maybe should not support F since no conversion is available
                case "℉":
                    return HwInfoSensorType.Temperature;
                case "RPM":
                    return HwInfoSensorType.RPM;
                default:
                    return HwInfoSensorType.NotSupported;
            }
        }

        private static bool TryGetIdentity(RegistryKey subKey, int index, out string id, out string name)
        {
            id = null;
            name = null;

            // Read sensor and label once to avoid duplicate GetValue calls and reduce
            // chance of inconsistent reads due to HWiNFO rewriting the key concurrently.
            var sensor = subKey.GetValue(SENSOR_REGISTRY_NAME + index)?.ToString()?.Trim() ?? string.Empty;
            var label = subKey.GetValue(LABEL_REGISTRY_NAME + index)?.ToString()?.Trim() ?? string.Empty;

            // If either sensor or label is missing/empty, treat this entry as invalid.
            if (string.IsNullOrEmpty(sensor) || string.IsNullOrEmpty(label))
                return false;

            var rawValue = subKey.GetValue(VALUE_REGISTRY_NAME + index) as string ?? string.Empty;

            var unit = (rawValue
                .Trim()
                .Split(' ')
                .Skip(1)
                .FirstOrDefault() ?? string.Empty).ToUpperInvariant();

            id = $"HWInfo/{sensor}/{label}/{unit}";
            name = $"{label} - {sensor}";

            return true;
        }
    }
}
