// Copyright: 2026 Robert Peter Meyer
// License: MIT
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
using BB84.SourceGenerators.Attributes;

namespace BB84.SourceGenerators.Tests;

[TestClass]
public sealed class AssemblyInformationGeneratorTests
{
  [TestMethod]
  public void TitleShouldReturnAssemblyTitle()
  {
    Assert.IsFalse(string.IsNullOrEmpty(AssemblyInformationTestModel.Title));
  }

  [TestMethod]
  public void VersionShouldReturnAssemblyVersion()
  {
    Assert.IsFalse(string.IsNullOrEmpty(AssemblyInformationTestModel.Version));
  }

  [TestMethod]
  public void FileVersionShouldReturnAssemblyFileVersion()
  {
    Assert.IsFalse(string.IsNullOrEmpty(AssemblyInformationTestModel.FileVersion));
  }

  [TestMethod]
  public void InformationalVersionShouldReturnAssemblyInformationalVersion()
  {
    Assert.IsFalse(string.IsNullOrEmpty(AssemblyInformationTestModel.InformationalVersion));
  }

  [TestMethod]
  public void PropertiesShouldBeConstants()
  {
    // Constants can be used in attribute arguments and switch expressions
    // This test verifies they are compile-time constants by using them in a switch
    string result = AssemblyInformationTestModel.Title switch
    {
      AssemblyInformationTestModel.Title => "matched",
      _ => "not matched"
    };

    Assert.AreEqual("matched", result);
  }

  [TestMethod]
  public void ConfigurationShouldBeAccessible()
  {
    // Configuration should be a valid string (possibly empty if not set)
    Assert.IsNotNull(AssemblyInformationTestModel.Configuration);
  }

  [TestMethod]
  public void AllPropertiesShouldBeNonNull()
  {
    Assert.IsNotNull(AssemblyInformationTestModel.Title);
    Assert.IsNotNull(AssemblyInformationTestModel.Description);
    Assert.IsNotNull(AssemblyInformationTestModel.Company);
    Assert.IsNotNull(AssemblyInformationTestModel.Product);
    Assert.IsNotNull(AssemblyInformationTestModel.Copyright);
    Assert.IsNotNull(AssemblyInformationTestModel.Trademark);
    Assert.IsNotNull(AssemblyInformationTestModel.Configuration);
    Assert.IsNotNull(AssemblyInformationTestModel.Version);
    Assert.IsNotNull(AssemblyInformationTestModel.FileVersion);
    Assert.IsNotNull(AssemblyInformationTestModel.InformationalVersion);
  }
}

[GenerateAssemblyInformation]
public partial class AssemblyInformationTestModel
{ }
