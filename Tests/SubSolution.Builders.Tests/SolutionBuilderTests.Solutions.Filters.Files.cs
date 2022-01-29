using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders.Configuration;

namespace SubSolution.Builders.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test] public Task ProcessSolutionsMatchingIgnoreAllFilter() => ProcessSolutionsMatchingIgnoreAllFilterBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingIgnoreAllFilter() => ProcessSolutionsMatchingIgnoreAllFilterBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingIgnoreAllFilterBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereFiles = new IgnorableFileFilterRoot
                            {
                                IgnoreAll = true
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test] public Task ProcessSolutionsMatchingEmptyFileFilter() => ProcessSolutionsMatchingEmptyFileFilterBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingEmptyFileFilter() => ProcessSolutionsMatchingEmptyFileFilterBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingEmptyFileFilterBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereFiles = new IgnorableFileFilterRoot()
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(2);

            ISolutionFolder toolsFolder = solution.Root.SubFolders["Tools"];
            {
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();

                toolsFolder.FilePaths.Should().HaveCount(1);
                toolsFolder.FilePaths.Should().Contain("external/MyFramework/tools/submit.bat");
            }

            ISolutionFolder externalFolder = solution.Root.SubFolders["External"];
            {
                externalFolder.Projects.Should().BeEmpty();
                externalFolder.SubFolders.Should().BeEmpty();

                externalFolder.FilePaths.Should().HaveCount(1);
                externalFolder.FilePaths.Should().Contain("external/MyFramework/external/MySubModule/README.txt");
            }
        }

        [Test] public Task ProcessSolutionsMatchingFileFilterPath() => ProcessSolutionsMatchingFileFilterPathBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFileFilterPath() => ProcessSolutionsMatchingFileFilterPathBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFileFilterPathBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereFiles = new IgnorableFileFilterRoot
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

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);

            ISolutionFolder toolsFolder = solution.Root.SubFolders["Tools"];
            {
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();

                toolsFolder.FilePaths.Should().HaveCount(1);
                toolsFolder.FilePaths.Should().Contain("external/MyFramework/tools/submit.bat");
            }
        }

        [Test] public Task ProcessSolutionsMatchingFileFilterNot() => ProcessSolutionsMatchingFileFilterNotBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFileFilterNot() => ProcessSolutionsMatchingFileFilterNotBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFileFilterNotBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereFiles = new IgnorableFileFilterRoot
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

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);

            ISolutionFolder externalFolder = solution.Root.SubFolders["External"];
            {
                externalFolder.Projects.Should().BeEmpty();
                externalFolder.SubFolders.Should().BeEmpty();

                externalFolder.FilePaths.Should().HaveCount(1);
                externalFolder.FilePaths.Should().Contain("external/MyFramework/external/MySubModule/README.txt");
            }
        }

        [Test] public Task ProcessSolutionsMatchingFileFilterAll() => ProcessSolutionsMatchingFileFilterAllBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFileFilterAll() => ProcessSolutionsMatchingFileFilterAllBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFileFilterAllBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereFiles = new IgnorableFileFilterRoot
                            {
                                FileFilters = new List<FileFilters>
                                {
                                    new FileMatchAll
                                    {
                                        FileFilters = new List<FileFilters>
                                        {
                                            new FilePath
                                            {
                                                Match = "external/**"
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

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);

            ISolutionFolder toolsFolder = solution.Root.SubFolders["Tools"];
            {
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();

                toolsFolder.FilePaths.Should().HaveCount(1);
                toolsFolder.FilePaths.Should().Contain("external/MyFramework/tools/submit.bat");
            }
        }

        [Test] public Task ProcessSolutionsMatchingFileFilterAnyOf() => ProcessSolutionsMatchingFileFilterAnyOfBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFileFilterAnyOf() => ProcessSolutionsMatchingFileFilterAnyOfBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFileFilterAnyOfBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereFiles = new IgnorableFileFilterRoot
                            {
                                FileFilters = new List<FileFilters>
                                {
                                    new FileMatchAnyOf
                                    {
                                        FileFilters = new List<FileFilters>
                                        {
                                            new FilePath
                                            {
                                                Match = "**/*.bat"
                                            },
                                            new FilePath
                                            {
                                                Match = "**/*.txt"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(2);

            ISolutionFolder toolsFolder = solution.Root.SubFolders["Tools"];
            {
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();

                toolsFolder.FilePaths.Should().HaveCount(1);
                toolsFolder.FilePaths.Should().Contain("external/MyFramework/tools/submit.bat");
            }

            ISolutionFolder externalFolder = solution.Root.SubFolders["External"];
            {
                externalFolder.Projects.Should().BeEmpty();
                externalFolder.SubFolders.Should().BeEmpty();

                externalFolder.FilePaths.Should().HaveCount(1);
                externalFolder.FilePaths.Should().Contain("external/MyFramework/external/MySubModule/README.txt");
            }
        }
    }
}
