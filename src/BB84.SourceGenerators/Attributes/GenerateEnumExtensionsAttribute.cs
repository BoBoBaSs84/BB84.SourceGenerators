namespace BB84.SourceGenerators.Attributes;

/// <summary>
/// Indicates that the decorated enumeration should have extension methods generated.
/// </summary>
[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public sealed class GenerateEnumExtensionsAttribute : Attribute
{ }
