using FanControl.Plugins;

namespace FanControl.HWInfo
{
    public class HWInfoPlugin : IPlugin2
    {
        public string Name => "HWInfo";

        public void Initialize()
        {
            using (var hwinfo = new HWInfoRegistry())
            {
                _isInitialized = hwinfo.IsActive();
            }
        }

        public void Close()
        {
            _isInitialized = false;
            _sensors = null;
        }

        public void Load(IPluginSensorsContainer _container)
        {
            if (!_isInitialized) return;

            using (var hwinfo = new HWInfoRegistry())
            {
                _sensors = hwinfo.GetSensors();

                foreach (var sensor in _sensors)
                {
                    switch(sensor.Type)
                    {
                        case HwInfoSensorType.Temperature:
                            _container.TempSensors.Add(sensor);
                            break;
                        case HwInfoSensorType.RPM:
                            _container.FanSensors.Add(sensor);
                            break;
                        default:
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
                    throw new System.Exception("HWInfo was closed during operation.");
                }

                if (!hwinfo.UpdateValues(_sensors))
                {
                    Close();
                    throw new System.Exception("HWInfo sensors were changed during operation");
                }
            }
        }

        private bool _isInitialized = false;
        private HWInfoPluginSensor[] _sensors;
    }
}
