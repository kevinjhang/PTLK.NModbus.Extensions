using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace PTLK.NModbus.Extensions.Analyzers
{
    public abstract class NModbusAnalyzer : DiagnosticAnalyzer
    {
        protected abstract string DiagnosticId { get; }
        protected abstract string Title { get; }
        protected abstract string MessageFormat { get; }

        protected DiagnosticDescriptor Rule => new
        (
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        protected static object GetNamedArgumentValue(AttributeData attr, string argName, object defaultValue)
        {
            return attr.NamedArguments.Any(c => c.Key == argName) ? attr.NamedArguments.First(c => c.Key == argName).Value.Value ?? defaultValue : defaultValue;
        }

        protected static ITypeSymbol? GetUnderlyingType(ITypeSymbol type)
        {
            if (type.Name == "Nullable" && type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            {
                return namedType.TypeArguments[0];
            }

            return null;
        }

        protected static int GetWordLengthOfType(SpecialType type, int bytesLength)
        {
            if (bytesLength % 2 != 0)
            {
                bytesLength++;
            }

            return type switch
            {
                SpecialType.System_Boolean => 1,
                SpecialType.System_Int16 => 1,
                SpecialType.System_UInt16 => 1,
                SpecialType.System_Int32 => 2,
                SpecialType.System_UInt32 => 2,
                SpecialType.System_Int64 => 4,
                SpecialType.System_UInt64 => 4,
                SpecialType.System_Single => 2,
                SpecialType.System_Double => 4,
                SpecialType.System_String => bytesLength / 2,
                _ => 1
            };
        }

        protected static SpecialType[] GetAllowTypes(int fc)
        {
            return fc switch
            {
                1 or 2 => new[] {
                    SpecialType.System_Boolean
                },
                3 or 4 => new[] {
                    SpecialType.System_Boolean,
                    SpecialType.System_Int16,
                    SpecialType.System_UInt16,
                    SpecialType.System_Int32,
                    SpecialType.System_UInt32,
                    SpecialType.System_Int64,
                    SpecialType.System_UInt64,
                    SpecialType.System_Single,
                    SpecialType.System_Double,
                    SpecialType.System_String
                },
                _ => new SpecialType[0]
            };
        }
    }
}
