using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SubSolution.Configuration;
using SubSolution.Configuration.Builders;
using SubSolution.Converters;
using SubSolution.FileSystems.Mock;
using SubSolution.ProjectReaders.Mock;
using SubSolution.Raw;

namespace SubSolution.Tests
{
    public partial class SolutionBuilderTests
    {
        private const string RootName = @"C:";
        private const string WorkspaceDirectoryRelativePath = @"Directory\SubDirectory\MyWorkspace\";
        static private readonly string WorkspaceDirectoryPath = $@"{RootName}\{WorkspaceDirectoryRelativePath}";

        private Task<ISolution> ProcessConfigurationMockFileAsync(SubSolutionConfiguration configuration, bool haveSubSolutions = false)
        {
            const string configurationFilePath = @"C:\Directory\SubDirectory\MyWorkspace\MyApplication.subsln";

            return ProcessConfigurationAsync(configuration, haveSubSolutions, (fileSystem, projectReader)
                => SolutionBuilderContext.FromConfigurationFileAsync(configurationFilePath, projectReader, fileSystem));
        }

        private Task<ISolution> ProcessConfigurationAsync(SubSolutionConfiguration configuration, string workspaceDirectoryPath, bool haveSubSolutions = false)
        {
            return ProcessConfigurationAsync(configuration, haveSubSolutions, (fileSystem, projectReader)
                => Task.FromResult(SolutionBuilderContext.FromConfiguration(configuration, projectReader, workspaceDirectoryPath, workspaceDirectoryPath, fileSystem)));
        }

