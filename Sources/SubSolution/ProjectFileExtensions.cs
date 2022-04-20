using System;
using System.Collections.Generic;
using System.Linq;

namespace SubSolution
{
    static public class ProjectFileExtensions
    {
        static public readonly string[] ExtensionPatterns = {"*proj", "vcxitems"};

        static public Dictionary<ProjectFileExtension, string> Extensions { get; }
        static public Dictionary<string, ProjectFileExtension> ByExtensions { get; }

        static ProjectFileExtensions()
        {
            Extensions = new Dictionary<ProjectFileExtension, string>();
            ByExtensions = new Dictionary<string, ProjectFileExtension>(StringComparer.OrdinalIgnoreCase);

            foreach (ProjectFileExtension extension in Enum.GetValues(typeof(ProjectFileExtension)))
            {
                string extensionString = extension.ToString().ToLowerInvariant();

                Extensions.Add(extension, extensionString);
                ByExtensions.Add(extensionString, extension);
            }
        }

        static public bool MatchAny(string filePath) => Extensions.Values.Any(x => Match(filePath, x));
        static public bool IsExtensionOf(this ProjectFileExtension extension, string filePath) => Match(filePath, Extensions[extension]);

        static private bool Match(string filePath, string extension) => filePath.EndsWith('.' + extension);
    }
}