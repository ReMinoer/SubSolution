using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Build.Evaluation;
using SubSolution.FileSystems;
using SubSolution.ProjectReaders;

namespace SubSolution.MsBuild
{
    public class SolutionProjectReader : ISolutionProjectReader
    {
        private readonly ISubSolutionFileSystem _fileSystem;

        public SolutionProjectReader(ISubSolutionFileSystem? fileSystem = null)
        {
            _fileSystem = fileSystem ?? StandardFileSystem.Instance;
        }

        public async Task<ISolutionProject> ReadAsync(string projectPath, string rootDirectory)
        {
            string absoluteProjectPath = _fileSystem.Combine(rootDirectory, projectPath);

            await using Stream projectStream = _fileSystem.OpenStream(absoluteProjectPath);
            using var xmlReader = XmlReader.Create(projectStream);

            Project project = await Task.Run(() => new Project(xmlReader, null, null, ProjectCollection.GlobalProjectCollection, ProjectLoadSettings.IgnoreMissingImports));

            var solutionProject = new SolutionProject(projectPath);

            string projectConfigurations = project.GetPropertyValue("Configurations");
            if (string.IsNullOrEmpty(projectConfigurations))
            {
                solutionProject.Configurations.Add("Debug");
                solutionProject.Configurations.Add("Release");
            }
            else
            {
                solutionProject.Configurations.AddRange(projectConfigurations.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }

            string platformConfigurations = project.GetPropertyValue("Platforms");
            if (string.IsNullOrEmpty(platformConfigurations))
            {
                solutionProject.Platforms.Add("Any CPU");
            }
            else
            {
                solutionProject.Platforms.AddRange(platformConfigurations.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }

            return solutionProject;
        }
    }
}
