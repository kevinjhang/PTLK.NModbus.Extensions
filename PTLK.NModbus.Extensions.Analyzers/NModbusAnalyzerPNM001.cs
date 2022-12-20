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

        protected override string MessageFormat => "Cannot support data type without Boolean, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double and String when FC is 3 or 4. Only support Boolean when FC is 1 or 2";

        protected override bool Illegal(SymbolAnalysisContext context, IPropertySymbol prop, AttributeData attr)
        {
            int fc = (int)GetNamedArgumentValue(attr, "FC", 3);
            var allowTypes = GetAllowTypes(fc).Select(c => context.Compilation.GetSpecialType(c));
            ITypeSymbol type = GetUnderlyingType(prop.Type) ?? prop.Type;
            return !allowTypes.Contains(type, SymbolEqualityComparer.Default);
        }
    }
}
