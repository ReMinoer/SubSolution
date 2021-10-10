using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;
using SubSolution.ProjectReaders;

namespace SubSolution.MsBuild
{
    [ExcludeFromCodeCoverage]
    public class MsBuildProjectReader : IProjectReader
    {
        private readonly ILogger? _logger;
        private readonly LogLevel _logLevel;

        public MsBuildProjectReader(ILogger? logger = null, LogLevel? logLevel = null)
        {
            _logger = logger;
            _logLevel = logLevel ?? LogLevel.Trace;
        }

        public async Task<ISolutionProject> ReadAsync(string absoluteProjectPath)
        {
            Project project = await Task.Run(() => new Project(absoluteProjectPath, null, null, new ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports));

            var solutionProject = new SolutionProject(GetType(project))
            {
                CanBuild = CanBuild(project),
                CanDeploy = CanDeploy(project),
                AlwaysDeploy = AlwaysDeploy(project)
            };

            GetConfigurationsAndPlatforms(project, solutionProject);
            GetProjectDependencies(project, solutionProject);

            LogProject(absoluteProjectPath, solutionProject);
            return solutionProject;
        }

        private ProjectType GetType(Project project)
        {
            string extensionString = Path.GetExtension(project.FullPath).TrimStart('.');
            if (!ProjectFileExtensions.ByExtensions.TryGetValue(extensionString, out ProjectFileExtension extension))
                throw new NotSupportedException($"Project extension \"{extensionString}\" is not supported.");

            switch (extension)
            {
                case ProjectFileExtension.Csproj:
                    return HasDotNetSdk(project) ? ProjectType.CSharpDotNetSdk : ProjectType.CSharpLegacy;
                case ProjectFileExtension.Fsproj:
                    return HasDotNetSdk(project) ? ProjectType.FSharpDotNetSdk : ProjectType.FSharpLegacy;
                case ProjectFileExtension.Vbproj:
                    return HasDotNetSdk(project) ? ProjectType.VisualBasicDotNetSdk : ProjectType.VisualBasicLegacy;
                case ProjectFileExtension.Vcxproj:
                case ProjectFileExtension.Vcproj:
                    return ProjectType.Cpp;
                case ProjectFileExtension.Njsproj:
                    return ProjectType.NodeJs;
                case ProjectFileExtension.Pyproj:
                    return ProjectType.Python;
                case ProjectFileExtension.Sqlproj:
                    return ProjectType.Sql;
                case ProjectFileExtension.Shproj:
                    return ProjectType.Shared;
                case ProjectFileExtension.Wapproj:
                    return ProjectType.Wap;
                default:
                    throw new NotSupportedException();
            }
        }

        static private bool CanBuild(Project project)
        {
            return !ProjectFileExtension.Pyproj.IsExtensionOf(project.FullPath);
        }

        static private bool CanDeploy(Project project)
        {
            return CanDeployExtensions.Any(x => x.IsExtensionOf(project.FullPath))
                || project.GetPropertyValue("OutputType").Equals("AppContainerExe", StringComparison.OrdinalIgnoreCase);
        }

        static private readonly ProjectFileExtension[] CanDeployExtensions =
        {
            ProjectFileExtension.Wapproj,
            ProjectFileExtension.Sqlproj
        };

        static private bool AlwaysDeploy(Project project)
        {
            return ProjectFileExtension.Sqlproj.IsExtensionOf(project.FullPath);
        }

        private void GetConfigurationsAndPlatforms(Project project, SolutionProject solutionProject)
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

        static private bool HasDotNetSdk(Project project)
        {
            return project.Xml.Sdk.Split(';').Any(x => x.StartsWith("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase));
        }

        static private void ReadConfigurationsAndPlatformsSdkProperties(Project project, SolutionProject solutionProject)
        {
            string configurations = project.GetPropertyValue("Configurations");
            if (!string.IsNullOrEmpty(configurations))
            {
                solutionProject.Configurations.AddRange(configurations.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                solutionProject.Configurations.Add("Debug");
                solutionProject.Configurations.Add("Release");
            }

            string platforms = project.GetPropertyValue("Platforms");
            if (!string.IsNullOrEmpty(platforms))
            {
                solutionProject.Platforms.AddRange(platforms.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(CleanPlatform));
            }
            else
            {
                solutionProject.Platforms.Add("Any CPU");
            }
        }

        static private bool HasProjectConfigurationItems(Project project)
        {
            return HasProjectConfigurationItemsExtensions.Any(x => x.IsExtensionOf(project.FullPath));
        }

        static private readonly ProjectFileExtension[] HasProjectConfigurationItemsExtensions =
        {
            ProjectFileExtension.Vcxproj,
            ProjectFileExtension.Vcproj,
            ProjectFileExtension.Wapproj
        };

        static private void ReadProjectConfigurationItems(Project project, SolutionProject solutionProject)
        {
            foreach (ProjectItem projectConfigurationItem in project.GetItems("ProjectConfiguration"))
            {
                string configuration = projectConfigurationItem.GetMetadataValue("Configuration");
                if (!string.IsNullOrEmpty(configuration) && !solutionProject.Configurations.Contains(configuration))
                    solutionProject.Configurations.Add(configuration);

                string platform = CleanPlatform(projectConfigurationItem.GetMetadataValue("Platform"));
                if (!string.IsNullOrEmpty(platform) && !solutionProject.Platforms.Contains(platform))
                    solutionProject.Platforms.Add(platform);
            }
        }

        static private void ReadConfigurationPlatformsConditions(Project project, SolutionProject solutionProject)
        {
            bool hasConfigurations = project.ConditionedProperties.TryGetValue("Configuration", out List<string>? conditionedConfigurations);
            bool hasPlatforms = project.ConditionedProperties.TryGetValue("Platform", out List<string>? conditionedPlatforms);

            if (hasConfigurations)
            {
                solutionProject.Configurations.AddRange(conditionedConfigurations!);
            }

            if (hasPlatforms)
            {
                solutionProject.Platforms.AddRange(conditionedPlatforms!.Select(CleanPlatform));
            }
            else
            {
                // Default to Any CPU for project without platforms (for example, .njsproj and .pyproj)
                solutionProject.Platforms.Add("Any CPU");
            }
        }

        static private void GetProjectDependencies(Project project, SolutionProject solutionProject)
        {
            foreach (string projectDependencyPath in project.GetItems("ProjectReference").Select(x => x.EvaluatedInclude))
                solutionProject.ProjectDependencies.Add(projectDependencyPath.Replace('\\', '/'));
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

            messageBuilder.AppendLine($"Read \"{absoluteProjectPath}\"");
            messageBuilder.AppendLine("- Type: " + (solutionProject.Type.HasValue ? ProjectTypes.DisplayNames[solutionProject.Type.Value] : solutionProject.TypeGuid.ToString()));
            messageBuilder.AppendLine("- CanBuild: " + solutionProject.CanBuild);
            messageBuilder.AppendLine("- CanDeploy: " + solutionProject.CanDeploy);
            messageBuilder.AppendLine("- AlwaysDeploy: " + solutionProject.AlwaysDeploy);
            messageBuilder.AppendLine("- Configurations: " + string.Join(", ", solutionProject.Configurations));
            messageBuilder.Append("- Platforms: " + string.Join(", ", solutionProject.Platforms));

            _logger.Log(_logLevel, messageBuilder.ToString());
        }
    }
}
