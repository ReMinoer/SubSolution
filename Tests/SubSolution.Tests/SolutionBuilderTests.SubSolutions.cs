using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test] public Task ProcessSolutions() => ProcessSolutionsBase<Solutions>();
        [Test] public Task ProcessSubSolutions() => ProcessSolutionsBase<SubSolutions>();
        private async Task ProcessSolutionsBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root);
        }

        [Test] public Task ProcessSolutionsMatchingFilter() => ProcessSolutionsMatchingFilterBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFilter() => ProcessSolutionsMatchingFilterBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFilterBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T { Path = "external/*/" }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, only: true);
        }

        [Test] public Task ProcessSolutionsMatchingMultipleFilters() => ProcessSolutionsMatchingMultipleFiltersBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingMultipleFilters() => ProcessSolutionsMatchingMultipleFiltersBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingMultipleFiltersBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T { Path = "external/*/" },
                        new T()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root);
        }

        [Test] public Task ProcessSolutionsMatchingMultipleFiltersInDifferentFolder() => ProcessSolutionsMatchingMultipleFiltersInDifferentFolderBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingMultipleFiltersInDifferentFolder() => ProcessSolutionsMatchingMultipleFiltersInDifferentFolderBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingMultipleFiltersInDifferentFolderBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "SubModule",
                            SolutionItems = new List<SolutionItems>
                            {
                                new T { Path = "**/MySubModule/" }
                            }
                        },
                        new T()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, butNotExternal: true);

            ISolutionFolder subModuleFolder = solution.Root.SubFolders["SubModule"];
            CheckFolderContainsMySubModule(subModuleFolder, only: true);
        }

        [Test] public Task ProcessSolutionsMatchingMultipleFiltersWithOverwrite() => ProcessSolutionsMatchingMultipleFiltersWithOverwriteBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingMultipleFiltersWithOverwrite() => ProcessSolutionsMatchingMultipleFiltersWithOverwriteBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingMultipleFiltersWithOverwriteBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "SubSolutions",
                            SolutionItems = new List<SolutionItems>
                            {
                                new T()
                            }
                        },
                        new Folder
                        {
                            Name = "MySubModule",
                            SolutionItems = new List<SolutionItems>
                            {
                                new T
                                {
                                    Path = "**/MySubModule/",
                                    Overwrite = true
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
            {
                ISolutionFolder subSolutionsFolder = solution.Root.SubFolders["SubSolutions"];
                CheckFolderContainsMyFramework(subSolutionsFolder, only: true, butNotExternal: true);

                ISolutionFolder mySubModuleFolder = solution.Root.SubFolders["MySubModule"];
                CheckFolderContainsMySubModule(mySubModuleFolder, only: true);
            }
        }

        [Test] public Task ProcessSolutionsWithReverseOrder() => ProcessSolutionsWithReverseOrderBase<Solutions>();
        [Test] public Task ProcessSubSolutionsWithReverseOrder() => ProcessSolutionsWithReverseOrderBase<SubSolutions>();
        private async Task ProcessSolutionsWithReverseOrderBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T { ReverseOrder = true }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, butNotExternal: true);
            CheckFolderContainsMySubModule(solution.Root);
        }

        [Test] public Task ProcessSolutionsWithCreateRootFolder() => ProcessSolutionsWithCreateRootFolderBase<Solutions>();
        [Test] public Task ProcessSubSolutionsWithCreateRootFolder() => ProcessSolutionsWithCreateRootFolderBase<SubSolutions>();
        private async Task ProcessSolutionsWithCreateRootFolderBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T { CreateRootFolder = true }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);

            ISolutionFolder myFrameworkFolder = solution.Root.SubFolders["MyFramework"];
            CheckFolderContainsMyFramework(myFrameworkFolder, only: true);
        }

        [Test] public Task ProcessSolutionsWithCreateRootFolderAndReverseOrder() => ProcessSolutionsWithCreateRootFolderAndReverseOrderBase<Solutions>();
        [Test] public Task ProcessSubSolutionsWithCreateRootFolderAndReverseOrder() => ProcessSolutionsWithCreateRootFolderAndReverseOrderBase<SubSolutions>();
        private async Task ProcessSolutionsWithCreateRootFolderAndReverseOrderBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T { CreateRootFolder = true, ReverseOrder = true }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(2);

            ISolutionFolder myFrameworkFolder = solution.Root.SubFolders["MyFramework"];
            CheckFolderContainsMyFramework(myFrameworkFolder, only: true, butNotExternal: true);

            ISolutionFolder mySubModuleFolder = solution.Root.SubFolders["MySubModule"];
            CheckFolderContainsMySubModule(mySubModuleFolder, only: true);
        }
    }
}
