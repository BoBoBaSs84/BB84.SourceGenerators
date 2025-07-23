// Copyright: 2025 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests.Generators;

[TestClass]
public sealed class NotificationPropertyGeneratorTests
{
	[TestMethod]
	public void InitializeTest()
	{
		int propertyChangingCount = 0;
		int propertyChangedCount = 0;
		NotificationProperty testClass = new(1, "TestName");
		testClass.PropertyChanging += (s, e) => propertyChangingCount++;
		testClass.PropertyChanged += (s, e) => propertyChangedCount++;

		testClass.Id = 2;
		testClass.Name = "NewName";

		Assert.AreEqual(2, propertyChangingCount, "PropertyChanging event should be raised 2 times.");
		Assert.AreEqual(2, propertyChangedCount, "PropertyChanged event should be raised 2 times.");
	}
}

public partial class NotificationProperty
{
	[GenerateNotificationProperty]
	private int _id;
	[GenerateNotificationProperty]
	private string _name;

	public NotificationProperty(int id, string name)
	{
		_id = id;
		_name = name;
	}
}
