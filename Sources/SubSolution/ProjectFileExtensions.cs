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
        static public Dictionary<ProjectFileExtension, string> ExcludedSuffixes { get; }

        static ProjectFileExtensions()
        {
            Extensions = new Dictionary<ProjectFileExtension, string>();
            ByExtensions = new Dictionary<string, ProjectFileExtension>(StringComparer.OrdinalIgnoreCase);
            ExcludedSuffixes = new Dictionary<ProjectFileExtension, string>();

            foreach (ProjectFileExtension extension in Enum.GetValues(typeof(ProjectFileExtension)))
            {
                string extensionString = extension.ToString().ToLowerInvariant();

                Extensions.Add(extension, extensionString);
                ByExtensions.Add(extensionString, extension);
            }

            ExcludedSuffixes[ProjectFileExtension.Csproj] = "_wpftmp.csproj";
        }

        static public bool MatchAny(string filePath) => Extensions.Any(x => Match(filePath, x.Value) && !Exclude(filePath, x.Key));
        static public bool IsExtensionOf(this ProjectFileExtension extension, string filePath) => Match(filePath, Extensions[extension]) && !Exclude(filePath, extension);

        static private bool Match(string filePath, string extension) => filePath.EndsWith('.' + extension);
        static private bool Exclude(string filePath, ProjectFileExtension matchedExtension)
            => ExcludedSuffixes.TryGetValue(matchedExtension, out string excludedSuffix) && filePath.EndsWith(excludedSuffix);
    }
}