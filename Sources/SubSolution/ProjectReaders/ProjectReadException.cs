using System;

namespace SubSolution.ProjectReaders
{
    public class ProjectReadException : Exception
    {
        public string ProjectPath { get; }
        
        public ProjectReadException(string projectPath, string subMessage, Exception? innerException = null)
            : base($"Failed to read project \"{projectPath}\": {subMessage}", innerException)
        {
            ProjectPath = projectPath;
        }
    }
}