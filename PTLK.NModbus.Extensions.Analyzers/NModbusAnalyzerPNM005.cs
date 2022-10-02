using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PTLK.NModbus.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NModbusAnalyzerPNM005 : NModbusAnalyzerBase001
    {
        protected override string DiagnosticId => nameof(NModbusAnalyzerPNM005).Substring(nameof(NModbusAnalyzer).Length);

        protected override string Title => "Scale with zero";

        protected override string MessageFormat => "Cannot assign scale number with zero";

        protected override bool Illegal(SymbolAnalysisContext context, IPropertySymbol prop, AttributeData attr)
        {
            double scale = (double)GetNamedArgumentValue(attr, "Scale", 1.0);
            return scale == 0;
        }
    }
}
