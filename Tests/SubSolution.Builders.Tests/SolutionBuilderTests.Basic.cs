using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders.Configuration;
using static FluentAssertions.FluentActions;

namespace SubSolution.Builders.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public async Task ProcessEmptyConfiguration()
        {
            var configuration = new Subsln();
            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);
            
            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessWithSolutionName()
        {
            var configuration = new Subsln
            {
                SolutionName = "MyCustomSolutionName"
            };

            SolutionBuilderContext context = await GetConfigurationMockFileContextAsync(configuration);
            
            context.SolutionPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace\MyCustomSolutionName.sln");
            context.SolutionDirectoryPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace");
            context.SolutionName.Should().Be("MyCustomSolutionName");

            configuration = new Subsln
            {
                SolutionName = "MyCustomSolutionName.sln"
            };

            context = await GetConfigurationMockFileContextAsync(configuration);

            context.SolutionPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace\MyCustomSolutionName.sln");
            context.SolutionDirectoryPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace");
            context.SolutionName.Should().Be("MyCustomSolutionName");

            configuration = new Subsln
            {
                SolutionName = "MyCustomSolutionName.txt"
            };

            context = await GetConfigurationMockFileContextAsync(configuration);

            context.SolutionPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace\MyCustomSolutionName.txt.sln");
            context.SolutionDirectoryPath.Should().Be(@"C:\Directory\SubDirectory\MyWorkspace");
            context.SolutionName.Should().Be("MyCustomSolutionName.txt");
        }

        [Test]
        public async Task ProcessWithOutput()
        {
            var configuration = new Subsln
            {
                SolutionName = "MyCustomSolutionName",
                OutputDirectory = @"C:\MySolutions\MyCustomSolutions"
            };

            SolutionBuilderContext context = await GetConfigurationMockFileContextAsync(configuration);

            context.SolutionPath.Should().Be(@"C:\MySolutions\MyCustomSolutions\MyCustomSolutionName.sln");
            context.SolutionDirectoryPath.Should().Be(@"C:\MySolutions\MyCustomSolutions");
            context.SolutionName.Should().Be("MyCustomSolutionName");
        }

        [Test]
        public async Task ProcessWithConfigurationWorkspaceDirectory()
        {
            var configuration = new Subsln
            {
                WorkspaceDirectory = WorkspaceDirectoryPath
            };

            ISolution solution = await ProcessConfigurationAsync(configuration, Environment.CurrentDirectory, workspaceDirectoryPath: null);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public async Task ThrowOnProcessWithoutAnyWorkspaceDirectory()
        {
            var configuration = new Subsln();
            await Invoking(async () => await ProcessConfigurationAsync(configuration, Environment.CurrentDirectory, workspaceDirectoryPath: null))
                .Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
