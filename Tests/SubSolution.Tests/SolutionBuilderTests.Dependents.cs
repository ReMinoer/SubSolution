using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test]
        public async Task ProcessDependentsProjects()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj"
                        },
                        new Dependents()
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(3);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsKeepOnlyDirect()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj"
                        },
                        new Dependents
                        {
                            KeepOnlyDirect = true
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
        public async Task ProcessDependentsProjectsKeepSatisfiedOnly()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj"
                        },
                        new Dependents
                        {
                            KeepOnlySatisfied = true
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
        public async Task ProcessDependentsProjectsKeepSatisfiedOnlyBis()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj"
                        },
                        new Projects
                        {
                            Path = "**/MyApplication.Core.csproj"
                        },
                        new Dependents
                        {
                            KeepOnlySatisfied = true
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(4);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsKeepSatisfiedOnlyWithFilter()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj"
                        },
                        new Projects
                        {
                            Path = "**/MyApplication.Core.csproj"
                        },
                        new Dependents
                        {
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyApplication.Console.csproj" }
                                }
                            },
                            KeepOnlySatisfied = true
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(2);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsKeepSatisfiedOnlyBeforeFilter()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj"
                        },
                        new Dependents
                        {
                            KeepOnlySatisfiedBeforeFilter = true
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
        public async Task ProcessDependentsProjectsKeepSatisfiedOnlyBeforeFilterBis()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj"
                        },
                        new Projects
                        {
                            Path = "**/MyApplication.Core.csproj"
                        },
                        new Dependents
                        {
                            KeepOnlySatisfiedBeforeFilter = true
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(4);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication/MyApplication.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Core/MyApplication.Core.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsKeepSatisfiedOnlyBeforeFilterWithFilter()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj"
                        },
                        new Projects
                        {
                            Path = "**/MyApplication.Core.csproj"
                        },
                        new Dependents
                        {
                            KeepOnlySatisfiedBeforeFilter = true,
                            Where = new ProjectFilterRoot
                            {
                                ProjectFilters =
                                {
                                    new ProjectPath { Match = "**/MyApplication.Console.csproj" }
                                }
                            }
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
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsWithTarget()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj",
                            Id = "MyTarget"
                        },
                        new Dependents
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
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsWithTargetVirtual()
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
                        new Dependents
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
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsWithTargetAndScope()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "src/*/",
                            Id = "MyScope"
                        }
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj",
                            Id = "MyTarget"
                        },
                        new Dependents
                        {
                            Target = "MyTarget",
                            Scope = "MyScope"
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
        public async Task ProcessDependentsProjectsWithTargetVirtualAndScope()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "src/*/",
                            Id = "MyScope"
                        },
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
                        new Dependents
                        {
                            Target = "MyTarget",
                            Scope = "MyScope"
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();

            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
        }

        [Test]
        public async Task ProcessDependentsDependenciesWithTarget()
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
                        new Dependents
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
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsDependenciesWithTargetVirtual()
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
                        new Dependents
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
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsDependentsWithTarget()
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
                        new Dependents
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
            solution.Root.Projects.Keys.Should().Contain("src/MyApplication.Configuration/MyApplication.Configuration.csproj");
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsDependentsWithTargetVirtual()
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
                        new Dependents
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
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsWithoutDependents()
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
                        new Dependents
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
            solution.Root.Projects.Keys.Should().Contain("src/Executables/MyApplication.Console/MyApplication.Console.csproj");
        }

        [Test]
        public async Task ProcessDependentsProjectsWithoutDependentsVirtual()
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
                        new Dependents
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
        public async Task ProcessDependentsProjectsWithDependentsComputedButIgnoredByScope()
        {
            var configuration = new SubSolutionConfiguration
            {
                Virtual = new VirtualProjectsSets
                {
                    SolutionProjects =
                    {
                        new Projects
                        {
                            Path = "src/*/",
                            Id = "MyScope"
                        },
                        // Pre-compute dependencies so all dependents are known at build
                        new Projects { Id = "PreCompute" },
                        new Dependencies { Target = "PreCompute" },
                    }
                },
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects
                        {
                            Path = "**/MyApplication.csproj",
                            Id = "MyTarget"
                        },
                        new Dependents
                        {
                            Target = "MyTarget",
                            Scope = "MyScope"
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
        public async Task IssueOnInvalidDependentsTarget()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependents
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

        [Test]
        public async Task IssueOnInvalidDependentsScope()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Dependents
                        {
                            Scope = "MyUnknownScope"
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
            issues[0].Message.Should().Contain("MyUnknownScope");
        }
    }
}