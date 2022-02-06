using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using SubSolution.Converters.Changes;

namespace SubSolution.VisualStudio.Helpers
{
    static public class SubSolutionMonikers
    {
        static public ImageMoniker GetObjectTypeMoniker(ProjectType? projectType)
        {
            return projectType switch
            {
                ProjectType.Folder => KnownMonikers.FolderClosed,
                ProjectType.Cpp => KnownMonikers.CPPProjectNode,
                ProjectType.CSharpLegacy => KnownMonikers.CSProjectNode,
                ProjectType.CSharpDotNetSdk => KnownMonikers.CSProjectNode,
                ProjectType.VisualBasicLegacy => KnownMonikers.VBProjectNode,
                ProjectType.VisualBasicDotNetSdk => KnownMonikers.VBProjectNode,
                ProjectType.FSharpLegacy => KnownMonikers.FSProjectNode,
                ProjectType.FSharpDotNetSdk => KnownMonikers.FSProjectNode,
                ProjectType.Python => KnownMonikers.PythonPackage,
                ProjectType.NodeJs => KnownMonikers.JSProjectNode,
                ProjectType.Sql => KnownMonikers.DatabaseApplication,
                ProjectType.Wap => KnownMonikers.Application,
                _ => KnownMonikers.Application
            };
        }

        static public ImageMoniker GetChangeTypeMoniker(SolutionChangeType changeType)
        {
            return changeType switch
            {
                SolutionChangeType.Remove => KnownMonikers.Cancel,
                SolutionChangeType.Move => KnownMonikers.Next,
                SolutionChangeType.Add => KnownMonikers.Add,
                _ => KnownMonikers.Edit
            };
        }

        static public ImageMoniker? GetObjectTypeMoniker(SolutionObjectType? objectType)
        {
            return objectType switch
            {
                SolutionObjectType.Folder => KnownMonikers.FolderClosed,
                SolutionObjectType.Project => KnownMonikers.ProjectFilterFile,
                SolutionObjectType.File => KnownMonikers.TextFile,
                SolutionObjectType.ConfigurationPlatform => KnownMonikers.ShowAllConfigurations,
                SolutionObjectType.ProjectContext => KnownMonikers.Property,
                _ => null
            };
        }
    }
}