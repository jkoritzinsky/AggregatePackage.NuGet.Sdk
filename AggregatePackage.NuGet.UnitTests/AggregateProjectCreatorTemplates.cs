using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using System;
using System.Collections.Generic;
using System.IO;

namespace AggregatePackage.NuGet.UnitTests
{
    public static class AggregateProjectCreatorTemplates
    {
        public static ProjectCreator AggregateProject(
            this ProjectCreatorTemplates templates,
            string path = null,
            string defaultTargets = null,
            string initialTargets = null,
            string sdk = null,
            string toolsVersion = null,
            string treatAsLocalProperty = null,
            ProjectCollection projectCollection = null,
            NewProjectFileOptions? projectFileOptions = NewProjectFileOptions.IncludeXmlDeclaration | NewProjectFileOptions.IncludeXmlNamespace,
            IReadOnlyDictionary<Project, bool> projectReferences = null,
            IReadOnlyList<string> targetFrameworks = null)
        {
            string currentDirectory = Environment.CurrentDirectory;

            return ProjectCreator.Create(
                path,
                defaultTargets,
                initialTargets,
                sdk,
                toolsVersion,
                treatAsLocalProperty,
                projectCollection,
                projectFileOptions)
                .Import(Path.Combine(currentDirectory, "Sdk", "Sdk.props"))
                .ForEach(projectReferences, (projectReference, project) =>
                    project.ItemProjectReference(projectReference.Key,
                        metadata: new Dictionary<string, string>
                        {
                            { "EmbedReference", projectReference.Value.ToString() }
                        }))
                .Property(
                    targetFrameworks?.Count > 1 ? "TargetFrameworks" : "TargetFramework",
                    string.Join(";", targetFrameworks ?? new[] { "netstandard2.0" }))
                .Import(Path.Combine(currentDirectory, "Sdk", "Sdk.targets"));
        }
    }
}