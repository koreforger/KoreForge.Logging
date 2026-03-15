using System.Collections.Immutable;
using KF.Logging.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KF.Logging.Tests;

/// <summary>
/// Validates code emitted by <see cref="KF.Logging.Generator.LogEventSourceGenerator"/>.
/// </summary>
public sealed class LogEventSourceGeneratorTests
{
    /// <summary>
    /// Ensures the generator creates root, area, and group loggers.
    /// </summary>
    [Fact]
    public void GeneratesExpectedTypes()
    {
        var source = """
using KF.Logging;

namespace MyApp.Logging;

[LogEventSource(LoggerRootTypeName = "MyLogger", BasePath = "MyApp")]
public enum SampleEvents
{
    APP_Startup = 1000,
    DB_Connection_Open = 2000,
    DB_Connection_Close = 2001
}
""";

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var references = CompilationReferenceHelper.CreateReferences(
            typeof(LogEventSourceAttribute),
            typeof(ILogger),
            typeof(ServiceCollection));

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: ImmutableArray.Create(syntaxTree),
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new LogEventSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).WithUpdatedParseOptions(parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(outputCompilation.GetTypeByMetadataName("MyApp.Logging.MyLogger`1"));
        Assert.NotNull(outputCompilation.GetTypeByMetadataName("MyApp.Logging.AppLogger`1"));
        Assert.NotNull(outputCompilation.GetTypeByMetadataName("MyApp.Logging.DbConnectionLogger`1"));
    }
}
