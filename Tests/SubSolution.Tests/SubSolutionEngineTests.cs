using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using SubSolution.Builders;
using SubSolution.Configuration;
using SubSolution.FileSystems;

namespace SubSolution.Tests
{
    public partial class SubSolutionEngineTests
    {
        private const string RootName = @"C:";
        private const string WorkspaceDirectoryRelativePath = @"Directory\SubDirectory\MyWorkspace\";
        static private readonly string WorkspaceDirectoryPath = $@"{RootName}\{WorkspaceDirectoryRelativePath}";

        private SolutionBuilder ProcessConfigurationMockFile(SubSolutionConfiguration configuration, bool haveSubSolutions = false)
            => SubSolutionEngine.ProcessConfigurationFile(@"C:\Directory\SubDirectory\MyWorkspace\MyApplication.subsln", GetMockFileSystem(configuration, haveSubSolutions));
        private SolutionBuilder ProcessConfiguration(SubSolutionConfiguration configuration, string workspaceDirectoryPath, bool haveSubSolutions = false)
            => SubSolutionEngine.ProcessConfiguration(configuration, workspaceDirectoryPath, GetMockFileSystem(null, haveSubSolutions));

        private MockSubSolutionFileSystem GetMockFileSystem(SubSolutionConfiguration configurationContent, bool haveSubSolutions)
        {
            var mockFileSystem = new MockSubSolutionFileSystem();
            var relativeFilePath = new List<string>();
            
            if (configurationContent is not null)
            {
                relativeFilePath.Add("MyApplication.subsln");
                AddConfigurationToFileSystem(mockFileSystem, @"C:\Directory\SubDirectory\MyWorkspace\MyApplication.subsln", configurationContent);
            }

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

            if (haveSubSolutions)
            {
                relativeFilePath.AddRange(new []
                {
                    @"external\MyFramework\MyFramework.subsln",
                    @"external\MyFramework\tools\submit.bat",
                    @"external\MyFramework\src\MyFramework\MyFramework.csproj",
                    @"external\MyFramework\src\MyFramework\MyClass.cs",
                    @"external\MyFramework\tests\MyFramework.Tests\MyFramework.Tests.csproj",
                    @"external\MyFramework\tests\MyFramework.Tests\MyTests.cs",
                    @"external\MyFramework\external\MySubModule\MySubModule.subsln",
                    @"external\MyFramework\external\MySubModule\src\MySubModule\MySubModule.csproj",
                    @"external\MyFramework\external\MySubModule\src\MySubModule\MyClass.cs",
                });

                var myFrameworkConfigurationPath = @"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\MyFramework.subsln";
                AddConfigurationToFileSystem(mockFileSystem, myFrameworkConfigurationPath, MyFrameworkConfiguration);

                var mySubModuleConfigurationPath = @"C:\Directory\SubDirectory\MyWorkspace\external\MyFramework\external\MySubModule\MySubModule.subsln";
                AddConfigurationToFileSystem(mockFileSystem, mySubModuleConfigurationPath, MySubModuleConfiguration);
            }

            mockFileSystem.AddRoot(RootName, relativeFilePath.Select(x => WorkspaceDirectoryRelativePath + x));
            return mockFileSystem;
        }

        private void AddConfigurationToFileSystem(MockSubSolutionFileSystem mockFileSystem, string filePath, SubSolutionConfiguration configuration)
        {
            using var memoryStream = new MemoryStream();

            using (TextWriter configurationWriter = new StreamWriter(memoryStream))
                configuration.Save(configurationWriter);

            var content = memoryStream.ToArray();
            mockFileSystem.AddFileContent(filePath, content);
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

        private void CheckFolderContainsMyFramework(SolutionBuilder.Folder rootFolder, bool only = false, bool butNotExternal = false)
        {
            rootFolder.ProjectPaths.Should().Contain("external/MyFramework/src/MyFramework/MyFramework.csproj");

            if (only)
            {
                rootFolder.FilePaths.Should().BeEmpty();
                rootFolder.ProjectPaths.Should().HaveCount(1);
                rootFolder.SubFolders.Should().HaveCount(butNotExternal ? 2 : 3);
            }

            var testsFolder = rootFolder.SubFolders.Should().ContainKey("Tests").WhichValue;
            {
                testsFolder.ProjectPaths.Should().Contain("external/MyFramework/tests/MyFramework.Tests/MyFramework.Tests.csproj");

                if (only)
                {
                    testsFolder.FilePaths.Should().BeEmpty();
                    testsFolder.ProjectPaths.Should().HaveCount(1);
                    testsFolder.SubFolders.Should().BeEmpty();
                }
            }

            var toolsFolder = rootFolder.SubFolders.Should().ContainKey("Tools").WhichValue;
            {
                toolsFolder.FilePaths.Should().Contain("external/MyFramework/tools/submit.bat");

                if (only)
                {
                    toolsFolder.FilePaths.Should().HaveCount(1);
                    toolsFolder.ProjectPaths.Should().BeEmpty();
                    toolsFolder.SubFolders.Should().BeEmpty();
                }
            }

            if (butNotExternal)
                return;

            var externalFolder = rootFolder.SubFolders.Should().ContainKey("External").WhichValue;
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

        private void CheckFolderContainsMySubModule(SolutionBuilder.Folder rootFolder, bool only = false)
        {
            rootFolder.ProjectPaths.Should().Contain("external/MyFramework/external/MySubModule/src/MySubModule/MySubModule.csproj");

            if (only)
            {
                rootFolder.FilePaths.Should().BeEmpty();
                rootFolder.ProjectPaths.Should().HaveCount(1);
                rootFolder.SubFolders.Should().BeEmpty();
            }
        }
    }
}
