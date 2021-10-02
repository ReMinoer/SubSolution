using System.Collections.Generic;
using System.Linq;
using System.Text;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution.Converters
{
    public class SolutionLogger
    {
        private readonly int _indentSize;
        private readonly IFileSystem _fileSystem;
        
        public bool ShowHierarchy { get; set; } = true;
        public bool ShowConfigurationPlatforms { get; set; } = true;
        public bool ShowProjectContexts { get; set; }

        public bool ShowFilePath { get; set; }
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

                LogConfigurationPlatforms(messageBuilder, solution.ConfigurationPlatforms);
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
            IEnumerable<string> projectLines = folder.Projects.Keys.Select(GetProjectDisplayName);

            foreach (string line in fileLines.Concat(projectLines).OrderBy(x => x))
                messageBuilder.AppendLine(Bullet() + line);

            string Bullet() => lineHeader + GetBullet(index++, count);
        }

        private void LogConfigurationPlatforms(StringBuilder messageBuilder, IReadOnlyList<ISolutionConfigurationPlatform> configurationPlatforms)
        {
            foreach (ISolutionConfigurationPlatform configurationPlatform in configurationPlatforms)
            {
                messageBuilder.AppendLine($"- {{{configurationPlatform.FullName}}}");
                if (!ShowProjectContexts)
                    continue;

                foreach ((string projectPath, SolutionProjectContext context) in configurationPlatform.ProjectContexts)
                {
                    messageBuilder.AppendLine($"\t- [{GetProjectDisplayName(projectPath)}]");
                    messageBuilder.AppendLine("\t\t- Configuration: " + context.ConfigurationName);
                    messageBuilder.AppendLine("\t\t- Platform: " + context.PlatformName);
                    messageBuilder.AppendLine("\t\t- Build: " + context.Build);
                    messageBuilder.AppendLine("\t\t- Deploy: " + context.Deploy);
                }
            }
        }

        private string GetFileDisplayName(string filePath) => ShowFilePath ? filePath : _fileSystem.GetName(filePath);
        private string GetProjectDisplayName(string projectPath) => ShowFilePath ? projectPath : _fileSystem.GetFileNameWithoutExtension(projectPath);

        private string GetLineHeader(IEnumerable<bool> showPreviousConnections)
        {
            return showPreviousConnections
                .Select(x => x ? "¦" + new string(' ', _indentSize - 1) : new string(' ', _indentSize))
                .Aggregate(string.Empty, (x, y) => x + y);
        }

        private string GetBullet(int index, int count)
        {
            return index == count - 1 ? "└─ " : "├─ ";
        }
    }
}