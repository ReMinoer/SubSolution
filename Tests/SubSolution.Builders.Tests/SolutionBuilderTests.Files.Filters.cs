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
        public async Task ProcessFilesMatchingEmptyFilter()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
                            Where = new FileFilterRoot()
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
        public async Task ProcessFilesMatchingFilterPath()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
                            Where = new FileFilterRoot
                            {
                                FileFilters = new List<FileFilters>
                                {
                                    new FilePath
                                    {
                                        Match = "**/*.bat"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(2);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
            solution.Root.FilePaths.Should().Contain("tools/pull.bat");
        }

        [Test]
        public async Task ProcessFilesMatchingFilterNot()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
                            Where = new FileFilterRoot
                            {
                                FileFilters = new List<FileFilters>
                                {
                                    new FileNot
                                    {
                                        FileFilters = new FilePath
                                        {
                                            Match = "**/*.bat"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(1);
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
        }

        [Test]
        public async Task ProcessFilesMatchingFilterAll()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
                            Where = new FileFilterRoot
                            {
                                FileFilters = new List<FileFilters>
                                {
                                    new FileMatchAll
                                    {
                                        FileFilters = new List<FileFilters>
                                        {
                                            new FilePath
                                            {
                                                Match = "**/*.bat"
                                            },
                                            new FilePath
                                            {
                                                Match = "**/submit.*"
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

            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(1);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
        }

        [Test]
        public async Task ProcessFilesMatchingFilterAnyOf()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
                            Where = new FileFilterRoot
                            {
                                FileFilters = new List<FileFilters>
                                {
                                    new FileMatchAnyOf
                                    {
                                        FileFilters = new List<FileFilters>
                                        {
                                            new FilePath
                                            {
                                                Match = "**/submit.*"
                                            },
                                            new FilePath
                                            {
                                                Match = "**/pull.*"
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

            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(2);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
            solution.Root.FilePaths.Should().Contain("tools/pull.bat");
        }
    }
}