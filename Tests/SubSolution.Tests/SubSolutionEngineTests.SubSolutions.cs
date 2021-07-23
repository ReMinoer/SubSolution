using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;

namespace SubSolution.Tests
{
    public partial class SubSolutionEngineTests
    {
        [Test]
        public void ProcessSubSolutions()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root);
        }

        [Test]
        public void ProcessSubSolutionsMatchingFilter()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, only: true);
        }

        [Test]
        public void ProcessSubSolutionsMatchingMultipleFilters()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root);
        }

        [Test]
        public void ProcessSubSolutionsMatchingMultipleFiltersInDifferentFolder()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, butNotExternal: true);

            ISolutionFolder subModuleFolder = solution.Root.SubFolders["SubModule"];
            CheckFolderContainsMySubModule(subModuleFolder, only: true);
        }

        [Test]
        public void ProcessSubSolutionsWithReverseOrder()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, butNotExternal: true);
            CheckFolderContainsMySubModule(solution.Root);
        }

        [Test]
        public void ProcessSubSolutionsWithCreateRootFolder()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);

            ISolutionFolder myFrameworkFolder = solution.Root.SubFolders["MyFramework"];
            CheckFolderContainsMyFramework(myFrameworkFolder, only: true);
        }

        [Test]
        public void ProcessSubSolutionsWithCreateRootFolderAndReverseOrder()
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

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(2);

            ISolutionFolder myFrameworkFolder = solution.Root.SubFolders["MyFramework"];
            CheckFolderContainsMyFramework(myFrameworkFolder, only: true, butNotExternal: true);

            ISolutionFolder mySubModuleFolder = solution.Root.SubFolders["MySubModule"];
            CheckFolderContainsMySubModule(mySubModuleFolder, only: true);
        }
    }
}
