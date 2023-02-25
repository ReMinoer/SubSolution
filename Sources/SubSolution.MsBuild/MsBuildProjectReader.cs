using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Extensions.Logging;
using SubSolution.ProjectReaders;

namespace SubSolution.MsBuild
{
    [ExcludeFromCodeCoverage]
    public class MsBuildProjectReader : IProjectReader
    {
        private readonly ILogger? _logger;
        public LogLevel LogLevel { get; set; } = LogLevel.Trace;
        
        public bool ImportFallback { get; set; }
        const string ImportNotFoundErrorCode = "MSB4019";

        static MsBuildProjectReader()
        {
        }

        public MsBuildProjectReader(ILogger? logger = null)
        {
            _logger = logger;
        }

        public async Task<ISolutionProject> ReadAsync(string absoluteProjectPath)
        {
            try
            {
                return await ReadProjectAsync(absoluteProjectPath);
            }
            catch (ProjectReadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ProjectReadException(absoluteProjectPath, ex.Message, ex);
            }
        }

        private async Task<ISolutionProject> ReadProjectAsync(string absoluteProjectPath)
        {
            if (ProjectFileExtension.Vdproj.IsExtensionOf(absoluteProjectPath))
                return await InstallerProjectReader.ReadInstallerProjectAsync(absoluteProjectPath);

            ProjectWrapper project = await Task.Run(() => ReadProject(absoluteProjectPath));

            var solutionProject = new SolutionProject(GetType(project))
            {
                CanBuild = CanBuild(project),
                CanDeploy = CanDeploy(project),
                AlwaysDeploy = AlwaysDeploy(project)
            };

            GetConfigurationsAndPlatforms(project, solutionProject);

            if (solutionProject.Type != ProjectType.Shared && solutionProject.Type != ProjectType.SharedItems)
            {
                if (solutionProject.Configurations.Count == 0)
                {
                    if (ImportFallback)
                    {
                        solutionProject.Configurations.Add("Debug");
                        solutionProject.Configurations.Add("Release");
                    }
                    else
                    {
                        throw new ProjectReadException(project.FullPath, "No configuration found.");
                    }
                }

                if (solutionProject.Platforms.Count == 0)
                    solutionProject.Platforms.Add("Any CPU");
            }

            GetProjectDependencies(project, solutionProject);

            LogProject(absoluteProjectPath, solutionProject);
            return solutionProject;
        }

        private ProjectWrapper ReadProject(string absoluteProjectPath)
        {
            try
            {
                var project = new Project(absoluteProjectPath, null, null, new ProjectCollection());
                return new ProjectWrapper(project);
            }
            catch (InvalidProjectFileException ex)
            {
                if (!ImportFallback || ex.ErrorCode != ImportNotFoundErrorCode)
                    throw;

                Project project = new Project(absoluteProjectPath, null, null, new ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports);
                Project? directoryBuildProps = null;

                string? directoryBuildPropsPath = GetDirectoryBuildPropsPath(absoluteProjectPath);
                if (directoryBuildPropsPath != null)
                {
                    directoryBuildProps = new Project(directoryBuildPropsPath, null, null, new ProjectCollection());
                }

                return new ProjectWrapper(project, directoryBuildProps);
            }
        }

        static private string? GetDirectoryBuildPropsPath(string absoluteProjectPath)
        {
            string? directoryPath = Path.GetDirectoryName(absoluteProjectPath);
            while (directoryPath != null)
            {
                string directoryBuildPropsPath = Path.Combine(directoryPath, "Directory.Build.props");
                if (File.Exists(directoryBuildPropsPath))
                    return directoryBuildPropsPath;

                directoryPath = Path.GetDirectoryName(directoryPath);
            }

            return null;
        }

        static private bool CanBuild(ProjectWrapper project)
        {
            return !ProjectFileExtension.Pyproj.IsExtensionOf(project.FullPath);
        }

        static private bool CanDeploy(ProjectWrapper project)
        {
            return CanDeployExtensions.Any(x => x.IsExtensionOf(project.FullPath))
                || string.Equals(project.GetPropertyValue("OutputType"), "AppContainerExe", StringComparison.OrdinalIgnoreCase);
        }

