using Microsoft.CodeAnalysis;

namespace KF.Logging.Analyzers;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor DuplicateValue = new(
        id: "KLG0001",
        title: "Duplicate log event value",
        messageFormat: "Duplicate log event value '{0}' in enum '{1}'",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AttributeOnNonEnum = new(
        id: "KLG0002",
        title: "[LogEventSource] is only valid on enums",
        messageFormat: "[LogEventSource] can only be applied to enums",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingSeparator = new(
        id: "KLG0003",
        title: "Enum member name should contain an underscore",
        messageFormat: "Enum member '{0}' should follow the 'AREA_Action' naming convention with at least one '_'",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonPositiveValue = new(
        id: "KLG0004",
        title: "Event identifiers should be positive",
        messageFormat: "Log event '{0}' has non-positive value '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SingleMemberArea = new(
        id: "KLG0005",
        title: "Area token used only once",
        messageFormat: "Area '{0}' is used only by event '{1}'",
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
