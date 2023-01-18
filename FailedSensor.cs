using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanControl.HWInfo
{
    internal struct FailedSensor
    {
        public string Id;
        public object ValueRaw;

        public override string ToString()
        {
            if ( ValueRaw != null)
            {
                return $"{Id} - Value not parsed: {ValueRaw}";
            }

            return $"{Id} - Missing";
        }
    }
}
