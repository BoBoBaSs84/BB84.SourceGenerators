// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class CloneableGeneratorTests
{
  [TestMethod]
  public void CloneShouldCreateShallowCopy()
  {
    CloneableTestModel original = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

    CloneableTestModel clone = original.Clone();

    Assert.AreNotSame(original, clone);
    Assert.AreEqual(original.Id, clone.Id);
    Assert.AreEqual(original.Name, clone.Name);
    Assert.AreEqual(original.Price, clone.Price);
    Assert.AreEqual(original.IsActive, clone.IsActive);
  }

  [TestMethod]
  public void CloneShouldShareReferenceTypeProperties()
  {
    CloneableNestedModel nested = new() { Value = "nested" };
    CloneableParentModel original = new() { Id = 1, Nested = nested };

    CloneableParentModel clone = original.Clone();

    Assert.AreNotSame(original, clone);
    Assert.AreSame(original.Nested, clone.Nested);
  }

  [TestMethod]
  public void DeepCloneShouldCreateDeepCopy()
  {
    CloneableNestedModel nested = new() { Value = "nested" };
    CloneableParentModel original = new() { Id = 1, Nested = nested };

    CloneableParentModel clone = original.DeepClone();

    Assert.AreNotSame(original, clone);
    Assert.AreNotSame(original.Nested, clone.Nested);
    Assert.AreEqual(original.Id, clone.Id);
    Assert.AreEqual(original.Nested.Value, clone.Nested?.Value);
  }

  [TestMethod]
  public void DeepCloneShouldHandleNullReferenceProperties()
  {
    CloneableParentModel original = new() { Id = 1, Nested = null };

    CloneableParentModel clone = original.DeepClone();

    Assert.AreNotSame(original, clone);
    Assert.AreEqual(original.Id, clone.Id);
    Assert.IsNull(clone.Nested);
  }

  [TestMethod]
  public void DeepCloneShouldNotAffectOriginal()
  {
    CloneableNestedModel nested = new() { Value = "original" };
    CloneableParentModel original = new() { Id = 1, Nested = nested };

    CloneableParentModel clone = original.DeepClone();
    clone.Id = 99;
    clone.Nested.Value = "modified";

    Assert.AreEqual(1, original.Id);
    Assert.AreEqual("original", original.Nested.Value);
  }

  [TestMethod]
  public void CloneShouldExcludeSpecifiedProperties()
  {
    CloneableExcludeTestModel original = new() { Id = 1, Name = "John", Secret = "abc" };

    CloneableExcludeTestModel clone = original.Clone();

    Assert.AreEqual(original.Id, clone.Id);
    Assert.AreEqual(original.Name, clone.Name);
    Assert.IsNull(clone.Secret);
  }

  [TestMethod]
  public void DeepCloneShouldExcludeSpecifiedProperties()
  {
    CloneableExcludeTestModel original = new() { Id = 1, Name = "John", Secret = "abc" };

    CloneableExcludeTestModel clone = original.DeepClone();

    Assert.AreEqual(original.Id, clone.Id);
    Assert.AreEqual(original.Name, clone.Name);
    Assert.IsNull(clone.Secret);
  }

  [TestMethod]
  public void ImplementsICloneable()
  {
    CloneableTestModel model = new() { Id = 1, Name = "John", Price = 9.99, IsActive = true };

    Assert.IsInstanceOfType<ICloneable>(model);
  }

  [TestMethod]
  public void ICloneableCloneShouldReturnDeepClone()
  {
    CloneableNestedModel nested = new() { Value = "nested" };
    CloneableParentModel original = new() { Id = 1, Nested = nested };

    object clone = ((ICloneable)original).Clone();

    Assert.IsInstanceOfType<CloneableParentModel>(clone);
    CloneableParentModel typedClone = (CloneableParentModel)clone;
    Assert.AreNotSame(original, typedClone);
    Assert.AreNotSame(original.Nested, typedClone.Nested);
    Assert.AreEqual(original.Nested.Value, typedClone.Nested?.Value);
  }

  [TestMethod]
  public void CloneShouldHandleNullStringProperties()
  {
    CloneableTestModel original = new() { Id = 1, Name = null, Price = 0.0, IsActive = false };

    CloneableTestModel clone = original.Clone();

    Assert.AreNotSame(original, clone);
    Assert.AreEqual(original.Id, clone.Id);
    Assert.IsNull(clone.Name);
  }

  [TestMethod]
  public void DeepCloneShouldCopyValueTypesDirectly()
  {
    CloneableTestModel original = new() { Id = 42, Name = "Test", Price = 3.14, IsActive = true };

    CloneableTestModel clone = original.DeepClone();

    Assert.AreEqual(42, clone.Id);
    Assert.AreEqual("Test", clone.Name);
    Assert.AreEqual(3.14, clone.Price);
    Assert.IsTrue(clone.IsActive);
  }
}

[GenerateCloneable]
public partial class CloneableTestModel
{
  public int Id { get; set; }
  public string? Name { get; set; }
  public double Price { get; set; }
  public bool IsActive { get; set; }
}

[GenerateCloneable]
public partial class CloneableParentModel
{
  public int Id { get; set; }
  public CloneableNestedModel? Nested { get; set; }
}

[GenerateCloneable]
public partial class CloneableNestedModel
{
  public string? Value { get; set; }
}

[GenerateCloneable("Secret")]
public partial class CloneableExcludeTestModel
{
  public int Id { get; set; }
  public string? Name { get; set; }
  public string? Secret { get; set; }
}
