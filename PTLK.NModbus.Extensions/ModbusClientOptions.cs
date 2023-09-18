using NModbus;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO.Ports;

namespace PTLK.NModbus.Extensions
{
    public class ModbusClientOptions
    {
        [Required, RegularExpression("^(?:[0-9]{1,3}\\.){3}[0-9]{1,3}$", ErrorMessage = "Illegal")]
        public string IP { get; set; } = "127.0.0.1";

        [Range(1, 65535)]
        public int Port { get; set; } = 502;

        [Range(1, 247)]
        public int UnitId { get; set; } = 1;

        [Range(0, 60000)]
        public int ReadRate { get; set; } = 1000;

        [Range(0, 60000)]
        public int Delay { get; set; } = 20;

        public bool SwapWord { get; set; }

        public bool UseSingleWrite { get; set; } = true;

        [Range(0, 125)]
        public int MaxRegisters { get; set; } = 32;

        [Range(0, 125)]
        public int MaxUnused { get; set; } = 5;

        public bool UseSerialPort { get; set; }

        [RegularExpression("^(COM\\d+|\\/dev\\/tty\\w+|)$", ErrorMessage = "Illegal")]
        public string SerialPort { get; set; } = "";

        [RegularExpression("^(9600|19200|38400|57600|115200)$", ErrorMessage = "Illegal")]
        public int BaudRate { get; set; } = 9600;

        public Parity Parity { get; set; } = Parity.None;

        [Range(7, 8)]
        public int DataBits { get; set; } = 8;

        public StopBits StopBits { get; set; } = StopBits.One;

        public LoggingLevel LoggingLevel { get; set; }

        [Range(0, 65535)]
        public int StartAddress { get; set; }

        public static void Validate(string identity, object instance)
        {
            ValidationContext validationContext = new(instance);
            List<ValidationResult> validationResults = new();

            if (!Validator.TryValidateObject(instance, validationContext, validationResults, true))
            {
                foreach (ValidationResult error in validationResults)
                {
                    throw new ValidationException($"{identity} [{string.Join(",", error.MemberNames)}] {error.ErrorMessage}");
                }
            }
        }
    }
}
