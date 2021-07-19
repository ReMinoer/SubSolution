using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders;
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

            SolutionBuilder solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

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

            SolutionBuilder solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

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

            SolutionBuilder solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

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

            SolutionBuilder solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            CheckFolderContainsMyFramework(solution.Root, butNotExternal: true);

            var subModuleFolder = solution.Root.SubFolders.Should().ContainKey("SubModule").WhichValue;
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

            SolutionBuilder solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

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

            SolutionBuilder solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);

            var myFrameworkFolder = solution.Root.SubFolders.Should().ContainKey("MyFramework").WhichValue;
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

            SolutionBuilder solution = ProcessConfigurationMockFile(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(2);

            var myFrameworkFolder = solution.Root.SubFolders.Should().ContainKey("MyFramework").WhichValue;
            CheckFolderContainsMyFramework(myFrameworkFolder, only: true, butNotExternal: true);

            var mySubModuleFolder = solution.Root.SubFolders.Should().ContainKey("MySubModule").WhichValue;
            CheckFolderContainsMySubModule(mySubModuleFolder, only: true);
        }
    }
}
