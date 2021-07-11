using System.IO;
using SubSolution.Configuration;

namespace SubSolution.Builders
{
    public class SolutionBuilder : ISolutionBuilder
    {
        static public SolutionBuilder FromPath(string solutionPath) => FromStream(File.OpenRead(solutionPath));
        static public SolutionBuilder FromStream(Stream solutionStream) => new SolutionBuilder(solutionStream);

        private SolutionBuilder(Stream solutionStream)
        {
            throw new System.NotImplementedException();
        }

        public void AddFile(string filePath, string[] solutionFolderPath)
        {
            throw new System.NotImplementedException();
        }

        public void AddProject(string projectPath, string[] solutionFolderPath)
        {
            throw new System.NotImplementedException();
        }
    }
}