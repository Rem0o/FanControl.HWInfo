using System;
using System.Collections.Generic;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.HWInfo
{
    public class HWInfoPlugin : IPlugin2
    {
        private const int MaxUpdateFailures = 10;

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
            _staleReported = false;

            foreach (var sensor in _sensors)
            {
                sensor.Invalidate();
                sensor.Value = null;
            }

            _sensors = Array.Empty<HWInfoPluginSensor>();
            _hwInfoRegistry?.Dispose();

            if (_wentMissing.Any())
            {
                var missingSensors = string.Join(Environment.NewLine, _wentMissing);
                _logger.Log($"HWInfo sensor failed momentarily during operation: {missingSensors}");
            }

            _wentMissing.Clear();
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

            if (_hwInfoRegistry == null || !_hwInfoRegistry.IsActive())
            {
                // A single failed check used to close the plugin permanently: if HWiNFO
                // was restarted, the plugin stayed dead until FanControl itself was
                // restarted. Try to reattach instead, and report no reading meanwhile.
                if (!TryReconnect())
                {
                    InvalidateAllValues();

                    if (++_updateFailCount >= MaxUpdateFailures)
                    {
                        Close();
                        throw new Exception("HWInfo was closed during operation.");
                    }

                    return;
                }
            }

            HWInfoRegistryUpdateResult result = _hwInfoRegistry.UpdateValues(_sensors);

            // HWiNFO stopped refreshing the key (closed, crashed, or restarting). The
            // values have already been cleared, so FanControl sees sensors with no
            // reading instead of a frozen temperature. This deliberately does not consume
            // the fatal counter: the plugin recovers on its own once HWiNFO comes back.
            if (result.IsStale)
            {
                if (!_staleReported)
                {
                    _logger.Log("HWInfo stopped refreshing its data. Sensor values cleared until it resumes.");
                    _staleReported = true;
                }

                return;
            }

            if (_staleReported)
            {
                _logger.Log("HWInfo resumed refreshing its data.");
                _staleReported = false;
            }

            if (!result.IsSuccess)
            {
                var ids = String.Join(", ", result.MissingSensors.Select(x => x.Id));
                if (++_updateFailCount >= MaxUpdateFailures)
                {
                    Close();
                    throw new Exception($"HWInfo sensors failed: {ids}");
                }
                else
                {
                    foreach (var sensor in result.MissingSensors)
                    {
                        _wentMissing.Add(sensor.ToString());
                    }
                }
            }
            else
            {
                _updateFailCount = 0;
            }
        }

        /// <summary>
        /// Reattaches to HWiNFO after it was closed or restarted.
        /// </summary>
        private bool TryReconnect()
        {
            _hwInfoRegistry?.Dispose();
            _hwInfoRegistry = new HWInfoRegistry();

            if (!_hwInfoRegistry.IsActive())
            {
                return false;
            }

            // On restart HWiNFO rebuilds the VSB list from scratch, so the indices cached
            // in the sensors are no longer trustworthy. The automatic remapping only
            // triggers when the value count changes, and it may well match: force it.
            _hwInfoRegistry.InvalidateIndexCache();
            _updateFailCount = 0;
            return true;
        }

        private void InvalidateAllValues()
        {
            foreach (var sensor in _sensors)
            {
                sensor.Value = null;
            }
        }

        private HWInfoPluginSensor[] _sensors = Array.Empty<HWInfoPluginSensor>();
        private HashSet<string> _wentMissing = new HashSet<string>();
        private HWInfoRegistry _hwInfoRegistry;
        private int _updateFailCount = 0;
        private bool _staleReported = false;
    }
}