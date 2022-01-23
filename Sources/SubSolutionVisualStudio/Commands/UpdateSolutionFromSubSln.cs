using System.IO;
using Community.VisualStudio.Toolkit;
using SubSolutionVisualStudio.Commands.Base;
using SubSolutionVisualStudio.Helpers;
using Task = System.Threading.Tasks.Task;

namespace SubSolutionVisualStudio.Commands
{
    [Command(PackageIds.UpdateSolutionFromSubSln)]
    internal sealed class UpdateSolutionFromSubSln : SubSolutionContextualCommandBase<UpdateSolutionFromSubSln>
    {
        protected override StatusAnimation AnimationIcon => StatusAnimation.General;

        protected override async Task ExecuteAsync(VisualStudioOutputLogger outputLogger)
        {
            string? solutionPath = await SubSolutionHelpers.GetCurrentSolutionPathAsync();
            if (solutionPath is null)
                return;

            string subSlnPath = SubSolutionHelpers.GetSubSlnPath(solutionPath);
            if (!File.Exists(subSlnPath))
                return;

            await SubSolutionHelpers.GenerateAndUpdateSolutionAsync(subSlnPath, outputLogger);
        }
    }
}