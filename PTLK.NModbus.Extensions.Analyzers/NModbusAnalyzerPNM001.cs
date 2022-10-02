using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace PTLK.NModbus.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NModbusAnalyzerPNM001 : NModbusAnalyzerBase001
    {
        protected override string DiagnosticId => nameof(NModbusAnalyzerPNM001).Substring(nameof(NModbusAnalyzer).Length);

        protected override string Title => "Data type not support";

        protected override string MessageFormat => "Cannot support data type without Boolean, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double and String";

        protected override bool Illegal(SymbolAnalysisContext context, IPropertySymbol prop, AttributeData attr)
        {
            var allowTypes = AllowTypes.Select(c => context.Compilation.GetSpecialType(c));
            ITypeSymbol type = GetUnderlyingType(prop.Type) ?? prop.Type;
            return !allowTypes.Contains(type, SymbolEqualityComparer.Default);
        }
    }
}
