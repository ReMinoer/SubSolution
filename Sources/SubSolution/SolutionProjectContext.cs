namespace SubSolution
{
    public class SolutionProjectContext
    {
        public string ConfigurationName { get; set; }
        public string? PlatformName { get; set; }
        public string ConfigurationPlatformName => GetConfigurationPlatformName("|");
        public bool Build { get; set; }
        public bool Deploy { get; set; }

        public SolutionProjectContext(string configurationName, string platformName)
        {
            ConfigurationName = configurationName;
            PlatformName = platformName;
        }

        public SolutionProjectContext(string configurationName)
        {
            ConfigurationName = configurationName;
            PlatformName = null;
        }

        public SolutionProjectContext(SolutionProjectContext projectContext)
        {
            ConfigurationName = projectContext.ConfigurationName;
            PlatformName = projectContext.PlatformName;
            Build = projectContext.Build;
            Deploy = projectContext.Deploy;
        }

        public string GetConfigurationPlatformName(string separator)
        {
            return PlatformName is null ? ConfigurationName : $"{ConfigurationName}{separator}{PlatformName}";
        }
    }
}