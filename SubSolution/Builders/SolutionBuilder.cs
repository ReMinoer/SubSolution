using System.Collections.Generic;
using SubSolution.Configuration;

namespace SubSolution.Builders
{
    public class SolutionBuilder : ISolutionBuilder
    {
        public string SolutionOutputPath { get; }
        public Folder Root { get; }

        public SolutionBuilder(string solutionOutputPath)
        {
            SolutionOutputPath = solutionOutputPath;
            Root = new Folder();
        }

        public void AddFile(string filePath, string[] solutionFolderPath) => GetSolutionFolder(solutionFolderPath).FilePaths.Add(filePath);
        public void AddProject(string projectPath, string[] solutionFolderPath) => GetSolutionFolder(solutionFolderPath).ProjectPaths.Add(projectPath);

        private Folder GetSolutionFolder(string[] solutionFolderPath)
        {
            Folder currentFolder = Root;
            foreach (string solutionFolderName in solutionFolderPath)
            {
                if (!currentFolder.SubFolders.TryGetValue(solutionFolderName, out Folder subFolder))
                    currentFolder.SubFolders[solutionFolderName] = subFolder = new Folder();

                currentFolder = subFolder;
            }

            return currentFolder;
        }

        public class Folder
        {
            public HashSet<string> FilePaths { get; }
            public HashSet<string> ProjectPaths { get; }
            public Dictionary<string, Folder> SubFolders { get; }

            public Folder()
            {
                FilePaths = new HashSet<string>();
                ProjectPaths = new HashSet<string>();
                SubFolders = new Dictionary<string, Folder>();
            }
        }
    }
}