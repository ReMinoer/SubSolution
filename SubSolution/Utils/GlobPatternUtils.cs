namespace SubSolution.Utils
{
    static public class GlobPatternUtils
    {
        static public string CompleteSimplifiedPattern(string? globPattern, string defaultFileExtension)
        {
            if (string.IsNullOrEmpty(globPattern))
                globPattern = "**/*." + defaultFileExtension;
            else if (globPattern.EndsWith("/") || globPattern.EndsWith("\\"))
                globPattern += "*." + defaultFileExtension;
            else if (globPattern.EndsWith("**"))
                globPattern += "/*." + defaultFileExtension;

            return globPattern;
        }
    }
}