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
            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);
            
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

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);
            solution.OutputPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace\MyCustomSolutionName.sln");
            solution.OutputDirectory.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace");
            solution.SolutionName.Should().Be("MyCustomSolutionName");

            configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName.sln"
            };

            solution = await ProcessConfigurationMockFileAsync(configuration);
            solution.OutputPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace\MyCustomSolutionName.sln");
            solution.OutputDirectory.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace");
            solution.SolutionName.Should().Be("MyCustomSolutionName");

            configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName.txt"
            };

            solution = await ProcessConfigurationMockFileAsync(configuration);
            solution.OutputPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace\MyCustomSolutionName.txt.sln");
            solution.OutputDirectory.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace");
            solution.SolutionName.Should().Be("MyCustomSolutionName.txt");
        }

        [Test]
        public async Task ProcessWithOutput()
        {
            var configuration = new SubSolutionConfiguration
            {
                SolutionName = "MyCustomSolutionName",
                OutputDirectory = @"C:\MySolutions\MyCustomSolutions"
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.OutputPath.Should().Be(@"C:\MySolutions\MyCustomSolutions\MyCustomSolutionName.sln");
            solution.OutputDirectory.Should().Be(@"C:\MySolutions\MyCustomSolutions");
            solution.SolutionName.Should().Be("MyCustomSolutionName");

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

            ISolution solution = await ProcessConfigurationAsync(configuration, Environment.CurrentDirectory, workspaceDirectoryPath: null);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public void ThrowOnProcessWithoutAnyWorkspaceDirectory()
        {
            var configuration = new SubSolutionConfiguration();
            Invoking(async () => await ProcessConfigurationAsync(configuration, Environment.CurrentDirectory, workspaceDirectoryPath: null)).Should().Throw<ArgumentNullException>();
        }
    }
}
