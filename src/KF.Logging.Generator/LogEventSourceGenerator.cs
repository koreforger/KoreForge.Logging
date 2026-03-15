using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KF.Logging.Generator;

/// <summary>
/// Generates strongly-typed logging wrappers for enums annotated with <c>LogEventSourceAttribute</c>.
/// </summary>
[Generator]
public sealed class LogEventSourceGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enumModels = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "KF.Logging.LogEventSourceAttribute",
                static (node, _) => node is EnumDeclarationSyntax,
                static (attributeContext, _) => CreateModel(attributeContext))
            .Where(static model => model is not null)!;

        context.RegisterSourceOutput(enumModels, static (spc, model) =>
        {
            if (model is null)
            {
                return;
            }

            var source = SourceEmitter.Emit(model);
            spc.AddSource($"{model.EnumName}.Logging.g.cs", source);
        });
    }

    private static LogEventSourceModel? CreateModel(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol enumSymbol || enumSymbol.TypeKind != TypeKind.Enum)
        {
            return null;
        }

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute is null)
        {
            return null;
        }

        var namespaceOverride = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == "Namespace").Value.Value as string;
        var targetNamespace = string.IsNullOrWhiteSpace(namespaceOverride)
            ? (enumSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : enumSymbol.ContainingNamespace.ToDisplayString())
            : namespaceOverride!.Trim();

        var basePathValue = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == "BasePath").Value.Value as string;
        var basePath = string.IsNullOrWhiteSpace(basePathValue) ? null : basePathValue!.Trim();

        var rootLoggerNameValue = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == "LoggerRootTypeName").Value.Value as string;
        var rootLoggerName = string.IsNullOrWhiteSpace(rootLoggerNameValue)
            ? enumSymbol.Name + "Logger"
            : rootLoggerNameValue!.Trim();

        var members = enumSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(member => member.HasConstantValue)
            .Select(member => new EnumMemberModel(member.Name, Tokenize(member.Name)))
            .ToImmutableArray();

        if (members.Length == 0)
        {
            return null;
        }

        return new LogEventSourceModel(
            enumSymbol.Name,
            targetNamespace,
            enumSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            rootLoggerName,
            basePath,
            members);
    }

    private static ImmutableArray<string> Tokenize(string name)
    {
        var parts = name.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return ImmutableArray.Create(name);
        }

        var builder = ImmutableArray.CreateBuilder<string>(parts.Length);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                builder.Add(trimmed);
            }
        }

        return builder.Count == 0 ? ImmutableArray.Create(name) : builder.MoveToImmutable();
    }

    private sealed class EnumMemberModel
    {
        public EnumMemberModel(string name, ImmutableArray<string> tokens)
        {
            Name = name;
            Tokens = tokens;
        }

        public string Name { get; }
        public ImmutableArray<string> Tokens { get; }
    }

    private sealed class LogEventSourceModel
    {
        public LogEventSourceModel(
            string enumName,
            string @namespace,
            string fullyQualifiedEnumName,
            string rootLoggerName,
            string? basePath,
            ImmutableArray<EnumMemberModel> members)
        {
            EnumName = enumName;
            Namespace = @namespace;
            FullyQualifiedEnumName = fullyQualifiedEnumName;
            RootLoggerName = rootLoggerName;
            BasePath = basePath;
            Members = members;
        }

        public string EnumName { get; }
        public string Namespace { get; }
        public string FullyQualifiedEnumName { get; }
        public string RootLoggerName { get; }
        public string? BasePath { get; }
        public ImmutableArray<EnumMemberModel> Members { get; }
    }

    private static class SourceEmitter
    {
        public static string Emit(LogEventSourceModel model)
        {
            var areas = TreeBuilder.Build(model);
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("#nullable enable");
            builder.AppendLine("using System;");
            builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            builder.AppendLine("using Microsoft.Extensions.Logging;");
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(model.Namespace))
            {
                builder.AppendLine($"namespace {model.Namespace};");
                builder.AppendLine();
            }

            var indented = new IndentedStringBuilder(builder);
            WriteRootLogger(indented, model, areas);
            foreach (var area in areas)
            {
                indented.AppendLine();
                WriteAreaLogger(indented, model, area);
                foreach (var group in area.Groups)
                {
                    indented.AppendLine();
                    WriteGroupLogger(indented, model, group);
                }
            }

            indented.AppendLine();
            WriteServiceExtensions(indented, model, areas);

            return builder.ToString();
        }

        private static void WriteRootLogger(IndentedStringBuilder builder, LogEventSourceModel model, IReadOnlyList<AreaDescription> areas)
        {
            using (builder.Scope($"public sealed class {model.RootLoggerName}<T>"))
            {
                var parameterScope = new NameScope();
                var parameters = areas.Select(a => new
                {
                    Area = a,
                    Parameter = parameterScope.Allocate(NameHelper.ToCamelCase(a.DisplayName))
                }).ToArray();

                using (builder.Scope($"public {model.RootLoggerName}({string.Join(", ", parameters.Select(p => $"{p.Area.TypeName}<T> {p.Parameter}"))})"))
                {
                    foreach (var parameter in parameters)
                    {
                        builder.AppendLine($"{parameter.Area.DisplayName} = {parameter.Parameter} ?? throw new global::System.ArgumentNullException(nameof({parameter.Parameter}));");
                    }
                }

                builder.AppendLine();
                foreach (var area in areas)
                {
                    builder.AppendLine($"public {area.TypeName}<T> {area.DisplayName} {{ get; }}");
                }
            }
        }

        private static void WriteAreaLogger(IndentedStringBuilder builder, LogEventSourceModel model, AreaDescription area)
        {
            using (builder.Scope($"public sealed class {area.TypeName}<T>"))
            {
                builder.AppendLine("private readonly global::Microsoft.Extensions.Logging.ILogger<T> _inner;");
                builder.AppendLine();
                using (builder.Scope($"public {area.TypeName}(global::Microsoft.Extensions.Logging.ILogger<T> inner)"))
                {
                    builder.AppendLine("_inner = inner ?? throw new global::System.ArgumentNullException(nameof(inner));");
                    foreach (var evt in area.Events)
                    {
                        builder.AppendLine($"{evt.PropertyName} = new global::KF.Logging.Internal.EventLogger(_inner, (int){model.FullyQualifiedEnumName}.{evt.EnumMemberName}, \"{evt.EventPath}\");");
                    }
                    foreach (var group in area.Groups)
                    {
                        builder.AppendLine($"{group.PropertyName} = new {group.TypeName}<T>(_inner);");
                    }
                }

                builder.AppendLine();
                foreach (var evt in area.Events)
                {
                    builder.AppendLine($"public global::KF.Logging.IEventLogger {evt.PropertyName} {{ get; }}");
                }
                foreach (var group in area.Groups)
                {
                    builder.AppendLine($"public {group.TypeName}<T> {group.PropertyName} {{ get; }}");
                }
            }
        }

        private static void WriteGroupLogger(IndentedStringBuilder builder, LogEventSourceModel model, GroupDescription group)
        {
            using (builder.Scope($"public sealed class {group.TypeName}<T>"))
            {
                builder.AppendLine("private readonly global::Microsoft.Extensions.Logging.ILogger<T> _inner;");
                builder.AppendLine();
                using (builder.Scope($"internal {group.TypeName}(global::Microsoft.Extensions.Logging.ILogger<T> inner)"))
                {
                    builder.AppendLine("_inner = inner ?? throw new global::System.ArgumentNullException(nameof(inner));");
                    foreach (var evt in group.Events)
                    {
                        builder.AppendLine($"{evt.PropertyName} = new global::KF.Logging.Internal.EventLogger(_inner, (int){model.FullyQualifiedEnumName}.{evt.EnumMemberName}, \"{evt.EventPath}\");");
                    }
                    foreach (var child in group.Children)
                    {
                        builder.AppendLine($"{child.PropertyName} = new {child.TypeName}<T>(_inner);");
                    }
                }

                builder.AppendLine();
                foreach (var evt in group.Events)
                {
                    builder.AppendLine($"public global::KF.Logging.IEventLogger {evt.PropertyName} {{ get; }}");
                }
                foreach (var child in group.Children)
                {
                    builder.AppendLine($"public {child.TypeName}<T> {child.PropertyName} {{ get; }}");
                }
            }

            foreach (var child in group.Children)
            {
                builder.AppendLine();
                WriteGroupLogger(builder, model, child);
            }
        }

        private static void WriteServiceExtensions(IndentedStringBuilder builder, LogEventSourceModel model, IReadOnlyList<AreaDescription> areas)
        {
            using (builder.Scope("public static class GeneratedLoggingServiceCollectionExtensions"))
            {
                using (builder.Scope("public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddGeneratedLogging(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)"))
                {
                    builder.AppendLine("if (services is null)");
                    builder.AppendLine("{");
                    builder.IncrementIndent();
                    builder.AppendLine("throw new global::System.ArgumentNullException(nameof(services));");
                    builder.DecrementIndent();
                    builder.AppendLine("}");
                    builder.AppendLine();
                    foreach (var area in areas)
                    {
                        builder.AppendLine($"services.AddScoped(typeof({area.TypeName}<>));");
                    }
                    builder.AppendLine($"services.AddScoped(typeof({model.RootLoggerName}<>));");
                    builder.AppendLine();
                    builder.AppendLine("return services;");
                }
            }
        }
    }

    private static class TreeBuilder
    {
        public static IReadOnlyList<AreaDescription> Build(LogEventSourceModel model)
        {
            var map = new Dictionary<string, AreaDescription>(StringComparer.Ordinal);
            foreach (var member in model.Members)
            {
                var normalizedTokens = Normalize(member);
                var areaToken = normalizedTokens[0];
                var actionToken = normalizedTokens[normalizedTokens.Length - 1];
                var groupTokens = ExtractGroups(normalizedTokens);

                if (!map.TryGetValue(areaToken, out var area))
                {
                    area = new AreaDescription(model, areaToken);
                    map.Add(areaToken, area);
                }

                area.AddMember(member, groupTokens, actionToken);
            }

            var ordered = map.Values.OrderBy(a => a.DisplayName, StringComparer.Ordinal).ToList();
            foreach (var area in ordered)
            {
                area.FinalizeNodes();
            }

            return ordered;
        }

        private static string[] Normalize(EnumMemberModel member)
        {
            if (member.Tokens.Length >= 2)
            {
                return member.Tokens.ToArray();
            }

            if (member.Tokens.Length == 1)
            {
                var token = member.Tokens[0];
                return new[] { token, token };
            }

            return new[] { member.Name, member.Name };
        }

        private static string[] ExtractGroups(string[] tokens)
        {
            if (tokens.Length <= 2)
            {
                return Array.Empty<string>();
            }

            var length = tokens.Length - 2;
            var result = new string[length];
            Array.Copy(tokens, 1, result, 0, length);
            return result;
        }
    }

    private sealed class AreaDescription
    {
        private readonly LogEventSourceModel _model;
        private readonly NameScope _eventScope = new();
        private readonly NameScope _groupScope = new();
        private readonly Dictionary<string, GroupDescription> _groupMap = new(StringComparer.Ordinal);
        private readonly List<GroupDescription> _groups = new();

        public AreaDescription(LogEventSourceModel model, string token)
        {
            _model = model;
            DisplayName = NameHelper.ToIdentifier(token);
            TypeName = DisplayName + "Logger";
            PathSegments = ImmutableArray.Create(DisplayName);
            ClassSegments = ImmutableArray.Create(DisplayName);
        }

        public string DisplayName { get; }
        public string TypeName { get; }
        public ImmutableArray<string> PathSegments { get; }
        public ImmutableArray<string> ClassSegments { get; }
        public List<EventDescription> Events { get; } = new();
        public IReadOnlyList<GroupDescription> Groups => _groups;

        public void AddMember(EnumMemberModel member, string[] groupTokens, string actionToken)
        {
            AddMember(member, groupTokens, 0, actionToken);
        }

        private void AddMember(EnumMemberModel member, string[] groupTokens, int index, string actionToken)
        {
            if (groupTokens.Length == 0 || index >= groupTokens.Length)
            {
                Events.Add(EventDescription.Create(_model, member, PathSegments, actionToken, _eventScope));
                return;
            }

            var next = GetOrAddGroup(groupTokens[index]);
            next.AddMember(member, groupTokens, index + 1, actionToken);
        }

        public string AllocateGroupName(string baseName) => _groupScope.Allocate(baseName);

        private GroupDescription GetOrAddGroup(string token)
        {
            if (_groupMap.TryGetValue(token, out var existing))
            {
                return existing;
            }

            var created = new GroupDescription(_model, this, null, token);
            _groupMap.Add(token, created);
            _groups.Add(created);
            return created;
        }

        public void FinalizeNodes()
        {
            Events.Sort(static (a, b) => string.CompareOrdinal(a.PropertyName, b.PropertyName));
            _groups.Sort(static (a, b) => string.CompareOrdinal(a.PropertyName, b.PropertyName));
            foreach (var group in _groups)
            {
                group.FinalizeNodes();
            }
        }
    }

    private sealed class GroupDescription
    {
        private readonly LogEventSourceModel _model;
        private readonly AreaDescription _area;
        private readonly NameScope _eventScope = new();
        private readonly NameScope _groupScope = new();
        private readonly Dictionary<string, GroupDescription> _childrenMap = new(StringComparer.Ordinal);
        private readonly List<GroupDescription> _children = new();

        public GroupDescription(LogEventSourceModel model, AreaDescription area, GroupDescription? parent, string token)
        {
            _model = model;
            _area = area;
            PascalName = NameHelper.ToIdentifier(token);
            PropertyName = parent is null
                ? area.AllocateGroupName(PascalName)
                : parent.AllocateChildGroupName(PascalName);
            ClassSegments = (parent?.ClassSegments ?? area.ClassSegments).Add(PascalName);
            TypeName = string.Concat(ClassSegments) + "Logger";
            PathSegments = ClassSegments;
        }

        public string PascalName { get; }
        public string PropertyName { get; }
        public string TypeName { get; }
        public ImmutableArray<string> ClassSegments { get; }
        public ImmutableArray<string> PathSegments { get; }
        public List<EventDescription> Events { get; } = new();
        public IReadOnlyList<GroupDescription> Children => _children;

        public void AddMember(EnumMemberModel member, string[] remainingGroups, int index, string actionToken)
        {
            if (remainingGroups.Length == 0 || index >= remainingGroups.Length)
            {
                Events.Add(EventDescription.Create(_model, member, PathSegments, actionToken, _eventScope));
                return;
            }

            var child = GetOrAddChild(remainingGroups[index]);
            child.AddMember(member, remainingGroups, index + 1, actionToken);
        }

        private GroupDescription GetOrAddChild(string token)
        {
            if (_childrenMap.TryGetValue(token, out var existing))
            {
                return existing;
            }

            var created = new GroupDescription(_model, _area, this, token);
            _childrenMap.Add(token, created);
            _children.Add(created);
            return created;
        }

        private string AllocateChildGroupName(string baseName) => _groupScope.Allocate(baseName);

        public void FinalizeNodes()
        {
            Events.Sort(static (a, b) => string.CompareOrdinal(a.PropertyName, b.PropertyName));
            _children.Sort(static (a, b) => string.CompareOrdinal(a.PropertyName, b.PropertyName));
            foreach (var child in _children)
            {
                child.FinalizeNodes();
            }
        }
    }

    private sealed class EventDescription
    {
        private EventDescription(string propertyName, string enumMemberName, string eventPath)
        {
            PropertyName = propertyName;
            EnumMemberName = enumMemberName;
            EventPath = eventPath;
        }

        public string PropertyName { get; }
        public string EnumMemberName { get; }
        public string EventPath { get; }

        public static EventDescription Create(LogEventSourceModel model, EnumMemberModel member, ImmutableArray<string> prefixSegments, string actionToken, NameScope scope)
        {
            var actionName = NameHelper.ToIdentifier(actionToken);
            var propertyName = scope.Allocate(actionName);
            var segments = prefixSegments.Add(actionName);
            var path = BuildEventPath(model.BasePath, segments);
            return new EventDescription(propertyName, member.Name, path);
        }
    }

        private static string BuildEventPath(string? basePath, ImmutableArray<string> segments)
        {
            var joined = string.Join(".", segments);
            return string.IsNullOrEmpty(basePath) ? joined : string.Concat(basePath, ".", joined);
        }

    private sealed class NameScope
    {
        private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

        public string Allocate(string baseName)
        {
            var sanitized = string.IsNullOrWhiteSpace(baseName) ? "Value" : baseName;
            if (!_counts.TryGetValue(sanitized, out var current))
            {
                _counts[sanitized] = 1;
                return sanitized;
            }

            current++;
            _counts[sanitized] = current;
            return sanitized + current.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static class NameHelper
    {
        public static string ToIdentifier(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "Value";
            }

            var builder = new StringBuilder(token.Length);
            var uppercase = true;
            foreach (var ch in token)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(uppercase ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch));
                    uppercase = false;
                }
                else
                {
                    uppercase = true;
                }
            }

            if (builder.Length == 0)
            {
                builder.Append('N');
            }

            var candidate = builder.ToString();
            if (!SyntaxFacts.IsIdentifierStartCharacter(candidate[0]))
            {
                candidate = "N" + candidate;
            }

            if (SyntaxFacts.GetKeywordKind(candidate) != SyntaxKind.None)
            {
                candidate += "_";
            }

            return candidate;
        }

        public static string ToCamelCase(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return "value";
            }

            return identifier.Length == 1
                ? identifier.ToLowerInvariant()
                : char.ToLowerInvariant(identifier[0]) + identifier.Substring(1);
        }
    }

    private sealed class IndentedStringBuilder
    {
        private readonly StringBuilder _inner;
        private int _indentLevel;
        private const string IndentText = "    ";

        public IndentedStringBuilder(StringBuilder inner) => _inner = inner;

        public IDisposable Scope(string header)
        {
            AppendLine(header);
            AppendLine("{");
            _indentLevel++;
            return new ScopeToken(this);
        }

        public void AppendLine(string text = "")
        {
            if (text.Length == 0)
            {
                _inner.AppendLine();
                return;
            }

            for (var i = 0; i < _indentLevel; i++)
            {
                _inner.Append(IndentText);
            }

            _inner.AppendLine(text);
        }

        public void IncrementIndent() => _indentLevel++;
        public void DecrementIndent() => _indentLevel = Math.Max(0, _indentLevel - 1);

        private sealed class ScopeToken : IDisposable
        {
            private readonly IndentedStringBuilder _builder;
            private bool _disposed;

            public ScopeToken(IndentedStringBuilder builder) => _builder = builder;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _builder._indentLevel = Math.Max(0, _builder._indentLevel - 1);
                _builder.AppendLine("}");
                _disposed = true;
            }
        }
    }
}
