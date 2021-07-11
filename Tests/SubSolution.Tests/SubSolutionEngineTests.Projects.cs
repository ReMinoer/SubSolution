using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders;
using SubSolution.Configuration;

namespace SubSolution.Tests
{
    public partial class SubSolutionEngineTests
    {
        [Test]
        public void ProcessProjects()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                }
            };

            MemorySolutionBuilder solution = Process(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.ProjectPaths.Should().HaveCount(3);
            solution.Root.ProjectPaths.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.ProjectPaths.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.ProjectPaths.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public void ProcessProjectsMatchingFilter()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Path = "src/Executables/**"
                        }
                    }
                }
            };

            MemorySolutionBuilder solution = Process(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.ProjectPaths.Should().HaveCount(1);
            solution.Root.ProjectPaths.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public void ProcessProjectsMatchingMultipleFilters()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Path = "src/Executables/**"
                        },
                        new Projects
                        {
                            Path = "**"
                        }
                    }
                }
            };

            MemorySolutionBuilder solution = Process(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.ProjectPaths.Should().HaveCount(3);
            solution.Root.ProjectPaths.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.ProjectPaths.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.ProjectPaths.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public void ProcessProjectsWithCreateFolders()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            CreateFolders = true
                        }
                    }
                }
            };

            MemorySolutionBuilder solution = Process(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                var srcFolder = solution.Root.SubFolders.Should().ContainKey("src").WhichValue;
                srcFolder.FilePaths.Should().BeEmpty();
                srcFolder.ProjectPaths.Should().BeEmpty();
                srcFolder.SubFolders.Should().HaveCount(3);
                {
                    var myApplicationFolder = srcFolder.SubFolders.Should().ContainKey("MyApplication").WhichValue;
                    myApplicationFolder.FilePaths.Should().BeEmpty();
                    myApplicationFolder.ProjectPaths.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
                    myApplicationFolder.SubFolders.Should().BeEmpty();

                    var myApplicationConfigurationFolder = srcFolder.SubFolders.Should().ContainKey("MyApplication.Configuration").WhichValue;
                    myApplicationConfigurationFolder.FilePaths.Should().BeEmpty();
                    myApplicationConfigurationFolder.ProjectPaths.Should().BeEquivalentTo("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
                    myApplicationConfigurationFolder.SubFolders.Should().BeEmpty();

                    var executablesFolder = srcFolder.SubFolders.Should().ContainKey("Executables").WhichValue;
                    executablesFolder.FilePaths.Should().BeEmpty();
                    executablesFolder.ProjectPaths.Should().BeEmpty();
                    executablesFolder.SubFolders.Should().HaveCount(1);
                    {
                        var myApplicationConsoleFolder = executablesFolder.SubFolders.Should().ContainKey("MyApplication.Console").WhichValue;
                        myApplicationConsoleFolder.FilePaths.Should().BeEmpty();
                        myApplicationConsoleFolder.ProjectPaths.Should().BeEquivalentTo("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                        myApplicationConsoleFolder.SubFolders.Should().BeEmpty();
                    }
                }
            }
        }
    }
}
