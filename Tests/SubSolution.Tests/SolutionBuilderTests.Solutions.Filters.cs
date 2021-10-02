using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using SubSolution.Builders.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test] public Task ProcessSolutionsMatchingEmptyFilter() => ProcessSolutionsMatchingEmptyFilterBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingEmptyFilter() => ProcessSolutionsMatchingEmptyFilterBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingEmptyFilterBase<T>()
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
                            Where = new FileFilterRoot()
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root);
        }

        [Test] public Task ProcessSolutionsMatchingFilterPath() => ProcessSolutionsMatchingFilterPathBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFilterPath() => ProcessSolutionsMatchingFilterPathBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFilterPathBase<T>()
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
                            Where = new FileFilterRoot
                            {
                                FileFilters = new List<FileFilters>
                                {
                                    new FilePath
                                    {
                                        Match = "external/*/"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, only: true);
        }

        [Test] public Task ProcessSolutionsMatchingFilterNot() => ProcessSolutionsMatchingFilterNotBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFilterNot() => ProcessSolutionsMatchingFilterNotBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFilterNotBase<T>()
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
                            Where = new FileFilterRoot
                            {
                                FileFilters = new List<FileFilters>
                                {
                                    new FileNot
                                    {
                                        FileFilters = new FilePath
                                        {
                                            Match = "external/*/"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);
            
            CheckFolderContainsMySubModule(solution.Root, only: true);
        }

        [Test] public Task ProcessSolutionsMatchingFilterAll() => ProcessSolutionsMatchingFilterAllBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFilterAll() => ProcessSolutionsMatchingFilterAllBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFilterAllBase<T>()
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
                                                Match = "external/*/"
                                            },
                                            new FilePath
                                            {
                                                Match = "**/MyFramework.*"
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

            CheckFolderContainsMyFramework(solution.Root, only: true);
        }

        [Test] public Task ProcessSolutionsMatchingFilterAnyOf() => ProcessSolutionsMatchingFilterAnyOfBase<Solutions>();
        [Test] public Task ProcessSubSolutionsMatchingFilterAnyOf() => ProcessSolutionsMatchingFilterAnyOfBase<SubSolutions>();
        private async Task ProcessSolutionsMatchingFilterAnyOfBase<T>()
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
                                                Match = "**/MySubModule.*"
                                            },
                                            new FileNot
                                            {
                                                FileFilters = new FilePath
                                                {
                                                    Match = "**/MyFramework.*"
                                                }
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
            
            CheckFolderContainsMySubModule(solution.Root, only: true);
        }
    }
}
