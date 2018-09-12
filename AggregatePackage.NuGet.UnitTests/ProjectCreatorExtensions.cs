using Microsoft.Build.Utilities.ProjectCreation;
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
    }
}
