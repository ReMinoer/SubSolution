using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SubSolution.Builders;
using SubSolution.Converters;
using SubSolution.FileSystems;
using SubSolution.MsBuild;
using SubSolution.Raw;
using SubSolution.VisualStudio.Dialogs;
using Task = System.Threading.Tasks.Task;

namespace SubSolution.VisualStudio.Helpers
{
    static public class SubSolutionHelpers
    {
        static public async Task<string?> GetCurrentSolutionPathAsync()
        {
            Community.VisualStudio.Toolkit.Solution? solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
                return null;

            string? solutionPath = solution.FullPath;
            if (string.IsNullOrEmpty(solution.FullPath))
                return null;
            
            return solutionPath;
        }

        static public string GetSubSlnPath(string solutionPath)
        {
            string solutionDirectoryPath = Path.GetDirectoryName(solutionPath)!;
            string solutionFileName = Path.GetFileNameWithoutExtension(solutionPath);

            return Path.Combine(solutionDirectoryPath, solutionFileName + ".subsln");
        }

        static public async Task<string?> GetCurrentSubSlnPathAsync()
        {
            string? solutionPath = await GetCurrentSolutionPathAsync();
            if (solutionPath is null)
                return null;

            return GetSubSlnPath(solutionPath);
        }

        static public async Task<bool> GenerateAndUpdateSolutionAsync(string subSlnPath, VisualStudioOutputLogger outputLogger)
        {
            try
            {
                await outputLogger.OutputPane.ClearAsync();
                await outputLogger.OutputPane.ActivateAsync();
                
                using WaitDialog waitDialog = await WaitDialog.ShowAsync("SubSolution", "Update solution from .subsln", maxProgress: 3);
                SolutionUpdate solutionUpdate = await PrepareUpdateAsync(subSlnPath, outputLogger, async (s, i) => await waitDialog.UpdateAsync(s, i));

                if (solutionUpdate.Issues.Count > 0)
                {
                    bool asWarning = solutionUpdate.Issues.Any(x => x.Level == IssueLevel.Warning)
                        && solutionUpdate.Issues.All(x => x.Level != IssueLevel.Error);

                    if (asWarning)
                        outputLogger.LogWarning("Warning(s) encountered when checking if the solution is up-to-date.");
                    else
                        outputLogger.LogError("Error(s) encountered when checking if the solution is up-to-date.");
                    
                    await outputLogger.OutputPane.ActivateAsync();
                    await VS.MessageBox.ShowAsync($"We failed to generate a preview of the solution.{Environment.NewLine}(See log for details)",
                        buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK, icon: OLEMSGICON.OLEMSGICON_CRITICAL);

                    return false;
                }

                if (!solutionUpdate.HasChanges)
                {
                    await VS.MessageBox.ShowAsync("No changes in solution", buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return true;
                }

                return await AskToApplyUpdateAsync(solutionUpdate);
            }
            catch (Exception ex)
            {
                outputLogger.LogError(ex, $"Failed to generate and update solution from {subSlnPath}.");

                await outputLogger.OutputPane.ActivateAsync();
                await VS.MessageBox.ShowAsync($"We failed to generate a preview of the solution.{Environment.NewLine}(See log for details)",
                    buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK, icon: OLEMSGICON.OLEMSGICON_CRITICAL);

                return false;
            }
        }

        static public async Task<SolutionUpdate> PrepareUpdateAsync(string subSlnPath, ILogger logger, Func<string, int, Task> progressAction)
        {
            int step = 0;

            await progressAction("Build solution content...", ++step);

            var projectReader = new MsBuildProjectReader(logger);
            SolutionBuilderContext builderContext = await SolutionBuilderContext.FromConfigurationFileAsync(subSlnPath, projectReader);
            builderContext.Logger = logger;

            var solutionBuilder = new SolutionBuilder(builderContext);
            Solution generatedSolution = await solutionBuilder.BuildAsync(builderContext.Configuration);
            
            var solutionConverter = new SolutionConverter(StandardFileSystem.Instance, builderContext.ProjectReader, builderContext.SolutionDirectoryPath);
            solutionConverter.Logger = logger;

            RawSolution rawSolution;
            if (File.Exists(builderContext.SolutionPath))
            {
                await progressAction("Read current solution...", ++step);
                using FileStream solutionStream = File.OpenRead(builderContext.SolutionPath);
                rawSolution = await RawSolution.ReadAsync(solutionStream);

                await progressAction("Update solution...", ++step);
                await solutionConverter.UpdateAsync(rawSolution, generatedSolution);
            }
            else
            {
                ++step;
                await progressAction("Generate solution...", ++step);
                rawSolution = await solutionConverter.ConvertAsync(generatedSolution);
            }

            return new SolutionUpdate(builderContext.SolutionPath, subSlnPath, generatedSolution, rawSolution, solutionConverter.Changes, solutionBuilder.Issues);
        }

        static private async Task<bool> AskToApplyUpdateAsync(SolutionUpdate solutionUpdate)
        {
            var dialog = new ShowSolutionUpdateDialog(solutionUpdate);
            if (await VS.Windows.ShowDialogAsync(dialog) == true)
            {
                string? currentSolutionPath = await GetCurrentSolutionPathAsync();
                bool isGeneratedSolutionOpened = PathComparer.Equals(solutionUpdate.SolutionFilePath, currentSolutionPath, PathCaseComparison.EnvironmentDefault);
                
                _DTE? dte = null;
                if (isGeneratedSolutionOpened)
                {
                    dte = await VS.GetServiceAsync<_DTE, _DTE>();

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    dte?.Solution.Close(SaveFirst: false);
                }

                await solutionUpdate.ApplyAsync();

                if (isGeneratedSolutionOpened)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    dte?.Solution.Open(currentSolutionPath);
                }
                else
                    await AskToOpenSolutionAsync(solutionUpdate.SolutionFilePath);

                return true;
            }

            return false;
        }

        static private async Task AskToOpenSolutionAsync(string solutionFilePath)
        {
            VSConstants.MessageBoxResult askToOpenResult = await VS.MessageBox.ShowAsync("Do you want to open the generated solution ?",
                solutionFilePath,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNO);

            if (askToOpenResult != VSConstants.MessageBoxResult.IDYES)
                return;
            
            IVsSolution solution = await VS.Services.GetSolutionAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            solution.OpenSolutionFile(0, solutionFilePath);
        }
    }
}