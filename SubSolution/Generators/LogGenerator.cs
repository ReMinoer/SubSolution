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

        public bool ShowFilePath { get; set; }

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

            messageBuilder.AppendLine("Solution Content:");
            LogFolder(messageBuilder, solutionOutput.Root, new List<bool>());

            _logger.Log(_logLevel, messageBuilder.ToString());
        }
        
        private void LogFolder(StringBuilder messageBuilder, ISolutionFolder folder, List<bool> showPreviousConnections)
        {
            string lineHeader = GetLineHeader(showPreviousConnections);

            int index = 0;
            int count = folder.SubFolders.Count + folder.FilePaths.Count + folder.ProjectPaths.Count;

            foreach (ICovariantKeyValuePair<string, ISolutionFolder> pair in folder.SubFolders)
            {
                messageBuilder.AppendLine(Bullet() + pair.Key);

                showPreviousConnections.Add(index < count);
                LogFolder(messageBuilder, pair.Value, showPreviousConnections);
                showPreviousConnections.RemoveAt(showPreviousConnections.Count - 1);
            }

            foreach (string filePath in folder.FilePaths)
                messageBuilder.AppendLine(Bullet() + (ShowFilePath ? filePath : _fileSystem.GetName(filePath)));
            foreach (string projectPath in folder.ProjectPaths)
                messageBuilder.AppendLine(Bullet() + (ShowFilePath ? projectPath : _fileSystem.GetFileNameWithoutExtension(projectPath)));

            string Bullet() => lineHeader + GetBullet(index++, count);
        }

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