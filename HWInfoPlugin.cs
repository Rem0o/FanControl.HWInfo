using System;
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
                if (++_updateFailCount >= 10)
                {
                    var names = String.Join(", ", result.MissingSensors.Select(x => x.Name));
                    Close();
                    throw new Exception($"HWInfo sensor value went missing from registry: {names}");
                }
            }
            else
            {
                _updateFailCount = 0;
            }
        }

        private HWInfoPluginSensor[] _sensors = Array.Empty<HWInfoPluginSensor>();
        private HWInfoRegistry _hwInfoRegistry;
        private int _updateFailCount = 0;
    }
}
