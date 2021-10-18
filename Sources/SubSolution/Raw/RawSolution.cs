using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SubSolution.Raw
{
    public class RawSolution : IRawSolution
    {
        // https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file

        private const string SlnFormatVersionHeaderPrefix = "Microsoft Visual Studio Solution File, Format Version ";
        private const string MajorVisualStudioVersionHeaderPrefix = "# Visual Studio Version ";
        private const string VisualStudioVersionHeaderPrefix = "VisualStudioVersion = ";
        private const string MinimumVisualStudioVersionHeaderPrefix = "MinimumVisualStudioVersion = ";

        private const string ProjectBlockName = "Project";
        private const string GlobalBlockName = "Global";

        public Version SlnFormatVersion { get; set; } = new Version(0, 0);
        public int MajorVisualStudioVersion { get; set; }
        public Version VisualStudioVersion { get; set; } = new Version(0, 0);
        public Version MinimumVisualStudioVersion { get; set; } = new Version(0, 0);

        public List<Project> Projects { get; }
        public List<Section> GlobalSections { get; }

        private readonly IReadOnlyList<IRawSolutionProject> _readOnlyProjects;
        private readonly IReadOnlyList<IRawSolutionSection> _readOnlyGlobalSections;

        IReadOnlyList<IRawSolutionProject> IRawSolution.Projects => _readOnlyProjects;
        IReadOnlyList<IRawSolutionSection> IRawSolution.GlobalSections => _readOnlyGlobalSections;

        public RawSolution()
        {
            Projects = new List<Project>();
            GlobalSections = new List<Section>();

            _readOnlyProjects = Projects.AsReadOnly();
            _readOnlyGlobalSections = GlobalSections.AsReadOnly();
        }

        public async Task WriteAsync(Stream stream)
        {
            await using IAsyncDisposable _ = new StreamWriter(stream, new UTF8Encoding(true), 1024, leaveOpen: true)
                .AsAsyncDisposable(out StreamWriter writer);

            await writer.WriteLineAsync();
            await writer.WriteLineAsync(SlnFormatVersionHeaderPrefix + SlnFormatVersion.Major + '.' + SlnFormatVersion.Minor.ToString("00"));
            await writer.WriteLineAsync(MajorVisualStudioVersionHeaderPrefix + MajorVisualStudioVersion);
            await writer.WriteLineAsync(VisualStudioVersionHeaderPrefix + VisualStudioVersion);
            await writer.WriteLineAsync(MinimumVisualStudioVersionHeaderPrefix + MinimumVisualStudioVersion);

            foreach (Project project in Projects)
            {
                string typeGuid = project.TypeGuid.ToRawFormat();
                string name = project.Name;
                string path = project.Path;
                string projectGuid = project.ProjectGuid.ToRawFormat();

                await writer.WriteLineAsync($"{ProjectBlockName}(\"{typeGuid}\") = \"{name}\", \"{path}\", \"{projectGuid}\"");

                foreach (Section section in project.Sections)
                    await WriteSectionAsync(writer, section);

                await writer.WriteLineAsync($"End{ProjectBlockName}");
            }

            await writer.WriteLineAsync(GlobalBlockName);

            foreach (Section section in GlobalSections)
                await WriteSectionAsync(writer, section);

            await writer.WriteLineAsync($"End{GlobalBlockName}");
        }

        static private async Task WriteSectionAsync(StreamWriter writer, Section section)
        {
            await writer.WriteAsync($"\t{section.Name}");
            if (section.Parameter != null)
                await writer.WriteAsync($"({section.Parameter})");
            if (section.Arguments.Count > 0)
                await writer.WriteAsync($" = {string.Join(", ", section.Arguments)}");
            await writer.WriteLineAsync();

            foreach (string valuePair in section.OrderedValuePairs)
                await writer.WriteLineAsync($"\t\t{valuePair}");

            await writer.WriteLineAsync($"\tEnd{section.Name}");
        }

        static public async Task<RawSolution> ReadAsync(Stream stream)
        {
            RawSolution solution = new RawSolution();

            using StreamReader reader = new StreamReader(stream, new UTF8Encoding(true), true, 1024, leaveOpen: true);

            solution.SlnFormatVersion = await ReadNextLineVersionAsync(reader, SlnFormatVersionHeaderPrefix);
            solution.MajorVisualStudioVersion = await ReadNextLineIntegerAsync(reader, MajorVisualStudioVersionHeaderPrefix);
            solution.VisualStudioVersion = await ReadNextLineVersionAsync(reader, VisualStudioVersionHeaderPrefix);
            solution.MinimumVisualStudioVersion = await ReadNextLineVersionAsync(reader, MinimumVisualStudioVersionHeaderPrefix);

            Block? block = await ReadNextBlockAsync(reader);
            while (block != null)
            {
                switch (block.Name)
                {
                    case ProjectBlockName:
                    {
                        if (!RawGuid.TryParse(TrimValue(block.Parameter!), out Guid typeGuid))
                            throw new Exception($"Expected a valid GUID but got \"{TrimValue(block.Parameter!)[1..^1]}\"");
                        if (!RawGuid.TryParse(block.Arguments[2], out Guid projectGuid))
                            throw new Exception($"Expected a valid GUID but got \"{block.Arguments[2][1..^1]}\"");

                        var project = new Project(typeGuid, block.Arguments[0], block.Arguments[1], projectGuid);
                        project.Sections.AddRange(block.Sections);

                        solution.Projects.Add(project);
                        break;
                    }
                    case GlobalBlockName:
                    {
                        solution.GlobalSections.AddRange(block.Sections);
                        break;
                    }
                    default:
                        throw new Exception($"Unknown block name \"{block.Name}\"");
                }

                block = await ReadNextBlockAsync(reader);
            }

            return solution;
        }

        static private async Task<Block?> ReadNextBlockAsync(StreamReader reader)
        {
            string? line = await ReadNextLineAsync(reader);
            if (line == null)
                return null;

            return await ReadBlockAsync(reader, line);
        }

        static private async Task<Block> ReadBlockAsync(StreamReader reader, string line)
        {
            Block newBlock = ReadBlockHeader(line);

            while (true)
            {
                string? nextLine = await ReadNextLineAsync(reader);
                if (nextLine == null)
                    break;
                if (nextLine == $"End{newBlock.Name}")
                    break;

                newBlock.Sections.Add(await ReadSectionAsync(reader, nextLine));
            }

            return newBlock;
        }

        static private async Task<Section> ReadSectionAsync(StreamReader reader, string line)
        {
            Block blockHeader = ReadBlockHeader(line);
            Section section = new Section(blockHeader.Name, blockHeader.Parameter, blockHeader.Arguments);

            while (true)
            {
                string? nextLine = await ReadNextLineAsync(reader);
                if (nextLine == null)
                    break;
                if (nextLine == $"End{blockHeader.Name}")
                    break;

                string[] pair = nextLine.Split('=');
                section.AddValue(TrimValue(pair[0]), TrimValue(pair[1]));
            }

            return section;
        }

        static private Block ReadBlockHeader(string line)
        {
            int openParenthesisIndex = line.IndexOf('(');
            if (openParenthesisIndex == -1)
                return new Block(line);

            int currentIndex = openParenthesisIndex + 1;
            int closeParenthesisIndex = line.IndexOf(')', currentIndex);
            if (closeParenthesisIndex == -1)
                throw new Exception("Expected character ).");

            string name = line[..openParenthesisIndex];
            string parameter = line[(openParenthesisIndex + 1)..closeParenthesisIndex];

            var newBlock = new Block(name)
            {
                Parameter = parameter
            };

            currentIndex = closeParenthesisIndex + 1;
            int equalIndex = line.IndexOf('=', currentIndex);
            if (equalIndex == -1)
                return new Block(name) { Parameter = parameter };

            currentIndex = equalIndex + 1;

            List<string> arguments = new List<string>();
            while (true)
            {
                int quoteIndex = line.IndexOf('"', currentIndex);
                if (quoteIndex == -1)
                {
                    string argumentNotQuoted = line[currentIndex..].Trim();
                    if (!string.IsNullOrWhiteSpace(argumentNotQuoted))
                        arguments.Add(argumentNotQuoted);
                    break;
                }

                int nextQuoteIndex = line.IndexOf('"', quoteIndex + 1);
                arguments.Add(line[(quoteIndex + 1)..nextQuoteIndex]);

                currentIndex = nextQuoteIndex + 1;
            }

            newBlock.Arguments = arguments.ToArray();

            return newBlock;
        }

        static private async Task<int> ReadNextLineIntegerAsync(StreamReader reader, string prefix)
        {
            try
            {
                return int.Parse(await ReadNextLineValueAsync(reader, prefix));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse integer after prefix \"{prefix}\".", ex);
            }
        }

        static private async Task<Version> ReadNextLineVersionAsync(StreamReader reader, string prefix)
        {
            try
            {
                return Version.Parse(await ReadNextLineValueAsync(reader, prefix));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse version after prefix \"{prefix}\".", ex);
            }
        }

        static private async Task<string> ReadNextLineValueAsync(StreamReader reader, string prefix)
        {
            string? nextLine = await ReadNextLineAsync(reader);
            if (nextLine == null)
                throw new Exception($"Cannot read expected line using prefix \"{prefix}\".");

            if (!nextLine.StartsWith(prefix))
                throw new Exception($"Expected prefix \"{prefix}\".");

            return nextLine[prefix.Length..];
        }

        static private async Task<string?> ReadNextLineAsync(StreamReader reader)
        {
            while (true)
            {
                string nextLine = await reader.ReadLineAsync();
                if (nextLine == null)
                    return null;
                if (!string.IsNullOrWhiteSpace(nextLine))
                    return nextLine.Trim();
            }
        }

        static private string TrimValue(string value)
        {
            value = value.Trim();

            if (value[0] == '"' && value[^1] == '"')
                return value[1..^1];
            return value;
        }

        private class Block
        {
            public string Name { get; }
            public string? Parameter { get; set; }
            public string[] Arguments { get; set; } = Array.Empty<string>();
            public List<Section> Sections { get; } = new List<Section>();

            public Block(string name)
            {
                Name = name;
            }
        }

        public class Project : IRawSolutionProject
        {
            public Guid TypeGuid { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public Guid ProjectGuid { get; set; }
            public List<Section> Sections { get; }
            
            private readonly IReadOnlyList<IRawSolutionSection> _readOnlySections;
            IReadOnlyList<IRawSolutionSection> IRawSolutionProject.Sections => _readOnlySections;

            public Project(Guid typeGuid, string name, string path, Guid projectGuid)
            {
                TypeGuid = typeGuid;
                Name = name;
                Path = path;
                ProjectGuid = projectGuid;
                Sections = new List<Section>();
                _readOnlySections = Sections.AsReadOnly();
            }
        }

        public class Section : IRawSolutionSection
        {
            public string Name { get; set; }
            public string? Parameter { get; set; }
            public List<string> Arguments { get; }

            private readonly IReadOnlyList<string> _readOnlyArguments;
            IReadOnlyList<string> IRawSolutionSection.Arguments => _readOnlyArguments;

            private readonly List<string> _orderedValuePairs;
            public IReadOnlyList<string> OrderedValuePairs { get; }

            private readonly Dictionary<string, string> _valuesByKey;
            public IReadOnlyDictionary<string, string> ValuesByKey { get; }

            public Section(string name, string? parameter)
            {
                Name = name;
                Parameter = parameter;

                Arguments = new List<string>();
                _readOnlyArguments = Arguments.AsReadOnly();

                _orderedValuePairs = new List<string>();
                OrderedValuePairs = _orderedValuePairs.AsReadOnly();

                _valuesByKey = new Dictionary<string, string>();
                ValuesByKey = new ReadOnlyDictionary<string, string>(_valuesByKey);
            }

            public Section(string name, string? parameter, params string[] arguments)
                : this(name, parameter)
            {
                Arguments.AddRange(arguments);
            }

            public void AddValue(string key, string value)
            {
                _valuesByKey.Add(key, value);
                _orderedValuePairs.Add(FormatValue(key, value));
            }

            public void ReplaceValue(string key, string value)
            {
                if (!TryReplaceValue(key, value))
                    throw new KeyNotFoundException();
            }

            public void SetOrAddValue(string key, string value)
            {
                if (!TryReplaceValue(key, value))
                    AddValue(key, value);
            }

            public bool TryReplaceValue(string key, string value)
            {
                if (!_valuesByKey.TryGetValue(key, out string currentValue))
                    return false;

                if (currentValue == value)
                    return true;

                _valuesByKey[key] = value;

                string currentFormattedValue = FormatValue(key, currentValue);
                int currentIndex = _orderedValuePairs.IndexOf(currentFormattedValue);
                _orderedValuePairs[currentIndex] = FormatValue(key, value);

                return true;
            }

            public void RemoveValue(string key)
            {
                if (_valuesByKey.Remove(key, out string value))
                    _orderedValuePairs.Remove(FormatValue(key, value));
            }

            static private string FormatValue(string key, string value) => key + " = " + value;
        }
    }
}