using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CommandLine;
using SubSolution.Builders.Configuration;
using SubSolution.CommandLine.Commands.Base;

namespace SubSolution.CommandLine.Commands
{
    [Verb("create", HelpText = "Create a new .subsln SubSolution configuration file")]
    public class CreateCommand : CommandBase
    {
        [Value(0, MetaName = "files", Required = true, HelpText = "Paths of the files to create, relative to working directory. " +
            "If no extension provided, default extension is \".subsln\".")]
        public IEnumerable<string>? FilePaths { get; set; }

        [Option('f', "force", HelpText = "Force to overwrite existing files, without asking user.")]
        public bool Force { get; set; }
        [Option('o', "open", HelpText = "Open .sln file with its default application.")]
        public bool Open { get; set; }

        protected override Task ExecuteCommandAsync()
        {
            if (FilePaths is null)
                return Task.CompletedTask;

            foreach (string path in FilePaths)
            {
                string filePath = ComputeFilePath(path);

                if (AbortByUser(filePath))
                    continue;

                CreateFile(filePath);
            }

            return Task.CompletedTask;
        }

        private string ComputeFilePath(string filePath)
        {
            if (Path.GetExtension(filePath) == string.Empty)
                filePath += ".subsln";

            Log($"Creating {filePath}...");

            return filePath;
        }

        private bool AbortByUser(string filePath)
        {
            if (Force)
                return false;
            if (!File.Exists(filePath))
                return false;
            if (AskUserValidation($"File {filePath} already exist.", "Do you want to overwrite it ?"))
                return false;
            
            Log($"Abort creation of {filePath}.");
            return true;
        }

        private void CreateFile(string filePath)
        {
            var configuration = new SubSolutionConfiguration
            {
                Root = new SolutionRoot
                {
                    SolutionItems =
                    {
                        new Projects()
                    }
                }
            };

            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string xsdFullPath = Path.Combine(executableLocation, "SubSolutionConfiguration.xsd");
            if (File.Exists(xsdFullPath))
            {
                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
                configuration.Untyped.SetAttributeValue(XNamespace.Xmlns + "xsi", xsi.NamespaceName);
                configuration.Untyped.SetAttributeValue(xsi + "noNamespaceSchemaLocation", xsdFullPath);
            }

            using XmlWriter xmlWriter = new XmlTextWriter(filePath, null)
            {
                Formatting = Formatting.Indented,
                Indentation = 4
            };

            configuration.Save(xmlWriter);
            Log($"Created {filePath}.");

            if (Open)
            {
                OpenFile(filePath);
                Log($"Open {filePath}.");
            }
        }
    }
}