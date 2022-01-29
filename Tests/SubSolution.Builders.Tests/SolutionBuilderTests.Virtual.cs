using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders.Configuration;

namespace SubSolution.Builders.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public async Task ProcessVirtualEmpty()
        {
            var configuration = new Subsln
            {
                Virtual = new VirtualProjectsSets()
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        static public IEnumerable<SolutionProjects> VirtualizableProjectSources
        {
            get
            {
                yield return new Projects();
                yield return new Solutions();
                yield return new SubSolutions();
                yield return new Dependencies();
                yield return new Dependents();
            }
        }

        [TestCaseSource(nameof(VirtualizableProjectSources))]
        public async Task ProcessVirtual<TProjectSource>(TProjectSource projectSource)
            where TProjectSource : SolutionProjects, new()
        {
            var configuration = new Subsln
            {
                Virtual = new VirtualProjectsSets
                {

                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.Configuration.csproj"
                        },
                        projectSource
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Files
                        {
                            Path = "tools/submit.bat"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.FilePaths.Should().HaveCount(1);
            solution.Root.FilePaths.Should().Contain("tools/submit.bat");
        }
    }
}