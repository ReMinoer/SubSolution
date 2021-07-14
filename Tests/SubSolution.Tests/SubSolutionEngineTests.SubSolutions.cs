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
            CheckFolderContainsMySubModule(solution.Root);
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
            solution.Root.SubFolders.Should().HaveCount(2);

            var myFrameworkFolder = solution.Root.SubFolders.Should().ContainKey("MyFramework").WhichValue;
            CheckFolderContainsMyFramework(myFrameworkFolder, only: true);

            var mySubModuleFolder = solution.Root.SubFolders.Should().ContainKey("MySubModule").WhichValue;
            CheckFolderContainsMySubModule(mySubModuleFolder, only: true);
        }
    }
}
