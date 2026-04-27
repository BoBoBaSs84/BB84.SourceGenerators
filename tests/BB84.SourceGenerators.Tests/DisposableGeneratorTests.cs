// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class DisposableGeneratorTests
{
	[TestMethod]
	public void DisposeShouldDisposeMarkedFields()
	{
		FakeStream stream = new();
		FakeConnection connection = new();
		BasicDisposableModel model = new(stream, connection);

		model.Dispose();

		Assert.IsTrue(stream.IsDisposed);
		Assert.IsTrue(connection.IsDisposed);
	}

	[TestMethod]
	public void DisposeShouldBeIdempotent()
	{
		FakeStream stream = new();
		BasicDisposableModel model = new(stream, new FakeConnection());

		model.Dispose();
		model.Dispose();

		Assert.IsTrue(stream.IsDisposed);
		Assert.AreEqual(1, stream.DisposeCount);
	}

	[TestMethod]
	public void SealedDisposableShouldCompileAndWork()
	{
		FakeStream stream = new();
		SealedDisposableModel model = new(stream);

		model.Dispose();

		Assert.IsTrue(stream.IsDisposed);
	}

	[TestMethod]
	public void InternalDisposableShouldCompileAndWork()
	{
		FakeStream stream = new();
		InternalDisposableModel model = new(stream);

		model.Dispose();

		Assert.IsTrue(stream.IsDisposed);
	}

	[TestMethod]
	public void OrderedDisposalShouldRespectOrder()
	{
		List<string> disposeOrder = [];
		TrackingResource first = new("first", disposeOrder);
		TrackingResource second = new("second", disposeOrder);
		TrackingResource third = new("third", disposeOrder);
		OrderedDisposableModel model = new(first, second, third);

		model.Dispose();

		// _second has order 1, _third has order 2, _first has order 0 (source order)
		Assert.HasCount(3, disposeOrder);
		Assert.AreEqual("second", disposeOrder[0]);
		Assert.AreEqual("third", disposeOrder[1]);
		Assert.AreEqual("first", disposeOrder[2]);
	}

	[TestMethod]
	public void FinalizerModelShouldCompile()
	{
		FakeStream stream = new();
		FinalizerDisposableModel model = new(stream);

		model.Dispose();

		Assert.IsTrue(stream.IsDisposed);
	}

	[TestMethod]
	public async Task AsyncDisposableShouldDisposeFields()
	{
		FakeAsyncStream stream = new();
		AsyncDisposableModel model = new(stream);

		await model.DisposeAsync();

		Assert.IsTrue(stream.IsDisposed);
	}

	[TestMethod]
	public void DisposeShouldHandleNullFields()
	{
		BasicDisposableModel model = new(null!, null!);

		// Should not throw
		model.Dispose();
	}
}

#region Test Helpers

public class FakeStream : IDisposable
{
	public bool IsDisposed { get; private set; }
	public int DisposeCount { get; private set; }

	public void Dispose()
	{
		if (!IsDisposed)
			IsDisposed = true;
		DisposeCount++;
	}
}

public class FakeConnection : IDisposable
{
	public bool IsDisposed { get; private set; }

	public void Dispose()
		=> IsDisposed = true;
}

public class TrackingResource(string name, List<string> order) : IDisposable
{
	public void Dispose()
		=> order.Add(name);
}

public class FakeAsyncStream : IAsyncDisposable, IDisposable
{
	public bool IsDisposed { get; private set; }

	public ValueTask DisposeAsync()
	{
		IsDisposed = true;
		return default;
	}

	public void Dispose()
		=> IsDisposed = true;
}

#endregion

#region Test Models

[GenerateDisposable]
public partial class BasicDisposableModel(FakeStream stream, FakeConnection connection)
{
	[DisposeResource]
	private readonly FakeStream _stream = stream;

	[DisposeResource]
	private readonly FakeConnection _connection = connection;
}

[GenerateDisposable]
public sealed partial class SealedDisposableModel(FakeStream stream)
{
	[DisposeResource]
	private readonly FakeStream _stream = stream;
}

[GenerateDisposable]
internal sealed partial class InternalDisposableModel(FakeStream stream)
{
	[DisposeResource]
	private readonly FakeStream _stream = stream;
}

[GenerateDisposable]
public partial class OrderedDisposableModel(TrackingResource first, TrackingResource second, TrackingResource third)
{
	[DisposeResource]
	private readonly TrackingResource _first = first;

	[DisposeResource(order: 1)]
	private readonly TrackingResource _second = second;

	[DisposeResource(order: 2)]
	private readonly TrackingResource _third = third;
}

[GenerateDisposable(generateFinalizer: true)]
public partial class FinalizerDisposableModel(FakeStream stream)
{
	[DisposeResource]
	private readonly FakeStream _stream = stream;
}

[GenerateDisposable(async: true)]
public partial class AsyncDisposableModel(FakeAsyncStream stream)
{
	[DisposeResource]
	private readonly FakeAsyncStream _stream = stream;
}

#endregion
