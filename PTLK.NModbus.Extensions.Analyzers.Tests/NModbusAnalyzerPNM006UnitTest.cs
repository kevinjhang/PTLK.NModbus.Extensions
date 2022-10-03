using Verify = PTLK.NModbus.Extensions.Analyzers.Tests.CSharpAnalyzerVerifier<
    PTLK.NModbus.Extensions.Analyzers.NModbusAnalyzerPNM006>;

namespace PTLK.NModbus.Extensions.Analyzers.Tests
{
    [TestClass]
    public class NModbusAnalyzerPNM006UnitTest
    {
        [TestMethod]
        public async Task NoDiagnostic()
        {
            string test = @"
using PTLK.NModbus.Extensions;

class ModbusDevice : IModbusDevice
{
    [ModbusDataItem(Address = 1)]
    public uint DataItem { get; set; }

    [ModbusDataItem(Address = 3)]
    public uint DataItem2 { get; set; }
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
    [ModbusDataItem(Address = 1)]
    public uint DataItem { get; set; }

    [ModbusDataItem(Address = 2)]
    public uint DataItem2 { get; set; }
}
";
            await Verify.VerifyAnalyzerAsync(test, 10, 17);
        }
    }
}