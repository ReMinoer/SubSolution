using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders.Configuration;

namespace SubSolution.Builders.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public async Task ProcessProjects()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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

            solution.Root.Projects.Should().HaveCount(4);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingPath()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
        public async Task ProcessProjectsMatchingPathAbsolute()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Path = @"C:\Directory\SubDirectory\MyWorkspace\src\Executables\MyApplication.Console\MyApplication.Console.csproj"
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
        public async Task ProcessProjectsMatchingPathAbsoluteFromOtherRoot()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            Path = @"D:\Directory\ExternalProject.csproj"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain(@"D:\Directory\ExternalProject.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingMultiplePath()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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

            solution.Root.Projects.Should().HaveCount(4);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessProjectsMatchingMultiplePathsInDifferentSolutionFolders()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            solution.Root.Projects.Should().HaveCount(3);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
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
        public async Task ProcessProjectsMatchingMultiplePathsWithOverwrite()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
                librariesFolder.Projects.Should().HaveCount(3);
                librariesFolder.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
                librariesFolder.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
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
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
                srcFolder.SubFolders.Should().HaveCount(4);
                {
                    ISolutionFolder myApplicationFolder = srcFolder.SubFolders["MyApplication"];
                    myApplicationFolder.FilePaths.Should().BeEmpty();
                    myApplicationFolder.Projects.Should().HaveCount(1);
                    myApplicationFolder.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
                    myApplicationFolder.SubFolders.Should().BeEmpty();

                    ISolutionFolder myApplicationCoreFolder = srcFolder.SubFolders["MyApplication.Core"];
                    myApplicationCoreFolder.FilePaths.Should().BeEmpty();
                    myApplicationCoreFolder.Projects.Should().HaveCount(1);
                    myApplicationCoreFolder.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
                    myApplicationCoreFolder.SubFolders.Should().BeEmpty();

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

        [Test]
        public async Task ProcessProjectsWithCreateFoldersAndCollapseFoldersWithUniqueSubFolder()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueSubFolder = true,
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
                srcFolder.SubFolders.Should().HaveCount(4);
                {
                    ISolutionFolder myApplicationFolder = srcFolder.SubFolders["MyApplication"];
                    myApplicationFolder.FilePaths.Should().BeEmpty();
                    myApplicationFolder.Projects.Should().HaveCount(1);
                    myApplicationFolder.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
                    myApplicationFolder.SubFolders.Should().BeEmpty();

                    ISolutionFolder myApplicationCoreFolder = srcFolder.SubFolders["MyApplication.Core"];
                    myApplicationCoreFolder.FilePaths.Should().BeEmpty();
                    myApplicationCoreFolder.Projects.Should().HaveCount(1);
                    myApplicationCoreFolder.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
                    myApplicationCoreFolder.SubFolders.Should().BeEmpty();

                    ISolutionFolder myApplicationConfigurationFolder = srcFolder.SubFolders["MyApplication.Configuration"];
                    myApplicationConfigurationFolder.FilePaths.Should().BeEmpty();
                    myApplicationConfigurationFolder.Projects.Should().HaveCount(1);
                    myApplicationConfigurationFolder.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
                    myApplicationConfigurationFolder.SubFolders.Should().BeEmpty();

                    ISolutionFolder myApplicationConsoleFolder = srcFolder.SubFolders["MyApplication.Console"];
                    myApplicationConsoleFolder.FilePaths.Should().BeEmpty();
                    myApplicationConsoleFolder.Projects.Should().HaveCount(1);
                    myApplicationConsoleFolder.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                    myApplicationConsoleFolder.SubFolders.Should().BeEmpty();
                }
            }
        }

        [Test]
        public async Task ProcessProjectsWithCreateFoldersAndCollapseFoldersWithUniqueItem()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueItem = true,
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            CreateFolders = true,
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
                srcFolder.Projects.Should().HaveCount(3);
                srcFolder.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
                srcFolder.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
                srcFolder.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
                srcFolder.SubFolders.Should().HaveCount(1);
                {
                    ISolutionFolder executablesFolder = srcFolder.SubFolders["Executables"];
                    executablesFolder.FilePaths.Should().BeEmpty();
                    executablesFolder.Projects.Should().HaveCount(1);
                    executablesFolder.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                    executablesFolder.SubFolders.Should().BeEmpty();
                }
            }
        }

        [Test]
        public async Task ProcessProjectsWithCreateFoldersAndCollapseFoldersWithUniqueSubFolderOrItem()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueSubFolder = true,
                    CollapseFoldersWithUniqueItem = true,
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects
                        {
                            CreateFolders = true,
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
                srcFolder.Projects.Should().HaveCount(4);
                srcFolder.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
                srcFolder.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
                srcFolder.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
                srcFolder.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
                srcFolder.SubFolders.Should().BeEmpty();
            }
        }
    }
}
