using Verify = PTLK.NModbus.Extensions.Analyzers.Tests.CSharpAnalyzerVerifier<
    PTLK.NModbus.Extensions.Analyzers.NModbusAnalyzerPNM004>;

namespace PTLK.NModbus.Extensions.Analyzers.Tests
{
    [TestClass]
    public class NModbusAnalyzerPNM004UnitTest
    {
        [TestMethod]
        public async Task NoDiagnostic()
        {
            string test = @"
using PTLK.NModbus.Extensions;

class ModbusDevice : IModbusDevice
{
    [ModbusDataItem(Length = 3)]
    public uint DataItem { get; set; }
}
";
            await Verify.VerifyAnalyzerAsync(test);
        }

    [TestMethod]
        public async Task Diagnostic()
        {
            string test = @"
using PTLK.NModbus.Extensions;

class ModbusDevice : IModbusDevice
{
    [ModbusDataItem(Length = 333)]
    public uint DataItem { get; set; }
}
";
            await Verify.VerifyAnalyzerAsync(test, 7, 17);
        }
    }
}