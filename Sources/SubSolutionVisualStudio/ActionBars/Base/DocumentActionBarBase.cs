using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;

namespace SubSolutionVisualStudio.ActionBars.Base
{
    public abstract class DocumentActionBarBase : ActionBarBase
    {
        protected readonly string DocumentFilePath;

        protected DocumentActionBarBase(string documentFilePath)
        {
            DocumentFilePath = documentFilePath;
        }

        protected override async Task<WindowFrame?> GetWindowFrameAsync(CancellationToken cancellationToken)
        {
            return await VS.Windows.FindDocumentWindowAsync(DocumentFilePath);
        }
    }
}