using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace PTLK.NModbus.Extensions.Analyzers
{
    public abstract class NModbusAnalyzerBase001 : NModbusAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNode, SymbolKind.Property);
        }

        private void AnalyzeNode(SymbolAnalysisContext context)
        {
            const string attributeName = "ModbusDataItemAttribute";

            if (context.Symbol is not IPropertySymbol prop) return;

            var attr = prop.GetAttributes().FirstOrDefault(c => c.AttributeClass?.Name == attributeName);

            if (attr != null && Illegal(context, prop, attr))
            {
                Location location = context.Symbol.Locations.First();
                Diagnostic diagnostic = Diagnostic.Create(Rule, location);
                context.ReportDiagnostic(diagnostic);
            }
        }

        protected abstract bool Illegal(SymbolAnalysisContext context, IPropertySymbol prop, AttributeData attr);
    }
}
