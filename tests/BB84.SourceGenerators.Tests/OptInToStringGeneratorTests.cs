// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class OptInToStringGeneratorTests
{
	[TestMethod]
	public void ToStringShouldReturnOnlySelectedProperties()
	{
		OptInToStringTestModel model = new()
		{
			Id = 42,
			Name = "John",
			Description = "A test model",
			Price = 19.99,
			IsActive = true
		};

		string? result = model.ToString();
		string expected = $"{nameof(OptInToStringTestModel)} {{ " +
			$"{nameof(model.Id)} = {model.Id:D3}, " +
			$"{nameof(model.Name)} = {model.Name}, " +
			$"{nameof(model.IsActive)} = {model.IsActive} }}";

		Assert.AreEqual(expected, result);
	}

}

[OptInGenerateToString()]
public partial class OptInToStringTestModel
{
	[ToString(order: 0, format: "D3")]
	public int Id { get; set; }

	[ToString(order: 1)]
	public string? Name { get; set; }
	public string? Description { get; set; }
	public double Price { get; set; }

	[ToString()]
	public bool IsActive { get; set; }

	/* Generation will fail with OTSG001 because if the ToString was generated it will break the build.
	public override string ToString()
	{
		return base.ToString();
	}
	*/
}

