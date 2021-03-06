using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SubSolution.Builders.Configuration;
using SubSolution.Builders.GlobPatterns.Mock;
using SubSolution.Converters;
using SubSolution.ProjectReaders.Mock;
using SubSolution.Raw;
using SubSolution.Utils;

namespace SubSolution.Builders.Tests
{
    public partial class SolutionBuilderTests
    {
        private const string RootName = @"C:";
        private const string AlternativeRootName = @"D:";

        private const string WorkspaceDirectoryRelativePath = @"Directory\SubDirectory\MyWorkspace\";
        static private readonly string WorkspaceDirectoryPath = $@"{RootName}\{WorkspaceDirectoryRelativePath}";

        private const string TestConfigurationFilePath = @"C:\Directory\SubDirectory\MyWorkspace\MyApplication.subsln";

        private async Task<ISolution> ProcessConfigurationMockFileAsync(Subsln configuration, bool haveSubSolutions = false)
        {
            (ISolution solution, Issue[] issues) = await ProcessConfigurationMockFileWithIssuesAsync(configuration, haveSubSolutions);
            issues.Should().BeEmpty();
            return solution;
        }

        private Task<(ISolution, Issue[])> ProcessConfigurationMockFileWithIssuesAsync(Subsln configuration, bool haveSubSolutions = false)
        {
            return ProcessConfigurationAsync(configuration, haveSubSolutions, (fileSystem, projectReader)
                => SolutionBuilderContext.FromConfigurationFileAsync(TestConfigurationFilePath, projectReader, fileSystem));
        }

        private async Task<ISolution> ProcessConfigurationAsync(Subsln configuration, string outputDirectoryPath, string? workspaceDirectoryPath, bool haveSubSolutions = false)
        {
            (ISolution solution, Issue[] issues) = await ProcessConfigurationWithIssuesAsync(configuration, outputDirectoryPath, workspaceDirectoryPath, haveSubSolutions);
            issues.Should().BeEmpty();
            return solution;
        }

        private Task<(ISolution, Issue[])> ProcessConfigurationWithIssuesAsync(Subsln configuration, string outputDirectoryPath, string? workspaceDirectoryPath, bool haveSubSolutions)
        {
            return ProcessConfigurationAsync(configuration, haveSubSolutions, (fileSystem, projectReader)
                => Task.FromResult(SolutionBuilderContext.FromConfiguration(Environment.CurrentDirectory, configuration, projectReader, outputDirectoryPath, workspaceDirectoryPath, fileSystem)));
        }

        private Task<SolutionBuilderContext> GetConfigurationMockFileContextAsync(Subsln configuration)
        {
            return PrepareContextAsync(configuration, haveSubSolutions: false, (fileSystem, projectReader)
                => SolutionBuilderContext.FromConfigurationFileAsync(TestConfigurationFilePath, projectReader, fileSystem));
        }

