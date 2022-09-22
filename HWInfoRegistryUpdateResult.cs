using System.Collections.Generic;
using System.Linq;

namespace FanControl.HWInfo
{
    internal class HWInfoRegistryUpdateResult
    {
        private static HWInfoRegistryUpdateResult SuccessSingleton = new HWInfoRegistryUpdateResult();

        public static HWInfoRegistryUpdateResult Success() => SuccessSingleton;

        public static HWInfoRegistryUpdateResult Failure(HWInfoPluginSensor sensor) => new HWInfoRegistryUpdateResult(sensor);
        public static HWInfoRegistryUpdateResult Failure(IEnumerable<HWInfoPluginSensor> sensors) => new HWInfoRegistryUpdateResult(sensors);

        private HWInfoRegistryUpdateResult(IEnumerable<HWInfoPluginSensor> sensors)
        {
            (MissingSensors as List<HWInfoPluginSensor>).AddRange(sensors);
        }

        private HWInfoRegistryUpdateResult(HWInfoPluginSensor sensor = null)
        {
            if ( sensor != null)
                (MissingSensors as List<HWInfoPluginSensor>).Add(sensor);
        }

        public bool IsSuccess => !MissingSensors.Any();

        public IEnumerable<HWInfoPluginSensor> MissingSensors { get; } = new List<HWInfoPluginSensor>();
    }
}
