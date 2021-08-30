using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public async Task ProcessProjects()
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

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(3);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingFilter()
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

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingMultipleFilters()
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

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(3);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingMultipleFiltersInDifferentSolutionFolders()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "Executables",
                            SolutionItems = new List<SolutionItems>
                            {
                                new Projects
                                {
                                    Path = "src/Executables/**"
                                }
                            }
                        },
                        new Projects
                        {
                            Path = "**"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder executablesFolder = solution.Root.SubFolders["Executables"];
                executablesFolder.FilePaths.Should().BeEmpty();
                executablesFolder.Projects.Should().HaveCount(1);
                executablesFolder.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                executablesFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public async Task ProcessProjectsMatchingMultipleFiltersWithOverwrite()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "Libraries",
                            SolutionItems = new List<SolutionItems>
                            {
                                new Projects
                                {
                                    Path = "**"
                                }
                            }
                        },
                        new Folder
                        {
                            Name = "Executables",
                            SolutionItems = new List<SolutionItems>
                            {
                                new Projects
                                {
                                    Path = "src/Executables/**",
                                    Overwrite = true
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(2);
            {
                ISolutionFolder librariesFolder = solution.Root.SubFolders["Libraries"];
                librariesFolder.FilePaths.Should().BeEmpty();
                librariesFolder.Projects.Should().HaveCount(2);
                librariesFolder.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
                librariesFolder.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
                librariesFolder.SubFolders.Should().BeEmpty();

                ISolutionFolder executablesFolder = solution.Root.SubFolders["Executables"];
                executablesFolder.FilePaths.Should().BeEmpty();
                executablesFolder.Projects.Should().HaveCount(1);
                executablesFolder.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                executablesFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public async Task ProcessProjectsWithCreateFolders()
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

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder srcFolder = solution.Root.SubFolders["src"];
                srcFolder.FilePaths.Should().BeEmpty();
                srcFolder.Projects.Should().BeEmpty();
                srcFolder.SubFolders.Should().HaveCount(3);
                {
                    ISolutionFolder myApplicationFolder = srcFolder.SubFolders["MyApplication"];
                    myApplicationFolder.FilePaths.Should().BeEmpty();
                    myApplicationFolder.Projects.Should().HaveCount(1);
                    myApplicationFolder.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
                    myApplicationFolder.SubFolders.Should().BeEmpty();

                    ISolutionFolder myApplicationConfigurationFolder = srcFolder.SubFolders["MyApplication.Configuration"];
                    myApplicationConfigurationFolder.FilePaths.Should().BeEmpty();
                    myApplicationConfigurationFolder.Projects.Should().HaveCount(1);
                    myApplicationConfigurationFolder.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
                    myApplicationConfigurationFolder.SubFolders.Should().BeEmpty();

                    ISolutionFolder executablesFolder = srcFolder.SubFolders["Executables"];
                    executablesFolder.FilePaths.Should().BeEmpty();
                    executablesFolder.Projects.Should().BeEmpty();
                    executablesFolder.SubFolders.Should().HaveCount(1);
                    {
                        ISolutionFolder myApplicationConsoleFolder = executablesFolder.SubFolders["MyApplication.Console"];
                        myApplicationConsoleFolder.FilePaths.Should().BeEmpty();
                        myApplicationConsoleFolder.Projects.Should().HaveCount(1);
                        myApplicationConsoleFolder.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                        myApplicationConsoleFolder.SubFolders.Should().BeEmpty();
                    }
                }
            }
        }
    }
}
