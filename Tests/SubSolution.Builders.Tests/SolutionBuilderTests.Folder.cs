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
        public async Task ProcessFolderEmpty()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "MySolutionFolder"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessFoldersEmbedded()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "FirstFolder",
                            SolutionItems =
                            {
                                new Folder
                                {
                                    Name = "SecondFolder",
                                    SolutionItems =
                                    {
                                        new Folder
                                        {
                                            Name = "ThirdFolder",
                                            SolutionItems =
                                            {
                                                new Projects { Path = "**/MyApplication.csproj" }
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
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder firstFolder = solution.Root.SubFolders["FirstFolder"];
                firstFolder.FilePaths.Should().BeEmpty();
                firstFolder.Projects.Should().BeEmpty();
                firstFolder.SubFolders.Should().HaveCount(1);
                {
                    ISolutionFolder secondFolder = firstFolder.SubFolders["SecondFolder"];
                    secondFolder.FilePaths.Should().BeEmpty();
                    secondFolder.Projects.Keys.Should().BeEmpty();
                    secondFolder.SubFolders.Should().HaveCount(1);
                    {
                        ISolutionFolder thirdFolder = secondFolder.SubFolders["ThirdFolder"];
                        thirdFolder.FilePaths.Should().BeEmpty();
                        thirdFolder.Projects.Keys.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
                        thirdFolder.SubFolders.Should().BeEmpty();
                    }
                }
            }
        }

        [Test]
        public async Task ProcessRootWithCollapseFoldersWithUniqueSubFolder()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueSubFolder = true,
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "FirstFolder",
                            SolutionItems =
                            {
                                new Folder
                                {
                                    Name = "SecondFolder",
                                    SolutionItems =
                                    {
                                        new Folder
                                        {
                                            Name = "ThirdFolder",
                                            SolutionItems =
                                            {
                                                new Projects { Path = "**/MyApplication.csproj" }
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
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder thirdFolder = solution.Root.SubFolders["ThirdFolder"];
                thirdFolder.FilePaths.Should().BeEmpty();
                thirdFolder.Projects.Keys.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
                thirdFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public async Task ProcessFolderWithCollapseFoldersWithUniqueSubFolder()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "FirstFolder",
                            CollapseFoldersWithUniqueSubFolder = true,
                            SolutionItems =
                            {
                                new Folder
                                {
                                    Name = "SecondFolder",
                                    SolutionItems =
                                    {
                                        new Folder
                                        {
                                            Name = "ThirdFolder",
                                            SolutionItems =
                                            {
                                                new Projects { Path = "**/MyApplication.csproj" }
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
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder firstFolder = solution.Root.SubFolders["FirstFolder"];
                firstFolder.FilePaths.Should().BeEmpty();
                firstFolder.Projects.Should().BeEmpty();
                firstFolder.SubFolders.Should().HaveCount(1);
                {
                    ISolutionFolder thirdFolder = firstFolder.SubFolders["ThirdFolder"];
                    thirdFolder.FilePaths.Should().BeEmpty();
                    thirdFolder.Projects.Keys.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
                    thirdFolder.SubFolders.Should().BeEmpty();
                }
            }
        }

        [Test]
        public async Task ProcessRootWithCollapseFoldersWithUniqueItem()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueItem = true,
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "FirstFolder",
                            SolutionItems =
                            {
                                new Folder
                                {
                                    Name = "SecondFolder",
                                    SolutionItems =
                                    {
                                        new Folder
                                        {
                                            Name = "ThirdFolder",
                                            SolutionItems =
                                            {
                                                new Projects { Path = "**/MyApplication.csproj" }
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
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder firstFolder = solution.Root.SubFolders["FirstFolder"];
                firstFolder.FilePaths.Should().BeEmpty();
                firstFolder.Projects.Should().BeEmpty();
                firstFolder.SubFolders.Should().HaveCount(1);
                {
                    ISolutionFolder secondFolder = firstFolder.SubFolders["SecondFolder"];
                    secondFolder.FilePaths.Should().BeEmpty();
                    secondFolder.Projects.Keys.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
                    secondFolder.SubFolders.Should().BeEmpty();
                }
            }
        }

        [Test]
        public async Task ProcessFolderWithCollapseFoldersWithUniqueItem()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "FirstFolder",
                            CollapseFoldersWithUniqueItem = true,
                            SolutionItems =
                            {
                                new Folder
                                {
                                    Name = "SecondFolder",
                                    SolutionItems =
                                    {
                                        new Folder
                                        {
                                            Name = "ThirdFolder",
                                            SolutionItems =
                                            {
                                                new Projects { Path = "**/MyApplication.csproj" }
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
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder firstFolder = solution.Root.SubFolders["FirstFolder"];
                firstFolder.FilePaths.Should().BeEmpty();
                firstFolder.Projects.Should().BeEmpty();
                firstFolder.SubFolders.Should().HaveCount(1);
                {
                    ISolutionFolder secondFolder = firstFolder.SubFolders["SecondFolder"];
                    secondFolder.FilePaths.Should().BeEmpty();
                    secondFolder.Projects.Keys.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
                    secondFolder.SubFolders.Should().BeEmpty();
                }
            }
        }

        [Test]
        public async Task ProcessRootWithCollapseFoldersWithUniqueSubFolderOrItem()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueSubFolder = true,
                    CollapseFoldersWithUniqueItem = true,
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "FirstFolder",
                            SolutionItems =
                            {
                                new Folder
                                {
                                    Name = "SecondFolder",
                                    SolutionItems =
                                    {
                                        new Folder
                                        {
                                            Name = "ThirdFolder",
                                            SolutionItems =
                                            {
                                                new Projects { Path = "**/MyApplication.csproj" }
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
            solution.Root.Projects.Keys.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessFolderWithCollapseFoldersWithUniqueSubFolderOrItem()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "FirstFolder",
                            CollapseFoldersWithUniqueSubFolder = true,
                            CollapseFoldersWithUniqueItem = true,
                            SolutionItems =
                            {
                                new Folder
                                {
                                    Name = "SecondFolder",
                                    SolutionItems =
                                    {
                                        new Folder
                                        {
                                            Name = "ThirdFolder",
                                            SolutionItems =
                                            {
                                                new Projects { Path = "**/MyApplication.csproj" }
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
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder firstFolder = solution.Root.SubFolders["FirstFolder"];
                firstFolder.FilePaths.Should().BeEmpty();
                firstFolder.Projects.Keys.Should().BeEquivalentTo("src/MyApplication/MyApplication.csproj");
                firstFolder.SubFolders.Should().BeEmpty();
            }
        }
    }
}
