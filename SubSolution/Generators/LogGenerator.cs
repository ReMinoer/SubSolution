using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using SubSolution.FileSystems;
using SubSolution.Utils;

namespace SubSolution.Generators
{
    public class LogGenerator : ISolutionGenerator
    {
        private readonly ILogger _logger;
        private readonly LogLevel _logLevel;
        private readonly int _indentSize;
        private readonly ISubSolutionFileSystem _fileSystem;

        public bool ShowOutputPath { get; set; }
        public bool ShowHierarchy { get; set; } = true;
        public bool ShowConfigurationPlatforms { get; set; } = true;
        public bool ShowProjectContexts { get; set; }

        public bool ShowFilePath { get; set; }
        public bool ShowHeaders { get; set; } = true;

        public LogGenerator(ILogger logger, LogLevel logLevel, int indentSize = 4, ISubSolutionFileSystem? fileSystem = null)
        {
            _logger = logger;
            _logLevel = logLevel;
            _indentSize = indentSize;
            _fileSystem = fileSystem ?? StandardFileSystem.Instance;
        }

        public void Generate(ISolutionOutput solutionOutput)
        {
            StringBuilder messageBuilder = new StringBuilder();

            if (ShowOutputPath)
                messageBuilder.AppendLine($"SOLUTION OUTPUT PATH: {solutionOutput.OutputPath}");

            if (ShowHierarchy)
            {
                if (ShowHeaders)
                    messageBuilder.AppendLine("SOLUTION HIERARCHY:");

                LogFolder(messageBuilder, solutionOutput.Root, new List<bool>());
            }

            if (ShowConfigurationPlatforms)
            {
                if (ShowHeaders)
                    messageBuilder.AppendLine("SOLUTION CONFIGURATION-PLATFORMS:");

                LogConfigurationPlatforms(messageBuilder, solutionOutput.ConfigurationPlatforms);
            }

            _logger.Log(_logLevel, messageBuilder.ToString());
        }

        private void LogFolder(StringBuilder messageBuilder, ISolutionFolder folder, List<bool> showPreviousConnections)
        {
            string lineHeader = GetLineHeader(showPreviousConnections);

            int index = 0;
            int count = folder.SubFolders.Count + folder.FilePaths.Count + folder.Projects.Count;

            foreach (ICovariantKeyValuePair<string, ISolutionFolder> pair in folder.SubFolders)
            {
                messageBuilder.AppendLine(Bullet() + pair.Key);

                showPreviousConnections.Add(index < count);
                LogFolder(messageBuilder, pair.Value, showPreviousConnections);
                showPreviousConnections.RemoveAt(showPreviousConnections.Count - 1);
            }

            foreach (string filePath in folder.FilePaths)
                messageBuilder.AppendLine(Bullet() + GetFileDisplayName(filePath));
            foreach (string projectPath in folder.Projects.Keys)
                messageBuilder.AppendLine(Bullet() + GetProjectDisplayName(projectPath));

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