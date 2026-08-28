using System.Collections.Generic;

namespace FanControl.HWInfo
{
    internal class HWInfoRegistryUpdateResult
    {
        private static readonly HWInfoRegistryUpdateResult SuccessSingleton = new HWInfoRegistryUpdateResult(false);

        // Distinct outcome for "HWiNFO is no longer refreshing the data". It is not a
        // per-sensor read failure, so it must not consume the counter that eventually
        // closes the plugin: it is a condition the plugin recovers from on its own as
        // soon as HWiNFO starts writing again.
        private static readonly HWInfoRegistryUpdateResult StaleSingleton = new HWInfoRegistryUpdateResult(true);

        private readonly List<FailedSensor> _missingSensors = new List<FailedSensor>();
        private readonly bool _isStale;

        public static HWInfoRegistryUpdateResult Success() => SuccessSingleton;

        public static HWInfoRegistryUpdateResult Stale() => StaleSingleton;

        public static HWInfoRegistryUpdateResult Failure(FailedSensor sensor) => new HWInfoRegistryUpdateResult(sensor);
        public static HWInfoRegistryUpdateResult Failure(IEnumerable<FailedSensor> sensors) => new HWInfoRegistryUpdateResult(sensors);

        private HWInfoRegistryUpdateResult(bool isStale)
        {
            _isStale = isStale;
        }

        private HWInfoRegistryUpdateResult(IEnumerable<FailedSensor> sensors)
        {
            _missingSensors.AddRange(sensors);
        }

        private HWInfoRegistryUpdateResult(FailedSensor sensor)
        {
            _missingSensors.Add(sensor);
        }

        /// <summary>HWiNFO is no longer refreshing the registry key.</summary>
        public bool IsStale => _isStale;

        public bool IsSuccess => _missingSensors.Count == 0 && !_isStale;

        public IEnumerable<FailedSensor> MissingSensors => _missingSensors;
    }
}
