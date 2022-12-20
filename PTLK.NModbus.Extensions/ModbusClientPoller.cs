using NModbus;
using NModbus.Serial;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PTLK.NModbus.Extensions   
{
    class ModbusClientPoller : IDisposable
    {
        public int ReferenceCount { get; set; } = 1;

        private int PollingTimeout => Math.Max(Options.ReadRate, 1000);

        private int KeepPollingTime => Math.Max(Options.ReadRate, 30000) + 30000;

        public ModbusClientPoller(ModbusClientOptions options)
        {
            ModbusLogger = new ModbusClientLogger();
            ModbusFactory = new ModbusFactory(logger: ModbusLogger);
            Options = options;

            Timer = new Timer(Polling, null, 0, 100);
        }

        #region Public
        public LoggingLevel LoggingLevel 
        { 
            get => ModbusLogger.LoggingLevel; 
            set => ModbusLogger.LoggingLevel = value; 
        }

        public event Action<LoggingLevel, DateTime, string>? LogChanged
        {
            add
            {
                ModbusLogger.LogChanged += value;
            }

            remove
            {
                ModbusLogger.LogChanged -= value;
            }
        }

        public bool IsConnected => ModbusMaster != null;

        public T[] GetValue<T>(int unitId, int fc, ushort startAddress, ushort numberOfPoints)
        {
            ModbusClientPacket packet = new()
            {
                UnitId = (byte)unitId,
                FC = fc,
                StartAddress = startAddress,
                NumberOfPoints = numberOfPoints
            };

            AddReadRequest(packet);

            if (ReadValues.TryGetValue(packet, out Array? value))
            {
                try
                {
                    return value.Cast<T>().ToArray();
                }
                catch
                {
                    throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalDataValue);
                }
            }

            throw new InvalidModbusRequestException(SlaveExceptionCodes.SlaveDeviceFailure);
        }

        public void SetValue<T>(int unitId, int fc, ushort startAddress, T[] value)
        {
            ModbusClientPacket packet = new()
            {
                UnitId = (byte)unitId,
                FC = fc,
                StartAddress = startAddress,
                NumberOfPoints = (ushort)value.Length,
                Values = value
            };

            Polling(packet);
        }

        public void Dispose()
        {
            _syncRoot.Wait();

            try
            {
                GC.SuppressFinalize(this);
                Timer.Dispose();
                Disconnect();
            }
            finally
            {
                _syncRoot.Release();
            }
        }
        #endregion

        #region Private
        private void OpenSerialPort()
        {
            try
            {
                if (SerialPort == null)
                {
                    SerialPort = new System.IO.Ports.SerialPort
                    {
                        PortName = Options.SerialPort,
                        BaudRate = Options.BaudRate,
                        Parity = Options.Parity,
                        DataBits = Options.DataBits,
                        StopBits = Options.StopBits,
                        ReadTimeout = PollingTimeout,
                        WriteTimeout = PollingTimeout
                    };
                    SerialPort.Open();
                }
                else if (!SerialPort.IsOpen)
                {
                    SerialPort.Open();
                }
            }
            catch
            {
                SerialPort?.Dispose();
                SerialPort = null;
            }
        }

        private bool Connect()
        {
            if (Options.UseSerialPort)
            {
                OpenSerialPort();

                if (SerialPort != null)
                {
                    ModbusMaster = ModbusFactory.CreateRtuMaster(SerialPort);
                    ModbusMaster.Transport.ReadTimeout = PollingTimeout;
                    ModbusMaster.Transport.WriteTimeout = PollingTimeout;
                    return true;
                }
            }
            else
            {
                TcpClient client = new();

                try
                {
                    if (client.ConnectAsync(Options.IP, Options.Port).Wait(PollingTimeout))
                    {
                        client.ReceiveTimeout = PollingTimeout;
                        client.SendTimeout = PollingTimeout;
                        ModbusMaster = ModbusFactory.CreateMaster(client);
                        ModbusMaster.Transport.ReadTimeout = PollingTimeout;
                        ModbusMaster.Transport.WriteTimeout = PollingTimeout;
                        return true;
                    }
                    else
                    {
                        throw new TimeoutException();
                    }
                }
                catch
                {
                    client.Dispose();
                }
            }

            return false;
        }

        private void AddReadRequest(ModbusClientPacket packet)
        {
            DateTime end = DateTime.Now + TimeSpan.FromMilliseconds(KeepPollingTime + Options.ReadRate);

            ReadRequests.AddOrUpdate(packet, new Lifespan(DateTime.Now, end), (k, v) =>
            {
                v.End = (v.End > end) ? v.End : end;

                return v;
            });
        }

        private Array? SendReadPacket(ModbusClientPacket packet)
        {
            if (ModbusMaster == null) return null;

            return packet.FC switch
            {
                1 => ModbusMaster.ReadCoils(packet.UnitId, packet.StartAddress, packet.NumberOfPoints),
                2 => ModbusMaster.ReadInputs(packet.UnitId, packet.StartAddress, packet.NumberOfPoints),
                3 => ModbusMaster.ReadHoldingRegisters(packet.UnitId, packet.StartAddress, packet.NumberOfPoints),
                4 => ModbusMaster.ReadInputRegisters(packet.UnitId, packet.StartAddress, packet.NumberOfPoints),
                _ => null
            };
        }

        private void SendWritePacket(ModbusClientPacket packet)
        {
            if (ModbusMaster == null || packet.Values == null) return;

            switch (packet.FC)
            {
                case 6:
                case 16:
                    if (packet.Values is bool[])
                    {
                        packet.Values = packet.Values.OfType<bool>().Select(c => (ushort)(c ? 1 : 0)).ToArray();
                    }
                    break;
            };

            switch (packet.FC)
            {
                case 5:
                    foreach (object value in packet.Values)
                    {
                        ModbusMaster.WriteSingleCoil(packet.UnitId, packet.StartAddress++, (bool)value);
                    }
                    break;
                case 6:
                    foreach (object value in packet.Values)
                    {
                        ModbusMaster.WriteSingleRegister(packet.UnitId, packet.StartAddress++, (ushort)value);
                    }
                    break;
                case 15:
                    ModbusMaster.WriteMultipleCoils(packet.UnitId, packet.StartAddress, (bool[])packet.Values);
                    break;
                case 16:
                    ModbusMaster.WriteMultipleRegisters(packet.UnitId, packet.StartAddress, (ushort[])packet.Values);
                    break;
            };
        }

        private void Polling(object? state)
        {
            ModbusClientPacket? writePacket = state as ModbusClientPacket;

            int waitTime = writePacket == null ? 0 : Timeout.Infinite;

            bool lockTaken = _syncRoot.Wait(waitTime);

            if (!lockTaken) return;

            try
            {
                if (writePacket == null)
                {
                    if (!ReadRequests.IsEmpty)
                    {
                        if (!IsConnected)
                        {
                            if (!Connect())
                            {
                                ReadValues.Clear();
                                return;
                            }
                        }

                        ModbusClientPacket? readPacket = OptimizeRead(out List<KeyValuePair<ModbusClientPacket, Lifespan>> processedPackets);

                        if (readPacket != null)
                        {
                            Array? response = null;

                            try
                            {
                                response = SendReadPacket(readPacket);
                            }
                            catch (Exception ex)
                            {
                                if (ex is not SlaveException)
                                {
                                    Disconnect();
                                    return;
                                }
                            }

                            Task delay = Task.Delay(Options.Delay);

                            List<ModbusClientPacket> processedPacketsList = processedPackets.Select(c => c.Key).ToList();

                            HandleReadPacketResponse(readPacket, processedPacketsList, response);

                            DateTime next = DateTime.Now + TimeSpan.FromMilliseconds(Options.ReadRate);
                            foreach (KeyValuePair<ModbusClientPacket, Lifespan> item in processedPackets)
                            {
                                if (item.Value.End < DateTime.Now)
                                {
                                    ReadValues.TryRemove(item.Key, out _);
                                    ReadRequests.TryRemove(item.Key, out _);
                                }
                                else
                                {
                                    ReadRequests[item.Key] = new(next, item.Value.End);
                                }
                            }

                            delay.Wait();
                        }
                    }
                }
                else
                {
                    if (!IsConnected)
                    {
                        if (!Connect())
                        {
                            throw new Exception("Connection failed");
                        }
                    }

                    try
                    {
                        SendWritePacket(writePacket);
                    }
                    catch (Exception ex)
                    {
                        if (ex is SlaveException se)
                        {
                            throw new InvalidModbusRequestException(se.SlaveExceptionCode);
                        }
                        else
                        {
                            Disconnect();
                        }
                    }

                    Task.Delay(Options.Delay).Wait();
                }
            }
            finally
            {
                _syncRoot.Release();
            }
        }

        private ModbusClientPacket? OptimizeRead(out List<KeyValuePair<ModbusClientPacket, Lifespan>> processedPackets)
        {
            processedPackets = new();

            IEnumerable<KeyValuePair<ModbusClientPacket, Lifespan>> readPackets = ReadRequests.ToArray().Where(c => c.Value.Start <= DateTime.Now);
            KeyValuePair<ModbusClientPacket, Lifespan> leaderPacket = readPackets.OrderBy(c => c.Value.Start).FirstOrDefault();

            if (leaderPacket.Equals(default)) return null;

            IEnumerable<KeyValuePair<ModbusClientPacket, Lifespan>> items = readPackets
                .OrderBy(c => c.Key.UnitId)
                .ThenBy(c => c.Key.FC)
                .ThenBy(c => c.Key.StartAddress)
                .ThenBy(c => c.Key.NumberOfPoints);

            ModbusClientPacket? packet = null;
            bool cutted = false;

            foreach (KeyValuePair<ModbusClientPacket, Lifespan> item in items)
            {
                if (item.Key.NumberOfPoints > Options.MaxRegisters)
                {
                    continue;
                }

                int startAddress = item.Key.StartAddress;

                if (packet == null)
                {
                    cutted = true;
                }
                else if (packet.UnitId != item.Key.UnitId || packet.FC != item.Key.FC)
                {
                    cutted = true;
                }
                else if (startAddress - (packet.StartAddress + packet.NumberOfPoints) > Options.MaxUnused)
                {
                    cutted = true;
                }
                else
                {
                    ushort number = (ushort)(startAddress - packet.StartAddress + item.Key.NumberOfPoints);

                    if (number > Options.MaxRegisters)
                    {
                        cutted = true;
                    }
                    else
                    {
                        packet.NumberOfPoints = Math.Max(packet.NumberOfPoints, number);
                        processedPackets.Add(item);
                    }
                }

                if (cutted)
                {
                    if (processedPackets.Contains(leaderPacket))
                    {
                        return packet;
                    }

                    packet = new ModbusClientPacket();
                    item.Key.CopyPropertiesTo(packet);
                    processedPackets.Clear();
                    processedPackets.Add(item);
                    cutted = false;
                }
            }

            if (processedPackets.Contains(leaderPacket))
            {
                return packet;
            }

            return null;
        }

        private void HandleReadPacketResponse(ModbusClientPacket readPacket, List<ModbusClientPacket> processedPackets, Array? response)
        {
            foreach (ModbusClientPacket packet in processedPackets)
            {
                if (response != null)
                {
                    int startIndex = packet.StartAddress - readPacket.StartAddress;

                    object[] values = new object[packet.NumberOfPoints];

                    Array.Copy(response, startIndex, values, 0, values.Length);

                    ReadValues[packet] = values;
                }
                else
                {
                    ReadValues.TryRemove(packet, out _);
                }
            }
        }

        private void Disconnect()
        {
            ReadValues.Clear();
            ModbusMaster?.Dispose();
            ModbusMaster = null;
        }
        #endregion

        private readonly Timer Timer;
        private IModbusMaster? ModbusMaster;
        private readonly ModbusClientOptions Options;
        private readonly IModbusFactory ModbusFactory;
        private System.IO.Ports.SerialPort? SerialPort;
        private readonly ModbusClientLogger ModbusLogger;
        private readonly SemaphoreSlim _syncRoot = new(1, 1);
        private readonly ConcurrentDictionary<ModbusClientPacket, Array> ReadValues = new();
        private readonly ConcurrentDictionary<ModbusClientPacket, Lifespan> ReadRequests = new();
    }
}
