# AutoMapper Generator

Generates property-to-property mapping implementations for partial methods at compile time. Decorated methods automatically map matching properties from the source parameter type to the return type, eliminating manual mapping boilerplate and runtime reflection.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Mapping between entities, DTOs and view models is repetitive code that breaks silently when properties are renamed. Runtime mapping libraries hide the mapping behind reflection and report errors only at runtime. This generator writes the mapping method at compile time and reports unmapped or mismatched properties as diagnostics during the build.

## Benefits

- Generates property-to-property mapping code at compile time with zero runtime overhead
- Replaces manual mapping boilerplate and runtime reflection-based mapping libraries
- Automatically matches properties by name and validates type compatibility at compile time
- Supports custom property name mappings via `[PropertyMapping]` attribute
- Compile-time diagnostics for unmapped and type-mismatched properties
- Handles nullable source parameters with automatic null guards

## Attributes

```csharp
[GenerateAutoMapper]
[PropertyMapping(string sourceProperty, string targetProperty)]
```

**Parameters:**

- `GenerateAutoMapper` - Marks a partial method for automatic mapping code generation. The method must have exactly one parameter (source) and a non-void return type (target).
- `PropertyMapping` - Specifies a custom property name mapping between source and target types. Can be applied multiple times for multiple custom mappings.

**Constraints:**

- The method must be `partial` and defined in a `partial class`
- The method must have exactly one parameter (the source type)
- The method must have a non-void return type (the target type)
- Unmapped target properties (no matching source property) produce a compile-time warning
- Type-mismatched properties (incompatible types) produce a compile-time error

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

public class UserEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
}

public static partial class UserMapper
{
    [GenerateAutoMapper]
    [PropertyMapping("Name", "DisplayName")]
    public static partial UserDto ToDto(UserEntity entity);
}
```

### Generated Code

The generator creates a mapping implementation that:

- Maps all properties with matching names and compatible types automatically
- Applies custom property mappings specified via `[PropertyMapping]`
- Handles nullable source parameters with `ArgumentNullException` guards
- Uses null-forgiving operators where needed (nullable source to non-nullable target properties)
- Reports compile-time diagnostics for unmapped or type-mismatched properties

### Usage Example

```csharp
var entity = new UserEntity
{
    Id = 1,
    Name = "John Doe",
    Email = "john@example.com",
    CreatedAt = DateTime.UtcNow
};

// Generated method maps properties automatically
UserDto dto = UserMapper.ToDto(entity);

Console.WriteLine(dto.Id);          // 1
Console.WriteLine(dto.Name);        // "John Doe"
Console.WriteLine(dto.Email);       // "john@example.com"
Console.WriteLine(dto.DisplayName); // "John Doe" (mapped from Name)

// Works with instance methods too
public partial class OrderMapper
{
    [GenerateAutoMapper]
    public partial OrderDto MapOrder(OrderEntity order);
}

// Nullable source parameter generates null check
public static partial class SafeMapper
{
    [GenerateAutoMapper]
    public static partial UserDto ToDto(UserEntity? entity);
    // Throws ArgumentNullException if entity is null
}
```

## Diagnostics

| ID           | Severity | Description                                  |
| ------------ | -------- | -------------------------------------------- |
| `BB84SG0007` | Warning  | Unmapped target property in AutoMapper       |
| `BB84SG0008` | Error    | Type mismatch in AutoMapper property mapping |
| `BB84SG0009` | Error    | GenerateAutoMapper requires a partial class  |
| `BB84SG0010` | Error    | GenerateAutoMapper requires a partial method |
