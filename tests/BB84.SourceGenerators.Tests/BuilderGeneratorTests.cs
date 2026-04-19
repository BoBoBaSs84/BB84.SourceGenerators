// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class BuilderGeneratorTests
{
	[TestMethod]
	public void BuildShouldCreateInstanceWithDefaultValues()
	{
		BuilderTestModel model = new BuilderTestModelBuilder().Build();

		Assert.AreEqual(0, model.Id);
		Assert.IsNull(model.Name);
		Assert.IsNull(model.Description);
		Assert.AreEqual(0.0, model.Price);
		Assert.IsFalse(model.IsActive);
	}

	[TestMethod]
	public void BuildShouldCreateInstanceWithConfiguredValues()
	{
		BuilderTestModel model = new BuilderTestModelBuilder()
			.WithId(42)
			.WithName("TestName")
			.WithDescription("TestDescription")
			.WithPrice(19.99)
			.WithIsActive(true)
			.Build();

		Assert.AreEqual(42, model.Id);
		Assert.AreEqual("TestName", model.Name);
		Assert.AreEqual("TestDescription", model.Description);
		Assert.AreEqual(19.99, model.Price);
		Assert.IsTrue(model.IsActive);
	}

	[TestMethod]
	public void BuildShouldCreateInstanceWithPartialValues()
	{
		BuilderTestModel model = new BuilderTestModelBuilder()
			.WithId(7)
			.WithName("PartialName")
			.Build();

		Assert.AreEqual(7, model.Id);
		Assert.AreEqual("PartialName", model.Name);
		Assert.IsNull(model.Description);
		Assert.AreEqual(0.0, model.Price);
		Assert.IsFalse(model.IsActive);
	}

	[TestMethod]
	public void WithMethodsShouldSupportFluentChaining()
	{
		BuilderTestModelBuilder builder = new();

		BuilderTestModelBuilder result = builder
			.WithId(1)
			.WithName("Fluent")
			.WithDescription("Test")
			.WithPrice(9.99)
			.WithIsActive(true);

		Assert.AreSame(builder, result);
	}

	[TestMethod]
	public void BuildShouldCreateSeparateInstances()
	{
		BuilderTestModelBuilder builder = new BuilderTestModelBuilder()
			.WithId(1)
			.WithName("Instance");

		BuilderTestModel first = builder.Build();
		BuilderTestModel second = builder.Build();

		Assert.AreNotSame(first, second);
		Assert.AreEqual(first.Id, second.Id);
		Assert.AreEqual(first.Name, second.Name);
	}
}

[GenerateBuilder]
public partial class BuilderTestModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Description { get; set; }
	public double Price { get; set; }
	public bool IsActive { get; set; }
}