        private async Task<SolutionBuilderContext> PrepareContextAsync(Subsln configuration, bool haveSubSolutions,
            Func<MockGlobPatternFileSystem, MockProjectReader, Task<SolutionBuilderContext>> createContext)
        {
            ILogger logger = new ConsoleLogger();
            logger.LogDebug("Configuration XML:" + Environment.NewLine + configuration.Untyped);

            var mockFileSystem = new MockGlobPatternFileSystem(new Regex(@"\w:"));

            var mockProjectReader = new MockProjectReader(
                mockFileSystem, new SolutionProject(ProjectType.CSharpDotNetSdk)
                {
                    Configurations = { "Debug, Release" },
                    Platforms = { "Any CPU" },
                    CanBuild = true
                }
            )
            {
                Projects =
                {
                    [@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication\MyApplication.csproj"] = new SolutionProject(ProjectType.CSharpDotNetSdk)
                    {
                        Configurations = { "Debug", "Release" },
                        Platforms = { "Any CPU" },
                        CanBuild = true
                    },
                    [@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication.Core\MyApplication.Core.csproj"] = new SolutionProject(ProjectType.CSharpDotNetSdk)
                    {
                        Configurations = { "Debug", "Release" },
                        Platforms = { "Any CPU" },
                        CanBuild = true
                    },
                    [@"C:\Directory\SubDirectory\MyWorkspace\src\MyApplication.Configuration\MyApplication.Configuration.csproj"] = new SolutionProject(ProjectType.CSharpDotNetSdk)
                    {
                        Configurations = { "debug", "release" },
                        Platforms = { "any cpu" },
                        CanBuild = true,
                        ProjectDependencies =
                        {
                            "../MyApplication/MyApplication.csproj"
                        }
                    },
                    [@"C:\Directory\SubDirectory\MyWorkspace\src\Executables\MyApplication.Console\MyApplication.Console.csproj"] = new SolutionProject(ProjectType.CSharpDotNetSdk)
                    {
                        Configurations = { "Debug", "Release", "Final" },
                        Platforms = { "x86", "x64" },
                        CanBuild = true,
                        ProjectDependencies =
                        {
                            "../../MyApplication.Core/MyApplication.Core.csproj",
                            "../../MyApplication.Configuration/MyApplication.Configuration.csproj"
                        }
                    },
                    [@"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\src\MyFramework\MyFramework.csproj"] = new SolutionProject(ProjectType.CSharpDotNetSdk)
                    {
                        Configurations = { "Debug", "Release" },
                        Platforms = { "Any CPU" },
                        CanBuild = true,
                        ProjectDependencies =
                        {
                            "../../external/MySubModule/src/MySubModule/MySubModule.csproj"
                        }
                    },
                    [@"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\tests\MyFramework.Tests\MyFramework.Tests.csproj"] = new SolutionProject(ProjectType.CSharpDotNetSdk)
                    {
                        Configurations = { "Debug", "Release" },
                        Platforms = { "Any CPU" },
                        CanBuild = true,
                        ProjectDependencies =
                        {
                            "../../src/MyFramework/MyFramework.csproj"
                        }
                    },
                    [@"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\external\MySubModule\src\MySubModule\MySubModule.csproj"] = new SolutionProject(ProjectType.CSharpDotNetSdk)
                    {
                        Configurations = { "Debug", "Release" },
                        Platforms = { "Any CPU" },
                        CanBuild = true
                    }
                }
            };

            await ConfigureMockFileSystemAsync(mockFileSystem, configuration, haveSubSolutions, mockProjectReader);

            SolutionBuilderContext context = await createContext(mockFileSystem, mockProjectReader);
            context.Logger = logger;
            context.LogLevel = LogLevel.Debug;

            return context;
        }

        private async Task<(ISolution, Issue[])> ProcessConfigurationAsync(Subsln configuration, bool haveSubSolutions,
            Func<MockGlobPatternFileSystem, MockProjectReader, Task<SolutionBuilderContext>> createContext)
        {
            SolutionBuilderContext context = await PrepareContextAsync(configuration, haveSubSolutions, createContext);

            var solutionBuilder = new SolutionBuilder(context);
            ISolution solution = await solutionBuilder.BuildAsync(context.Configuration);

            Issue[] buildIssues = solutionBuilder.Issues.ToArray();

            var solutionLogger = new SolutionLogger(fileSystem: context.FileSystem)
            {
                ShowHeaders = true,
                ShowHierarchy = true,
                ShowConfigurationPlatforms = true,
                ShowAllProjectContexts = true,
                ShowDivergentProjectContexts = false
            };

            string logMessage = solutionLogger.Convert(solution);
            context.Logger.LogDebug(logMessage);

            // Full pipe checks
            var rawSolutionConverter = new SolutionConverter(context.FileSystem!, context.ProjectReader, context.SolutionDirectoryPath)
            {
                Logger = NullLogger.Instance,
                LogLevel = LogLevel.Trace
            };
            var solutionConverter = new RawSolutionConverter(context.FileSystem!, context.ProjectReader);
            
            ISolution checkSolution = solution;
            RawSolution rawSolution;

            rawSolution = await rawSolutionConverter.ConvertAsync(checkSolution);
            using var firstPassStream = new MemoryStream();
            await rawSolution.WriteAsync(firstPassStream);
            firstPassStream.Position = 0;
            rawSolution = await RawSolution.ReadAsync(firstPassStream);
            checkSolution = await solutionConverter.ConvertAsync(rawSolution, context.SolutionDirectoryPath);
            solutionConverter.Issues.Should().BeEmpty();

            rawSolution = await rawSolutionConverter.ConvertAsync(checkSolution);
            using var secondPassStream = new MemoryStream();
            await rawSolution.WriteAsync(secondPassStream);
            secondPassStream.Position = 0;
            rawSolution = await RawSolution.ReadAsync(secondPassStream);
            checkSolution = await solutionConverter.ConvertAsync(rawSolution, context.SolutionDirectoryPath);
            solutionConverter.Issues.Should().BeEmpty();

            SolutionBuilderContext referenceContext = SolutionBuilderContext.FromConfiguration(Environment.CurrentDirectory, MyApplicationConfiguration, context.ProjectReader, WorkspaceDirectoryPath, WorkspaceDirectoryPath, context.FileSystem);
            var referenceSolutionBuilder = new SolutionBuilder(referenceContext);
            ISolution referenceSolution = await referenceSolutionBuilder.BuildAsync(referenceContext.Configuration);
            RawSolution referenceRawSolution = await rawSolutionConverter.ConvertAsync(referenceSolution);

            await rawSolutionConverter.UpdateAsync(referenceRawSolution, checkSolution);
            CheckRawSolutionAreEqual(rawSolution, referenceRawSolution);

            using var thirdPassStream = new MemoryStream();
            await referenceRawSolution.WriteAsync(thirdPassStream);
            thirdPassStream.Position = 0;
            referenceRawSolution = await RawSolution.ReadAsync(thirdPassStream);
            checkSolution = await solutionConverter.ConvertAsync(referenceRawSolution, context.SolutionDirectoryPath);
            solutionConverter.Issues.Should().BeEmpty();

            referenceRawSolution = await rawSolutionConverter.ConvertAsync(referenceSolution);

            await rawSolutionConverter.UpdateAsync(referenceRawSolution, checkSolution);
            CheckRawSolutionAreEqual(rawSolution, referenceRawSolution);
            
            using var forthPassStream = new MemoryStream();
            await referenceRawSolution.WriteAsync(forthPassStream);
            forthPassStream.Position = 0;
            await RawSolution.ReadAsync(forthPassStream);
            _ = await solutionConverter.ConvertAsync(referenceRawSolution, context.SolutionDirectoryPath);
            solutionConverter.Issues.Should().BeEmpty();

            return (solution, buildIssues);
        }

        private async Task ConfigureMockFileSystemAsync(MockGlobPatternFileSystem mockFileSystem, Subsln configurationContent, bool haveSubSolutions, MockProjectReader mockProjectReader)
        {
            var relativeFilePath = new List<string>();

            relativeFilePath.AddRange(new[]
            {
                @"tools\pull.bat",
                @"tools\submit.bat",
                @"tools\debug\Debug.exe",
                @"src\MyApplication\MyClass.cs",
                @"src\MyApplication\MyApplication.csproj",
                @"src\MyApplication.Core\MyClass.cs",
                @"src\MyApplication.Core\MyApplication.Core.csproj",
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
                    @"external\MyFramework\external\MySubModule\README.txt",
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

                AddConfigurationToFileSystem(mySubModuleConfigurationPath + ".subsln", MySubModuleConfiguration);
                await AddSolutionToFileSystemAsync(mySubModuleConfigurationPath + ".sln", MySubModuleConfiguration);

                mockFileSystem.AddRoot(RootName, relativeFilePath.Select(x => WorkspaceDirectoryRelativePath + x));

                const string myFrameworkConfigurationPath = @"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\MyFramework";
                relativeFilePath.Add(@"external\MyFramework\MyFramework.sln");
                relativeFilePath.Add(@"external\MyFramework\MyFramework.subsln");

                AddConfigurationToFileSystem(myFrameworkConfigurationPath + ".subsln", MyFrameworkConfiguration);
                await AddSolutionToFileSystemAsync(myFrameworkConfigurationPath + ".sln", MyFrameworkConfiguration);

                mockFileSystem.AddRoot(RootName, relativeFilePath.Select(x => WorkspaceDirectoryRelativePath + x));
            }
            
            relativeFilePath.Add("MyApplication.sln");
            relativeFilePath.Add("MyApplication.subsln");

            AddConfigurationToFileSystem(@"C:\Directory\SubDirectory\MyWorkspace\MyApplication.subsln", configurationContent);
            await AddSolutionToFileSystemAsync(@"C:\Directory\SubDirectory\MyWorkspace\MyApplication.sln", configurationContent);

            mockFileSystem.AddRoot(RootName, relativeFilePath.Select(x => WorkspaceDirectoryRelativePath + x));

            relativeFilePath.Clear();
            relativeFilePath.Add(@"Directory\ExternalProject.csproj");
            relativeFilePath.Add(@"Directory\ExternalFile.txt");

            mockFileSystem.AddRoot(AlternativeRootName, relativeFilePath);

            const string externalSolutionConfigurationPath = @"D:\Directory\ExternalSolution";
            relativeFilePath.Add(@"Directory\ExternalSolution.sln");
            relativeFilePath.Add(@"Directory\ExternalSolution.subsln");

            AddConfigurationToFileSystem(externalSolutionConfigurationPath + ".subsln", ExternalSolutionConfiguration);
            await AddSolutionToFileSystemAsync(externalSolutionConfigurationPath + ".sln", ExternalSolutionConfiguration);

            mockFileSystem.AddRoot(AlternativeRootName, relativeFilePath);

            void AddConfigurationToFileSystem(string filePath, Subsln configuration)
            {
                using var memoryStream = new MemoryStream();

                using (TextWriter configurationWriter = new StreamWriter(memoryStream))
                    configuration.Save(configurationWriter);

                byte[] content = memoryStream.ToArray();
                mockFileSystem.AddFileContent(filePath, content);
            }

            async Task AddSolutionToFileSystemAsync(string filePath, Subsln configuration)
            {
                string defaultWorkspaceDirectoryPath = mockFileSystem.GetParentDirectoryPath(filePath)!;

                SolutionBuilderContext context = SolutionBuilderContext.FromConfiguration(Environment.CurrentDirectory, configuration, mockProjectReader, defaultWorkspaceDirectoryPath, defaultWorkspaceDirectoryPath, mockFileSystem);
                var solutionBuilder = new SolutionBuilder(context);
                ISolution solution = await solutionBuilder.BuildAsync(configuration);

                var rawSolutionConverter = new SolutionConverter(mockFileSystem, context.ProjectReader, context.SolutionDirectoryPath);
                RawSolution rawSolution = await rawSolutionConverter.ConvertAsync(solution);

                using var memoryStream = new MemoryStream();
                await rawSolution.WriteAsync(memoryStream);

                byte[] content = memoryStream.ToArray();
                mockFileSystem.AddFileContent(filePath, content);
            }
        }

        static private readonly Subsln MyApplicationConfiguration = new Subsln
        {
            Root = new SolutionRoot
            {
                SolutionItems = new List<SolutionItems>
                {
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
                        Name = "Executables",
                        SolutionItems = new List<SolutionItems>
                        {
                            new Projects { Path = "src/Executables/**" }
                        }
                    },
                    new Projects { Path = "src/**" }
                }
            }
        };

        static private readonly Subsln MyFrameworkConfiguration = new Subsln
        {
            Root = new SolutionRoot
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

        static private readonly Subsln MySubModuleConfiguration = new Subsln
        {
            Root = new SolutionRoot
            {
                SolutionItems = new List<SolutionItems>
                {
                    new Files { Path = "*.txt" },
                    new Projects { Path = "src/**" }
                }
            }
        };

        private void CheckFolderContainsMySubModule(ISolutionFolder rootFolder, bool only = false)
        {
            rootFolder.FilePaths.Should().Contain("external/MyFramework/external/MySubModule/README.txt");
            rootFolder.Projects.Keys.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");

            if (only)
            {
                rootFolder.FilePaths.Should().HaveCount(1);
                rootFolder.Projects.Should().HaveCount(1);
                rootFolder.SubFolders.Should().BeEmpty();
            }
        }

        static private readonly Subsln ExternalSolutionConfiguration = new Subsln
        {
            Root = new SolutionRoot
            {
                SolutionItems = new List<SolutionItems>
                {
                    new Files { Path = "*.txt" },
                    new Projects()
                }
            }
        };

        private void CheckFolderContainsExternalSolution(ISolutionFolder rootFolder)
        {
            rootFolder.FilePaths.Should().HaveCount(1);
            rootFolder.FilePaths.Should().Contain(@"D:\Directory\ExternalFile.txt");
            rootFolder.Projects.Should().HaveCount(1);
            rootFolder.Projects.Keys.Should().Contain(@"D:\Directory\ExternalProject.csproj");
            rootFolder.SubFolders.Should().BeEmpty();
        }

        static private void CheckConfigurationPlatforms(ISolution solution, string configurationName, string platformName,
            string[] projectConfigurationNames, string[] projectPlatformNames, bool[] projectBuild)
        {
            ISolutionConfigurationPlatform solutionConfiguration = solution.ConfigurationPlatforms.First(x => x.ConfigurationName == configurationName && x.PlatformName == platformName);
            solutionConfiguration.ConfigurationName.Should().Be(configurationName);
            solutionConfiguration.PlatformName.Should().Be(platformName);
            solutionConfiguration.FullName.Should().Be($"{configurationName}|{platformName}");
            solutionConfiguration.ProjectContexts.Should().HaveCount(4);

            string[] projectPaths =
            {
                "src/MyApplication/MyApplication.csproj",
                "src/MyApplication.Core/MyApplication.Core.csproj",
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

        private void CheckRawSolutionAreEqual(RawSolution rawSolution, RawSolution expected)
        {
            rawSolution.Projects.Should().HaveCount(expected.Projects.Count);

            foreach (RawSolution.Project project in rawSolution.Projects)
            {
                RawSolution.Project expectedProject = expected.Projects.Should().Contain(x => x.Path == project.Path).Which;
                project.TypeGuid.Should().Be(expectedProject.TypeGuid);
                project.Name.Should().Be(expectedProject.Name);
                //project.ProjectGuid.Should().Be(expectedProject.ProjectGuid);

                project.Sections.Should().HaveCount(expectedProject.Sections.Count);

                foreach (RawSolution.Section section in project.Sections)
                {
                    RawSolution.Section expectedSection = expectedProject.Sections.Should().Contain(x => x.Parameter == section.Parameter).Which;
                    section.Name.Should().Be(expectedSection.Name);
                    section.Arguments.Should().ContainInOrder(expectedSection.Arguments);
                    section.OrderedValuePairs.Should().BeEquivalentTo(expectedSection.OrderedValuePairs);
                    section.ValuesByKey.Should().BeEquivalentTo(expectedSection.ValuesByKey);
                }
            }

            rawSolution.GlobalSections.Should().HaveCount(expected.GlobalSections.Count);

            foreach (RawSolution.Section globalSection in rawSolution.GlobalSections)
            {
                RawSolution.Section expectedGlobalSection = expected.GlobalSections.Should().Contain(x => x.Parameter == globalSection.Parameter).Which;
                globalSection.Name.Should().Be(expectedGlobalSection.Name);
                globalSection.Arguments.Should().ContainInOrder(expectedGlobalSection.Arguments);
                //globalSection.OrderedValuePairs.Should().BeEquivalentTo(expectedGlobalSection.OrderedValuePairs);
                //globalSection.ValuesByKey.Should().BeEquivalentTo(expectedGlobalSection.ValuesByKey);
                globalSection.OrderedValuePairs.Should().HaveCount(expectedGlobalSection.OrderedValuePairs.Count);
                globalSection.ValuesByKey.Should().HaveCount(expectedGlobalSection.ValuesByKey.Count);
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
            public IDisposable BeginScope<TState>(TState state) => new Disposable();
        }
    }
}
