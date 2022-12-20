using NModbus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PTLK.NModbus.Extensions
{
    public abstract class ModbusClientBase : IModbusDevice, IDisposable
    {
        public bool IsConnected() => Poller.IsConnected;

        public event Action<LoggingLevel, DateTime, string>? LogChanged
        {
            add
            {
                Poller.LogChanged += value;
            }

            remove
            {
                Poller.LogChanged -= value;
            }
        }

        public ModbusClientBase(ModbusClientOptions? modbusClientOptions = default)
        {
            Type type = GetType();

            ModbusClientOptions = modbusClientOptions ?? ModbusClientOptions;

            ModbusClientOptions.Validate(type.Name, ModbusClientOptions);

            PropertyInfo[] props = GetType().GetProperties();

            foreach (PropertyInfo prop in props)
            {
                object[] attrs = prop.GetCustomAttributes(true);

                foreach (object attr in attrs)
                {
                    if (attr is ModbusDataItemAttribute modbusInfo)
                    {
                        string propName = prop.Name;

                        ModbusInfos.Add(propName, modbusInfo);
                        PropertyInfos.Add(propName, prop);
                    }
                }
            }

            _syncRoot.Wait();

            try
            {
                string key = GetSharedKey(ModbusClientOptions);

                if (Pollers.TryGetValue(key, out ModbusClientPoller? poller))
                {
                    Poller = poller;
                    Poller.ReferenceCount++;
                }
                else
                {
                    Poller = new ModbusClientPoller(ModbusClientOptions)
                    {
                        LoggingLevel = ModbusClientOptions.LoggingLevel
                    };
                    Pollers.Add(key, Poller);
                }
            }
            finally
            {
                _syncRoot.Release();
            }
        }

        private static string GetSharedKey(ModbusClientOptions options)
        {
            return options.UseSerialPort ? options.SerialPort : $"{options.IP}:{options.Port}";
        }

        protected dynamic? Get()
        {
            string? methodName = new StackTrace().GetFrame(1)?.GetMethod()?.Name;
            string propName = GetPropName(methodName);

            ModbusDataItemAttribute modbusInfo = ModbusInfos[propName];
            PropertyInfo propertyInfo = PropertyInfos[propName];
            Type type = propertyInfo.PropertyType;
            type = Nullable.GetUnderlyingType(type) ?? type;
            TypeCode typeCode = Type.GetTypeCode(type);

            int unitId = ModbusClientOptions.UnitId;
            int fc = modbusInfo.FC;
            ushort address = (ushort)modbusInfo.Address;
            ushort length = (ushort)typeCode.GetWordsLength(modbusInfo.Length);

            Array? points = fc switch
            {
                1 or 2 => Poller.GetValue<bool>(unitId, fc, address, length),
                3 or 4 => Poller.GetValue<ushort>(unitId, fc, address, length),
                _ => null
            };

            object? defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;
            if (points == null) return defaultValue;

            ushort[] payload = points.Cast<ushort>().ToArray();
            bool swapWord = ModbusClientOptions.SwapWord;
            double scale = modbusInfo.Scale;

            object? value = typeCode switch
            {
                TypeCode.Boolean => payload.ToBoolean(0),
                TypeCode.Int16 => payload.ToInt16(0) * scale,
                TypeCode.UInt16 => payload.ToUInt16(0) * scale,
                TypeCode.Int32 => payload.ToInt32(0, swapWord) * scale,
                TypeCode.UInt32 => payload.ToUInt32(0, swapWord) * scale,
                TypeCode.Int64 => payload.ToInt64(0, swapWord, swapWord) * scale,
                TypeCode.UInt64 => payload.ToUInt64(0, swapWord, swapWord) * scale,
                TypeCode.Single => payload.ToFloat(0, swapWord) * scale,
                TypeCode.Double => payload.ToDouble(0, swapWord, swapWord) * scale,
                TypeCode.String => payload.ToString(0, length, true),
                _ => null
            };

            if (value == null) return defaultValue;
            return Convert.ChangeType(value, type);
        }

        protected void Set(dynamic? value)
        {
            if (value == null) return;

            string? methodName = new StackTrace().GetFrame(1)?.GetMethod()?.Name;
            string propName = GetPropName(methodName);

            ModbusDataItemAttribute modbusInfo = ModbusInfos[propName];
            PropertyInfo propertyInfo = PropertyInfos[propName];
            Type type = propertyInfo.PropertyType;
            type = Nullable.GetUnderlyingType(type) ?? type;
            TypeCode typeCode = Type.GetTypeCode(type);

            int fc = modbusInfo.FC;
            double scale = modbusInfo.Scale;
            bool swapWord = ModbusClientOptions.SwapWord;
            ushort length = (ushort)typeCode.GetWordsLength(modbusInfo.Length);

            Array? values;

            checked
            {
                try
                {
                    values = typeCode switch
                    {
                        TypeCode.Boolean => fc == 1 ? new[] { value.ToBoolean() } : ((bool)value.ToBoolean()).ToUInt16(),
                        TypeCode.Int16 => ((short)(Convert.ToDouble(value) / scale)).ToUInt16(),
                        TypeCode.UInt16 => ((ushort)(Convert.ToDouble(value) / scale)).ToUInt16(),
                        TypeCode.Int32 => ((int)(Convert.ToDouble(value) / scale)).ToUInt16(swapWord),
                        TypeCode.UInt32 => ((uint)(Convert.ToDouble(value) / scale)).ToUInt16(swapWord),
                        TypeCode.Int64 => ((long)(Convert.ToDouble(value) / scale)).ToUInt16(swapWord, swapWord),
                        TypeCode.UInt64 => ((ulong)(Convert.ToDouble(value) / scale)).ToUInt16(swapWord, swapWord),
                        TypeCode.Single => ((float)(Convert.ToDouble(value) / scale)).ToUInt16(swapWord),
                        TypeCode.Double => ((double)Convert.ToDouble(value) / scale).ToUInt16(swapWord, swapWord),
                        TypeCode.String => ((string)Convert.ToString(value)).ToUInt16(length, true),
                        _ => null
                    };
                }
                catch
                {
                    values = null;
                }
            }

            if (values == null) return;

            bool useSingleWrite = ModbusClientOptions.UseSingleWrite;

            if (fc == 1)
            {
                fc = values.Length == 1 && useSingleWrite ? 5 : 15;
            }

            if (fc == 3)
            {
                fc = values.Length == 1 && useSingleWrite ? 6 : 16;
            }

            byte unitId = (byte)ModbusClientOptions.UnitId;
            ushort address = (ushort)modbusInfo.Address;

            switch (fc)
            {
                case 5:
                    foreach (object val in values)
                    {
                        Poller.SetValue(unitId, fc, address++, new bool[] { (bool)val });
                    }
                    break;
                case 6:
                    foreach (object val in values)
                    {
                        Poller.SetValue(unitId, fc, address++, new ushort[] { (ushort)val });
                    }
                    break;
                case 15:
                    Poller.SetValue(unitId, fc, address, (bool[])values);
                    break;
                case 16:
                    Poller.SetValue(unitId, fc, address, (ushort[])values);
                    break;
            }; ;
        }

        private static string GetPropName(string? methodName)
        {
            if (methodName == null) throw new ArgumentNullException(nameof(methodName));

            string[] methodNameArray = methodName.Split('_');
            if (methodNameArray.Length < 2) throw new ArgumentException($"{nameof(methodName)} sure be get_XXX or set_XXX");

            string propType = methodNameArray[0];
            return methodName.Substring(propType.Length + 1);
        }

        private bool disposedValue;
        private readonly ModbusClientPoller Poller;
        private readonly ModbusClientOptions ModbusClientOptions = new();
        private readonly Dictionary<string, PropertyInfo> PropertyInfos = new();
        private readonly Dictionary<string, ModbusDataItemAttribute> ModbusInfos = new();

        private static readonly SemaphoreSlim _syncRoot = new(1, 1);
        private static readonly Dictionary<string, ModbusClientPoller> Pollers = new();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _syncRoot.Wait();

                    try
                    {
                        string key = GetSharedKey(ModbusClientOptions);

                        if (Pollers.TryGetValue(key, out ModbusClientPoller? poller))
                        {
                            poller.ReferenceCount--;

                            if (poller.ReferenceCount == 0)
                            {
                                poller.Dispose();
                                Pollers.Remove(key);
                            }
                        }
                    }
                    finally
                    {
                        _syncRoot.Release();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(disposing: true);
        }
    }
}
