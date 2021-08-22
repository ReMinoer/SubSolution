using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public void IgnoreEmptyFolders()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Folder
                        {
                            Name = "MySolutionFolder"
                        }
                    }
                }
            };

            ISolutionOutput solution = ProcessConfigurationMockFile(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }
    }
}
