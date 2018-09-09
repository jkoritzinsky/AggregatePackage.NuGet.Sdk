using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using Xunit;
using Shouldly;
using Microsoft.Build.Execution;

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

            var buildResult = RunTarget(sdkProject, "CollectPackageReferences");

            buildResult.ResultsByTarget["CollectPackageReferences"].Items
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

        [Fact]
        public void GetTargetPath_Output_All_Child_Project_Outputs()
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
                    path: Path.Combine(TestRootPath, "test", "test.pkgproj"),
                    projectReferences: new Dictionary<Project, bool>
                    {
                        { referenced1Project.Project, true },
                        { referenced2Project.Project, true }
                    },
                    targetFrameworks: new[] { targetFramework }
                ).Save();

            RunTarget(sdkProject, "Restore");

            BuildResult buildResult = RunTarget(sdkProject, "GetTargetPath");

            var buildOutputs = buildResult.ResultsByTarget["GetTargetPath"].Items
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

        private static BuildResult RunTarget(ProjectCreator sdkProject, string targetName)
        {
            var manager = new BuildManager();
            var logger = BuildOutput.Create();

            var buildResult = manager.Build(
                new BuildParameters
                {
                    Loggers = logger.AsEnumerable()
                },
                new BuildRequestData(
                    sdkProject.Project.CreateProjectInstance(),
                    new[] { targetName }));

            buildResult.OverallResult.ShouldBe(BuildResultCode.Success, () => logger.GetConsoleLog());
            return buildResult;
        }
    }
}
