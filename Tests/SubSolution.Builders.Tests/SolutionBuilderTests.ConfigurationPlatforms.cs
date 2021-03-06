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
        public async Task ProcessNoConfigurationsNorPlatforms()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(9);
            CheckConfigurationPlatforms(solution, "Debug", "Any CPU", new []{ "Debug", "Debug", "debug", "Debug"}, new[] {"Any CPU", "Any CPU", "any cpu", "x86"}, new []{ true, true, true, false});
            CheckConfigurationPlatforms(solution, "Debug", "x86", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Debug", "x64", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Release", "Any CPU", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { true, true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "x86", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Release", "x64", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "Any CPU", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "x86", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "x64", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
        }

        [Test]
        public async Task ProcessEmptyConfigurations()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            CheckConfigurationPlatforms(solution, "Release", "Any CPU", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { true, true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "x86", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Release", "x64", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "Any CPU", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "x86", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "x64", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
        }

        [Test]
        public async Task ProcessPlatformsAndNoConfigurations()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            CheckConfigurationPlatforms(solution, "Debug", "Any CPU", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { true, true, true, false });
            CheckConfigurationPlatforms(solution, "Debug", "x64", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Release", "Any CPU", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { true, true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "x64", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "Any CPU", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "x64", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
        }

        [Test]
        public async Task ProcessConfigurationsAndPlatforms()
        {
            var configuration = new Subsln
            {
                Root = new SolutionRoot
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
            CheckConfigurationPlatforms(solution, "Release", "Any CPU", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { true, true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "x64", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Final", "Any CPU", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "x64", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
        }
    }
}
