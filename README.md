# PTLK.NModbus.Extensions
Support build modbus slave over custom model.

```csharp
using NModbus;
using PTLK.NModbus.Extensions;
using System;
using System.Net;
using System.Net.Sockets;

ModbusFactory modbusFactory = new();
ModbusDevice device = new();

IModbusSlaveNetwork slaveNetwork = modbusFactory.CreateSlaveNetwork(new TcpListener(IPAddress.Any, 502));
IModbusSlave slave = modbusFactory.CreateSlave(1, device);
slaveNetwork.AddSlave(slave);

await slaveNetwork.ListenAsync();

class ModbusDevice : IModbusDevice
{
    [ModbusDataItem(Address = 0)]
    public static ushort Heartbeat => (ushort)DateTime.Now.Second;

    [ModbusDataItem(Address = 1, Length = 10)]
    public string Username
    {
        get => _Username;

        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _Username = value;
                Console.WriteLine($"Hello {_Username}!");
            }
            else
            {
                throw new InvalidModbusRequestException(SlaveExceptionCodes.IllegalDataValue);
            }
        }
    }

    private string _Username = string.Empty;
}
```
