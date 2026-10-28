# Notification Properties Generator

Automatically generates properties with `INotifyPropertyChanged` and/or `INotifyPropertyChanging` support for private fields in a class. Both interfaces are independently configurable.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

Implementing `INotifyPropertyChanged` by hand means writing the same backing-field, equality-check and raise-event code for every property of every view model. The code is repetitive, easy to get wrong (forgotten notifications, typos in property names) and hard to keep in sync when dependent properties change. IL-weaving tools solve this but add build-time magic. This generator writes the properties for you from private fields or partial properties, as normal C# you can read and debug.

## Benefits

- Generates optimized property setters with inline equality checks
- Avoids reflection overhead of PropertyChanged.Fody or similar tools
- Compile-time code generation means zero runtime overhead
- Able to generate additional notifications without runtime attribute scanning
- Also provides fine-grained control over generated properties

## Attributes

```csharp
[GenerateNotifications(bool propertyChanged = true, bool propertyChanging = true, bool hasChanged = false)]
[AlsoNotify(params string[] propertyNames)]
[ExcludeFromNotification]
[NotifyCanExecuteChanged(params string[] commandNames)]
```

**Parameters:**

- `propertyChanged` - When `true` (default), implements `INotifyPropertyChanged` and generates `PropertyChanged` event and `RaisePropertyChanged()` method
- `propertyChanging` - When `true` (default), implements `INotifyPropertyChanging` and generates `PropertyChanging` event and `RaisePropertyChanging()` method
- `hasChanged` - When `true`, generates an additional `HasChanged` boolean property that is set to `true` when any property changes

**Field-Level Attributes:**

- `[AlsoNotify(...)]` - Specifies additional property names that should raise change notifications when the decorated field or partial property changes. Works for both `PropertyChanged` and `PropertyChanging` events.
- `[ExcludeFromNotification]` - Excludes the decorated field or partial property from notification generation. A field will not have a corresponding public property generated; a partial property is implemented as a plain property without change notification.
- `[NotifyCanExecuteChanged(...)]` - Calls `RaiseCanExecuteChanged()` on the named commands (see [ActionCommand Generator](action-command.md)) whenever the decorated field or partial property changes. Unknown names are reported as warning `BB84SG0024`.

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateNotifications(hasChanged: true)]
public partial class Person
{
    private int _id;

    [AlsoNotify(nameof(FullName))]
    private string _firstName;

    [AlsoNotify(nameof(FullName))]
    private string _lastName;

    private string _fullName;

    [ExcludeFromNotification]
    private string _internalToken;

    private DateTime _createdAt;
    private DateTime? _updatedAt;

    public Person(int id, string firstName, string lastName)
    {
        _id = id;
        _firstName = firstName;
        _lastName = lastName;
        _createdAt = DateTime.UtcNow;
    }

		public string FullName => $"{_firstName} {_lastName}";
}
```

### Generated Code

The generator creates:

- Public properties for each private field with change notification (unless excluded with `[ExcludeFromNotification]`)
- Implementation of `INotifyPropertyChanged` and/or `INotifyPropertyChanging` interfaces (configurable)
- `PropertyChanged` and/or `PropertyChanging` events
- `RaisePropertyChanged()` and/or `RaisePropertyChanging()` methods (`protected virtual` for non-sealed classes, `private` for sealed classes)
- Additional change notifications for properties specified via `[AlsoNotify]`
- Optional `HasChanged` property (when `hasChanged` parameter is `true`)
- Correct accessibility modifier matching the user's class declaration

### Usage Example

```csharp
var person = new Person(1, "John", "Doe");

// Subscribe to change notifications
person.PropertyChanging += (s, e) => Console.WriteLine($"Property {e.PropertyName} is changing");
person.PropertyChanged += (s, e) => Console.WriteLine($"Property {e.PropertyName} has changed");

// Change FirstName - fires notifications for both FirstName and FullName
person.FirstName = "Jane";
// Output:
// Property FirstName is changing
// Property FullName is changing
// Property FirstName has changed
// Property FullName has changed

// Check if object has been modified (when hasChanged: true)
if (person.HasChanged)
{
    Console.WriteLine("Person has been modified");
}

// Fields with [ExcludeFromNotification] have no generated property
// person.InternalToken  <-- does not exist

// Only INotifyPropertyChanged (no PropertyChanging events)
[GenerateNotifications(propertyChanging: false)]
public partial class LightweightModel
{
    private string _name;
}

// Works correctly with sealed classes
[GenerateNotifications]
public sealed partial class SealedModel
{
    private int _value;
}
```

### Partial Property Support

Besides private fields, `partial` properties (C# 13 or later) declared in the decorated type are implemented with change notification. Unlike field-based generation, the declared accessibility of the property and its accessors is respected, and the property can carry its own XML documentation and attributes:

```csharp
[GenerateNotifications(hasChanged: true)]
public partial class Person
{
    private int _id; // field-based generation still works

    [AlsoNotify(nameof(FullName))]
    public partial string FirstName { get; set; }

    internal partial DateTime? UpdatedAt { get; set; }

    public partial int Version { get; private set; }

    [ExcludeFromNotification]
    public partial string? InternalToken { get; set; }

    public string FullName => FirstName;
}
```

For each partial property the generator emits a private backing field (`FirstName` → `_firstName`) and an implementing declaration with the same modifiers, e.g.:

```csharp
private string _firstName = default!;

public partial string FirstName
{
    get => _firstName;
    set
    {
        if (_firstName != value)
        {
            RaisePropertyChanging(nameof(FirstName), nameof(FullName));
            _firstName = value;
            RaisePropertyChanged(nameof(FirstName), nameof(FullName));
            HasChanged = true;
        }
    }
}
```

Only partial properties of the declaration carrying `[GenerateNotifications]` are considered; non-partial properties are ignored. The following compile-time errors are reported:

- `BB84SG0015` - The partial property does not declare both a `get` and a `set` accessor (get-only and `init` properties are not supported)
- `BB84SG0016` - The backing field name (e.g. `_firstName`) collides with an existing member
- `BB84SG0017` - The partial property is `static`

### Record Support

`[GenerateNotifications]` can also target `record` (record class) declarations, with the generated members emitted inside a `partial record`. Field-to-property conversion works exactly as for classes, as long as the record declares its fields in the body:

```csharp
[GenerateNotifications]
public partial record PersonRecord
{
    private string _firstName;
    private string _lastName;
}
```

**Positional records are not supported** - a record with a primary constructor parameter list (e.g. `record PersonRecord(string FirstName, string LastName);`) has no body-declared fields to convert into notification properties, so applying `[GenerateNotifications]` to one is a compile-time error (`BB84SG0014`):

```csharp
// Error BB84SG0014: The [GenerateNotifications] attribute cannot be applied to positional record
// 'PersonRecord' because it has no body-declared fields to convert into notification properties
[GenerateNotifications]
public partial record PersonRecord(string FirstName, string LastName);
```

## Diagnostics

| ID           | Severity | Description                                                           |
| ------------ | -------- | --------------------------------------------------------------------- |
| `BB84SG0014` | Error    | GenerateNotifications cannot be applied to positional records         |
| `BB84SG0015` | Error    | GenerateNotifications partial property requires get and set accessors |
| `BB84SG0016` | Error    | GenerateNotifications backing field name conflict                     |
| `BB84SG0017` | Error    | GenerateNotifications does not support static partial properties      |
| `BB84SG0024` | Warning  | NotifyCanExecuteChanged references an unknown command                 |
