using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public void ProcessFilesMatchingFilter()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(2);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
        }

        [Test]
        public void ProcessFilesMatchingMultipleFilters()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(2);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
        }

        [Test]
        public void ProcessFilesMatchingMultipleFiltersInDifferentSolutionFolders()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.FilePaths.Should().HaveCount(1);
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder batchFolder = solution.Root.SubFolders["Batch"];
                batchFolder.ProjectPaths.Should().BeEmpty();
                batchFolder.FilePaths.Should().HaveCount(1);
                batchFolder.FilePaths.Should().Contain("tools/submit.bat");
                batchFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public void ProcessFilesMatchingMultipleFiltersWithOverwrite()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(2);
            {
                ISolutionFolder toolsFolder = solution.Root.SubFolders["Tools"];
                toolsFolder.FilePaths.Should().BeEquivalentTo("tools/debug/Debug.exe");
                toolsFolder.ProjectPaths.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();

                ISolutionFolder batchFolder = solution.Root.SubFolders["Batch"];
                batchFolder.FilePaths.Should().BeEquivalentTo("tools/submit.bat");
                batchFolder.ProjectPaths.Should().BeEmpty();
                batchFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test]
        public void ProcessFilesWithCreateFolders()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder toolsFolder = solution.Root.SubFolders["tools"];
                toolsFolder.FilePaths.Should().BeEquivalentTo("tools/submit.bat");
                toolsFolder.ProjectPaths.Should().BeEmpty();
                toolsFolder.SubFolders.Should().HaveCount(1);
                {
                    ISolutionFolder debugFolder = toolsFolder.SubFolders["debug"];
                    debugFolder.FilePaths.Should().BeEquivalentTo("tools/debug/Debug.exe");
                    debugFolder.ProjectPaths.Should().BeEmpty();
                    debugFolder.SubFolders.Should().BeEmpty();
                }
            }
        }
    }
}
