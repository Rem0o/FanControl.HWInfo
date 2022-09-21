namespace FanControl.HWInfo
{
    internal class HWInfoRegistryUpdateResult
    {
        private static HWInfoRegistryUpdateResult SuccessSingleton = new HWInfoRegistryUpdateResult();

        public static HWInfoRegistryUpdateResult Success() => SuccessSingleton;

        public static HWInfoRegistryUpdateResult Failure(HWInfoPluginSensor sensor) => new HWInfoRegistryUpdateResult(sensor);

        private HWInfoRegistryUpdateResult(HWInfoPluginSensor sensor = null)
        {
            MissingSensor = sensor;
        }

        public bool IsSuccess => MissingSensor == null;

        public HWInfoPluginSensor MissingSensor { get; }
    }
}
