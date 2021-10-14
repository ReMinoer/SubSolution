using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SubSolution.FileSystems
{
    // Copied from https://github.com/ReMinoer/Simulacra/blob/master/Simulacra.IO/Utils/PathComparer.cs

    public enum PathCaseComparison
    {
        EnvironmentDefault,
        RespectCase,
        IgnoreCase
    }

    public class PathComparer : IEqualityComparer<string>, IComparer<string>
    {
        static private PathComparer? _default;
        static public PathComparer Default => _default ??= new PathComparer();

        public PathCaseComparison CaseComparison { get; }

        public PathComparer()
            : this(PathCaseComparison.EnvironmentDefault)
        {
        }

        public PathComparer(PathCaseComparison caseComparison)
        {
            CaseComparison = caseComparison;
        }

        public bool Equals(string? x, string? y) => Equals(x, y, CaseComparison);
        public int Compare(string? x, string? y) => Compare(x, y, CaseComparison);
        public int GetHashCode(string obj) => ApplyCaseComparison(Normalize(obj), CaseComparison).GetHashCode();

        static public bool Equals(string? first, string? second, PathCaseComparison caseComparison)
        {
            if (first is null && second is null)
                return true;
            if (first is null || second is null)
                return false;

            return string.Equals(Normalize(first), Normalize(second), GetStringComparison(caseComparison));
        }

        static public int Compare(string? first, string? second, PathCaseComparison caseComparison)
        {
            if (first is null && second is null)
                return 0;
            if (first is null)
                return -1;
            if (second is null)
                return 1;

            return string.Compare(Normalize(first), Normalize(second), GetStringComparison(caseComparison));
        }

        static private string ApplyCaseComparison(string? path, PathCaseComparison caseComparison)
        {
            if (path is null)
                throw new ArgumentNullException();

            switch (caseComparison)
            {
                case PathCaseComparison.EnvironmentDefault:
                    if (IsEnvironmentCaseSensitive())
                        goto default;
                    else goto case PathCaseComparison.IgnoreCase;
                case PathCaseComparison.IgnoreCase:
                    return path.ToLowerInvariant();
                default:
                    return path;
            }
        }

        static private StringComparison GetStringComparison(PathCaseComparison caseComparison)
        {
            switch (caseComparison)
            {
                case PathCaseComparison.EnvironmentDefault:
                    return IsEnvironmentCaseSensitive() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                case PathCaseComparison.RespectCase:
                    return StringComparison.Ordinal;
                case PathCaseComparison.IgnoreCase:
                    return StringComparison.OrdinalIgnoreCase;
                default:
                    throw new NotSupportedException();
            }
        }

        static public bool IsEnvironmentCaseSensitive()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix: return true;
                default: return false;
            }
        }

        static private char AbsoluteSeparator => Path.DirectorySeparatorChar;
        static private char RelativeSeparator => Path.AltDirectorySeparatorChar;

        static private bool IsValidPath(string path) => IsValidPathInternal(path) && (IsValidAbsolutePathInternal(path) || IsValidRelativePathInternal(path));
        static private bool IsValidPathInternal(string path) => !Path.GetInvalidPathChars().Any(path.Contains);
        static private bool IsValidAbsolutePathInternal(string path) => Path.IsPathRooted(path);
        static private bool IsValidRelativePathInternal(string path) => !Path.IsPathRooted(path) && path[0] != AbsoluteSeparator && path[0] != RelativeSeparator;
        
        static private string Normalize(string path)
        {
            if (!IsValidPath(path))
                throw new ArgumentException();

            bool isAbsolute = Path.IsPathRooted(path);
            char separator = isAbsolute ? AbsoluteSeparator : RelativeSeparator;
            char otherSeparator = isAbsolute ? RelativeSeparator : AbsoluteSeparator;

            // Use unique separator
            return path.Replace(otherSeparator, separator).TrimEnd(AbsoluteSeparator, RelativeSeparator);
        }
    }
}