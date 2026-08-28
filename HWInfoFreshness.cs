using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

// Both System.Runtime.InteropServices and its ComTypes namespace define FILETIME.
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace FanControl.HWInfo
{
    /// <summary>
    /// Tells whether the values in the VSB registry key are still being refreshed.
    ///
    /// The VSB key outlives the HWiNFO process: when HWiNFO is closed or crashes, the key
    /// stays behind holding the last snapshot. IsActive() still returns true because the
    /// key exists and has values, so the plugin keeps reading it and keeps handing
    /// FanControl frozen temperatures indefinitely, without reporting anything.
    ///
    /// Detection relies on the key's last write time. HWiNFO rewrites the whole key on
    /// every polling cycle, so that timestamp advances continuously while it runs. When it
    /// stops advancing, the data is frozen.
    ///
    /// A frozen timestamp alone is not conclusive, because a user can configure a long
    /// polling interval. Past the tolerance we also check whether the HWiNFO process still
    /// exists, and treat the data as valid if it does.
    /// </summary>
    internal class HWInfoFreshness
    {
        /// <summary>How long a frozen timestamp is tolerated before checking the process.</summary>
        private static readonly TimeSpan FrozenTolerance = TimeSpan.FromSeconds(10);

        /// <summary>How often the (more expensive) process check may run.</summary>
        private static readonly TimeSpan ProcessCheckInterval = TimeSpan.FromSeconds(2);

        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private long _lastWriteTime = -1;
        private TimeSpan _lastChangeSeen;
        private TimeSpan _lastProcessCheck = TimeSpan.Zero;
        private bool _lastProcessAlive = true;

        /// <summary>
        /// True when the values in the key can be considered up to date.
        /// </summary>
        public bool IsFresh(RegistryKey key)
        {
            if (key == null)
            {
                return false;
            }

            var now = _clock.Elapsed;
            long writeTime = GetLastWriteTime(key);

            if (writeTime < 0)
            {
                // The timestamp could not be read. That is not enough reason to declare
                // the data stale and stall the plugin.
                return true;
            }

            if (writeTime != _lastWriteTime)
            {
                _lastWriteTime = writeTime;
                _lastChangeSeen = now;
                return true;
            }

            if (now - _lastChangeSeen < FrozenTolerance)
            {
                // Normal gap between two HWiNFO polling cycles.
                return true;
            }

            // Frozen for a while now: is HWiNFO alive with a slow interval, or gone?
            return IsHwInfoRunning(now);
        }

        /// <summary>Clears the tracked state, for instance after reconnecting.</summary>
        public void Reset()
        {
            _lastWriteTime = -1;
            _lastChangeSeen = _clock.Elapsed;
            _lastProcessCheck = TimeSpan.Zero;
            _lastProcessAlive = true;
        }

        private bool IsHwInfoRunning(TimeSpan now)
        {
            if (now - _lastProcessCheck < ProcessCheckInterval)
            {
                return _lastProcessAlive;
            }

            _lastProcessCheck = now;
            _lastProcessAlive = AnyProcess("HWiNFO64") || AnyProcess("HWiNFO32");
            return _lastProcessAlive;
        }

        private static bool AnyProcess(string name)
        {
            Process[] processes = null;

            try
            {
                processes = Process.GetProcessesByName(name);
                return processes.Length > 0;
            }
            catch (Exception)
            {
                // When in doubt, do not declare the data stale.
                return true;
            }
            finally
            {
                if (processes != null)
                {
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }
                }
            }
        }

        private static long GetLastWriteTime(RegistryKey key)
        {
            try
            {
                int result = RegQueryInfoKey(
                    key.Handle.DangerousGetHandle(),
                    null, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, out FILETIME lastWrite);

                if (result != 0)
                {
                    return -1;
                }

                return ((long)lastWrite.dwHighDateTime << 32) | (uint)lastWrite.dwLowDateTime;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegQueryInfoKey(
            IntPtr hKey,
            System.Text.StringBuilder lpClass,
            IntPtr lpcchClass,
            IntPtr lpReserved,
            IntPtr lpcSubKeys,
            IntPtr lpcbMaxSubKeyLen,
            IntPtr lpcbMaxClassLen,
            IntPtr lpcValues,
            IntPtr lpcbMaxValueNameLen,
            IntPtr lpcbMaxValueLen,
            IntPtr lpcbSecurityDescriptor,
            out FILETIME lpftLastWriteTime);
    }
}
