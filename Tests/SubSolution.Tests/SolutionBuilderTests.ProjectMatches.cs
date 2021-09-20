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
        public async Task ProcessEmptyConfigurationMatch()
        {
            var configuration = new SubSolutionConfiguration
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
                        new SolutionConfiguration
                        {
                            Name = "Custom",
                            ProjectConfiguration = new List<ProjectConfigurationMatch>()
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(3);
            CheckConfigurationPlatforms(solution, "Custom", "Any CPU", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
            CheckConfigurationPlatforms(solution, "Custom", "x86", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
            CheckConfigurationPlatforms(solution, "Custom", "x64", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, false });
        }

        [Test]
        public async Task ProcessEmptyPlatformMatch()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Platforms = new SolutionPlatformList()
                {
                    Platform = new List<SolutionPlatform>
                    {
                        new SolutionPlatform
                        {
                            Name = "Console",
                            ProjectPlatform = new List<ProjectPlatformMatch>()
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(3);
            CheckConfigurationPlatforms(solution, "Debug", "Console", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
            CheckConfigurationPlatforms(solution, "Release", "Console", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
            CheckConfigurationPlatforms(solution, "Final", "Console", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
        }

        [Test]
        public async Task ProcessConfigurationMatch()
        {
            var configuration = new SubSolutionConfiguration
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
                        new SolutionConfiguration
                        {
                            Name = "Custom",
                            ProjectConfiguration = new List<ProjectConfigurationMatch>
                            {
                                new ProjectConfigurationMatch { Match = "Release" }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(3);
            CheckConfigurationPlatforms(solution, "Custom", "Any CPU", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { true, true, true, false });
            CheckConfigurationPlatforms(solution, "Custom", "x86", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, true });
            CheckConfigurationPlatforms(solution, "Custom", "x64", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { false, false, false, true });
        }

        [Test]
        public async Task ProcessPlatformMatch()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new Projects()
                    }
                },
                Platforms = new SolutionPlatformList()
                {
                    Platform = new List<SolutionPlatform>
                    {
                        new SolutionPlatform
                        {
                            Name = "Console",
                            ProjectPlatform = new List<ProjectPlatformMatch>
                            {
                                new ProjectPlatformMatch { Match = "Any CPU" }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(3);
            CheckConfigurationPlatforms(solution, "Debug", "Console", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { true, true, true, false });
            CheckConfigurationPlatforms(solution, "Release", "Console", new[] { "Release", "Release", "release", "Release" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { true, true, true, false });
            CheckConfigurationPlatforms(solution, "Final", "Console", new[] { "Debug", "Debug", "debug", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
        }

        [Test]
        public async Task ProcessEmptyConfigurationAndPlatformMatch()
        {
            var configuration = new SubSolutionConfiguration
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
                        new SolutionConfiguration
                        {
                            Name = "Custom",
                            ProjectConfiguration = new List<ProjectConfigurationMatch>()
                        }
                    }
                },
                Platforms = new SolutionPlatformList()
                {
                    Platform = new List<SolutionPlatform>
                    {
                        new SolutionPlatform
                        {
                            Name = "Console",
                            ProjectPlatform = new List<ProjectPlatformMatch>()
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(1);
            CheckConfigurationPlatforms(solution, "Custom", "Console", new[] { "Debug", "Debug", "debug", "Debug" }, new[] { "Any CPU", "Any CPU", "any cpu", "x86" }, new[] { false, false, false, false });
        }

        [Test]
        public async Task ProcessConfigurationAndPlatformMatches()
        {
            var configuration = new SubSolutionConfiguration
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
                        new SolutionConfiguration
                        {
                            Name = "Custom",
                            ProjectConfiguration = new List<ProjectConfigurationMatch>
                            {
                                new ProjectConfigurationMatch { Match = "final" },
                                new ProjectConfigurationMatch { Match = "release" }
                            }
                        }
                    }
                },
                Platforms = new SolutionPlatformList()
                {
                    Platform = new List<SolutionPlatform>
                    {
                        new SolutionPlatform
                        {
                            Name = "Console",
                            ProjectPlatform = new List<ProjectPlatformMatch>
                            {
                                new ProjectPlatformMatch { Match = "64" },
                                new ProjectPlatformMatch { Match = "any" },
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.ConfigurationPlatforms.Should().HaveCount(1);
            CheckConfigurationPlatforms(solution, "Custom", "Console", new[] { "Release", "Release", "release", "Final" }, new[] { "Any CPU", "Any CPU", "any cpu", "x64" }, new[] { true, true, true, true });
        }
    }
}
