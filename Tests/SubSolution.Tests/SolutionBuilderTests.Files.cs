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
        public async Task ProcessFilesMatchingPath()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(3);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
            solution.Root.FilePaths.Should().Contain("tools/pull.bat");
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
        }

        [Test]
        public async Task ProcessFilesMatchingMultiplePaths()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**"
                        },
                        new Files
                        {
                            Path = "**/*.bat"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(3);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
            solution.Root.FilePaths.Should().Contain("tools/pull.bat");
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
        }

        [Test]
        public async Task ProcessFilesMatchingMultiplePathsInDifferentSolutionFolders()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "Batch",
                            SolutionItems = new List<SolutionItems>
                            {
                                new Files
                                {
                                    Path = "tools/**/*.bat"
                                }
                            }
                        },
                        new Files
                        {
                            Path = "tools/**"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.Projects.Should().BeEmpty();
            solution.Root.FilePaths.Should().HaveCount(1);
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder batchFolder = solution.Root.SubFolders["Batch"];
                batchFolder.Projects.Should().BeEmpty();
                batchFolder.FilePaths.Should().HaveCount(2);
                batchFolder.FilePaths.Should().Contain("tools/submit.bat");
                batchFolder.FilePaths.Should().Contain("tools/pull.bat");
                batchFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public async Task ProcessFilesMatchingMultiplePathsWithOverwrite()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "Tools",
                            SolutionItems = new List<SolutionItems>
                            {
                                new Files
                                {
                                    Path = "tools/**"
                                }
                            }
                        },
                        new Folder
                        {
                            Name = "Batch",
                            SolutionItems = new List<SolutionItems>
                            {
                                new Files
                                {
                                    Path = "**/*.bat",
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
                ISolutionFolder toolsFolder = solution.Root.SubFolders["Tools"];
                toolsFolder.FilePaths.Should().HaveCount(1);
                toolsFolder.FilePaths.Should().Contain("tools/debug/Debug.exe");
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();

                ISolutionFolder batchFolder = solution.Root.SubFolders["Batch"];
                batchFolder.FilePaths.Should().HaveCount(2);
                batchFolder.FilePaths.Should().Contain("tools/submit.bat");
                batchFolder.FilePaths.Should().Contain("tools/pull.bat");
                batchFolder.Projects.Should().BeEmpty();
                batchFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public async Task ProcessFilesWithCreateFolders()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
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
                ISolutionFolder toolsFolder = solution.Root.SubFolders["tools"];
                toolsFolder.FilePaths.Should().HaveCount(2);
                toolsFolder.FilePaths.Should().Contain("tools/submit.bat");
                toolsFolder.FilePaths.Should().Contain("tools/pull.bat");
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().HaveCount(1);
                {
                    ISolutionFolder debugFolder = toolsFolder.SubFolders["debug"];
                    debugFolder.FilePaths.Should().BeEquivalentTo("tools/debug/Debug.exe");
                    debugFolder.Projects.Should().BeEmpty();
                    debugFolder.SubFolders.Should().BeEmpty();
                }
            }
        }

        [Test]
        public async Task ProcessFilesWithCreateFoldersAndCollapseFoldersWithUniqueSubFolder()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueSubFolder = true,
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
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
                ISolutionFolder toolsFolder = solution.Root.SubFolders["tools"];
                toolsFolder.FilePaths.Should().HaveCount(2);
                toolsFolder.FilePaths.Should().Contain("tools/submit.bat");
                toolsFolder.FilePaths.Should().Contain("tools/pull.bat");
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().HaveCount(1);
                {
                    ISolutionFolder debugFolder = toolsFolder.SubFolders["debug"];
                    debugFolder.FilePaths.Should().BeEquivalentTo("tools/debug/Debug.exe");
                    debugFolder.Projects.Should().BeEmpty();
                    debugFolder.SubFolders.Should().BeEmpty();
                }
            }
        }

        [Test]
        public async Task ProcessFilesWithCreateFoldersAndCollapseFoldersWithUniqueItem()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueItem = true,
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
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
                ISolutionFolder toolsFolder = solution.Root.SubFolders["tools"];
                toolsFolder.FilePaths.Should().HaveCount(3);
                toolsFolder.FilePaths.Should().Contain("tools/submit.bat");
                toolsFolder.FilePaths.Should().Contain("tools/pull.bat");
                toolsFolder.FilePaths.Should().Contain("tools/debug/Debug.exe");
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public async Task ProcessFilesWithCreateFoldersAndCollapseFoldersWithUniqueSubFolderOrItem()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    CollapseFoldersWithUniqueSubFolder = true,
                    CollapseFoldersWithUniqueItem = true,
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
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
                ISolutionFolder toolsFolder = solution.Root.SubFolders["tools"];
                toolsFolder.FilePaths.Should().HaveCount(3);
                toolsFolder.FilePaths.Should().Contain("tools/submit.bat");
                toolsFolder.FilePaths.Should().Contain("tools/pull.bat");
                toolsFolder.FilePaths.Should().Contain("tools/debug/Debug.exe");
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();
            }
        }
    }
}