        static private readonly ProjectFileExtension[] CanDeployExtensions =
        {
            ProjectFileExtension.Wapproj,
            ProjectFileExtension.Sqlproj
        };

        static private bool AlwaysDeploy(ProjectWrapper project)
        {
            return ProjectFileExtension.Sqlproj.IsExtensionOf(project.FullPath);
        }

        static private ProjectType GetType(ProjectWrapper project)
        {
            string extension = Path.GetExtension(project.FullPath).TrimStart('.');

            return ProjectTypes.FromExtension(extension, () => HasDotNetSdk(project))
                ?? throw new NotSupportedException($"Failed to resolve a project type from extension \"{extension}\".");
        }
        
        static private void GetConfigurationsAndPlatforms(ProjectWrapper project, SolutionProject solutionProject)
        {
            if (HasDotNetSdk(project))
            {
                ReadConfigurationsAndPlatformsSdkProperties(project, solutionProject);
                return;
            }

            if (HasProjectConfigurationItems(project))
            {
                ReadProjectConfigurationItems(project, solutionProject);
                return;
            }

            ReadConfigurationPlatformsConditions(project, solutionProject);
        }

        static private bool HasDotNetSdk(ProjectWrapper project)
        {
            return project.GetSdks().Any(x => x.StartsWith("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase));
        }

        static private void ReadConfigurationsAndPlatformsSdkProperties(ProjectWrapper project, SolutionProject solutionProject)
        {
            string? configurations = project.GetPropertyValue("Configurations");
            if (configurations != null)
                solutionProject.Configurations.AddRange(configurations.Split(';', StringSplitOptions.RemoveEmptyEntries));

            string? platforms = project.GetPropertyValue("Platforms");
            if (platforms != null)
                solutionProject.Platforms.AddRange(platforms.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(CleanPlatform));
        }

        static private bool HasProjectConfigurationItems(ProjectWrapper project)
        {
            return HasProjectConfigurationItemsExtensions.Any(x => x.IsExtensionOf(project.FullPath));
        }

        static private readonly ProjectFileExtension[] HasProjectConfigurationItemsExtensions =
        {
            ProjectFileExtension.Vcxproj,
            ProjectFileExtension.Vcproj,
            ProjectFileExtension.Wapproj
        };

        static private void ReadProjectConfigurationItems(ProjectWrapper project, SolutionProject solutionProject)
        {
            foreach (string configuration in project.GetItemsValues("ProjectConfiguration", "Configuration"))
            {
                if (!string.IsNullOrEmpty(configuration) && !solutionProject.Configurations.Contains(configuration))
                    solutionProject.Configurations.Add(configuration);
            }

            foreach (string platform in project.GetItemsValues("ProjectConfiguration", "Platform").Select(CleanPlatform))
            {
                if (!string.IsNullOrEmpty(platform) && !solutionProject.Platforms.Contains(platform))
                    solutionProject.Platforms.Add(platform);
            }
        }

        static private void ReadConfigurationPlatformsConditions(ProjectWrapper project, SolutionProject solutionProject)
        {
            solutionProject.Configurations.AddRange(project.GetConditionedPropertyValues("Configuration"));
            solutionProject.Platforms.AddRange(project.GetConditionedPropertyValues("Platform").Select(CleanPlatform));
        }

        static private void GetProjectDependencies(ProjectWrapper project, SolutionProject solutionProject)
        {
            foreach (string projectDependencyPath in project.GetItemsIncludes("ProjectReference"))
                solutionProject.ProjectDependencies.Add(projectDependencyPath.Replace('\\', '/'));

            // Add shared items imports as dependency on the shared project (if shared project is not the current project itself).
            foreach (string sharedProjectFilePath in project.GetImportsProjects("Shared").Where(x => !string.IsNullOrEmpty(Path.GetDirectoryName(x))))
            {
                string sharedProjectExtension = Path.GetExtension(sharedProjectFilePath).TrimStart('.');
                string sharedDependencyRelativePath = sharedProjectFilePath.Replace('\\', '/');

                if (sharedProjectExtension == ProjectFileExtensions.Extensions[ProjectFileExtension.Projitems])
                {
                    string shprojFilePath = Path.ChangeExtension(sharedDependencyRelativePath, ProjectFileExtensions.Extensions[ProjectFileExtension.Shproj]);
                    solutionProject.ProjectDependencies.Add(shprojFilePath);
                }
                else if (sharedProjectExtension == ProjectFileExtensions.Extensions[ProjectFileExtension.Vcxitems])
                {
                    solutionProject.ProjectDependencies.Add(sharedDependencyRelativePath);
                }
                else
                {
                    throw new ProjectReadException(project.FullPath, $"Shared items files using extension {sharedProjectExtension} are unknown.");
                }
            }
        }

        static private string CleanPlatform(string platform)
        {
            // In solutions, project platform "AnyCPU" is replaced by "Any CPU".
            if (platform.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase))
                return "Any CPU";

            return platform;
        }

        private void LogProject(string absoluteProjectPath, ISolutionProject solutionProject)
        {
            if (_logger is null)
                return;

            var messageBuilder = new StringBuilder();

            messageBuilder.AppendLine($"Read project \"{absoluteProjectPath}\"");
            messageBuilder.AppendLine("- Type: " + (solutionProject.Type.HasValue ? ProjectTypes.DisplayNames[solutionProject.Type.Value] : solutionProject.TypeGuid.ToString()));
            messageBuilder.AppendLine("- CanBuild: " + solutionProject.CanBuild);
            messageBuilder.AppendLine("- CanDeploy: " + solutionProject.CanDeploy);
            messageBuilder.AppendLine("- AlwaysDeploy: " + solutionProject.AlwaysDeploy);
            messageBuilder.AppendLine("- NoPlatform: " + solutionProject.NoPlatform);
            messageBuilder.AppendLine("- Configurations: " + string.Join(", ", solutionProject.Configurations));
            messageBuilder.Append("- Platforms: " + string.Join(", ", solutionProject.Platforms));

            _logger.Log(LogLevel, messageBuilder.ToString());
        }

        public class ProjectWrapper
        {
            private readonly Project _project;
            private readonly Project? _manualDirectoryBuildProps;

            public ProjectWrapper(Project project, Project? manualDirectoryBuildProps = null)
            {
                _project = project;
                _manualDirectoryBuildProps = manualDirectoryBuildProps;
            }

            public string FullPath => _project.FullPath;

            public string? GetPropertyValue(string propertyName)
            {
                string? value = _project.GetPropertyValue(propertyName)
                    ?? _manualDirectoryBuildProps?.GetPropertyValue(propertyName);

                return !string.IsNullOrEmpty(value) ? value : null;
            }

            public IEnumerable<string> GetSdks()
            {
                return ConcatValues(p => p.Xml.Sdk.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Concat(p.Imports.SelectMany(x => x.ImportingElement.Sdk.Split(';'))));
            }

            public IEnumerable<string> GetConditionedPropertyValues(string propertyName)
            {
                return ConcatValues(p => p.ConditionedProperties.TryGetValue(propertyName, out List<string>? conditionedValues)
                    ? conditionedValues
                    : Enumerable.Empty<string>())
                    .Distinct();
            }

            public IEnumerable<string> GetItemsValues(string itemTypeName, string itemPropertyName)
            {
                return ConcatValues(p => p.GetItems(itemTypeName).Select(x => x.GetMetadataValue(itemPropertyName)));
            }

            public IEnumerable<string> GetItemsIncludes(string itemTypeName)
            {
                return ConcatValues(p => p.GetItems(itemTypeName).Select(x => x.EvaluatedInclude));
            }

            public IEnumerable<string> GetImportsProjects(string? label = null)
            {
                return ConcatValues(p => p.Imports.Where(x => label is null || x.ImportingElement.Label == label).Select(x => x.ImportingElement.Project));
            }

            private IEnumerable<string> ConcatValues(Func<Project, IEnumerable<string>> valuesFunc)
            {
                foreach (string value in valuesFunc(_project))
                    yield return value;

                if (_manualDirectoryBuildProps != null)
                {
                    foreach (string value in valuesFunc(_manualDirectoryBuildProps))
                        yield return value;
                }
            }
        }
    }
}
