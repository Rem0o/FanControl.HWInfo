using System.Collections.Generic;

namespace FanControl.HWInfo
{
    internal class HWInfoRegistryUpdateResult
    {
        private static readonly HWInfoRegistryUpdateResult SuccessSingleton = new HWInfoRegistryUpdateResult();

        private readonly List<FailedSensor> _missingSensors = new List<FailedSensor>();

        public static HWInfoRegistryUpdateResult Success() => SuccessSingleton;

        public static HWInfoRegistryUpdateResult Failure(FailedSensor sensor) => new HWInfoRegistryUpdateResult(sensor);
        public static HWInfoRegistryUpdateResult Failure(IEnumerable<FailedSensor> sensors) => new HWInfoRegistryUpdateResult(sensors);

        private HWInfoRegistryUpdateResult()
        {
        }

        private HWInfoRegistryUpdateResult(IEnumerable<FailedSensor> sensors)
        {
            _missingSensors.AddRange(sensors);
        }

        private HWInfoRegistryUpdateResult(FailedSensor sensor)
        {
            _missingSensors.Add(sensor);
        }

        public bool IsSuccess => _missingSensors.Count == 0;

        public IEnumerable<FailedSensor> MissingSensors => _missingSensors;
    }
}
