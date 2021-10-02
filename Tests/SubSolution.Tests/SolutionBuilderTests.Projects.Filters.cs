using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public async Task ProcessProjectsMatchingEmptyFilter()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Where = new ProjectFilterRoot()
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Keys.Should().HaveCount(4);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingFilterPath()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters = new List<ProjectFilters>
                                {
                                    new ProjectPath
                                    {
                                        Match = "src/*/"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(3);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingFilterNot()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters = new List<ProjectFilters>
                                {
                                    new ProjectNot
                                    {
                                        ProjectFilters = new ProjectPath
                                        {
                                            Match = "src/*/"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingFilterAll()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters = new List<ProjectFilters>
                                {
                                    new ProjectMatchAll
                                    {
                                        ProjectFilters =  new List<ProjectFilters>
                                        {
                                            new ProjectPath
                                            {
                                                Match = "src/*/"
                                            },
                                            new ProjectPath
                                            {
                                                Match = "**/*.Configuration.*"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingFilterAnyOf()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters = new List<ProjectFilters>
                                {
                                    new ProjectMatchAnyOf
                                    {
                                        ProjectFilters =  new List<ProjectFilters>
                                        {
                                            new ProjectPath
                                            {
                                                Match = "**/*.Configuration.*"
                                            },
                                            new ProjectPath
                                            {
                                                Match = "**/*.Console.*"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }
    }
}