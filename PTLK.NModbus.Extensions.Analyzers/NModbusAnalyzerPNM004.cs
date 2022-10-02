using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PTLK.NModbus.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NModbusAnalyzerPNM004 : NModbusAnalyzerBase001
    {
        protected override string DiagnosticId => nameof(NModbusAnalyzerPNM004).Substring(nameof(NModbusAnalyzer).Length);

        protected override string Title => "Length over range";

        protected override string MessageFormat => "Cannot assign length number not in between 1 and 254";

        protected override bool Illegal(SymbolAnalysisContext context, IPropertySymbol prop, AttributeData attr)
        {
            int fc = (int)GetNamedArgumentValue(attr, "Length", 1);
            return fc < 1 || fc > 254;
        }
    }
}
