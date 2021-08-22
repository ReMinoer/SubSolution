using System;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;
using static FluentAssertions.FluentActions;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public void ProcessEmptyConfiguration()
        {
            var configuration = new SubSolutionConfiguration();
            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);
            
            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public void ProcessWithSolutionName()
        {
            var configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName"
            };

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);
            solution.OutputPath.Should().EndWith("MyCustomSolutionName.sln");

            configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName.sln"
            };

            solution = ProcessConfigurationMockFile(configuration);
            solution.OutputPath.Should().EndWith("MyCustomSolutionName.sln");

            configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName.txt"
            };

            solution = ProcessConfigurationMockFile(configuration);
            solution.OutputPath.Should().EndWith("MyCustomSolutionName.txt.sln");
        }

        [Test]
        public void ProcessWithOutput()
        {
            var configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName",
                OutputDirectory = "MySolutions/MyCustomSolutions"
            };

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.OutputPath.Should().EndWith("MySolutions/MyCustomSolutions/MyCustomSolutionName.sln");
            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public void ProcessWithConfigurationWorkspaceDirectory()
        {
            var configuration = new SubSolutionConfiguration
            {
                WorkspaceDirectory = WorkspaceDirectoryPath
            };

            ISolutionOutput solution = ProcessConfiguration(configuration, workspaceDirectoryPath: null);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public void ThrowOnProcessWithoutAnyWorkspaceDirectory()
        {
            var configuration = new SubSolutionConfiguration();
            Invoking(() => ProcessConfiguration(configuration, workspaceDirectoryPath: null)).Should().Throw<ArgumentNullException>();
        }
    }
}
