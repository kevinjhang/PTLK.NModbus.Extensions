using NModbus;
using System;

namespace PTLK.NModbus.Extensions
{
    class ModbusClientLogger : IModbusLogger
    {
        public event Action<LoggingLevel, DateTime, string>? LogChanged;

        public LoggingLevel LoggingLevel { get; set; }

        public void Log(LoggingLevel level, string message)
        {
            if (level != LoggingLevel) return;

            LogChanged?.Invoke(level, DateTime.Now, message);
        }

        public bool ShouldLog(LoggingLevel level)
        {
            return level == LoggingLevel;
        }
    }
}
