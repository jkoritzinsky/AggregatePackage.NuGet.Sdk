using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using NuGet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
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
                ).Save();

            var package = BuildPackage(sdkProject.FullPath, Path.Combine(TestRootPath, "test", "bin", "Debug"));

            Assert.Null(package.FindDependency(referenced1, NetStandard20));
            Assert.Null(package.FindDependency(referenced2, NetStandard20));
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
                ).Save();
            
            var package = BuildPackage(sdkProject.FullPath, Path.Combine(TestRootPath, "test", "bin", "Debug"));
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
                .Save();

            var package = BuildPackage(
                referencingProject.FullPath,
                Path.Combine(TestRootPath, "referencing", "bin", "Debug"),
                "referencing");

            Assert.NotNull(package.FindDependency("test", NetStandard20));
            Assert.Null(package.FindDependency(referenced1, NetStandard20));
            Assert.Null(package.FindDependency(referenced2, NetStandard20));
        }

        private IPackage BuildPackage(string path, string packageOutputDir, string packageId = "test")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"pack {path}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            process.WaitForExit(100000);

            var repository = new LocalPackageRepository(packageOutputDir);

            return repository.FindPackage(packageId);
        }
    }
}
