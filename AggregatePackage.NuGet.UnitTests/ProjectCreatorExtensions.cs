using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;

namespace AggregatePackage.NuGet.UnitTests
{
    static class ProjectCreatorExtensions
    {
        public static ProjectCreator ForceProjectReevaluation(this ProjectCreator sdkProject)
        {
            return sdkProject
                .ItemGroup()
                .Save();
        }

        public static ProjectCreator TryBuild(this ProjectCreator sdkProject, string targetName, out TargetResult result, out BuildOutput output)
        {
            lock (BuildManager.DefaultBuildManager)
            {
                output = BuildOutput.Create();

                var buildResult = BuildManager.DefaultBuildManager.Build(
                    new BuildParameters
                    {
                        Loggers = output.AsEnumerable()
                    },
                    new BuildRequestData(
                        sdkProject.Project.CreateProjectInstance(),
                        new[] { targetName }));

                result = buildResult.ResultsByTarget[targetName];
                return sdkProject;
            }
        }
    }
}
