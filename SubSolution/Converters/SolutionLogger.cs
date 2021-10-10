using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution.Converters
{
    public class SolutionLogger
    {
        private readonly IFileSystem _fileSystem;

        private readonly int _indentSize;
        private string Tab => new string(' ', _indentSize);
        
        public bool ShowHierarchy { get; set; } = true;
        public bool ShowConfigurationPlatforms { get; set; } = true;

        public bool ShowProjectTypes { get; set; }

        public bool ShowAllProjectContexts { get; set; }
        public bool ShowInterestingProjectContexts { get; set; }

        public bool ShowFilePaths { get; set; }
        public bool ShowHeaders { get; set; } = true;

        public SolutionLogger(int indentSize = 4, IFileSystem? fileSystem = null)
        {
            _indentSize = indentSize;
            _fileSystem = fileSystem ?? StandardFileSystem.Instance;
        }

        public string Convert(ISolution solution)
        {
            StringBuilder messageBuilder = new StringBuilder();

            if (ShowHierarchy)
            {
                if (ShowHeaders)
                    messageBuilder.AppendLine("SOLUTION HIERARCHY:");

                LogFolder(messageBuilder, solution.Root, new List<bool>());
            }

            if (ShowConfigurationPlatforms)
            {
                if (messageBuilder.Length > 0)
                    messageBuilder.AppendLine();

                if (ShowHeaders)
                    messageBuilder.AppendLine("SOLUTION CONFIGURATION-PLATFORMS:");

                LogConfigurationPlatforms(messageBuilder, solution);
            }

            return messageBuilder.ToString();
        }

        private void LogFolder(StringBuilder messageBuilder, ISolutionFolder folder, List<bool> showPreviousConnections)
        {
            string lineHeader = GetLineHeader(showPreviousConnections);

            int index = 0;
            int count = folder.SubFolders.Count + folder.FilePaths.Count + folder.Projects.Count;

            foreach (ICovariantKeyValuePair<string, ISolutionFolder> pair in folder.SubFolders.OrderBy(x => x.Key))
            {
                messageBuilder.AppendLine(Bullet() + pair.Key);

                showPreviousConnections.Add(index < count);
                LogFolder(messageBuilder, pair.Value, showPreviousConnections);
                showPreviousConnections.RemoveAt(showPreviousConnections.Count - 1);
            }

            IEnumerable<string> fileLines = folder.FilePaths.Select(GetFileDisplayName);
            IEnumerable<string> projectLines = folder.Projects.Select(x => GetProjectDisplayName(x.Key, x.Value));

            foreach (string line in fileLines.Concat(projectLines).OrderBy(x => x))
                messageBuilder.AppendLine(Bullet() + line);

            string Bullet() => lineHeader + GetBullet(index++, count);
        }

        private void LogConfigurationPlatforms(StringBuilder messageBuilder, ISolution solution)
        {
            foreach (ISolutionConfigurationPlatform configurationPlatform in solution.ConfigurationPlatforms)
            {
                messageBuilder.AppendLine($"- {{{configurationPlatform.FullName}}}");

                if (ShowAllProjectContexts)
                {
                    foreach ((string projectPath, SolutionProjectContext context) in configurationPlatform.ProjectContexts)
                    {
                        messageBuilder.AppendLine(Tab + $"- [{GetProjectDisplayName(projectPath)}]");
                        messageBuilder.AppendLine(Tab + Tab + "- Configuration: " + context.ConfigurationName);
                        messageBuilder.AppendLine(Tab + Tab + "- Platform: " + context.PlatformName);
                        messageBuilder.AppendLine(Tab + Tab + "- Build: " + context.Build);
                        messageBuilder.AppendLine(Tab + Tab + "- Deploy: " + context.Deploy);
                    }
                }

                if (ShowInterestingProjectContexts)
                {
                    var lines = new List<(string, string)>();
                    foreach ((string projectPath, ISolutionProject project) in solution.Root.AllProjects)
                    {
                        if (!configurationPlatform.ProjectContexts.TryGetValue(projectPath, out SolutionProjectContext projectContext))
                            continue;

                        bool noBuild = project.CanBuild && !projectContext.Build;
                        bool noDeploy = project.CanDeploy && !projectContext.Deploy;
                        bool differentConfiguration = !projectContext.ConfigurationName.Equals(configurationPlatform.ConfigurationName, StringComparison.OrdinalIgnoreCase);
                        bool differentPlatform = !projectContext.PlatformName.Equals(configurationPlatform.PlatformName, StringComparison.OrdinalIgnoreCase);

                        if (noBuild || noDeploy || differentConfiguration || differentPlatform)
                        {
                            List<string> differences = new List<string>();
                            if (!noBuild && (differentConfiguration || differentPlatform))
                                differences.Add($"{(differentConfiguration ? projectContext.ConfigurationName : "*")}|{(differentPlatform ? projectContext.PlatformName : "*")}");
                            if (noBuild)
                                differences.Add("No build");
                            if (noDeploy)
                                differences.Add("No deploy");

                            lines.Add((GetProjectDisplayName(projectPath), string.Join(", ", differences)));
                        }
                    }

                    int longestNameSize = lines.Select(x => x.Item1.Length).Max();

                    foreach ((string name, string differences) in lines)
                        messageBuilder.AppendLine(Tab + $"- [{name}] {new string(' ', longestNameSize - name.Length)}-> {differences}");
                }
            }
        }

        private string GetFileDisplayName(string filePath) => ShowFilePaths ? filePath : _fileSystem.GetName(filePath);
        private string GetProjectDisplayName(string projectPath, ISolutionProject? project = null)
        {
            string displayName = ShowFilePaths ? projectPath : _fileSystem.GetFileNameWithoutExtension(projectPath);

            if (ShowProjectTypes && project != null)
            {
                string projectType = project.Type.HasValue ? ProjectTypes.DisplayNames[project.Type.Value] : "Unknown type";
                displayName = $"{displayName} [{projectType}]";
            }

            return displayName;
        }

        private string GetLineHeader(IEnumerable<bool> showPreviousConnections)
        {
            return showPreviousConnections
                .Select(x => x ? "¦" + new string(' ', _indentSize - 1) : Tab)
                .Aggregate(string.Empty, (x, y) => x + y);
        }

        private string GetBullet(int index, int count)
        {
            return index == count - 1 ? "└─ " : "├─ ";
        }
    }
}