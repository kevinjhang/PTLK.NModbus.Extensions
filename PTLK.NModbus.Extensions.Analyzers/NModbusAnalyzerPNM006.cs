using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace PTLK.NModbus.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NModbusAnalyzerPNM006 : NModbusAnalyzerBase002
    {
        protected override string DiagnosticId => nameof(NModbusAnalyzerPNM006).Substring(nameof(NModbusAnalyzer).Length);

        protected override string Title => "Address assignment error";

        protected override string MessageFormat => "Cannot assign same address renge both dataItems";

        protected override bool Illegal(SyntaxNodeAnalysisContext context, IEnumerable<(int Depth, IPropertySymbol Prop, AttributeData Attr)> dataItems, out Location? location)
        {
            IEnumerable<IGrouping<object, (int Depth, IPropertySymbol Prop, AttributeData Attr)>> groups =
                dataItems.GroupBy(c => GetNamedArgumentValue(c.Attr, "FC", 3));

            foreach (var group in groups)
            {
                var orderedGroup = group.OrderBy(c => GetNamedArgumentValue(c.Attr, "Address", 0)).ThenByDescending(c => c.Depth).Select(c => (c.Prop, c.Attr));

                int checkPoint = -1;
                foreach (var (prop, attr) in orderedGroup)
                {
                    int addrStart = (int)GetNamedArgumentValue(attr, "Address", 0);

                    if (addrStart <= checkPoint)
                    {
                        location = prop.Locations.First();
                        return true;
                    }

                    int length = (int)GetNamedArgumentValue(attr, "Length", 0);
                    ITypeSymbol type = GetUnderlyingType(prop.Type) ?? prop.Type;
                    int addrEnd = addrStart + GetWordLengthOfType(type.SpecialType, length) - 1;
                    checkPoint = addrEnd;
                }
            }

            location = null;
            return false;
        }
    }
}
