using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KoreForge.Logging.Analyzers;

/// <summary>
/// Validates enums annotated with <c>LogEventSourceAttribute</c> to ensure best practices.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LogEventSourceAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.DuplicateValue,
            DiagnosticDescriptors.AttributeOnNonEnum,
            DiagnosticDescriptors.MissingSeparator,
            DiagnosticDescriptors.NonPositiveValue,
            DiagnosticDescriptors.SingleMemberArea);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            const string attributeMetadataName = "KoreForge.Logging.LogEventSourceAttribute";
            const string attributeDisplayName = "global::KoreForge.Logging.LogEventSourceAttribute";
            var attributeSymbol = compilationContext.Compilation.GetTypeByMetadataName(attributeMetadataName);
            if (attributeSymbol is null)
            {
                // Continue even if the symbol lookup fails so metadata-name fallback can apply.
            }

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                AnalyzeSymbol(symbolContext, attributeSymbol, attributeDisplayName);
            }, SymbolKind.NamedType);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol? attributeSymbol, string attributeDisplayName)
    {
        if (context.Symbol is not INamedTypeSymbol namedType)
        {
            return;
        }

        var hasAttribute = namedType.GetAttributes().Any(a =>
            (attributeSymbol is not null && SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol)) ||
            string.Equals(a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), attributeDisplayName, StringComparison.Ordinal));
        if (!hasAttribute)
        {
            return;
        }

        if (namedType.TypeKind != TypeKind.Enum)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.AttributeOnNonEnum, namedType.Locations.FirstOrDefault()));
            return;
        }

        AnalyzeEnum(context, namedType);
    }

    private static void AnalyzeEnum(SymbolAnalysisContext context, INamedTypeSymbol enumSymbol)
    {
        var members = enumSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .ToList();

        var valueUsage = new Dictionary<long, List<IFieldSymbol>>();
        var areaUsage = new Dictionary<string, List<IFieldSymbol>>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in members)
        {
            var value = Convert.ToInt64(member.ConstantValue, CultureInfo.InvariantCulture);
            if (!valueUsage.TryGetValue(value, out var bucket))
            {
                bucket = new List<IFieldSymbol>();
                valueUsage[value] = bucket;
            }
            bucket.Add(member);

            if (member.Name.IndexOf('_') < 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingSeparator, member.Locations.FirstOrDefault(), member.Name));
            }

            if (value <= 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NonPositiveValue, member.Locations.FirstOrDefault(), member.Name, value));
            }

            var areaToken = ExtractArea(member.Name);
            if (!areaUsage.TryGetValue(areaToken, out var areaBucket))
            {
                areaBucket = new List<IFieldSymbol>();
                areaUsage[areaToken] = areaBucket;
            }
            areaBucket.Add(member);
        }

        foreach (var pair in valueUsage.Where(p => p.Value.Count > 1))
        {
            foreach (var member in pair.Value)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicateValue, member.Locations.FirstOrDefault(), pair.Key, enumSymbol.Name));
            }
        }

        foreach (var pair in areaUsage)
        {
            if (pair.Value.Count == 1)
            {
                var member = pair.Value[0];
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SingleMemberArea, member.Locations.FirstOrDefault(), pair.Key, member.Name));
            }
        }
    }

    private static string ExtractArea(string name)
    {
        var separator = name.IndexOf('_');
        return separator < 0 ? name : name.Substring(0, separator);
    }
}
