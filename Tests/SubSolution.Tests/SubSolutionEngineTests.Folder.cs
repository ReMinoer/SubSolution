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

            SolutionBuilder solution = Process(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }
    }
}
