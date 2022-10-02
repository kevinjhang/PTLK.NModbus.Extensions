using Verify = PTLK.NModbus.Extensions.Analyzers.Tests.CSharpAnalyzerVerifier<
    PTLK.NModbus.Extensions.Analyzers.NModbusAnalyzerPNM005>;

namespace PTLK.NModbus.Extensions.Analyzers.Tests
{
    [TestClass]
    public class NModbusAnalyzerPNM005UnitTest
    {
        [TestMethod]
        public async Task NoDiagnostic()
        {
            string test = @"
using PTLK.NModbus.Extensions;

class ModbusDevice : IModbusDevice
{
    public byte UnitId => 1;

    [ModbusDataItem(Scale = 0.001)]
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
    public byte UnitId => 1;

    [ModbusDataItem(Scale = 0)]
    public uint DataItem { get; set; }
}
";
            await Verify.VerifyAnalyzerAsync(test, 9, 17);
        }
    }
}