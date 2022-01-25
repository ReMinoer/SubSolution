using System;
using System.IO;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell.Interop;
using SubSolution.Builders;
using SubSolution.Converters;
using SubSolution.FileSystems;
using SubSolution.MsBuild;
using SubSolution.Raw;
using SubSolutionVisualStudio.Dialogs;
using Solution = SubSolution.Solution;

namespace SubSolutionVisualStudio.Helpers
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

                SolutionUpdate solutionUpdate;
                {
                    using WaitDialog waitDialog = await WaitDialog.ShowAsync("SubSolution", "Update solution from .subsln", maxProgress: 3);
                    solutionUpdate = await PrepareUpdateAsync(subSlnPath, outputLogger, async (s, i) => await waitDialog.UpdateAsync(s, i));
                }

                bool? updateDone = await AskToApplyUpdateAsync(solutionUpdate);
                return updateDone != false;
            }
            catch (Exception ex)
            {
                outputLogger.LogError(ex, $"Failed to generate and update solution from {subSlnPath}.");
                await outputLogger.OutputPane.ActivateAsync();

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

            await progressAction("Read current solution...", ++step);

            RawSolution rawSolution;
            {
                using FileStream solutionStream = File.OpenRead(builderContext.SolutionPath);
                rawSolution = await RawSolution.ReadAsync(solutionStream);
            }

            await progressAction("Update solution...", ++step);

            var solutionConverter = new SolutionConverter(StandardFileSystem.Instance);
            solutionConverter.Logger = logger;
            solutionConverter.Update(rawSolution, generatedSolution);

            return new SolutionUpdate(builderContext.SolutionPath, subSlnPath, generatedSolution, rawSolution, solutionConverter.Changes);
        }

        static public async Task<bool?> AskToApplyUpdateAsync(SolutionUpdate solutionUpdate)
        {
            if (!solutionUpdate.HasChanges)
            {
                await VS.MessageBox.ShowAsync("No changes in solution", buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);
                return null;
            }

            var dialog = new ShowSolutionUpdateDialog(solutionUpdate);
            if (await VS.Windows.ShowDialogAsync(dialog) == true)
            {
                await solutionUpdate.ApplyAsync();
                return true;
            }

            return false;
        }
    }
}