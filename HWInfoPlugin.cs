using FanControl.Plugins;
using System.Linq;

namespace FanControl.HWInfo
{
    public class HWInfoPlugin : IPlugin
    {
        private HWInfo _hWInfo;

        public string Name => "HWInfo";

        public void Close()
        {
            _hWInfo?.Dispose();
            _hWInfo = null;
        }

        public void Initialize() => _hWInfo = new HWInfo();

        public void Load(IPluginSensorsContainer _container)
        {
            foreach (var source in _hWInfo?.SensorsSource)
                foreach (var sensor in source.Sensors)
                    AddSensorToContainer(_container, source, sensor);
        }

        private static void AddSensorToContainer(IPluginSensorsContainer _container, HWInfo.HWInfoSensorSource source, HWInfo.HWInfoSensor sensor)
        {
            var sensorElement = sensor.GetUpdatedElement();
            var pluginSensor = new HWInfoPluginSensor(source.Sensor.szSensorNameUser, sensor.GetUpdatedElement);

            if (sensorElement.szUnit == "RPM")
                _container.FanSensors.Add(pluginSensor);
            else if (sensorElement.szUnit == "°C")
                _container.TempSensors.Add(pluginSensor);
        }
    }
}
