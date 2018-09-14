// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

// From the Microsoft/MSBuildSdks repository on GitHub.

using Microsoft.Build.Locator;

namespace AggregatePackage.NuGet.UnitTests
{
    public abstract class MSBuildTestBase
    {
        public static readonly VisualStudioInstance CurrentVisualStudioInstance = MSBuildLocator.RegisterDefaults();

        protected MSBuildTestBase()
        {
            MSBuildPath = CurrentVisualStudioInstance.MSBuildPath;
        }

        protected string MSBuildPath { get; }
    }
}