using System;
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
            using (var hwinfo = new HWInfoRegistry())
            {
                _isInitialized = hwinfo.IsActive();
                if (!_isInitialized)
                {
                    throw new Exception("HWInfo is not running or reporting to gadget is not enabled.");
                }
            }
        }

        public void Close()
        {
            _isInitialized = false;
            _sensors = Array.Empty<HWInfoPluginSensor>();
        }

        public void Load(IPluginSensorsContainer container)
        {
            if (!_isInitialized) return;

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
            if (!_isInitialized) return;

            using (var hwinfo = new HWInfoRegistry())
            {
                if (!hwinfo.IsActive())
                {
                    Close();
                    throw new Exception("HWInfo was closed during operation.");
                }

                if (!hwinfo.UpdateValues(_sensors))
                {
                    Close();
                    throw new Exception("HWInfo sensors were changed during operation");
                }
            }
        }

        private bool _isInitialized;
        private HWInfoPluginSensor[] _sensors = Array.Empty<HWInfoPluginSensor>();
    }
}
