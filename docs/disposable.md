# Disposable Generator

Generates the complete dispose pattern for classes, implementing `IDisposable` and optionally `IAsyncDisposable`. Fields marked with `[DisposeResource]` are automatically disposed with configurable ordering.

[← Back to overview](../README.md) · [All generators](README.md)

## Why

The full dispose pattern needs a guard flag, `Dispose(bool)`, `GC.SuppressFinalize`, optional finalizer and optional `DisposeAsync`. It is easy to forget a field, dispose in the wrong order, or get the sealed/virtual visibility wrong. This generator emits the complete pattern from the fields you mark with `[DisposeResource]`.

## Benefits

- Generates the complete dispose pattern at compile time with zero boilerplate
- Replaces error-prone manual `IDisposable` implementations (missed fields, wrong guard logic, missing `GC.SuppressFinalize`)
- Supports configurable disposal ordering via `[DisposeResource(order)]` for expressing resource dependencies
- Optionally generates `IAsyncDisposable` with `DisposeAsync()` and `DisposeAsyncCore()`
- Sealed class support with correct method visibility (`private` vs `protected virtual`)
- Optional finalizer generation for classes with unmanaged resources

## Attributes

```csharp
[GenerateDisposable(bool generateFinalizer = false, bool async = false)]
[DisposeResource(int order = 0)]
```

**Parameters:**

- `GenerateDisposable`:
  - `generateFinalizer` - When `true`, generates a finalizer that calls `Dispose(false)`
  - `async` - When `true`, additionally implements `IAsyncDisposable` with `DisposeAsync()` and `DisposeAsyncCore()`
- `DisposeResource`:
  - `order` - Disposal order. Non-zero values are disposed first (ascending), then zero-order fields in source declaration order

**Constraints:**

- `GenerateDisposable` can only be applied to classes.
- `DisposeResource` can only be applied to fields.
- `IAsyncDisposable` is only implemented when `async: true` is specified. A compile-time warning (`BB84SG0006`) will be emitted if the framework does not support `IAsyncDisposable` and `async: true` is used.

## Usage

### Example

```csharp
using BB84.SourceGenerators.Attributes;

[GenerateDisposable]
public partial class ConnectionManager
{
    [DisposeResource]
    private Stream _stream;

    [DisposeResource(order: 1)]
    private DbConnection _connection;

    private string _name; // not disposed (no attribute)

    public ConnectionManager(Stream stream, DbConnection connection)
    {
        _stream = stream;
        _connection = connection;
    }
}
```

### Generated Code

The generator creates:

- `private bool _disposed` guard field
- `Dispose(bool disposing)` method (`protected virtual` for non-sealed, `private` for sealed classes)
- `Dispose()` implementing `IDisposable`
- `ThrowIfDisposed()` guard method
- Optional finalizer (when `generateFinalizer: true`)
- Optional `DisposeAsync()` and `DisposeAsyncCore()` (when `async: true`)
- Correct accessibility modifier matching the user's class declaration

### Usage Example

```csharp
// Basic usage
var manager = new ConnectionManager(stream, connection);
// ... use resources ...
manager.Dispose(); // _connection disposed first (order 1), then _stream (order 0)

// With finalizer for unmanaged resources
[GenerateDisposable(generateFinalizer: true)]
public partial class NativeResourceWrapper
{
    [DisposeResource]
    private SafeHandle _handle;
}

// With async disposal
[GenerateDisposable(async: true)]
public partial class AsyncService
{
    [DisposeResource]
    private HttpClient _client;

    [DisposeResource(order: 1)]
    private DbConnection _connection;
}

await using var service = new AsyncService(client, connection);

// Sealed classes work correctly (private methods instead of protected virtual)
[GenerateDisposable]
public sealed partial class SealedResource
{
    [DisposeResource]
    private Stream _stream;
}

// Ordered disposal - dispose writer before underlying stream
[GenerateDisposable]
public partial class FileProcessor
{
    [DisposeResource(order: 2)]
    private Stream _stream;

    [DisposeResource(order: 1)]
    private StreamWriter _writer;
}
```

## Diagnostics

| ID           | Severity | Description                                               |
| ------------ | -------- | --------------------------------------------------------- |
| `BB84SG0006` | Warning  | IAsyncDisposable is not available in the target framework |
