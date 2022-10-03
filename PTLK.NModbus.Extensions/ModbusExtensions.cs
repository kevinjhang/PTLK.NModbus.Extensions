using NModbus;
using PTLK.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PTLK.NModbus.Extensions
{
    public static class ModbusExtensions
    {
        #region Decode modbus value
        /// <summary>
        /// Decode modbus value to Boolean.
        /// </summary>
        public static bool ToBoolean(this ushort[] value, int startIndex)
        {
            return value[startIndex] != 0;
        }

        /// <summary>
        /// Decode modbus value to Int16.
        /// </summary>
        public static short ToInt16(this ushort[] value, int startIndex)
        {
            return BitConverter.ToInt16(BitConverter.GetBytes(value[startIndex]), 0);
        }

        /// <summary>
        /// Decode modbus value to UInt16.
        /// </summary>
        public static ushort ToUInt16(this ushort[] value, int startIndex)
        {
            return BitConverter.ToUInt16(BitConverter.GetBytes(value[startIndex]), 0);
        }

        /// <summary>
        /// Decode modbus value to Int32.
        /// </summary>
        public static int ToInt32(this ushort[] value, int startIndex, bool isSwaped)
        {
            return BitConverter.ToInt32(GetBytes(value[startIndex], value[startIndex + 1], isSwaped), 0);
        }

        /// <summary>
        /// Decode modbus value to UInt32.
        /// </summary>
        public static uint ToUInt32(this ushort[] value, int startIndex, bool isSwaped)
        {
            return BitConverter.ToUInt32(GetBytes(value[startIndex], value[startIndex + 1], isSwaped), 0);
        }

        /// <summary>
        /// Decode modbus value to Float.
        /// </summary>
        public static float ToFloat(this ushort[] value, int startIndex, bool isSwaped)
        {
            return BitConverter.ToSingle(GetBytes(value[startIndex], value[startIndex + 1], isSwaped), 0);
        }

        /// <summary>
        /// Decode modbus value to Int64.
        /// </summary>
        public static long ToInt64(this ushort[] value, int startIndex, bool isSwaped, bool isSwaped64)
        {
            return BitConverter.ToInt64(GetBytes(value[startIndex], value[startIndex + 1], value[startIndex + 2], value[startIndex + 3], isSwaped, isSwaped64), 0);
        }

        /// <summary>
        /// Decode modbus value to UInt64.
        /// </summary>
        public static ulong ToUInt64(this ushort[] value, int startIndex, bool isSwaped, bool isSwaped64)
        {
            return BitConverter.ToUInt64(GetBytes(value[startIndex], value[startIndex + 1], value[startIndex + 2], value[startIndex + 3], isSwaped, isSwaped64), 0);
        }

        /// <summary>
        /// Decode modbus value to Double.
        /// </summary>
        public static double ToDouble(this ushort[] value, int startIndex, bool isSwaped, bool isSwaped64)
        {
            return BitConverter.ToDouble(GetBytes(value[startIndex], value[startIndex + 1], value[startIndex + 2], value[startIndex + 3], isSwaped, isSwaped64), 0);
        }

        /// <summary>
        /// Decode modbus value to String.
        /// </summary>
        public static string ToString(this ushort[] value, int startIndex, int bytesLength, bool isSwapedByte)
        {
            if (bytesLength % 2 != 0)
            {
                bytesLength++;
            }

            byte[] buffer = new byte[bytesLength];

            Buffer.BlockCopy(value, startIndex, buffer, 0, bytesLength);

            if (isSwapedByte)
            {
                for (int i = 0; i < buffer.Length; i += 2)
                {
                    (buffer[i + 1], buffer[i]) = (buffer[i], buffer[i + 1]);
                }
            }

            return Encoding.ASCII.GetString(buffer).Replace("\0", "");
        }

        private static byte[] GetBytes(ushort lo, ushort hi, bool isSwaped)
        {
            if (isSwaped)
            {
                // Swap words inside dwords.
                return BitConverter.GetBytes(hi).Concat(BitConverter.GetBytes(lo)).ToArray();
            }
            else
            {
                return BitConverter.GetBytes(lo).Concat(BitConverter.GetBytes(hi)).ToArray();
            }
        }

        private static byte[] GetBytes(ushort lolo, ushort lohi, ushort hilo, ushort hihi, bool isSwaped, bool isSwaped64)
        {
            if (isSwaped)
            {
                if (isSwaped64)
                {
                    // Swap words inside dwords and Swap dwords inside 64-bit registers.
                    return BitConverter.GetBytes(hihi).Concat(BitConverter.GetBytes(hilo)).Concat(BitConverter.GetBytes(lohi)).Concat(BitConverter.GetBytes(lolo)).ToArray();
                }
                else
                {
                    // Swap words inside dwords.
                    return BitConverter.GetBytes(lohi).Concat(BitConverter.GetBytes(lolo)).Concat(BitConverter.GetBytes(hihi)).Concat(BitConverter.GetBytes(hilo)).ToArray();
                }
            }
            else
            {
                if (isSwaped64)
                {
                    // Swap dwords inside 64-bit registers.
                    return BitConverter.GetBytes(hilo).Concat(BitConverter.GetBytes(hihi)).Concat(BitConverter.GetBytes(lolo)).Concat(BitConverter.GetBytes(lohi)).ToArray();
                }
                else
                {
                    return BitConverter.GetBytes(lolo).Concat(BitConverter.GetBytes(lohi)).Concat(BitConverter.GetBytes(hilo)).Concat(BitConverter.GetBytes(hihi)).ToArray();
                }
            }
        }
        #endregion

        #region Encode modbus value
        /// <summary>
        /// Encode modbus value from Boolean.
        /// </summary>
        public static ushort[] ToUInt16(this bool value)
        {
            return new[] { (ushort)(value ? 1 : 0) };
        }

        /// <summary>
        /// Encode modbus value from Int16.
        /// </summary>
        public static ushort[] ToUInt16(this short value)
        {
            return new[] { BitConverter.ToUInt16(BitConverter.GetBytes(value), 0) };
        }

        /// <summary>
        /// Encode modbus value from UInt16.
        /// </summary>
        public static ushort[] ToUInt16(this ushort value)
        {
            return new[] { BitConverter.ToUInt16(BitConverter.GetBytes(value), 0) };
        }

        /// <summary>
        /// Encode modbus value from Int32
        /// </summary>
        public static ushort[] ToUInt16(this int value, bool isSwaped)
        {
            return GetUInt16(BitConverter.GetBytes(value), isSwaped);
        }

        /// <summary>
        /// Encode modbus value from UInt32
        /// </summary>
        public static ushort[] ToUInt16(this uint value, bool isSwaped)
        {
            return GetUInt16(BitConverter.GetBytes(value), isSwaped);
        }

        /// <summary>
        /// Encode modbus value from float
        /// </summary>
        public static ushort[] ToUInt16(this float value, bool isSwaped)
        {
            return GetUInt16(BitConverter.GetBytes(value), isSwaped);
        }

        /// <summary>
        /// Decode modbus value from Int64.
        /// </summary>
        public static ushort[] ToUInt16(this long value, bool isSwaped, bool isSwaped64)
        {
            return GetUInt16(BitConverter.GetBytes(value), isSwaped, isSwaped64);
        }

        /// <summary>
        /// Decode modbus value from UInt64.
        /// </summary>
        public static ushort[] ToUInt16(this ulong value, bool isSwaped, bool isSwaped64)
        {
            return GetUInt16(BitConverter.GetBytes(value), isSwaped, isSwaped64);
        }

        /// <summary>
        /// Decode modbus value from Double.
        /// </summary>
        public static ushort[] ToUInt16(this double value, bool isSwaped, bool isSwaped64)
        {
            return GetUInt16(BitConverter.GetBytes(value), isSwaped, isSwaped64);
        }

        /// <summary>
        /// Decode modbus value from String.
        /// </summary>
        public static ushort[] ToUInt16(this string value, int bytesLength, bool isSwapedByte)
        {
            if (bytesLength % 2 != 0)
            {
                bytesLength++;
            }

            value = value.PadRight(bytesLength, '\0').Substring(0, bytesLength);

            byte[] buffer = Encoding.ASCII.GetBytes(value);

            ushort[] words = new ushort[bytesLength / 2];

            Buffer.BlockCopy(buffer, 0, words, 0, bytesLength);

            if (isSwapedByte)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    words[i] = (ushort)(((words[i] << 8) & 0xff00) | ((words[i] >> 8) & 0x00ff));
                }
            }

            return words;
        }

        private static ushort[] GetUInt16(byte[] bytes, bool isSwaped)
        {
            if (isSwaped)
            {
                // Swap words inside dwords.
                return new[] { BitConverter.ToUInt16(bytes, 2), BitConverter.ToUInt16(bytes, 0) };
            }
            else
            {
                return new[] { BitConverter.ToUInt16(bytes, 0), BitConverter.ToUInt16(bytes, 2) };
            }
        }

        private static ushort[] GetUInt16(byte[] bytes, bool isSwaped, bool isSwaped64)
        {
            if (isSwaped)
            {
                if (isSwaped64)
                {
                    // Swap words inside dwords and Swap dwords inside 64-bit registers.
                    return new[] { BitConverter.ToUInt16(bytes, 6), BitConverter.ToUInt16(bytes, 4), BitConverter.ToUInt16(bytes, 2), BitConverter.ToUInt16(bytes, 0) };
                }
                else
                {
                    // Swap words inside dwords.
                    return new[] { BitConverter.ToUInt16(bytes, 2), BitConverter.ToUInt16(bytes, 0), BitConverter.ToUInt16(bytes, 6), BitConverter.ToUInt16(bytes, 4) };
                }
            }
            else
            {
                if (isSwaped64)
                {
                    // Swap dwords inside 64-bit registers.
                    return new[] { BitConverter.ToUInt16(bytes, 4), BitConverter.ToUInt16(bytes, 6), BitConverter.ToUInt16(bytes, 0), BitConverter.ToUInt16(bytes, 2) };
                }
                else
                {
                    return new[] { BitConverter.ToUInt16(bytes, 0), BitConverter.ToUInt16(bytes, 2), BitConverter.ToUInt16(bytes, 4), BitConverter.ToUInt16(bytes, 6) };
                }
            }
        }
        #endregion

        #region ModbusSlave factory
        public static IModbusSlave CreateSlave(this IModbusFactory factory, byte unitId, IModbusDevice device)
        {
            ISlaveDataStore dataStore = new DeviceDataStore(device);

            return factory.CreateSlave(unitId, dataStore);
        }

        public static int GetWordsLength(this TypeCode type, int bytesLength)
        {
            if (bytesLength % 2 != 0)
            {
                bytesLength++;
            }

            return type switch
            {
                TypeCode.Boolean => 1,
                TypeCode.Int16 => 1,
                TypeCode.UInt16 => 1,
                TypeCode.Int32 => 2,
                TypeCode.UInt32 => 2,
                TypeCode.Int64 => 4,
                TypeCode.UInt64 => 4,
                TypeCode.Single => 2,
                TypeCode.Double => 4,
                TypeCode.String => bytesLength / 2,
                _ => 1
            };
        }

        class DeviceDataStore : ISlaveDataStore
        {
            public DeviceDataStore(IModbusDevice device)
            {
                HoldingPointSource = new DevicePointSource(3, device);
                InputPointSource = new DevicePointSource(4, device);
            }

            public IPointSource<bool>? CoilDiscretes => throw new NotImplementedException();
            public IPointSource<bool>? CoilInputs => throw new NotImplementedException();
            public IPointSource<ushort>? HoldingRegisters => HoldingPointSource;
            public IPointSource<ushort>? InputRegisters => InputPointSource;

            private readonly DevicePointSource HoldingPointSource;
            private readonly DevicePointSource InputPointSource;
        }

        class DevicePointSource : IPointSource<ushort>
        {
            public DevicePointSource(int fc, IModbusDevice device)
            {
                DeviceProperties = device.GetType().GetProperties()
                    .Select(c => new DeviceProperty(device, c))
                    .Where(c => c.Verified && c.ModbusInfo.FC == fc)
                    .ToDictionary(c => c.ModbusInfo.Address, c => c);
            }

            public ushort[] ReadPoints(ushort startAddress, ushort numberOfPoints)
            {
                bool lockTaken = _syncRoot.Wait(1000);

                if (!lockTaken) throw new InvalidModbusRequestException(SlaveExceptionCodes.SlaveDeviceBusy);

                try
                {
                    ushort[] result = new ushort[numberOfPoints];

                    int step = 1;
                    for (int i = startAddress; i < startAddress + numberOfPoints; i += step)
                    {
                        if (DeviceProperties.TryGetValue(i, out DeviceProperty? propety))
                        {
                            ushort[]? value = propety.GetValue();

                            if (value != null)
                            {
                                Array.Copy(value, 0, result, i - startAddress, value.Length);
                            }

                            step = propety.WordLength;
                        }
                        else
                        {
                            step = 1;
                        }
                    }

                    return result;
                }
                finally
                {
                    _syncRoot.Release();
                }
            }

            public void WritePoints(ushort startAddress, ushort[] points)
            {
                bool lockTaken = _syncRoot.Wait(1000);

                if (!lockTaken) throw new InvalidModbusRequestException(SlaveExceptionCodes.SlaveDeviceBusy);

                try
                {
                    int step = 1;
                    for (int i = startAddress; i < startAddress + points.Length; i += step)
                    {
                        if (DeviceProperties.TryGetValue(i, out DeviceProperty? propety))
                        {
                            propety.SetValue(points.Skip(i - startAddress).ToArray());

                            step = propety.WordLength;
                        }
                        else
                        {
                            step = 1;
                        }
                    }
                }
                finally
                {
                    _syncRoot.Release();
                }
            }

            private readonly SemaphoreSlim _syncRoot = new(1, 1);
            private readonly Dictionary<int, DeviceProperty> DeviceProperties;
        }

        class DeviceProperty
        {
            public DeviceProperty(IModbusDevice device, PropertyInfo propertyInfo)
            {
                Device = device;
                PropertyInfo = propertyInfo;

                ModbusDataItemAttribute? modbusInfo = propertyInfo.GetCustomAttribute<ModbusDataItemAttribute>();

                if (modbusInfo == null)
                {
                    ModbusInfo = null!;
                }
                else
                {
                    ModbusInfo = modbusInfo;
                    TypeCode = Type.GetTypeCode(Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType);
                    WordLength = TypeCode.GetWordsLength(ModbusInfo.Length);
                    Verified = true;
                }
            }

            public ModbusDataItemAttribute ModbusInfo { get; set; }

            public bool Verified { get; private set; }

            public int WordLength { get; private set; }

            public ushort[]? GetValue()
            {
                object? value = PropertyInfo.GetValue(Device);
                if (value == null) return null;

                int length = ModbusInfo.Length;
                double scale = ModbusInfo.Scale;
                return TypeCode switch
                {
                    TypeCode.Boolean => value.ToBoolean().ToUInt16(),
                    TypeCode.Int16 => ((short)(Convert.ToDouble(value) / scale)).ToUInt16(),
                    TypeCode.UInt16 => ((ushort)(Convert.ToDouble(value) / scale)).ToUInt16(),
                    TypeCode.Int32 => ((int)(Convert.ToDouble(value) / scale)).ToUInt16(false),
                    TypeCode.UInt32 => ((uint)(Convert.ToDouble(value) / scale)).ToUInt16(false),
                    TypeCode.Int64 => ((long)(Convert.ToDouble(value) / scale)).ToUInt16(false, false),
                    TypeCode.UInt64 => ((ulong)(Convert.ToDouble(value) / scale)).ToUInt16(false, false),
                    TypeCode.Single => ((float)(Convert.ToDouble(value) / scale)).ToUInt16(false),
                    TypeCode.Double => (Convert.ToDouble(value) / scale).ToUInt16(false, false),
                    TypeCode.String => ((string)value).ToUInt16(length, true),
                    _ => null
                };
            }

            public void SetValue(ushort[] points)
            {
                int length = ModbusInfo.Length;
                double scale = ModbusInfo.Scale;
                object? value = TypeCode switch
                {
                    TypeCode.Boolean => points.ToBoolean(0),
                    TypeCode.Int16 => points.ToInt16(0) * scale,
                    TypeCode.UInt16 => points.ToUInt16(0) * scale,
                    TypeCode.Int32 => points.ToInt32(0, false) * scale,
                    TypeCode.UInt32 => points.ToUInt32(0, false) * scale,
                    TypeCode.Int64 => points.ToInt64(0, false, false) * scale,
                    TypeCode.UInt64 => points.ToUInt64(0, false, false) * scale,
                    TypeCode.Single => points.ToFloat(0, false) * scale,
                    TypeCode.Double => points.ToDouble(0, false, false) * scale,
                    TypeCode.String => points.ToString(0, length, true),
                    _ => null
                };

                if (value != null)
                {
                    value = Convert.ChangeType(value, TypeCode);
                    PropertyInfo.SetValue(Device, value);
                }
            }

            private readonly TypeCode TypeCode;
            private readonly IModbusDevice Device;
            private readonly PropertyInfo PropertyInfo;
        }
        #endregion
    }
}
