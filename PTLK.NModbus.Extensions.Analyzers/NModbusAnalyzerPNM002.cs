using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PTLK.NModbus.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NModbusAnalyzerPNM002 : NModbusAnalyzerBase001
    {
        protected override string DiagnosticId => nameof(NModbusAnalyzerPNM002).Substring(nameof(NModbusAnalyzer).Length);

        protected override string Title => "FC over range";

        protected override string MessageFormat => "Cannot assign FC number without 3 or 4";

        protected override bool Illegal(SymbolAnalysisContext context, IPropertySymbol prop, AttributeData attr)
        {
            int fc = (int)GetNamedArgumentValue(attr, "FC", 3);
            return fc < 3 || fc > 4;
        }
    }
}
