// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using System.Reflection;

using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class NotificationsGeneratorTests
{
	private static readonly string[] ExpectedPartialMethodsLog =
	[
		"Changing:John", "Changing:->John", "PropertyChanging:Name", "Changed:John", "Changed:->John", "PropertyChanged:Name",
		"Changing:Jane", "Changing:John->Jane", "PropertyChanging:Name", "Changed:Jane", "Changed:John->Jane", "PropertyChanged:Name"
	];

	private static readonly string[] ExpectedPartialPropertyPartialMethodsLog = ["Count:0->5", "Count:5->7"];

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

	[TestMethod]
	public void ExcludeFromNotificationShouldNotGenerateProperty()
	{
		ExcludeFieldModel model = new() { Id = 0 };

		// Id should be generated
		model.Id = 42;
		Assert.AreEqual(42, model.Id);

		// Secret should not have a generated property
		Assert.IsNull(typeof(ExcludeFieldModel).GetProperty("Secret"), "Secret property should not be generated.");
	}

	[TestMethod]
	public void AlsoNotifyForShouldRaiseAdditionalNotifications()
	{
		List<string> changedProperties = [];
		List<string> changingProperties = [];
		AlsoNotifyModel model = new();
		model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);
		model.PropertyChanging += (s, e) => changingProperties.Add(e.PropertyName!);

		model.FirstName = "John";
		model.LastName = "Doe";
		model.DateOfBirth = DateTime.MinValue;

		Assert.Contains(nameof(model.FirstName), changedProperties);
		Assert.Contains(nameof(model.LastName), changedProperties);
		Assert.Contains(nameof(model.FullName), changedProperties);
		Assert.Contains(nameof(model.DateOfBirth), changedProperties);
		Assert.Contains(nameof(model.Age), changedProperties);
		Assert.Contains(nameof(model.YearOfBirth), changedProperties);
		Assert.Contains(nameof(model.FirstName), changingProperties);
		Assert.Contains(nameof(model.LastName), changingProperties);
		Assert.Contains(nameof(model.FullName), changingProperties);
		Assert.Contains(nameof(model.DateOfBirth), changingProperties);
		Assert.Contains(nameof(model.Age), changingProperties);
		Assert.Contains(nameof(model.YearOfBirth), changingProperties);
	}

	[TestMethod]
	public void RecordShouldRaiseNotifications()
	{
		int changingCount = 0;
		int changedCount = 0;
		NotificationRecordModel model = new();
		model.PropertyChanging += (s, e) => changingCount++;
		model.PropertyChanged += (s, e) => changedCount++;

		model.Id = 1;
		model.Name = "Test";

		Assert.AreEqual(2, changingCount);
		Assert.AreEqual(2, changedCount);
	}

	[TestMethod]
	public void PartialPropertiesShouldRaiseNotifications()
	{
		List<string> changedProperties = [];
		List<string> changingProperties = [];
		PartialPropertyModel model = new();
		model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);
		model.PropertyChanging += (s, e) => changingProperties.Add(e.PropertyName!);

		model.Name = "Test";
		model.Count = 5;
		model.UpdatedAt = DateTime.MinValue;
		model.IncrementVersion();

		Assert.AreEqual("Test", model.Name);
		Assert.AreEqual(5, model.Count);
		Assert.AreEqual(DateTime.MinValue, model.UpdatedAt);
		Assert.AreEqual(1, model.Version);
		string[] expected = [nameof(model.Name), nameof(model.Count), nameof(model.UpdatedAt), nameof(model.Version)];
		Assert.AreSequenceEqual(expected, changedProperties);
		Assert.AreSequenceEqual(expected, changingProperties);
	}

	[TestMethod]
	public void PartialPropertiesShouldNotNotifyWhenValueIsUnchanged()
	{
		int changedCount = 0;
		PartialPropertyModel model = new() { Name = "Test" };
		model.PropertyChanged += (s, e) => changedCount++;

		model.Name = "Test";

		Assert.AreEqual(0, changedCount);
	}

	[TestMethod]
	public void PartialPropertiesShouldRespectDeclaredAccessibility()
	{
		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		Type type = typeof(PartialPropertyModel);

		Assert.IsTrue(type.GetProperty(nameof(PartialPropertyModel.Name), flags)!.GetMethod!.IsPublic);
		Assert.IsTrue(type.GetProperty(nameof(PartialPropertyModel.Count), flags)!.GetMethod!.IsAssembly);
		Assert.IsTrue(type.GetProperty(nameof(PartialPropertyModel.UpdatedAt), flags)!.GetMethod!.IsFamilyOrAssembly);
		Assert.IsTrue(type.GetProperty("Notes", flags)!.GetMethod!.IsFamily);

		PropertyInfo version = type.GetProperty(nameof(PartialPropertyModel.Version), flags)!;
		Assert.IsTrue(version.GetMethod!.IsPublic);
		Assert.IsTrue(version.SetMethod!.IsPrivate);
	}

	[TestMethod]
	public void PartialPropertyAlsoNotifyShouldRaiseAdditionalNotifications()
	{
		List<string> changedProperties = [];
		List<string> changingProperties = [];
		PartialPropertyAlsoNotifyModel model = new();
		model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);
		model.PropertyChanging += (s, e) => changingProperties.Add(e.PropertyName!);

		model.FirstName = "John";

		Assert.AreSequenceEqual([nameof(model.FirstName), nameof(model.FullName)], changedProperties);
		Assert.AreSequenceEqual([nameof(model.FirstName), nameof(model.FullName)], changingProperties);
	}

	[TestMethod]
	public void PartialPropertyExcludeFromNotificationShouldStoreValueWithoutNotification()
	{
		int changedCount = 0;
		int changingCount = 0;
		PartialPropertyExcludeModel model = new();
		model.PropertyChanged += (s, e) => changedCount++;
		model.PropertyChanging += (s, e) => changingCount++;

		model.Secret = "hidden";

		Assert.AreEqual("hidden", model.Secret);
		Assert.AreEqual(0, changedCount);
		Assert.AreEqual(0, changingCount);
		Assert.IsFalse(model.HasChanged);
	}

	[TestMethod]
	public void PartialPropertyHasChangedShouldBeSetOnPropertyChange()
	{
		PartialPropertyExcludeModel model = new();

		model.Name = "Changed";

		Assert.IsTrue(model.HasChanged);
	}

	[TestMethod]
	public void SealedPartialPropertyModelShouldCompileAndWork()
	{
		int changedCount = 0;
		SealedPartialPropertyModel model = new();
		model.PropertyChanged += (s, e) => changedCount++;

		model.Value = 42;

		Assert.AreEqual(42, model.Value);
		Assert.AreEqual(1, changedCount);
	}

	[TestMethod]
	public void PartialPropertyRecordShouldRaiseNotifications()
	{
		int changedCount = 0;
		PartialPropertyRecordModel model = new();
		model.PropertyChanged += (s, e) => changedCount++;

		model.Name = "Test";

		Assert.AreEqual("Test", model.Name);
		Assert.AreEqual(1, changedCount);
	}

	[TestMethod]
	public void MixedFieldsAndPartialPropertiesShouldRaiseNotifications()
	{
		List<string> changedProperties = [];
		MixedFieldAndPartialPropertyModel model = new();
		model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

		model.Id = 1;
		model.Name = "Test";

		Assert.AreSequenceEqual([nameof(model.Id), nameof(model.Name)], changedProperties);
	}

	[TestMethod]
	public void PartialMethodsShouldBeCalledAroundNotifications()
	{
		PartialMethodsModel model = new();
		model.PropertyChanging += (s, e) => model.Log.Add($"PropertyChanging:{e.PropertyName}");
		model.PropertyChanged += (s, e) => model.Log.Add($"PropertyChanged:{e.PropertyName}");

		model.Name = "John";
		model.Name = "Jane";

		Assert.AreSequenceEqual(ExpectedPartialMethodsLog, model.Log);
	}

	[TestMethod]
	public void PartialMethodsShouldNotBeCalledWhenValueIsUnchanged()
	{
		PartialMethodsModel model = new() { Name = "John" };
		model.Log.Clear();

		model.Name = "John";

		Assert.IsEmpty(model.Log);
	}

	[TestMethod]
	public void PartialMethodsShouldWorkForPartialPropertiesWithPartialImplementation()
	{
		PartialMethodsModel model = new();

		model.Count = 5;
		model.Count = 7;

		Assert.AreSequenceEqual(ExpectedPartialPropertyPartialMethodsLog, model.Log);
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

[GenerateNotifications]
public partial class ExcludeFieldModel
{
	private int _id;
	[ExcludeFromNotification]
	private readonly string? _secret;
}

[GenerateNotifications]
public partial class AlsoNotifyModel
{
	private int _id;
	[AlsoNotify(nameof(FullName))]
	private string? _firstName;
	[AlsoNotify(nameof(FullName))]
	private string? _lastName;
	[AlsoNotify("Age", nameof(YearOfBirth))]
	private DateTime? _dateOfBirth;

	public string? FullName => $"{_firstName} {_lastName}";
	public int? Age => _dateOfBirth.HasValue ? (int)((DateTime.UtcNow - _dateOfBirth.Value).TotalDays / 365.25) : null;
	public int? YearOfBirth => _dateOfBirth?.Year;
}

[GenerateNotifications]
public partial record NotificationRecordModel
{
	private int _id;
	private string? _name;
}

[GenerateNotifications]
public partial class PartialPropertyModel
{
	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public partial string? Name { get; set; }

	internal partial int Count { get; set; }

	protected internal partial DateTime? UpdatedAt { get; set; }

	protected partial string? Notes { get; set; }

	public partial int Version { get; private set; }

	public void IncrementVersion()
		=> Version++;
}

[GenerateNotifications]
public partial class PartialPropertyAlsoNotifyModel
{
	[AlsoNotify(nameof(FullName))]
	public partial string? FirstName { get; set; }

	[AlsoNotify(nameof(FullName))]
	public partial string? LastName { get; set; }

	public string FullName => $"{FirstName} {LastName}";
}

[GenerateNotifications(hasChanged: true)]
public partial class PartialPropertyExcludeModel
{
	public partial string? Name { get; set; }

	[ExcludeFromNotification]
	public partial string? Secret { get; set; }
}

[GenerateNotifications]
public sealed partial class SealedPartialPropertyModel
{
	public partial int Value { get; set; }
}

[GenerateNotifications]
public partial record PartialPropertyRecordModel
{
	public partial string Name { get; set; }
}

[GenerateNotifications]
public partial class MixedFieldAndPartialPropertyModel
{
	private int _id;

	public partial string? Name { get; set; }
}

[GenerateNotifications(partialMethods: true)]
public partial class PartialMethodsModel
{
	private string? _name;

	public partial int Count { get; set; }

	public List<string> Log { get; } = [];

	partial void OnNameChanging(string? value)
		=> Log.Add($"Changing:{value}");

	partial void OnNameChanging(string? oldValue, string? newValue)
		=> Log.Add($"Changing:{oldValue}->{newValue}");

	partial void OnNameChanged(string? value)
		=> Log.Add($"Changed:{value}");

	partial void OnNameChanged(string? oldValue, string? newValue)
		=> Log.Add($"Changed:{oldValue}->{newValue}");

	partial void OnCountChanged(int oldValue, int newValue)
		=> Log.Add($"Count:{oldValue}->{newValue}");
}
