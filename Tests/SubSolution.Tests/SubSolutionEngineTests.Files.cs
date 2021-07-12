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
        public void ProcessFilesMatchingFilter()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**"
                        }
                    }
                }
            };

            SolutionBuilder solution = Process(configuration);

            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(2);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
        }

        [Test]
        public void ProcessFilesMatchingMultipleFilters()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**"
                        },
                        new Files
                        {
                            Path = "**/*.bat"
                        }
                    }
                }
            };

            SolutionBuilder solution = Process(configuration);

            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(2);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
            solution.Root.FilePaths.Should().Contain("tools/debug/Debug.exe");
        }

        [Test]
        public void ProcessFilesWithCreateFolders()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Files
                        {
                            Path = "tools/**",
                            CreateFolders = true
                        }
                    }
                }
            };

            SolutionBuilder solution = Process(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.ProjectPaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                var toolsFolder = solution.Root.SubFolders.Should().ContainKey("tools").WhichValue;
                toolsFolder.FilePaths.Should().BeEquivalentTo("tools/submit.bat");
                toolsFolder.ProjectPaths.Should().BeEmpty();
                toolsFolder.SubFolders.Should().HaveCount(1);
                {
                    var debugFolder = toolsFolder.SubFolders.Should().ContainKey("debug").WhichValue;
                    debugFolder.FilePaths.Should().BeEquivalentTo("tools/debug/Debug.exe");
                    debugFolder.ProjectPaths.Should().BeEmpty();
                    debugFolder.SubFolders.Should().BeEmpty();
                }
            }
        }
    }
}
