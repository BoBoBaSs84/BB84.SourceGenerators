// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class NotificationsGeneratorTests
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

	[TestMethod]
	public void SealedClassShouldCompileAndWork()
	{
		int changedCount = 0;
		SealedNotificationModel model = new();
		model.PropertyChanged += (s, e) => changedCount++;

		model.Value = 42;

		Assert.AreEqual(1, changedCount);
	}

	[TestMethod]
	public void InternalClassShouldCompileAndWork()
	{
		int changingCount = 0;
		int changedCount = 0;
		InternalNotificationModel model = new();
		model.PropertyChanging += (s, e) => changingCount++;
		model.PropertyChanged += (s, e) => changedCount++;

		model.Name = "Test";

		Assert.AreEqual(1, changingCount);
		Assert.AreEqual(1, changedCount);
	}

	[TestMethod]
	public void PropertyChangedOnlyShouldNotGenerateChanging()
	{
		int changedCount = 0;
		PropertyChangedOnlyModel model = new();
		model.PropertyChanged += (s, e) => changedCount++;

		model.Name = "Test";

		Assert.AreEqual(1, changedCount);
	}

	[TestMethod]
	public void PropertyChangingOnlyShouldNotGenerateChanged()
	{
		int changingCount = 0;
		PropertyChangingOnlyModel model = new();
		model.PropertyChanging += (s, e) => changingCount++;

		model.Name = "Test";

		Assert.AreEqual(1, changingCount);
	}

	[TestMethod]
	public void HasChangedShouldBeSetOnPropertyChange()
	{
		HasChangedModel model = new();

		Assert.IsFalse(model.HasChanged);

		model.Name = "Test";

		Assert.IsTrue(model.HasChanged);
	}
}

[GenerateNotifications]
public partial class NotificationProperties(int id, string name, string description, DateTime createdAt, DateTime? updatedAt = null)
{
	private int _id = id;
	private string _name = name;
	private string _description = description;
	private DateTime _createdAt = createdAt;
	private DateTime? _updatedAt = updatedAt;
	private int _quanity;
	private float _price;
}

[GenerateNotifications]
public sealed partial class SealedNotificationModel
{
	private int _value;
}

[GenerateNotifications]
internal sealed partial class InternalNotificationModel
{
	private string? _name;
}

[GenerateNotifications(propertyChanging: false)]
public partial class PropertyChangedOnlyModel
{
	private string? _name;
}

[GenerateNotifications(propertyChanged: false)]
public partial class PropertyChangingOnlyModel
{
	private string? _name;
}

[GenerateNotifications(hasChanged: true)]
public partial class HasChangedModel
{
	private string? _name;
}
