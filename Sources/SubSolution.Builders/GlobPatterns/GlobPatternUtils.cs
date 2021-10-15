using System.Text;
using System.Text.RegularExpressions;

namespace SubSolution.Builders.GlobPatterns
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

        static public Regex ConvertToRegex(string globPattern, bool caseSensitive)
        {
            var patternRegexBuilder = new StringBuilder();
            patternRegexBuilder.Append('^');

            for (int i = 0; i < globPattern.Length; i++)
            {
                char c = globPattern[i];
                switch (c)
                {
                    case '*':
                    {
                        if (i + 1 < globPattern.Length && globPattern[i + 1] == '*')
                        {
                            patternRegexBuilder.Append(@".*");
                            i++;
                        }
                        else
                        {
                            patternRegexBuilder.Append(@"[^\\\/]*");
                        }
                        break;
                    }
                    case '/':
                    case '\\':
                    {
                        patternRegexBuilder.Append(@"[\\\/]");
                        break;
                    }
                    default:
                    {
                        patternRegexBuilder.Append(Regex.Escape(c.ToString()));
                        break;
                    }
                }
            }

            patternRegexBuilder.Append('$');

            var regexOptions = RegexOptions.Compiled;
            if (!caseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;

            return new Regex(patternRegexBuilder.ToString(), regexOptions);
        }
    }
}