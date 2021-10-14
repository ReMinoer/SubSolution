﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using SubSolution.Builders;
using SubSolution.CommandLine.Commands.Base;
using SubSolution.Raw;

namespace SubSolution.CommandLine.Commands
{
    [Verb("generate", HelpText = "Generate .sln Visual Studio solution files from .subsln files.")]
    public class GenerateCommand : BuildCommandBase
    {
        [Value(0, MetaName = "files", HelpText = "Paths of SubSolution configuration files to generate. " +
            "Also accept relative file path pattern using wildcards \"*\", \"**\" and \"?\". " +
            "If not provided, all .subsln files in working directory and its sub-directories will be generated.")]
        public override IEnumerable<string>? FilePaths { get; set; }

        [Option('p', "preview", HelpText = "Preview the command result without creating files.")]
        public bool Preview { get; set; }
        [Option('f', "force", HelpText = "Force to apply changes, without asking user.")]
        public bool Force { get; set; }
        [Option('o', "open", HelpText = "Open generated file with its default application.")]
        public bool Open { get; set; }
        [Option('s', "show", HelpText = "Show representation of generated solution.")]
        public bool Show { get; set; }

        protected override async Task ExecuteCommandAsync(string configurationFilePath)
        {
            SolutionBuilderContext? context = await GetBuildContextAsync(configurationFilePath);
            if (context is null)
                return;

            Log($"Generating {configurationFilePath}...");
            
            Solution? solution = await BuildSolutionAsync(context);
            if (solution is null)
                return;

            if (Show)
                LogSolution(solution);
            
            RawSolution rawSolution;
            if (File.Exists(context.SolutionPath))
            {
                (RawSolution? updatedSolution, bool changed) = await UpdateSolutionAsync(solution, context.SolutionPath);
                if (updatedSolution is null)
                    return;

                if (!changed)
                    Log($"No changes in {context.SolutionPath}.");

                if (Preview)
                {
                    Log($"End of {context.SolutionPath} preview.");
                    return;
                }

                if (!changed)
                    return;

                if (!Force && !AskUserValidation("Apply the changes ?"))
                {
                    Log($"Abort generation of {configurationFilePath}.");
                    return;
                }

                rawSolution = updatedSolution;
            }
            else
            {
                RawSolution? convertedSolution = ConvertSolution(solution, configurationFilePath);
                if (convertedSolution is null)
                    return;

                if (Preview)
                    return;

                rawSolution = convertedSolution;
            }

            if (!await WriteSolutionAsync(rawSolution, context.SolutionPath))
                return;

            Log($"Generated {context.SolutionPath}.");

            if (Open)
            {
                Log($"Opening {context.SolutionPath}...");
                OpenFile(context.SolutionPath);
            }
        }

        protected async Task<bool> WriteSolutionAsync(RawSolution rawSolution, string outputPath)
        {
            try
            {
                await using FileStream fileStream = File.Create(outputPath);
                await rawSolution.WriteAsync(fileStream);
                return true;
            }
            catch (Exception exception)
            {
                LogError($"Failed to write {outputPath}.", exception);
                UpdateErrorCode(ErrorCode.FailWriteSolution);
                return false;
            }
        }
    }
}