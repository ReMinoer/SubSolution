using System;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;

namespace SubSolution.VisualStudio.ActionBars.Base
{
    public abstract class SolutionExplorerActionBarBase : ActionBarBase
    {
        protected override async Task<WindowFrame?> GetWindowFrameAsync(CancellationToken cancellationToken)
        {
            return await VS.Windows.FindOrShowToolWindowAsync(Guid.Parse(ToolWindowGuids.SolutionExplorer));
        }
    }
}