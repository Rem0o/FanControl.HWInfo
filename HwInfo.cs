using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace FanControl.HWInfo
{
    public class HWInfo : IDisposable
    {
        private const string HWINFO_SHARED_MEM_FILE_NAME = "Global\\HWiNFO_SENS_SM2";
        private const int HWINFO_SENSORS_STRING_LEN = 128;
        private const int HWINFO_UNIT_STRING_LEN = 16;

        private MemoryMappedFile _memoryMappedFile;
        private MemoryMappedViewAccessor _accessor;
        private _HWiNFO_SHARED_MEM _sharedMemory;

        private readonly List<HWInfoSensorSource> _sensors = new List<HWInfoSensorSource>();

        public HWInfo()
        {
            try
            {
                _memoryMappedFile = MemoryMappedFile.OpenExisting(HWINFO_SHARED_MEM_FILE_NAME, MemoryMappedFileRights.Read);
                _accessor = _memoryMappedFile.CreateViewAccessor(0L, Marshal.SizeOf(typeof(_HWiNFO_SHARED_MEM)), MemoryMappedFileAccess.Read);

                _sharedMemory = new _HWiNFO_SHARED_MEM();
                _accessor.Read(0L, out _sharedMemory);

                _sensors.AddRange(ReadSensorSources());

                FillSensors();
            }
            catch (Exception exception)
            {
                Dispose();

                throw exception;
            }

        }

        public void Dispose()
        {
            _accessor?.Dispose();
            _memoryMappedFile?.Dispose();
        }

        public IEnumerable<HWInfoSensorSource> SensorsSource => _sensors;

        private IEnumerable<HWInfoSensorSource> ReadSensorSources()
        {
            for (uint index = 0; index < _sharedMemory.dwNumSensorElements; ++index)
            {
                using (MemoryMappedViewStream viewStream = _memoryMappedFile.CreateViewStream(_sharedMemory.dwOffsetOfSensorSection + index * _sharedMemory.dwSizeOfSensorElement, _sharedMemory.dwSizeOfSensorElement, MemoryMappedFileAccess.Read))
                {
                    byte[] buffer = new byte[(int)_sharedMemory.dwSizeOfSensorElement];
                    viewStream.Read(buffer, 0, (int)_sharedMemory.dwSizeOfSensorElement);
                    GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                    _HWiNFO_SENSOR structure = (_HWiNFO_SENSOR)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(_HWiNFO_SENSOR));

                    gcHandle.Free();

                    yield return new HWInfoSensorSource(structure);
                }
            }
        }

        private void FillSensors()
        {
            for (uint index = 0; index < _sharedMemory.dwNumReadingElements; ++index)
            {
                uint i = index;
                _HWiNFO_ELEMENT element = GetHWInfoElement(i);

                _sensors[(int)element.dwSensorIndex].Sensors.Add(new HWInfoSensor(() => GetHWInfoElement(i)));
            }
        }

        private _HWiNFO_ELEMENT GetHWInfoElement(uint index)
        {
            _HWiNFO_ELEMENT structure;
            using (MemoryMappedViewStream viewStream = _memoryMappedFile.CreateViewStream(_sharedMemory.dwOffsetOfReadingSection + index * _sharedMemory.dwSizeOfReadingElement, _sharedMemory.dwSizeOfReadingElement, MemoryMappedFileAccess.Read))
            {
                byte[] buffer = new byte[(int)_sharedMemory.dwSizeOfReadingElement];
                viewStream.Read(buffer, 0, (int)_sharedMemory.dwSizeOfReadingElement);
                GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                structure = (_HWiNFO_ELEMENT)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(_HWiNFO_ELEMENT));

                gcHandle.Free();
            }

            return structure;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _HWiNFO_SHARED_MEM
        {
            public uint dwSignature;
            public uint dwVersion;
            public uint dwRevision;
            public long poll_time;
            public uint dwOffsetOfSensorSection;
            public uint dwSizeOfSensorElement;
            public uint dwNumSensorElements;
            public uint dwOffsetOfReadingSection;
            public uint dwSizeOfReadingElement;
            public uint dwNumReadingElements;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _HWiNFO_SENSOR
        {
            public uint dwSensorID;
            public uint dwSensorInst;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWINFO_SENSORS_STRING_LEN)]
            public string szSensorNameOrig;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWINFO_SENSORS_STRING_LEN)]
            public string szSensorNameUser;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _HWiNFO_ELEMENT
        {
            public SENSOR_READING_TYPE tReading;
            public uint dwSensorIndex;
            public uint dwSensorID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWINFO_SENSORS_STRING_LEN)]
            public string szLabelOrig;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWINFO_SENSORS_STRING_LEN)]
            public string szLabelUser;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HWINFO_UNIT_STRING_LEN)]
            public string szUnit;
            public double Value;
            public double ValueMin;
            public double ValueMax;
            public double ValueAvg;
        }
        public enum SENSOR_READING_TYPE
        {
            SENSOR_TYPE_NONE,
            SENSOR_TYPE_TEMP,
            SENSOR_TYPE_VOLT,
            SENSOR_TYPE_FAN,
            SENSOR_TYPE_CURRENT,
            SENSOR_TYPE_POWER,
            SENSOR_TYPE_CLOCK,
            SENSOR_TYPE_USAGE,
            SENSOR_TYPE_OTHER,
        }

        public class HWInfoSensorSource
        {
            public HWInfoSensorSource(_HWiNFO_SENSOR _sensor) => Sensor = _sensor;

            public _HWiNFO_SENSOR Sensor { get; }

            public readonly List<HWInfoSensor> Sensors = new List<HWInfoSensor>();
        }

        public class HWInfoSensor
        {
            private readonly Func<_HWiNFO_ELEMENT> _getter;

            public HWInfoSensor(Func<_HWiNFO_ELEMENT> getter) => _getter = getter;

            public _HWiNFO_ELEMENT GetUpdatedElement() => _getter();
        }
    }
}
