using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using KF.Logging.Analyzers;

namespace KF.Logging.Tests;

/// <summary>
/// Verifies analyzer diagnostics for <see cref="KF.Logging.LogEventSourceAttribute"/> scenarios.
/// </summary>
public sealed class LogEventSourceAnalyzerTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes analyzer tests with <see cref="ITestOutputHelper"/> for diagnostics.
    /// </summary>
    public LogEventSourceAnalyzerTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// Ensures duplicates, missing separators, and non-positive values are flagged.
    /// </summary>
    [Fact]
    public async Task FlagsDuplicateAndInvalidValues()
    {
        var source = """
using KF.Logging;

[LogEventSource]
public enum LogEventIds
{
    APP_Start = 0,
    APP_Restart = 0,
    APPNoSeparator = -1
}
""";

        var diagnostics = await AnalyzeAsync(source);
        var summary = string.Join(", ", diagnostics.Select(d => $"{d.Id}:{d.GetMessage()}"));
        _output.WriteLine(summary);

        Assert.Equal(2, diagnostics.Count(d => d.Id == "KLG0001"));
        Assert.Equal(3, diagnostics.Count(d => d.Id == "KLG0004"));
        Assert.Equal(1, diagnostics.Count(d => d.Id == "KLG0003"));
        Assert.Equal(1, diagnostics.Count(d => d.Id == "KLG0005"));
    }

    /// <summary>
    /// Ensures applying the attribute to a non-enum emits an error.
    /// </summary>
    [Fact]
    public async Task FlagsAttributeOnNonEnum()
    {
        var source = """
using KF.Logging;

[LogEventSource]
public class BadLogger
{
}
""";

        var diagnostics = await AnalyzeAsync(source);
        Assert.Contains(diagnostics, d => d.Id == "KLG0002");
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var references = CompilationReferenceHelper.CreateReferences(typeof(KF.Logging.LogEventSourceAttribute));
        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.NotNull(compilation.GetTypeByMetadataName("KF.Logging.LogEventSourceAttribute"));
        var compileErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(compileErrors.Length == 0, string.Join(" | ", compileErrors.Select(d => d.ToString())));

        var analyzer = new LogEventSourceAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
