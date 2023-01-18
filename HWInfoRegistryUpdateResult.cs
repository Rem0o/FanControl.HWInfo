using System.Collections.Generic;
using System.Linq;

namespace FanControl.HWInfo
{
    internal class HWInfoRegistryUpdateResult
    {
        private static HWInfoRegistryUpdateResult SuccessSingleton = new HWInfoRegistryUpdateResult();

        public static HWInfoRegistryUpdateResult Success() => SuccessSingleton;

        public static HWInfoRegistryUpdateResult Failure(FailedSensor sensor) => new HWInfoRegistryUpdateResult(sensor);
        public static HWInfoRegistryUpdateResult Failure(IEnumerable<FailedSensor> sensors) => new HWInfoRegistryUpdateResult(sensors);

        private HWInfoRegistryUpdateResult(IEnumerable<FailedSensor> sensors)
        {
            (MissingSensors as List<FailedSensor>).AddRange(sensors);
        }

        private HWInfoRegistryUpdateResult(FailedSensor? sensor = null)
        {
            if (sensor != null)
                (MissingSensors as List<FailedSensor>).Add(sensor.Value);
        }

        public bool IsSuccess => !MissingSensors.Any();

        public IEnumerable<FailedSensor> MissingSensors { get; } = new List<FailedSensor>();
    }
}
