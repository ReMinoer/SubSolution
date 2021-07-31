using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.ProjectPaths.Should().HaveCount(3);
            solution.Root.ProjectPaths.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.ProjectPaths.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.ProjectPaths.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public void ProcessProjectsMatchingMultipleFiltersInDifferentSolutionFolders()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().HaveCount(2);
            solution.Root.ProjectPaths.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.ProjectPaths.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder executablesFolder = solution.Root.SubFolders["Executables"];
                executablesFolder.FilePaths.Should().BeEmpty();
                executablesFolder.ProjectPaths.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                executablesFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public void ProcessProjectsMatchingMultipleFiltersWithOverwrite()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(2);
            {
                ISolutionFolder librariesFolder = solution.Root.SubFolders["Libraries"];
                librariesFolder.FilePaths.Should().BeEmpty();
                librariesFolder.ProjectPaths.Should().HaveCount(2);
                librariesFolder.ProjectPaths.Should().Contain("src/MyApplication/MyApplication.csproj");
                librariesFolder.ProjectPaths.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
                librariesFolder.SubFolders.Should().BeEmpty();

                ISolutionFolder executablesFolder = solution.Root.SubFolders["Executables"];
                executablesFolder.FilePaths.Should().BeEmpty();
                executablesFolder.ProjectPaths.Should().HaveCount(1);
                executablesFolder.ProjectPaths.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                executablesFolder.SubFolders.Should().BeEmpty();
            }
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder srcFolder = solution.Root.SubFolders["src"];
                srcFolder.FilePaths.Should().BeEmpty();
                srcFolder.ProjectPaths.Should().BeEmpty();
                srcFolder.SubFolders.Should().HaveCount(3);
                {
                    ISolutionFolder myApplicationFolder = srcFolder.SubFolders["MyApplication"];
                    myApplicationFolder.FilePaths.Should().BeEmpty();
                    myApplicationFolder.ProjectPaths.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
                    myApplicationFolder.SubFolders.Should().BeEmpty();

                    ISolutionFolder myApplicationConfigurationFolder = srcFolder.SubFolders["MyApplication.Configuration"];
                    myApplicationConfigurationFolder.FilePaths.Should().BeEmpty();
                    myApplicationConfigurationFolder.ProjectPaths.Should().BeEquivalentTo("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
                    myApplicationConfigurationFolder.SubFolders.Should().BeEmpty();

                    ISolutionFolder executablesFolder = srcFolder.SubFolders["Executables"];
                    executablesFolder.FilePaths.Should().BeEmpty();
                    executablesFolder.ProjectPaths.Should().BeEmpty();
                    executablesFolder.SubFolders.Should().HaveCount(1);
                    {
                        ISolutionFolder myApplicationConsoleFolder = executablesFolder.SubFolders["MyApplication.Console"];
                        myApplicationConsoleFolder.FilePaths.Should().BeEmpty();
                        myApplicationConsoleFolder.ProjectPaths.Should().BeEquivalentTo("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                        myApplicationConsoleFolder.SubFolders.Should().BeEmpty();
                    }
                }
            }
        }
    }
}
