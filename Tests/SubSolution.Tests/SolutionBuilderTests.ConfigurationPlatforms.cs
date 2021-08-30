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
        public async Task ProcessNoConfigurationsNorPlatforms()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(9);
            CheckConfigurationPlatforms(solution, "Debug", "Any CPU", new []{"Debug", "debug", "Debug"}, new[] {"Any CPU", "any cpu", "x86"}, new []{true, true, false});
            CheckConfigurationPlatforms(solution, "Debug", "x86", new[] { "Debug", "debug", "Debug" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Debug", "x64", new[] { "Debug", "debug", "Debug" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Release", "Any CPU", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "x86", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Release", "x64", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "Any CPU", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "x86", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "x64", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
        }

        [Test]
        public async Task ProcessEmptyConfigurations()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Configurations = new SolutionConfigurationList()
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(0);
        }

        [Test]
        public async Task ProcessEmptyPlatforms()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Platforms = new SolutionPlatformList()
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(0);
        }

        [Test]
        public async Task ProcessEmptyConfigurationsAndPlatforms()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Configurations = new SolutionConfigurationList(),
                Platforms = new SolutionPlatformList()
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(0);
        }

        [Test]
        public async Task ProcessConfigurationsWithEmptyPlatforms()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Configurations = new SolutionConfigurationList
                {
                    Configuration = new List<SolutionConfiguration>
                    {
                        new SolutionConfiguration {Name = "Debug"}
                    }
                },
                Platforms = new SolutionPlatformList()
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(0);
        }

        [Test]
        public async Task ProcessPlatformsWithEmptyConfigurations()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Configurations = new SolutionConfigurationList(),
                Platforms = new SolutionPlatformList
                {
                    Platform = new List<SolutionPlatform>
                    {
                        new SolutionPlatform {Name = "Any CPU"}
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(0);
        }

        [Test]
        public async Task ProcessConfigurationsAndNoPlatforms()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Configurations = new SolutionConfigurationList
                {
                    Configuration = new List<SolutionConfiguration>
                    {
                        new SolutionConfiguration {Name = "Release"},
                        new SolutionConfiguration {Name = "Final"}
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(6);
            CheckConfigurationPlatforms(solution, "Release", "Any CPU", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "x86", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Release", "x64", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "Any CPU", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "x86", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "x64", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
        }

        [Test]
        public async Task ProcessPlatformsAndNoConfigurations()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Platforms = new SolutionPlatformList
                {
                    Platform = new List<SolutionPlatform>
                    {
                        new SolutionPlatform {Name = "Any CPU"},
                        new SolutionPlatform {Name = "x64"}
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(6);
            CheckConfigurationPlatforms(solution, "Debug", "Any CPU", new[] { "Debug", "debug", "Debug" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { true, true, false });
            CheckConfigurationPlatforms(solution, "Debug", "x64", new[] { "Debug", "debug", "Debug" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Release", "Any CPU", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "x64", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "Any CPU", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "x64", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
        }

        [Test]
        public async Task ProcessConfigurationsAndPlatforms()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRootConfiguration
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Configurations = new SolutionConfigurationList
                {
                    Configuration = new List<SolutionConfiguration>
                    {
                        new SolutionConfiguration {Name = "Release"},
                        new SolutionConfiguration {Name = "Final"}
                    }
                },
                Platforms = new SolutionPlatformList
                {
                    Platform = new List<SolutionPlatform>
                    {
                        new SolutionPlatform {Name = "Any CPU"},
                        new SolutionPlatform {Name = "x64"}
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(4);
            CheckConfigurationPlatforms(solution, "Release", "Any CPU", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "x64", new[] { "Release", "release", "Release" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "Any CPU", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x86" }, new[] { false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "x64", new[] { "Debug", "debug", "Final" }, new[] { "Any CPU", "any cpu", "x64" }, new[] { false, false, true });
        }
    }
}
