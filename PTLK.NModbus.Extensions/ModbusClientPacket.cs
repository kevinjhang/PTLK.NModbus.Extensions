using System;

namespace PTLK.NModbus.Extensions
{
    class ModbusClientPacket
    {
        public byte UnitId { get; set; }

        public int FC { get; set; }

        public ushort StartAddress { get; set; }

        public ushort NumberOfPoints { get; set; }

        public Array? Values { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is ModbusClientPacket packet &&
                   UnitId == packet.UnitId &&
                   FC == packet.FC &&
                   StartAddress == packet.StartAddress &&
                   NumberOfPoints == packet.NumberOfPoints;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UnitId, FC, StartAddress, NumberOfPoints);
        }
    }
}
