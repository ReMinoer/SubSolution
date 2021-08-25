namespace SubSolution
{
    public class SolutionProjectContext
    {
        public string ConfigurationName { get; set; }
        public string PlatformName { get; set; }
        public bool Build { get; set; }
        public bool Deploy { get; set; }

        public SolutionProjectContext(string configurationName, string platformName)
        {
            ConfigurationName = configurationName;
            PlatformName = platformName;
            Build = true;
            Deploy = false;
        }
    }
}