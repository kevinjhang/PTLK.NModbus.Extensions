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
    public byte UnitId => 1;

    [ModbusDataItem(Address = 1)]
    public int DataItem { get; set; }

    [ModbusDataItem(Address = 3)]
    public int DataItem2 { get; set; }
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

    [ModbusDataItem(Address = 1)]
    public int DataItem { get; set; }

    [ModbusDataItem(Address = 2)]
    public int DataItem2 { get; set; }
}
";
            await Verify.VerifyAnalyzerAsync(test, 4, 1);
        }
    }
}