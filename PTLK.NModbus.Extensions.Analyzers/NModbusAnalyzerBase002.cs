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

            List<(int Depth, INamedTypeSymbol Type)> types = new();
            int depth = 0;
            while (ContainsInterface(type, interfaceName))
            {
                types.Add((depth++, type));
                type = type.BaseType;
                if (type == null) break;
            }
            if (types.Count == 0) return;

            List<(int Weights, IPropertySymbol Prop, AttributeData Attr)> dataItems = types.SelectMany(c => c.Type.GetMembers().Select(prop => (c.Depth, Prop: prop)))
                .Where(c => c.Prop.Kind == SymbolKind.Property)
                .Select(c => (c.Depth, Prop: (IPropertySymbol)c.Prop, Attr: c.Prop.GetAttributes().FirstOrDefault(c => c.AttributeClass?.Name == attributeName)))
                .Where(c => c.Attr != null)
                .OfType<(int, IPropertySymbol, AttributeData)>()
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

        protected abstract bool Illegal(SyntaxNodeAnalysisContext context, IEnumerable<(int Depth, IPropertySymbol Prop, AttributeData Attr)> dataItems, out Location? location);
    }
}
