using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using NuGet;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using Xunit;

namespace AggregatePackage.NuGet.UnitTests
{
    public class PackTests : MSBuildSdkTestBase
    {
        private static readonly FrameworkName NetStandard20 = new FrameworkName("NETStandard", new Version(2, 0));

        [Fact]
        public void Packed_Package_Doesnt_Depend_On_Embedded_Projects()
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
                .ForceProjectReevaluation()
                .TryBuild("Pack", out var result, out var output);

            restoreResult.ShouldBeTrue(() => restoreOutput.GetConsoleLog());
            result.ShouldBeTrue(() => output.GetConsoleLog());

            var package = ReadPackage("test");

            package.FindDependency(referenced1, NetStandard20).ShouldBeNull();
            package.FindDependency(referenced2, NetStandard20).ShouldBeNull();
        }

        [Fact]
        public void Packed_Package_Does_Depend_On_Non_Embedded_Projects()
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
                        { referenced2Project.Project, false }
                    },
                    targetFrameworks: new[] { targetFramework }
                ).Save()
                .TryBuild("Restore", out var restoreResult, out var restoreOutput)
                .ForceProjectReevaluation()
                .TryBuild("Pack", out var result, out var output);

            var package = ReadPackage("test");
            Assert.Null(package.FindDependency(referenced1, NetStandard20));
            Assert.NotNull(package.FindDependency(referenced2, NetStandard20));
        }

        [Fact]
        public void Package_Referencing_Aggregating_Package_Does_Not_Depend_On_Embedded_Projects()
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
                ).Save();

            var referencingProject = ProjectCreator
                .Templates
                .SdkCsproj(
                    path: Path.Combine(TestRootPath, "referencing", "referencing.csproj"),
                    targetFramework: targetFramework)
                .ItemProjectReference(sdkProject.Project)
                .Save()
                .TryBuild("Restore", out var restoreResult, out var restoreOutput)
                .ForceProjectReevaluation()
                .TryBuild("Pack", out var result, out var output);

            var package = ReadPackage("referencing");

            Assert.NotNull(package.FindDependency("test", NetStandard20));
            Assert.Null(package.FindDependency(referenced1, NetStandard20));
            Assert.Null(package.FindDependency(referenced2, NetStandard20));
        }

        private IPackage ReadPackage(string packageName)
        {
            var path = Path.Combine(TestRootPath, packageName, "bin/Debug");
            
            var repository = new LocalPackageRepository(path);

            return repository.FindPackage(packageName);
        }
    }
}
