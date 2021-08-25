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
        public async Task ProcessSubSolutions()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new SubSolutions()
                    }
                }
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root);
        }

        [Test]
        public async Task ProcessSubSolutionsMatchingFilter()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new SubSolutions { Path = "external/*/" }
                    }
                }
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, only: true);
        }

        [Test]
        public async Task ProcessSubSolutionsMatchingMultipleFilters()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new SubSolutions { Path = "external/*/" },
                        new SubSolutions()
                    }
                }
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root);
        }

        [Test]
        public async Task ProcessSubSolutionsMatchingMultipleFiltersInDifferentFolder()
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
                                new SubSolutions { Path = "**/MySubModule/" }
                            }
                        },
                        new SubSolutions()
                    }
                }
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, butNotExternal: true);

            ISolutionFolder subModuleFolder = solution.Root.SubFolders["SubModule"];
            CheckFolderContainsMySubModule(subModuleFolder, only: true);
        }

        [Test]
        public async Task ProcessSubSolutionsMatchingMultipleFiltersWithOverwrite()
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
                                new SubSolutions()
                            }
                        },
                        new Folder
                        {
                            Name = "MySubModule",
                            SolutionItems = new List<SolutionItems>
                            {
                                new SubSolutions
                                {
                                    Path = "**/MySubModule/",
                                    Overwrite = true
                                }
                            }
                        }
                    }
                }
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

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

        [Test]
        public async Task ProcessSubSolutionsWithReverseOrder()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new SubSolutions { ReverseOrder = true }
                    }
                }
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, butNotExternal: true);
            CheckFolderContainsMySubModule(solution.Root);
        }

        [Test]
        public async Task ProcessSubSolutionsWithCreateRootFolder()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new SubSolutions { CreateRootFolder = true }
                    }
                }
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);

            ISolutionFolder myFrameworkFolder = solution.Root.SubFolders["MyFramework"];
            CheckFolderContainsMyFramework(myFrameworkFolder, only: true);
        }

        [Test]
        public async Task ProcessSubSolutionsWithCreateRootFolderAndReverseOrder()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new SubSolutions { CreateRootFolder = true, ReverseOrder = true }
                    }
                }
            };

            ISolutionOutput solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

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
