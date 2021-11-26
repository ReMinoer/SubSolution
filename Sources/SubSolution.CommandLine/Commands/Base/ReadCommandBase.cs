using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using SubSolution.Builders;
using SubSolution.Converters;
using SubSolution.FileSystems;
using SubSolution.MsBuild;
using SubSolution.ProjectReaders;
using SubSolution.Raw;

namespace SubSolution.CommandLine.Commands.Base
{
    public abstract class ReadCommandBase : CommandBase
    {
        static private IProjectReader? _projectReader;
        static private CacheProjectReader? _cacheProjectReader;

        private Assembly? _executingAssembly;
        private string[]? _manifestResourceNames;

        [Option('d', "detailed", HelpText = "Show more details in solution output.")]
        public bool ShowDetailedSolution { get; set; }
        [Option('D', "divergent", HelpText = "Show divergent projects contexts in solution output (divergent configuration-platforms, disabled build, disabled deploy).")]
        public bool ShowDivergentProjects { get; set; }
        [Option('P', "paths", HelpText = "Show project paths in solution output.")]
        public bool ShowProjectPaths { get; set; }

        [Option('v', "verbose", HelpText = "Enable verbose log (solution build, project reading, ...)")]
        public bool Verbose { get; set; }

        [Option("dotnet", SetName = "dotnet", HelpText = "Read projects using given dotnet directory path.")]
        public string? DotNetDirectory { get; set; }
        [Option("msbuild", SetName = "msbuild", HelpText = "Read projects using given MSBuild directory path.")]
        public string? MsBuildDirectory { get; set; }
        [Option("embedded", SetName = "embedded", HelpText = "Read projects using embedded MSBuild DLLs.")]
        public bool UseEmbeddedMsBuild { get; set; }

        [Option("import-fallback", HelpText = "On missing imports or SDks, read manually Directory.Build.props and use fallback values. Result may differ from reality.")]
        public bool ImportFallback { get; set; }

        private bool UseSpecifiedMsBuild => DotNetDirectory is not null || MsBuildDirectory is not null;

        protected override sealed Task ExecuteCommandAsync()
        {
            if (_projectReader is null)
            {
                if (!SetMsBuildLocation())
                {
                    UpdateErrorCode(ErrorCode.FailLocateMsBuild);
                    return Task.CompletedTask;
                }

                var projectReader = new MsBuildProjectReader(Logger)
                {
                    LogLevel = Verbose ? LogLevel.Information : LogLevel.None,
                    ImportFallback = ImportFallback
                };

                _projectReader = projectReader;
                _cacheProjectReader = new CacheProjectReader(StandardFileSystem.Instance, _projectReader);
            }

            return ExecuteReadCommandAsync();
        }

        protected abstract Task ExecuteReadCommandAsync();

        private bool SetMsBuildLocation()
        {
            if (UseEmbeddedMsBuild)
                return RegisterEmbeddedMsBuild();
            if (UseSpecifiedMsBuild)
                return RegisterSpecifiedMsBuild();
            
            return RegisterDefaultMsBuild();
        }

        static private readonly string EmbeddedAssemblyTempFolder = Path.Combine(Path.GetTempPath(), nameof(SubSolution) + "_EmbeddedAssemblies");

        private bool RegisterEmbeddedMsBuild()
        {
            if (Directory.Exists(EmbeddedAssemblyTempFolder))
                Directory.Delete(EmbeddedAssemblyTempFolder, recursive: true);
            Directory.CreateDirectory(EmbeddedAssemblyTempFolder);

            AssemblyLoadContext.Default.Resolving += ResolveEmbeddedAssembly;
            return true;
        }

        private Assembly? ResolveEmbeddedAssembly(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            _executingAssembly ??= Assembly.GetExecutingAssembly();
            _manifestResourceNames ??= _executingAssembly.GetManifestResourceNames();

            string? resourceName = _manifestResourceNames.FirstOrDefault(x => x.EndsWith(assemblyName.Name + ".dll"));
            if (resourceName is null)
                return null;

            // Copy embedded DLL into a temporary file because MSBuild need the assembly path.
            string tempAssemblyPath = Path.Combine(EmbeddedAssemblyTempFolder, assemblyName.Name + ".dll");

            using (Stream stream = _executingAssembly.GetManifestResourceStream(resourceName)!)
            using (FileStream tempAssemblyStream = File.Create(tempAssemblyPath))
                stream.CopyTo(tempAssemblyStream);

            return assemblyLoadContext.LoadFromAssemblyPath(tempAssemblyPath);
        }

        private bool RegisterSpecifiedMsBuild()
        {
            string msBuildPath = DotNetDirectory ?? MsBuildDirectory!;
            if (!Directory.Exists(msBuildPath))
            {
                Logger.LogError($"Specified MSBuild directory \"{msBuildPath}\" not found.");
                return false;
            }

            // Code below copy behavior of MsBuildLocator.RegisterInstance

            if (!Path.EndsInDirectorySeparator(msBuildPath))
                msBuildPath += Path.DirectorySeparatorChar;

            if (DotNetDirectory is not null)
            {
                typeof(MSBuildLocator)
                    .GetMethod("ApplyDotNetSdkEnvironmentVariables", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, new object[] { msBuildPath });
            }

            string nugetPath = Path.GetFullPath(Path.Combine(msBuildPath, "..", "..", "..", "Common7", "IDE", "CommonExtensions", "Microsoft", "NuGet"));
            if (Directory.Exists(nugetPath))
            {
                MSBuildLocator.RegisterMSBuildPath(new [] { msBuildPath, nugetPath });
            }
            else
            {
                MSBuildLocator.RegisterMSBuildPath(msBuildPath);
            }

            return true;
        }

