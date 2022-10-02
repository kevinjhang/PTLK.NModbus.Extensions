using System;

namespace PTLK.NModbus.Extensions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ModbusDataItemAttribute : Attribute
    {
        public int FC { get; set; } = 3;

        public int Address { get; set; }

        public int Length { get; set; } = 1;

        public double Scale { get; set; } = 1;
    }
}