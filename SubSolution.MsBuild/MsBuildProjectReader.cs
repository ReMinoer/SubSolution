﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using SubSolution.ProjectReaders;

namespace SubSolution.MsBuild
{
    [ExcludeFromCodeCoverage]
    public class MsBuildProjectReader : IProjectReader
    {
        public async Task<ISolutionProject> ReadAsync(string absoluteProjectPath)
        {
            Project project = await Task.Run(()
                => new Project(absoluteProjectPath, null, null, new ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports));

            var solutionProject = new SolutionProject
            {
                CanBuild = true,
                CanDeploy = false
            };

            string projectConfigurations = project.GetPropertyValue("Configurations");
            if (string.IsNullOrEmpty(projectConfigurations))
            {
                solutionProject.Configurations.Add("Debug");
                solutionProject.Configurations.Add("Release");
            }
            else
            {
                solutionProject.Configurations.AddRange(projectConfigurations.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }

            string platformConfigurations = project.GetPropertyValue("Platforms");
            if (string.IsNullOrEmpty(platformConfigurations))
            {
                solutionProject.Platforms.Add("Any CPU");
            }
            else
            {
                solutionProject.Platforms.AddRange(platformConfigurations.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }
            
            foreach (string projectDependencyPath in project.Items.Where(x => x.ItemType == "ProjectReference").Select(x => x.EvaluatedInclude))
                solutionProject.ProjectDependencies.Add(projectDependencyPath.Replace('\\', '/'));

            return solutionProject;
        }
    }
}
