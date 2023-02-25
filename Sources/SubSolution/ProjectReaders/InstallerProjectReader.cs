using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System;

namespace SubSolution.ProjectReaders
{
    static public class InstallerProjectReader
    {
        static public async Task<ISolutionProject> ReadInstallerProjectAsync(string absoluteProjectPath)
        {
            var solutionProject = new SolutionProject(ProjectType.Installer)
            {
                CanBuild = true,
                NoPlatform = true
            };

            await using IAsyncDisposable _ = File.OpenRead(absoluteProjectPath).AsAsyncDisposable(out FileStream projectStream);
            XElement rootElement = await ParseInstallerProject(projectStream);

            XElement? deployProjectElement = rootElement.Element("DeployProject");
            if (deployProjectElement is null)
                return solutionProject;

            XElement? configurationsElement = deployProjectElement.Element("Configurations");
            if (configurationsElement is null)
                return solutionProject;

            foreach (XElement configurationElement in configurationsElement.Elements())
            {
                solutionProject.Configurations.Add(configurationElement.Name.LocalName);
            }

            return solutionProject;
        }

        static private readonly Regex CloseBraceRegex = new Regex(@"^\s*}", RegexOptions.Compiled);
        static private readonly Regex KeyValuePairRegex = new Regex(@"\x22(\w+)\x22 = \x22(.+)\x22", RegexOptions.Compiled);
        static private readonly Regex RawContentRegex = new Regex(@"^\s*\x22(.+)\x22\s*$", RegexOptions.Compiled);
        static private readonly Regex AlphaNumericRegex = new Regex(@"^\w+$", RegexOptions.Compiled);

        // https://gfkeogh.blogspot.com/2020/01/parsing-vdproj-file.html
        static private async Task<XElement> ParseInstallerProject(Stream stream)
        {
            // TODO: Support non-standard formatting (manually edited files)
            using var streamReader = new StreamReader(stream);
            
            var root = new XElement("node", new XAttribute("name", "root"));
            XElement head = root;

            var stack = new Stack<XElement>();
            stack.Push(root);

            for (string? line = await streamReader.ReadLineAsync(); line != null; line = await streamReader.ReadLineAsync())
            {
                if (CloseBraceRegex.IsMatch(line))
                {
                    // A close brace pops the stack back a level
                    stack.Pop();
                    head = stack.Peek();
                    continue;
                }

                Match m = KeyValuePairRegex.Match(line);
                if (m.Success)
                {
                    // A key = value is added to the current stack head node
                    string key = m.Groups[1].Value;
                    string value = m.Groups[2].Value;

                    var element = new XElement(key, value);
                    head.Add(element);
                }
                else
                {
                    // Otherwise we must be pushing a new head node onto the stack.
                    // If the name is a simple alphanum string then it's used
                    // as the node name, otherwise use a fake <node> with the strange
                    // name as a data attribute.

                    XElement element;
                    string rawName = RawContentRegex.Match(line).Groups[1].Value;

                    if (AlphaNumericRegex.IsMatch(rawName))
                    {
                        element = new XElement(rawName);
                    }
                    else
                    {
                        element = new XElement("node", new XAttribute("data", rawName));
                    }

                    head.Add(element);
                    stack.Push(element);
                    head = element;

                    await streamReader.ReadLineAsync();  // Eat the opening brace
                }
            }

            return root;
        }
    }
}