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
                ).Save();

            var targetResult = GetTargetOutputs(sdkProject, "CollectPackageReferences");

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

            var targetResult = GetTargetOutputs(sdkProject, target);

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

        private static TargetResult GetTargetOutputs(ProjectCreator sdkProject, string targetName)
        {
            lock (BuildManager.DefaultBuildManager)
            {
                var logger = BuildOutput.Create();

                var buildResult = BuildManager.DefaultBuildManager.Build(
                    new BuildParameters
                    {
                        Loggers = logger.AsEnumerable()
                    },
                    new BuildRequestData(
                        sdkProject.Project.CreateProjectInstance(),
                        new[] { targetName }));

                buildResult.OverallResult.ShouldBe(BuildResultCode.Success, () => logger.GetConsoleLog());
                return buildResult.ResultsByTarget[targetName]; 
            }
        }
    }
}
