using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PTLK.NModbus.Extensions.Analyzers
{
    public abstract class NModbusAnalyzerBase002 : NModbusAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            const string interfaceName = "IModbusDevice";
            const string attributeName = "ModbusDataItemAttribute";

            SemanticModel semanticModel = context.SemanticModel;
            if (context.Node is not ClassDeclarationSyntax classSyntax) return;

            INamedTypeSymbol? type = semanticModel.GetDeclaredSymbol(classSyntax);
            if (type == null) return;

            List<INamedTypeSymbol> types = new();
            while (ContainsInterface(type, interfaceName))
            {
                types.Add(type);
                type = type.BaseType;
                if (type == null) break;
            }
            if (types.Count == 0) return;

            List<(IPropertySymbol Prop, AttributeData Attr)> dataItems = types.SelectMany(c => c.GetMembers())
                .Where(c => c.Kind == SymbolKind.Property)
                .Select(c => ((IPropertySymbol)c, c.GetAttributes().FirstOrDefault(c => c.AttributeClass?.Name == attributeName)))
                .Where(c => c.Item2 != null)
                .OfType<(IPropertySymbol, AttributeData)>()
                .ToList();

            if (dataItems.Count > 0 && Illegal(context, dataItems, out Location? location))
            {
                Diagnostic diagnostic = Diagnostic.Create(Rule, location);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool ContainsInterface(ITypeSymbol symbol, string targetName)
        {
            return symbol.AllInterfaces.Any(c => c.Name == targetName);
        }

        protected abstract bool Illegal(SyntaxNodeAnalysisContext context, IEnumerable<(IPropertySymbol Prop, AttributeData Attr)> dataItems, out Location? location);
    }
}
