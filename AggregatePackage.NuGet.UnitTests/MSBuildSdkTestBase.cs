// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

// From the Microsoft/MSBuildSdks repository on GitHub.

using System;
using System.IO;

namespace AggregatePackage.NuGet.UnitTests
{
    public abstract class MSBuildSdkTestBase : MSBuildTestBase, IDisposable
    {
        private static readonly string TestAssemblyPathValue = typeof(MSBuildSdkTestBase).Assembly.ManifestModule.FullyQualifiedName;

        private readonly string _testRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public string TestAssemblyPath => TestAssemblyPathValue;

        public string TestRootPath
        {
            get
            {
                Directory.CreateDirectory(_testRootPath);
                return _testRootPath;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Directory.Exists(TestRootPath))
                {
                    Directory.Delete(TestRootPath, recursive: true);
                }
            }
        }

        protected string GetTempFile(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return Path.Combine(TestRootPath, name);
        }

        protected string GetTempFileWithExtension(string extension = null)
        {
            return Path.Combine(TestRootPath, $"{Path.GetRandomFileName()}{extension ?? String.Empty}");
        }
    }
}