using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using SubSolutionVisualStudio.Watchers;
using Task = System.Threading.Tasks.Task;

namespace SubSolutionVisualStudio
{
    // https://www.vsixcookbook.com
    // https://docs.microsoft.com/en-us/visualstudio/xml-tools/schema-cache?view=vs-2022
    // https://www.visualstudioextensibility.com/2016/10/18/samples-how-to-create-top-menus-sub-menus-context-menus-toolbars/
    // https://stackoverflow.com/questions/7825489/how-do-i-subscribe-to-solution-and-project-events-from-a-vspackage

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuids.SubSolutionVisualStudioString)]
    public sealed class SubSolutionVisualStudioPackage : ToolkitPackage
    {
        private IDisposable? _outdatedSolutionWatcher;
        private IDisposable? _savedSubSlnWatcher;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _outdatedSolutionWatcher = await OutdatedSolutionWatcher.RunAsync();
            _savedSubSlnWatcher = await SavedSubSlnWatcher.RunAsync();

            await this.RegisterCommandsAsync();
        }

        protected override void Dispose(bool disposing)
        {
            _savedSubSlnWatcher?.Dispose();
            _outdatedSolutionWatcher?.Dispose();

            base.Dispose(disposing);
        }
    }
}