using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test] public Task ProcessSolutionsMatchingEmptyProjectFilter() => ProcessSolutionsMatchingEmptyProjectFilterBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingEmptyProjectFilter() => ProcessSolutionsMatchingEmptyProjectFilterBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingEmptyProjectFilterBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereFiles = new IgnorableFileFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereProjects = new IgnorableProjectFilterRoot()
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");

            solution.Root.SubFolders.Should().HaveCount(2);

            ISolutionFolder testsFolder = solution.Root.SubFolders["Tests"];
            {
                testsFolder.FilePaths.Should().BeEmpty();
                testsFolder.SubFolders.Should().BeEmpty();

                testsFolder.Projects.Should().HaveCount(1);
                testsFolder.Projects.Keys.Should().Contain("external/MyFramework/tests/MyFramework.Tests/MyFramework.Tests.csproj");
            }

            ISolutionFolder externalFolder = solution.Root.SubFolders["External"];
            {
                externalFolder.FilePaths.Should().BeEmpty();
                externalFolder.SubFolders.Should().BeEmpty();

                externalFolder.Projects.Should().HaveCount(1);
                externalFolder.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");
            }
        }

        [Test] public Task ProcessSolutionsMatchingProjectFilterPath() => ProcessSolutionsMatchingProjectFilterPathBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingProjectFilterPath() => ProcessSolutionsMatchingProjectFilterPathBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingProjectFilterPathBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereFiles = new IgnorableFileFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                ProjectFilters = new List<ProjectFilters>
                                {
                                    new ProjectPath
                                    {
                                        Match = "**/MyFramework.csproj"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");
        }

        [Test] public Task ProcessSolutionsMatchingProjectFilterNot() => ProcessSolutionsMatchingProjectFilterNotBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingProjectFilterNot() => ProcessSolutionsMatchingProjectFilterNotBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingProjectFilterNotBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereFiles = new IgnorableFileFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                ProjectFilters = new List<ProjectFilters>
                                {
                                    new ProjectNot
                                    {
                                        ProjectFilters = new ProjectPath
                                        {
                                            Match = "**/MyFramework.csproj"
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

            ISolutionFolder testsFolder = solution.Root.SubFolders["Tests"];
            {
                testsFolder.FilePaths.Should().BeEmpty();
                testsFolder.SubFolders.Should().BeEmpty();

                testsFolder.Projects.Should().HaveCount(1);
                testsFolder.Projects.Keys.Should().Contain("external/MyFramework/tests/MyFramework.Tests/MyFramework.Tests.csproj");
            }

            ISolutionFolder externalFolder = solution.Root.SubFolders["External"];
            {
                externalFolder.FilePaths.Should().BeEmpty();
                externalFolder.SubFolders.Should().BeEmpty();

                externalFolder.Projects.Should().HaveCount(1);
                externalFolder.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");
            }
        }

        [Test] public Task ProcessSolutionsMatchingProjectFilterAll() => ProcessSolutionsMatchingProjectFilterAllBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingProjectFilterAll() => ProcessSolutionsMatchingProjectFilterAllBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingProjectFilterAllBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereFiles = new IgnorableFileFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                ProjectFilters = new List<ProjectFilters>
                                {
                                    new ProjectMatchAll
                                    {
                                        ProjectFilters =
                                        {
                                            new ProjectPath
                                            {
                                                Match = "**/MyFramework/**"
                                            },
                                            new ProjectPath
                                            {
                                                Match = "**/MyFramework.csproj"
                                            },
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
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");
        }

        [Test] public Task ProcessSolutionsMatchingProjectFilterAnyOf() => ProcessSolutionsMatchingProjectFilterAnyOfBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingProjectFilterAnyOf() => ProcessSolutionsMatchingProjectFilterAnyOfBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingProjectFilterAnyOfBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            WhereFiles = new IgnorableFileFilterRoot
                            {
                                IgnoreAll = true
                            },
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                ProjectFilters = new List<ProjectFilters>
                                {
                                    new ProjectMatchAnyOf
                                    {
                                        ProjectFilters =
                                        {
                                            new ProjectPath
                                            {
                                                Match = "**/MyFramework.csproj"
                                            },
                                            new ProjectPath
                                            {
                                                Match = "**/MySubModule.csproj"
                                            },
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
            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");

            solution.Root.SubFolders.Should().HaveCount(1);

            ISolutionFolder externalFolder = solution.Root.SubFolders["External"];
            {
                externalFolder.FilePaths.Should().BeEmpty();
                externalFolder.SubFolders.Should().BeEmpty();

                externalFolder.Projects.Should().HaveCount(1);
                externalFolder.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");
            }
        }
    }
}
