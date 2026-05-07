// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class AutoMapperGeneratorTests
{
	[TestMethod]
	public void AutoMapperGenerationShouldGenerateToDtoCorrectly()
	{
		TestEntity entity = new()
		{
			Id = 1,
			Name = "Test Entity",
			Description = "This is a test entity."
		};

		TestDto dto = EntityMapper.ToDto(entity);

		Assert.AreEqual(entity.Id, dto.Id);
		Assert.AreEqual(entity.Name, dto.Name);
		Assert.AreEqual(entity.Description, dto.Description);
	}

	[TestMethod]
	public void AutoMapperGenerationShouldGenerateToEntityCorrectly()
	{
		TestDto dto = new()
		{
			Id = 1,
			Name = "Test DTO",
			Description = "This is a test DTO."
		};
		TestEntity entity = EntityMapper.ToEntity(dto);
		Assert.AreEqual(dto.Id, entity.Id);
		Assert.AreEqual(dto.Name, entity.Name);
		Assert.AreEqual(dto.Description, entity.Description);
	}
}

public class TestEntity
{
	public int Id { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
	public DateTime CreatedAt { get; set; }
}

public class TestDto
{
	public int Id { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
}

public static partial class EntityMapper
{
	[GenerateAutoMapper]
	public static partial TestEntity ToEntity(TestDto source);

	[GenerateAutoMapper]
	public static partial TestDto ToDto(TestEntity source);
}
