// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests.Generators;

[TestClass]
public sealed class NotificationPropertiesGeneratorTests
{
	[TestMethod]
	public void InitializeTest()
	{
		int propertyChangingCount = 0;
		int propertyChangedCount = 0;
		NotificationProperties testClass = new(1, "TestName", "Description", DateTime.UtcNow);
		testClass.PropertyChanging += (s, e) => propertyChangingCount++;
		testClass.PropertyChanged += (s, e) => propertyChangedCount++;

		testClass.Id = 2;
		testClass.Name = "NewName";
		testClass.Description = "NewDescription";
		testClass.CreatedAt = DateTime.UtcNow.AddDays(1);
		testClass.UpdatedAt = DateTime.UtcNow.AddDays(2);

		Assert.AreEqual(5, propertyChangingCount, "PropertyChanging event should be raised 5 times.");
		Assert.AreEqual(5, propertyChangedCount, "PropertyChanged event should be raised 5 times.");
	}
}

[GenerateNotifications]
public partial class NotificationProperties
{
	private int _id;
	private string _name;
	private string _description;
	private DateTime _createdAt;
	private DateTime? _updatedAt;
	private int _quanity;
	private float _price;

	public NotificationProperties(int id, string name, string description, DateTime createdAt, DateTime? updatedAt = null)
	{
		_id = id;
		_name = name;
		_description = description;
		_createdAt = createdAt;
		_updatedAt = updatedAt;
	}
}