        static private bool RegisterDefaultMsBuild()
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
                return true;
            }
            catch (InvalidOperationException)
            {
                Logger.LogError("No MSBuild install found.");
                Logger.LogError("Use options \"dotnet\" or \"msbuild\" to specify an MSBuild path, " +
                    "or \"embedded\" to use the embedded but limited DLLs.");
                return false;
            }
        }

        protected void LogSolution(ISolution solution)
        {
            var solutionLogger = new SolutionLogger
            {
                ShowProjectTypes = ShowDetailedSolution,
                ShowAllProjectContexts = ShowDetailedSolution && !ShowDivergentProjects,
                ShowDivergentProjectContexts = ShowDivergentProjects,
                ShowFilePaths = ShowProjectPaths
            };
            string logMessage = solutionLogger.Convert(solution);

            LogEmptyLine();
            Log(logMessage);
        }

        protected async Task<SolutionBuilderContext?> GetBuildContextAsync(string configurationFilePath)
        {
            configurationFilePath = StandardFileSystem.Instance.MakeAbsolutePath(Environment.CurrentDirectory, configurationFilePath);

            if (!CheckFileExist(configurationFilePath))
                return null;

            SolutionBuilderContext context = await SolutionBuilderContext.FromConfigurationFileAsync(configurationFilePath, _cacheProjectReader!);
            context.Logger = Logger;
            context.LogLevel = Verbose ? LogLevel.Information : LogLevel.None;

            return context;
        }

        protected async Task<Solution?> BuildSolutionAsync(SolutionBuilderContext context)
        {
            try
            {
                SolutionBuilder solutionBuilder = new SolutionBuilder(context);
                Solution solution = await solutionBuilder.BuildAsync(context.Configuration);

                foreach (Issue issue in solutionBuilder.Issues)
                    LogIssue(issue);

                if (solutionBuilder.Issues.Any(x => x.Level == IssueLevel.Error))
                {
                    LogEmptyLine();
                    LogError($"Failed to build {context.ConfigurationFilePath}.");

                    if (solutionBuilder.Issues.Any(x => x.Exception is ProjectReadException))
                        LogMsBuildConfigurationMessage(solutionBuilder.Issues);

                    UpdateErrorCode(ErrorCode.FailBuildSolution);
                    return null;
                }

                return solution;
            }
            catch (Exception exception)
            {
                LogError($"Failed to build {context.ConfigurationFilePath}.", exception);
                UpdateErrorCode(ErrorCode.FailBuildSolution);
                return null;
            }
        }

        protected async Task<RawSolution?> ReadSolutionAsync(string filePath)
        {
            try
            {
                await using FileStream fileStream = File.OpenRead(filePath);
                return await RawSolution.ReadAsync(fileStream);
            }
            catch (Exception exception)
            {
                LogError($"Failed to read {filePath}.", exception);
                UpdateErrorCode(ErrorCode.FailReadSolution);
                return null;
            }
        }

        protected async Task<ISolution?> ConvertRawSolutionAsync(RawSolution rawSolution, string filePath, bool skipProjectLoading)
        {
            try
            {
                string solutionDirectoryPath = StandardFileSystem.Instance.GetParentDirectoryPath(filePath)!;

                RawSolutionConverter converter = new RawSolutionConverter(StandardFileSystem.Instance, _cacheProjectReader!);
                ISolution solution = await converter.ConvertAsync(rawSolution, solutionDirectoryPath, skipProjectLoading);

                foreach (Issue issue in converter.Issues)
                    LogIssue(issue);

                if (converter.Issues.Any(x => x.Level == IssueLevel.Error))
                {
                    LogEmptyLine();
                    LogError($"Failed to interpret {filePath}.");

                    if (converter.Issues.Any(x => x.Exception is ProjectReadException))
                        LogMsBuildConfigurationMessage(converter.Issues);

                    UpdateErrorCode(ErrorCode.FailInterpretSolution);
                    return null;
                }

                return solution;
            }
            catch (Exception exception)
            {
                LogError($"Failed to interpret {filePath}.", exception);
                UpdateErrorCode(ErrorCode.FailInterpretSolution);
                return null;
            }
        }

        private void LogMsBuildConfigurationMessage(List<Issue> issues)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            if (issues.Select(x => x.Exception).OfType<ProjectReadException>()
                .Select(x => x.InnerException).OfType<MissingMethodException>()
                .Any(x => x.Message.Contains("System.AppDomainSetup.get_ConfigurationFile")))
            {
                LogEmptyLine();
                Log("\"Method not found\" errors are known to happen if you are using a .NET Framework build of MSBuild.");
                Log("Those errors actually result of others project reading errors. Try to fix those first (see below).");
            }

            LogEmptyLine();
            Log("Project reading errors can be caused by missing dependencies in your Visual Studio, dotnet or MSBuild setup.");
            Log("Use \"import-fallback\" to read project even on missing imports/SDKs.");
            Log("Use \"msbuild\" or \"dotnet\" if you want to use another MBuild setup.");

            Console.ResetColor();
        }
    }
}