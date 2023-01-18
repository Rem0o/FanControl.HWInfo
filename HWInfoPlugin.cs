using System;
using System.Collections.Generic;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.HWInfo
{
    public class HWInfoPlugin : IPlugin2
    {
        private readonly IPluginLogger _logger;
        private readonly IPluginDialog _dialog;

        public HWInfoPlugin(IPluginLogger logger, IPluginDialog dialog)
        {
            _logger = logger;
            _dialog = dialog;
        }

        public string Name => "HWInfo";

        public void Initialize()
        {
            _hwInfoRegistry = new HWInfoRegistry();
            if (!_hwInfoRegistry.IsActive())
            {
                Close();
                throw new Exception("HWInfo is not running or reporting to gadget is not enabled.");
            }
        }

        public void Close()
        {
            _updateFailCount = 0;

            foreach (var sensor in _sensors)
            {
                sensor.Invalidate();
                sensor.Value = null;
            }

            _sensors = Array.Empty<HWInfoPluginSensor>();
            _hwInfoRegistry?.Dispose();

            if (_wentMissing.Any())
            {
                var missingSensors = string.Join(Environment.NewLine, _wentMissing);
                _logger.Log($"HWInfo sensor failed momentarily during operation: {missingSensors}");
            }

            _wentMissing.Clear();
        }

        public void Load(IPluginSensorsContainer container)
        {
            if (!_hwInfoRegistry.IsActive()) return;

            using (var hwinfo = new HWInfoRegistry())
            {
                _sensors = hwinfo.GetSensors();

                foreach (var sensor in _sensors)
                {
                    switch (sensor.Type)
                    {
                        case HwInfoSensorType.Temperature:
                            container.TempSensors.Add(sensor);
                            break;
                        case HwInfoSensorType.RPM:
                            container.FanSensors.Add(sensor);
                            break;
                    }
                }
            }
        }

        public void Update()
        {
            if (_sensors.Length == 0) return;

            if (!_hwInfoRegistry.IsActive())
            {
                Close();
                throw new Exception("HWInfo was closed during operation.");
            }

            HWInfoRegistryUpdateResult result = _hwInfoRegistry.UpdateValues(_sensors);
            if (!result.IsSuccess)
            {
                var ids = String.Join(", ", result.MissingSensors.Select(x => x.Id));
                if (++_updateFailCount >= 10)
                {
                    Close();
                    throw new Exception($"HWInfo sensors failed: {ids}");
                }
                else
                {
                    foreach (var sensor in result.MissingSensors)
                    {
                        _wentMissing.Add(sensor.ToString());
                    }
                }
            }
            else
            {
                _updateFailCount = 0;
            }
        }

        private HWInfoPluginSensor[] _sensors = Array.Empty<HWInfoPluginSensor>();
        private HashSet<string> _wentMissing = new HashSet<string>();
        private HWInfoRegistry _hwInfoRegistry;
        private int _updateFailCount = 0;
    }
}