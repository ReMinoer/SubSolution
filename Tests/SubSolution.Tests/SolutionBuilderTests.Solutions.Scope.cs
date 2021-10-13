using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SubSolution.Builders.Configuration;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        [Test] public Task ProcessSolutionsEmptyScope() => ProcessSolutionsEmptyScopeBase<Solutions>();
        [Test] public Task ProcessSubSolutionsEmptyScope() => ProcessSolutionsEmptyScopeBase<SubSolutions>();
        private async Task ProcessSolutionsEmptyScopeBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            KeepOnly = new SolutionsScope()
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().BeEmpty();
        }

        [Test] public Task ProcessSolutionsScopeFiles() => ProcessSolutionsScopeFilesBase<Solutions>();
        [Test] public Task ProcessSubSolutionsScopeFiles() => ProcessSolutionsScopeFilesBase<SubSolutions>();
        private async Task ProcessSolutionsScopeFilesBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            KeepOnly = new SolutionsScope
                            {
                                SolutionFiles =
                                {
                                    new Files
                                    {
                                        Path = "**/*.bat"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);
            {
                ISolutionFolder toolsFolder = solution.Root.SubFolders["Tools"];
                toolsFolder.Projects.Should().BeEmpty();
                toolsFolder.SubFolders.Should().BeEmpty();

                toolsFolder.FilePaths.Should().HaveCount(1);
                toolsFolder.FilePaths.Should().Contain("external/MyFramework/tools/submit.bat");
            }
        }

        [Test] public Task ProcessSolutionsScopeProjects() => ProcessSolutionsScopeProjectsBase<Solutions>();
        [Test] public Task ProcessSubSolutionsScopeProjects() => ProcessSolutionsScopeProjectsBase<SubSolutions>();
        private async Task ProcessSolutionsScopeProjectsBase<T>()
            where T : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            KeepOnly = new SolutionsScope
                            {
                                SolutionFiles =
                                {
                                    new Projects()
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().HaveCount(1);
            solution.Root.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");
            solution.Root.SubFolders.Should().HaveCount(2);
            {
                ISolutionFolder testsFolder = solution.Root.SubFolders["Tests"];
                testsFolder.FilePaths.Should().BeEmpty();
                testsFolder.Projects.Should().HaveCount(1);
                testsFolder.Projects.Keys.Should().Contain("external/MyFramework/tests/MyFramework.Tests/MyFramework.Tests.csproj");
                testsFolder.SubFolders.Should().BeEmpty();

                ISolutionFolder externalFolder = solution.Root.SubFolders["External"];
                externalFolder.FilePaths.Should().BeEmpty();
                externalFolder.Projects.Should().HaveCount(1);
                externalFolder.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");
                externalFolder.SubFolders.Should().BeEmpty();
            }
        }

        [Test] public Task ProcessSolutionsScopeSolutionsSolutions() => ProcessSolutionsScopeSolutionsBase<Solutions, Solutions>("sln");
        [Test] public Task ProcessSolutionsScopeSolutionsSubSolutions() => ProcessSolutionsScopeSolutionsBase<Solutions, SubSolutions>("subsln");
        [Test] public Task ProcessSolutionsScopeSubSolutionsSolutions() => ProcessSolutionsScopeSolutionsBase<SubSolutions, Solutions>("sln");
        [Test] public Task ProcessSolutionsScopeSubSolutionsSubSolutions() => ProcessSolutionsScopeSolutionsBase<SubSolutions, SubSolutions>("subsln");
        private async Task ProcessSolutionsScopeSolutionsBase<T, TScoped>(string fileExtension)
            where T : SolutionContentFiles, new()
            where TScoped : SolutionContentFiles, new()
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems = new List<SolutionItems>
                    {
                        new T
                        {
                            KeepOnly = new SolutionsScope
                            {
                                SolutionFiles =
                                {
                                    new TScoped
                                    {
                                        Path = @"external\MyFramework\external\MySubModule\MySubModule." + fileExtension
                                    }
                                }
                            }
                        }
                    }
                }
            };

            ISolution solution = await ProcessConfigurationMockFileAsync(configuration, haveSubSolutions: true);

            solution.Root.FilePaths.Should().BeEmpty();
            solution.Root.Projects.Should().BeEmpty();
            solution.Root.SubFolders.Should().HaveCount(1);

            CheckFolderContainsMySubModule(solution.Root.SubFolders["External"]);
        }
    }
}
