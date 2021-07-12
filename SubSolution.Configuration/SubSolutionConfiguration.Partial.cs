using System; 
using System.IO;
using SubSolution.Configuration.FileSystems;

namespace SubSolution.Configuration
{
    public partial class SubSolutionConfiguration
    {
        public string ComputeSolutionName(string configurationFilePath, IConfigurationFileSystem? fileSystem = null)
        {
            if (SolutionName == null)
                return (fileSystem ?? StandardFileSystem.Instance).GetFileNameWithoutExtension(configurationFilePath);

            if (SolutionName.EndsWith(".sln"))
                return SolutionName[..^4];

            return SolutionName;
        }

        public string ComputeSolutionPath(string defaultOutputDirectory, string configurationFilePath, IConfigurationFileSystem? fileSystem = null)
        {
            string outputDirectory = OutputDirectory ?? defaultOutputDirectory;
            string solutionFileName = ComputeSolutionName(configurationFilePath) + ".sln";

            return (fileSystem ?? StandardFileSystem.Instance).Combine(outputDirectory, solutionFileName);
        }

        public string ComputeWorkspaceDirectoryPath(string configurationFilePath, IConfigurationFileSystem? fileSystem = null)
        {
            var configurationFolderPath = (fileSystem ?? StandardFileSystem.Instance).GetParentDirectoryPath(configurationFilePath);
            if (configurationFolderPath is null)
                throw new ArgumentException($"\"{configurationFolderPath}\" has no directory.");

            return WorkspaceDirectory ?? configurationFolderPath;
        }
    }

    public partial class SolutionItemsSourceBase
    {
        public abstract void AddToSolution(ISolutionBuildContext context);
    }

    public partial class SolutionRootConfiguration
    {
        public override void AddToSolution(ISolutionBuildContext context)
        {
            foreach (SolutionItems items in SolutionItems)
                items.AddToSolution(context);
        }
    }

    public partial class SolutionFolderConfiguration
    {
        public override void AddToSolution(ISolutionBuildContext context)
        {
            base.AddToSolution(context.GetSubFolderContext(Name));
        }
    }

    public partial class SolutionFilesSourceBase
    {
        protected abstract string DefaultFileExtension { get; }

        public override void AddToSolution(ISolutionBuildContext context)
        {
            string? globPattern = Path;
            if (string.IsNullOrEmpty(globPattern))
                globPattern = "**/*." + DefaultFileExtension;
            else if(globPattern.EndsWith("/") || globPattern.EndsWith("\\"))
                globPattern += "*." + DefaultFileExtension;
            else if (globPattern.EndsWith("**"))
                globPattern += "/*." + DefaultFileExtension;
            else if(globPattern.EndsWith("*") && !globPattern.EndsWith(".*"))
                globPattern += "." + DefaultFileExtension;

            foreach (string matchingFilePath in context.FileSystem.GetFilesMatchingGlobPattern(context.CurrentWorkspaceDirectoryPath, globPattern))
                AddFoldersAndFileToSolution(matchingFilePath, context);
        }

        protected virtual void AddFoldersAndFileToSolution(string relativeFilePath, ISolutionBuildContext context)
        {
            var filePath = context.FileSystem.Combine(context.CurrentWorkspaceDirectoryPath, relativeFilePath);
            relativeFilePath = context.FileSystem.MakeRelativePath(context.OriginWorkspaceDirectoryPath, filePath);

            if (CreateFolders ?? false)
            {
                string relativeDirectoryPath = context.FileSystem.GetParentDirectoryPath(relativeFilePath) ?? string.Empty;
                string[] solutionFolderPath = context.FileSystem.SplitPath(relativeDirectoryPath);

                AddFileToSolution(relativeFilePath, context.GetSubFolderContext(solutionFolderPath));
            }
            else
            {
                AddFileToSolution(relativeFilePath, context);
            }
        }

        protected abstract void AddFileToSolution(string relativeFilePath, ISolutionBuildContext context);
    }

    public partial class FilesSource
    {
        protected override string DefaultFileExtension => "*";

        protected override void AddFileToSolution(string relativeFilePath, ISolutionBuildContext context)
        {
            context.SolutionBuilder.AddFile(relativeFilePath, context.CurrentFolderPath);
        }
    }

    public partial class ProjectsSource
    {
        protected override string DefaultFileExtension => "csproj";

        protected override void AddFileToSolution(string relativeFilePath, ISolutionBuildContext context)
        {
            context.SolutionBuilder.AddProject(relativeFilePath, context.CurrentFolderPath);
        }
    }

    public partial class SubSolutionsSource
    {
        protected override string DefaultFileExtension => "subsln";
        
        protected override void AddFileToSolution(string relativeFilePath, ISolutionBuildContext context)
        {
            string filePath = context.FileSystem.Combine(context.CurrentWorkspaceDirectoryPath, relativeFilePath);

            SubSolutionConfiguration configuration;
            using (Stream configurationStream = context.FileSystem.OpenStream(filePath))
            using (TextReader configurationReader = new StreamReader(configurationStream))
                configuration = SubSolutionConfiguration.Load(configurationReader);

            var workspaceDirectoryPath = configuration.ComputeWorkspaceDirectoryPath(filePath);

            if (CreateRootFolder ?? false)
                context = context.GetSubFolderContext(configuration.ComputeSolutionName(filePath));

            context = context.GetNewWorkspaceDirectoryContext(workspaceDirectoryPath);

            configuration.Root.AddToSolution(context);
        }
    }

    public partial class SolutionItems
    {
        public abstract void AddToSolution(ISolutionBuildContext context);
    }

    public partial class Folder
    {
        public override void AddToSolution(ISolutionBuildContext context) => Content.AddToSolution(context);
    }

    public partial class Files
    {
        public override void AddToSolution(ISolutionBuildContext context) => Content.AddToSolution(context);
    }

    public partial class Projects
    {
        public override void AddToSolution(ISolutionBuildContext context) => Content.AddToSolution(context);
    }

    public partial class SubSolutions
    {
        public override void AddToSolution(ISolutionBuildContext context) => Content.AddToSolution(context);
    }
}