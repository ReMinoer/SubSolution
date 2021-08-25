using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;
using static FluentAssertions.FluentActions;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public async Task ProcessEmptyConfiguration()
        {
            var configuration = new SubSolutionConfiguration();
            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration);
            
            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessWithSolutionName()
        {
            var configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName"
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration);
            solution.OutputPath.Should().EndWith("MyCustomSolutionName.sln");

            configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName.sln"
            };

            solution = await ProcessConfigurationMockFileAsync(configuration);
            solution.OutputPath.Should().EndWith("MyCustomSolutionName.sln");

            configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName.txt"
            };

            solution = await ProcessConfigurationMockFileAsync(configuration);
            solution.OutputPath.Should().EndWith("MyCustomSolutionName.txt.sln");
        }

        [Test]
        public async Task ProcessWithOutput()
        {
            var configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName",
                OutputDirectory = "MySolutions/MyCustomSolutions"
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.OutputPath.Should().EndWith("MySolutions/MyCustomSolutions/MyCustomSolutionName.sln");
            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessWithConfigurationWorkspaceDirectory()
        {
            var configuration = new SubSolutionConfiguration
            {
                WorkspaceDirectory = WorkspaceDirectoryPath
            };

            ISolutionOutput solution = await ProcessConfigurationAsync(configuration, workspaceDirectoryPath: null);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public void ThrowOnProcessWithoutAnyWorkspaceDirectory()
        {
            var configuration = new SubSolutionConfiguration();
            Invoking(async () => await ProcessConfigurationAsync(configuration, workspaceDirectoryPath: null)).Should().Throw<ArgumentNullException>();
        }
    }
}
