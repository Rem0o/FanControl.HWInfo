namespace FanControl.HWInfo
{
    internal class HWInfoRegistryUpdateResult
    {
        public static HWInfoRegistryUpdateResult Success() => new HWInfoRegistryUpdateResult();

        public static HWInfoRegistryUpdateResult Failure(HWInfoPluginSensor sensor) => new HWInfoRegistryUpdateResult(sensor);

        private HWInfoRegistryUpdateResult(HWInfoPluginSensor sensor = null)
        {
            MissingSensor = sensor;
        }

        public bool IsSuccess => MissingSensor == null;

        public HWInfoPluginSensor MissingSensor { get; }
    }
}
