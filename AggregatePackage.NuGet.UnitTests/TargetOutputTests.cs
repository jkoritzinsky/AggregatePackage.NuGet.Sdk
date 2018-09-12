using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace AggregatePackage.NuGet.UnitTests
{
    public class TargetOutputTests : MSBuildSdkTestBase
    {
        [Fact]
        public void CollectPackageReferences_Collects_Embedded_Project_PackageReferences()
        {
            var referencedProject = ProjectCreator
                .Templates
                .SdkCsproj(
                    path: Path.Combine(TestRootPath, "referenced.csproj"),
                    projectCreator: creator => creator.ItemPackageReference("ReferencedPackage", "1.0.0")
                ).Save();

            var sdkProject = ProjectCreator
                .Templates
                .AggregateProject(
                    path: Path.Combine(TestRootPath, "test.pkgproj"),
                    projectReferences: new Dictionary<Project, bool>
                    {
                        { referencedProject.Project, true }
                    }
                ).Save()
                .TryBuild("CollectPackageReferences", out TargetResult targetResult, out var output);

            targetResult.ResultCode.ShouldBe(TargetResultCode.Success, () => output.GetConsoleLog());

            targetResult.Items
                .Where(item => item.ItemSpec != "NETStandard.Library")
                .ToDictionary(item => item.ItemSpec, item => item.GetMetadata("Version"))
                .ShouldBe(
                    new Dictionary<string, string>
                    {
                        { "ReferencedPackage", "1.0.0"}
                    },
                    ignoreOrder: true
                );
        }

        [Theory]
        [InlineData("GetTargetPath")]
        [InlineData("Build")]
        public void Build_Targets_Output_All_Child_Project_Outputs(string target)
        {
            var targetFramework = "netstandard2.0";

            var referenced1 = "referenced1";
            var referenced2 = "referenced2";

            var referenced1Project = ProjectCreator
                .Templates
                .SdkCsproj(
                    path: Path.Combine(TestRootPath, referenced1, $"{referenced1}.csproj"),
                    targetFramework: targetFramework
                ).Save();

            var referenced2Project = ProjectCreator
                .Templates
                .SdkCsproj(
                    path: Path.Combine(TestRootPath, referenced2, $"{referenced2}.csproj"),
                    targetFramework: targetFramework
                ).Save();

            var sdkProject = ProjectCreator
                .Templates
                .AggregateProject(
                    path: Path.Combine(TestRootPath, "test", "test.csproj"),
                    projectReferences: new Dictionary<Project, bool>
                    {
                        { referenced1Project.Project, true },
                        { referenced2Project.Project, true }
                    },
                    targetFrameworks: new[] { targetFramework }
                ).Save()
                .TryBuild("Restore", out var restoreResult, out var restoreOutput)
                .ForceProjectReevaluation();

            restoreResult.ShouldBeTrue(() => restoreOutput.GetConsoleLog());

            sdkProject.TryBuild(target, out TargetResult targetResult, out var output);

            targetResult.ResultCode.ShouldBe(TargetResultCode.Success, () => output.GetConsoleLog());

            var buildOutputs = targetResult.Items
                .Select(item => item.ItemSpec);

            buildOutputs
                .ShouldContain(
                    Path.Combine(
                        TestRootPath,
                        referenced1,
                        "bin",
                        "Debug",
                        targetFramework,
                        $"{referenced1}.dll"));

            buildOutputs
                .ShouldContain(
                    Path.Combine(
                        TestRootPath,
                        referenced2,
                        "bin",
                        "Debug",
                        targetFramework,
                        $"{referenced2}.dll"));
        }

        [Fact]
        public void TfmSpecificContent_In_Embedded_Project_Outputed_By_Target_In_Aggregate()
        {
            var targetFramework = "netstandard2.0";

            var referenced1 = "referenced1";

            var referenced1Project = ProjectCreator
                .Templates
                .SdkCsproj(
                    path: Path.Combine(TestRootPath, referenced1, $"{referenced1}.csproj"),
                    targetFramework: targetFramework
                )
                .ItemInclude("TfmSpecificPackageFile", "TfmContent")
                .Save();

            var sdkProject = ProjectCreator
                .Templates
                .AggregateProject(
                    path: Path.Combine(TestRootPath, "test", "test.csproj"),
                    projectReferences: new Dictionary<Project, bool>
                    {
                        { referenced1Project.Project, true }
                    },
                    targetFrameworks: new[] { targetFramework }
                ).Save()
                .TryBuild("Restore", out var restoreResult, out var restoreOutput)
                .ForceProjectReevaluation()
                .TryBuild("_GetTfmSpecificContentForPackage", out TargetResult result, out var output);

            restoreResult.ShouldBeTrue(() => restoreOutput.GetConsoleLog());
            result.ResultCode.ShouldBe(TargetResultCode.Success, () => output.GetConsoleLog());

            result.Items.ShouldHaveSingleItem();
            result.Items[0].ItemSpec.ShouldBe("TfmContent");
        }

        [Fact]
        public void TfmSpecificContent_In_Non_Embedded_Project_Ignored_By_Target_In_Aggregate()
        {
            var targetFramework = "netstandard2.0";

            var referenced1 = "referenced1";

            var referenced1Project = ProjectCreator
                .Templates
                .SdkCsproj(
                    path: Path.Combine(TestRootPath, referenced1, $"{referenced1}.csproj"),
                    targetFramework: targetFramework
                )
                .ItemInclude("TfmSpecificPackageFile", "TfmContent")
                .Save();

            var sdkProject = ProjectCreator
                .Templates
                .AggregateProject(
                    path: Path.Combine(TestRootPath, "test", "test.csproj"),
                    projectReferences: new Dictionary<Project, bool>
                    {
                        { referenced1Project.Project, false }
                    },
                    targetFrameworks: new[] { targetFramework }
                ).Save()
                .TryBuild("Restore", out var restoreResult, out var restoreOutput)
                .ForceProjectReevaluation()
                .TryBuild("_GetTfmSpecificContentForPackage", out TargetResult result, out var output);

            restoreResult.ShouldBeTrue(() => restoreOutput.GetConsoleLog());
            result.ResultCode.ShouldBe(TargetResultCode.Success, () => output.GetConsoleLog());

            result.Items.ShouldBeEmpty();
        }

        [Fact]
        public void Referenced_Project_Uses_Matching_TargetFramework_If_No_Exact_Match()
        {
            var referenced1 = "referenced1";

            var referenced1Project = ProjectCreator
                .Templates
                .SdkCsproj(
                    path: Path.Combine(TestRootPath, referenced1, $"{referenced1}.csproj"),
                    targetFramework: "netstandard2.0"
                )
                .ItemInclude("TfmSpecificPackageFile", "TfmContent")
                .Save();

            var sdkProject = ProjectCreator
                .Templates
                .AggregateProject(
                    path: Path.Combine(TestRootPath, "test", "test.csproj"),
                    projectReferences: new Dictionary<Project, bool>
                    {
                        { referenced1Project.Project, true }
                    },
                    targetFrameworks: new[] { "netcoreapp2.0" }
                ).Save()
                .TryBuild("Restore", out var restoreResult, out var restoreOutput)
                .ForceProjectReevaluation()
                .TryBuild("_GetTfmSpecificContentForPackage", out TargetResult result, out var output);

            restoreResult.ShouldBeTrue(() => restoreOutput.GetConsoleLog());
            result.ResultCode.ShouldBe(TargetResultCode.Success, () => output.GetConsoleLog());

            result.Items.ShouldHaveSingleItem();
            result.Items[0].ItemSpec.ShouldBe("TfmContent");
            result.Items[0].GetMetadata("TargetFramework").ShouldBe("netcoreapp2.0");
        }
    }
}
