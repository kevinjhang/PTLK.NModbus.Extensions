using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PTLK.NModbus.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NModbusAnalyzerPNM003 : NModbusAnalyzerBase001
    {
        protected override string DiagnosticId => nameof(NModbusAnalyzerPNM003).Substring(nameof(NModbusAnalyzer).Length);

        protected override string Title => "Address over range";

        protected override string MessageFormat => "Cannot assign address range not in between 0 and 65535";

        protected override bool Illegal(SymbolAnalysisContext context, IPropertySymbol prop, AttributeData attr)
        {
            int fc = (int)GetNamedArgumentValue(attr, "Address", 0);
            int length = (int)GetNamedArgumentValue(attr, "Length", 0);
            ITypeSymbol type = GetUnderlyingType(prop.Type) ?? prop.Type;
            return fc < 0 || fc + GetWordLengthOfType(type.SpecialType, length) - 1 > 65535;
        }
    }
}
