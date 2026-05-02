using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace KoreForge.Logging.Tests;

internal static class CompilationReferenceHelper
{
    private static readonly string[] FrameworkAssemblies =
    {
        "System.Private.CoreLib.dll",
        "System.Runtime.dll",
        "System.Console.dll",
        "System.Linq.dll",
        "System.Collections.dll",
        "System.Runtime.Extensions.dll",
        "netstandard.dll"
    };

    public static IReadOnlyList<MetadataReference> CreateReferences(params Type[] additionalTypes)
    {
        var references = new List<MetadataReference>();
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        foreach (var name in FrameworkAssemblies)
        {
            var path = tpa.FirstOrDefault(p => p.EndsWith(name, StringComparison.OrdinalIgnoreCase));
            if (path is not null)
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        foreach (var type in additionalTypes)
        {
            references.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
        }

        return references;
    }
}
