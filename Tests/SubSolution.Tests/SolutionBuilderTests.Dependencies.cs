using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public async Task ProcessDependenciesProjects()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.Console.csproj"
                        },
                        new Dependencies()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(4);
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessDependenciesProjectsWithTarget()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.Console.csproj",
                            Id = "MyTarget"
                        },
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(4);
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessDependenciesProjectsWithTargetVirtual()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.Console.csproj",
                            Id = "MyTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(3);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessDependenciesDependencies()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.Console.csproj",
                            Id = "MyVirtualTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependencies
                        {
                            Target = "MyVirtualTarget",
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyApplication.Configuration.csproj" }
                                }
                            }
                        },
                        new Dependencies()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessDependenciesDependenciesWithTarget()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.Console.csproj",
                            Id = "MyVirtualTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependencies
                        {
                            Target = "MyVirtualTarget",
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyApplication.Configuration.csproj" }
                                }
                            },
                            Id = "MyTarget"
                        },
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessDependenciesDependenciesWithTargetVirtual()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.Console.csproj",
                            Id = "MyVirtualTarget"
                        },
                        new Dependencies
                        {
                            Target = "MyVirtualTarget",
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyApplication.Configuration.csproj" }
                                }
                            },
                            Id = "MyTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
        }

        [Test]
        public async Task ProcessDependenciesDependents()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj",
                            Id = "MyVirtualTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependents
                        {
                            Target = "MyVirtualTarget",
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyApplication.Configuration.csproj" }
                                }
                            }
                        },
                        new Dependencies()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessDependenciesDependentsWithTarget()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj",
                            Id = "MyVirtualTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependents
                        {
                            Target = "MyVirtualTarget",
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyApplication.Configuration.csproj" }
                                }
                            },
                            Id = "MyTarget"
                        },
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessDependenciesDependentsWithTargetVirtual()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj",
                            Id = "MyVirtualTarget"
                        },
                        new Dependents
                        {
                            Target = "MyVirtualTarget",
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyApplication.Configuration.csproj" }
                                }
                            },
                            Id = "MyTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
        }

        [Test] public Task ProcessDependenciesSolutions() => ProcessDependenciesSolutionsBase<Solutions>();
        [Test] public Task ProcessDependenciesSubSolutions() => ProcessDependenciesSolutionsBase<SubSolutions>();
        private async Task ProcessDependenciesSolutionsBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new T
                        {
                            WhereFiles = new IgnorableFileFilterRoot {IgnoreAll = true},
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyFramework.Tests.csproj" }
                                }
                            }
                        },
                        new Dependencies()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");

            solution.Root.SubFolders.Should().HaveCount(1);
            ISolutionFolder testsFolder = solution.Root.SubFolders["Tests"];
            {
                testsFolder.FilePaths.Should().BeEmpty();
                testsFolder.SubFolders.Should().BeEmpty();

                testsFolder.Projects.Should().HaveCount(1);
                testsFolder.Projects.Keys.Should().Contain("external/MyFramework/tests/MyFramework.Tests/MyFramework.Tests.csproj");
            }
        }

        [Test] public Task ProcessDependenciesSolutionsWithTarget() => ProcessDependenciesSolutionsWithTargetBase<Solutions>();
        [Test] public Task ProcessDependenciesSubSolutionsWithTarget() => ProcessDependenciesSolutionsWithTargetBase<SubSolutions>();
        private async Task ProcessDependenciesSolutionsWithTargetBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new T
                        {
                            Id = "MyTarget",
                            WhereFiles = new IgnorableFileFilterRoot {IgnoreAll = true},
                            WhereProjects = new IgnorableProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyFramework.Tests.csproj" }
                                }
                            }
                        },
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");

            solution.Root.SubFolders.Should().HaveCount(1);
            ISolutionFolder testsFolder = solution.Root.SubFolders["Tests"];
            {
                testsFolder.FilePaths.Should().BeEmpty();
                testsFolder.SubFolders.Should().BeEmpty();

                testsFolder.Projects.Should().HaveCount(1);
                testsFolder.Projects.Keys.Should().Contain("external/MyFramework/tests/MyFramework.Tests/MyFramework.Tests.csproj");
            }
        }

        [Test] public Task ProcessDependenciesSolutionsWithTargetVirtual() => ProcessDependenciesSolutionsWithTargetVirtualBase<Solutions>();
        [Test] public Task ProcessDependenciesSubSolutionsWithTargetVirtual() => ProcessDependenciesSolutionsWithTargetVirtualBase<SubSolutions>();
        private async Task ProcessDependenciesSolutionsWithTargetVirtualBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new T
                        {
                            Id = "MyTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");
        }

        [Test]
        public async Task ProcessDependenciesProjectsWithoutDependencies()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj",
                            Id = "MyTarget"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependencies
                        {
                            Target = "MyTarget"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test]
        public async Task IssueOnInvalidDependenciesTarget()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependencies
                        {
                            Target = "MyUnknownTarget"
                        }
                    }
                }
            };

            (ISolution solution, Issue[] issues) = await ProcessConfigurationMockFileWithIssuesAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            issues.Should().HaveCount(1);
            issues[0].Level.Should().Be(IssueLevel.Warning);
            issues[0].Message.Should().Contain("MyUnknownTarget");
        }
    }
}