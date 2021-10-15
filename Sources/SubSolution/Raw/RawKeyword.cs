namespace SubSolution.Raw
{
    static public class RawKeyword
    {
        public const string DefaultRootFileFolderName = "Solution Items";

        public const string GlobalSection = "GlobalSection";
        public const string ProjectSection = "ProjectSection";

        public const string PreSolution = "preSolution";
        public const string PostSolution = "postSolution";
        public const string PreProject = "preProject";

        public const string NestedProjects = "NestedProjects";
        public const string SolutionItems = "SolutionItems";
        public const string SolutionConfigurationPlatforms = "SolutionConfigurationPlatforms";
        public const string ProjectConfigurationPlatforms = "ProjectConfigurationPlatforms";

        public const string ActiveCfg = "ActiveCfg";
        public const string Build0 = "Build.0";
        public const string Deploy0 = "Deploy.0";

        static public string GetGlobalSectionArgument(string sectionParameter) => sectionParameter == ProjectConfigurationPlatforms ? PostSolution : PreSolution;
    }
}