        private async Task<ISolution> ProcessConfigurationAsync(SubSolutionConfiguration configuration, bool haveSubSolutions,
            Func<MockFileSystem, MockProjectReader, Task<SolutionBuilderContext>> createContext)
        {
            ILogger logger = new ConsoleLogger();

            var mockProjectReader = new MockProjectReader(new[] { "Debug", "Release" }, new[] { "Any CPU" })
            {
                ProjectCanBuild = true,
                ProjectCanDeploy = false,
                ProjectConfigurations =
                {
                    {@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication\MyApplication.csproj", new []{"Debug", "Release"}},
                    {@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication.Configuration\MyApplication.Configuration.csproj", new []{"debug", "release"}},
                    {@"C:\Directory\SubDirectory\MyWorkspace\src\Executables\MyApplication.Console\MyApplication.Console.csproj", new []{"Debug", "Release", "Final"}}
                },
                ProjectPlatforms =
                {
                    {@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication\MyApplication.csproj", new []{"Any CPU"}},
                    {@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication.Configuration\MyApplication.Configuration.csproj", new []{"any cpu"}},
                    {@"C:\Directory\SubDirectory\MyWorkspace\src\Executables\MyApplication.Console\MyApplication.Console.csproj", new []{"x86", "x64"}}
                }
            };

            MockFileSystem mockFileSystem = await GetMockFileSystemAsync(configuration, haveSubSolutions, mockProjectReader);

            SolutionBuilderContext context = await createContext(mockFileSystem, mockProjectReader);
            context.Logger = logger;
            context.LogLevel = LogLevel.Debug;

            var solutionBuilder = new SolutionBuilder(context);
            ISolution solution = await solutionBuilder.BuildAsync(context.Configuration);

            var solutionLogger = new SolutionLogger(fileSystem: mockFileSystem)
            {
                ShowHeaders = true,
                ShowHierarchy = true,
                ShowConfigurationPlatforms = true,
                ShowProjectContexts = true
            };

            string logMessage = solutionLogger.Convert(solution);
            logger.LogDebug(logMessage);

            // Full pipe checks
            var rawSolutionConverter = new SolutionConverter(mockFileSystem);
            var solutionConverter = new RawSolutionConverter(mockFileSystem, mockProjectReader);

            ISolution checkSolution = solution;
            RawSolution rawSolution;
            List<Issue> issues;

            rawSolution = rawSolutionConverter.Convert(checkSolution);
            await using var firstPassStream = new MemoryStream();
            await rawSolution.WriteAsync(firstPassStream);
            firstPassStream.Position = 0;
            rawSolution = await RawSolution.ReadAsync(firstPassStream);
            (checkSolution, issues) = await solutionConverter.ConvertAsync(rawSolution, context.SolutionPath);
            issues.Should().BeEmpty();

            rawSolution = rawSolutionConverter.Convert(checkSolution);
            await using var secondPassStream = new MemoryStream();
            await rawSolution.WriteAsync(secondPassStream);
            secondPassStream.Position = 0;
            await RawSolution.ReadAsync(secondPassStream);
            (_, issues) = await solutionConverter.ConvertAsync(rawSolution, context.SolutionPath);
            issues.Should().BeEmpty();

            return solution;
        }

        private async Task<MockFileSystem> GetMockFileSystemAsync(SubSolutionConfiguration configurationContent, bool haveSubSolutions, MockProjectReader mockProjectReader)
        {
            var mockFileSystem = new MockFileSystem();
            var relativeFilePath = new List<string>();

            relativeFilePath.AddRange(new[]
            {
                @"tools\submit.bat",
                @"tools\debug\Debug.exe",
                @"src\MyApplication\MyClass.cs",
                @"src\MyApplication\MyApplication.csproj",
                @"src\MyApplication.Configuration\MyConfiguration.cs",
                @"src\MyApplication.Configuration\MyApplication.Configuration.csproj",
                @"src\Executables\MyApplication.Console\Program.cs",
                @"src\Executables\MyApplication.Console\MyApplication.Console.csproj",
                @"src\Executables\MyApplication.Console\bin\MyApplication.Console.exe",
            });

            byte[] emptyProjectContent = Encoding.UTF8.GetBytes("<Project></Project>");
            mockFileSystem.AddFileContent(@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication\MyApplication.csproj", emptyProjectContent);
            mockFileSystem.AddFileContent(@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication.Configuration\MyApplication.Configuration.csproj", emptyProjectContent);
            mockFileSystem.AddFileContent(@"C:\Directory\SubDirectory\MyWorkspace\src\Executables\MyApplication.Console\MyApplication.Console.csproj", emptyProjectContent);

            if (haveSubSolutions)
            {
                relativeFilePath.AddRange(new []
                {
                    @"external\MyFramework\tools\submit.bat",
                    @"external\MyFramework\src\MyFramework\MyFramework.csproj",
                    @"external\MyFramework\src\MyFramework\MyClass.cs",
                    @"external\MyFramework\tests\MyFramework.Tests\MyFramework.Tests.csproj",
                    @"external\MyFramework\tests\MyFramework.Tests\MyTests.cs",
                    @"external\MyFramework\external\MySubModule\src\MySubModule\MySubModule.csproj",
                    @"external\MyFramework\external\MySubModule\src\MySubModule\MyClass.cs",
                });

                mockFileSystem.AddFileContent(@"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\src\MyFramework\MyFramework.csproj", emptyProjectContent);
                mockFileSystem.AddFileContent(@"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\tests\MyFramework.Tests\MyFramework.Tests.csproj", emptyProjectContent);
                mockFileSystem.AddFileContent(@"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\external\MySubModule\src\MySubModule\MySubModule.csproj", emptyProjectContent);
            }
            
            mockFileSystem.AddRoot(RootName, relativeFilePath.Select(x => WorkspaceDirectoryRelativePath + x));

            if (haveSubSolutions)
            {
                const string mySubModuleConfigurationPath = @"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\external\MySubModule\MySubModule";
                relativeFilePath.Add(@"external\MyFramework\external\MySubModule\MySubModule.sln");
                relativeFilePath.Add(@"external\MyFramework\external\MySubModule\MySubModule.subsln");

                await AddConfigurationToFileSystemAsync(mySubModuleConfigurationPath + ".subsln", MySubModuleConfiguration);
                await AddSolutionToFileSystemAsync(mySubModuleConfigurationPath + ".sln", MySubModuleConfiguration);

                mockFileSystem.AddRoot(RootName, relativeFilePath.Select(x => WorkspaceDirectoryRelativePath + x));

                const string myFrameworkConfigurationPath = @"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\MyFramework";
                relativeFilePath.Add(@"external\MyFramework\MyFramework.sln");
                relativeFilePath.Add(@"external\MyFramework\MyFramework.subsln");

                await AddConfigurationToFileSystemAsync(myFrameworkConfigurationPath + ".subsln", MyFrameworkConfiguration);
                await AddSolutionToFileSystemAsync(myFrameworkConfigurationPath + ".sln", MyFrameworkConfiguration);

                mockFileSystem.AddRoot(RootName, relativeFilePath.Select(x => WorkspaceDirectoryRelativePath + x));
            }

            if (configurationContent is not null)
            {
                relativeFilePath.Add("MyApplication.sln");
                relativeFilePath.Add("MyApplication.subsln");
            }

            await AddConfigurationToFileSystemAsync(@"C:\Directory\SubDirectory\MyWorkspace\MyApplication.subsln", configurationContent);
            await AddSolutionToFileSystemAsync(@"C:\Directory\SubDirectory\MyWorkspace\MyApplication.sln", configurationContent);

            mockFileSystem.AddRoot(RootName, relativeFilePath.Select(x => WorkspaceDirectoryRelativePath + x));
            return mockFileSystem;
            
            async Task AddConfigurationToFileSystemAsync(string filePath, SubSolutionConfiguration configuration)
            {
                await using var memoryStream = new MemoryStream();

                await using (TextWriter configurationWriter = new StreamWriter(memoryStream))
                    configuration.Save(configurationWriter);

                byte[] content = memoryStream.ToArray();
                mockFileSystem.AddFileContent(filePath, content);
            }

            async Task AddSolutionToFileSystemAsync(string filePath, SubSolutionConfiguration configuration)
            {
                string defaultWorkspaceDirectoryPath = mockFileSystem.GetParentDirectoryPath(filePath)!;

                SolutionBuilderContext context = SolutionBuilderContext.FromConfiguration(configuration, mockProjectReader, defaultWorkspaceDirectoryPath, defaultWorkspaceDirectoryPath, mockFileSystem);
                var solutionBuilder = new SolutionBuilder(context);
                ISolution solution = await solutionBuilder.BuildAsync(configuration);

                var rawSolutionConverter = new SolutionConverter(mockFileSystem);
                RawSolution rawSolution = rawSolutionConverter.Convert(solution);

                await using var memoryStream = new MemoryStream();
                await rawSolution.WriteAsync(memoryStream);

                byte[] content = memoryStream.ToArray();
                mockFileSystem.AddFileContent(filePath, content);
            }
        }

        static private readonly SubSolutionConfiguration MyFrameworkConfiguration = new SubSolutionConfiguration
        {
            Root = new SolutionRootConfiguration
            {
                SolutionItems = new List<SolutionItems>
                {
                    new Projects { Path = "src/**" },
                    new Folder
                    {
                        Name = "Tests",
                        SolutionItems = new List<SolutionItems>
                        {
                            new Projects { Path = "tests/**" }
                        }
                    },
                    new Folder
                    {
                        Name = "Tools",
                        SolutionItems = new List<SolutionItems>
                        {
                            new Files { Path = "tools/**" }
                        }
                    },
                    new Folder
                    {
                        Name = "External",
                        SolutionItems = new List<SolutionItems>
                        {
                            new SubSolutions { Path = "external/**" }
                        }
                    }
                }
            }
        };

        private void CheckFolderContainsMyFramework(ISolutionFolder rootFolder, bool only = false, bool butNotExternal = false)
        {
            rootFolder.Projects.Keys.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");

            if (only)
            {
                rootFolder.FilePaths.Should().BeEmpty();
                rootFolder.Projects.Should().HaveCount(1);
                rootFolder.SubFolders.Should().HaveCount(butNotExternal ? 2 : 3);
            }
            
            ISolutionFolder testsFolder = rootFolder.SubFolders["Tests"];
            {
                testsFolder.Projects.Keys.Should().Contain("external/MyFramework/tests/MyFramework.Tests/MyFramework.Tests.csproj");

                if (only)
                {
                    testsFolder.FilePaths.Should().BeEmpty();
                    testsFolder.Projects.Should().HaveCount(1);
                    testsFolder.SubFolders.Should().BeEmpty();
                }
            }

            ISolutionFolder toolsFolder = rootFolder.SubFolders["Tools"];
            {
                toolsFolder.FilePaths.Should().Contain("external/MyFramework/tools/submit.bat");

                if (only)
                {
                    toolsFolder.FilePaths.Should().HaveCount(1);
                    toolsFolder.Projects.Should().BeEmpty();
                    toolsFolder.SubFolders.Should().BeEmpty();
                }
            }

            if (butNotExternal)
                return;

            ISolutionFolder externalFolder = rootFolder.SubFolders["External"];
            {
                CheckFolderContainsMySubModule(externalFolder, true);
            }
        }

        static private readonly SubSolutionConfiguration MySubModuleConfiguration = new SubSolutionConfiguration
        {
            Root = new SolutionRootConfiguration
            {
                SolutionItems = new List<SolutionItems>
                {
                    new Projects { Path = "src/**" }
                }
            }
        };

        private void CheckFolderContainsMySubModule(ISolutionFolder rootFolder, bool only = false)
        {
            rootFolder.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");

            if (only)
            {
                rootFolder.FilePaths.Should().BeEmpty();
                rootFolder.Projects.Should().HaveCount(1);
                rootFolder.SubFolders.Should().BeEmpty();
            }
        }

        static private void CheckConfigurationPlatforms(ISolution solution, string configurationName, string platformName,
            string[] projectConfigurationNames, string[] projectPlatformNames, bool[] projectBuild)
        {
            ISolutionConfigurationPlatform solutionConfiguration = solution.ConfigurationPlatforms.First(x => x.ConfigurationName == configurationName && x.PlatformName == platformName);
            solutionConfiguration.ConfigurationName.Should().Be(configurationName);
            solutionConfiguration.PlatformName.Should().Be(platformName);
            solutionConfiguration.FullName.Should().Be($"{configurationName}|{platformName}");
            solutionConfiguration.ProjectContexts.Should().HaveCount(3);

            string[] projectPaths =
            {
                "src/MyApplication/MyApplication.csproj",
                "src/MyApplication.Configuration/MyApplication.Configuration.csproj",
                "src/Executables/MyApplication.Console/MyApplication.Console.csproj"
            };

            int i = 0;
            foreach (string projectPath in projectPaths)
            {
                SolutionProjectContext projectContext = solutionConfiguration.ProjectContexts[projectPath];
                projectContext.ConfigurationName.Should().Be(projectConfigurationNames[i]);
                projectContext.PlatformName.Should().Be(projectPlatformNames[i]);
                projectContext.Build.Should().Be(projectBuild[i]);
                projectContext.Deploy.Should().BeFalse();
                i++;
            }
        }

        [ExcludeFromCodeCoverage]
        private class ConsoleLogger : ILogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Console.WriteLine($"[{logLevel.ToString().ToUpperInvariant()}] {state}");
            }

            public bool IsEnabled(LogLevel logLevel) => true;
            public IDisposable BeginScope<TState>(TState state) => null;
        }
    }
}
